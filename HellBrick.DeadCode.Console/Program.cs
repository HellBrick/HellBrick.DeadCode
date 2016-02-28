using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.CodeAnalysis.Text;

namespace HellBrick.DeadCode
{
	internal class Program
	{
		private static void Main( string[] args )
		{
			if ( args.Length == 0 )
			{
				Console.WriteLine( "Pass path to a solution file as an argument" );
				return;
			}

			string solutionPath = args[ 0 ];
			MainAsync( solutionPath ).GetAwaiter().GetResult();
		}

		private static async Task MainAsync( string solutionPath )
		{
			Solution solution = await MSBuildWorkspace
				.Create( new Dictionary<string, string>() { { "Configuration", "Debug" }, { "Platform", "AnyCPU" } } )
				.OpenSolutionAsync( solutionPath )
				.ConfigureAwait( false );

			DeadCodeCompilationVisitor compilationVisitor = new DeadCodeCompilationVisitor();

			foreach ( Project project in solution.Projects )
			{
				Console.WriteLine( $"Analyzing {project.Name}..." );
				Compilation projectCompilation = await project.GetCompilationAsync().ConfigureAwait( false );
				Diagnostic[] errors = projectCompilation.GetDiagnostics().Where( d => d.Severity >= DiagnosticSeverity.Error ).ToArray();

				if ( errors.Length > 0 )
					WriteError( $"{errors.Length} errors. {errors[ 0 ]}" );
				else
					compilationVisitor.VisitCompilation( projectCompilation );
			}

			var unusedSymbolGroups = compilationVisitor
				.GetUnusedSymbols()
				.Where( s => !s.MetadataName.StartsWith( "op_" ) )
				.GroupBy( s => s.ContainingAssembly );

			foreach ( var group in unusedSymbolGroups )
			{
				Console.WriteLine( group.Key );
				foreach ( var symbol in group )
				{
					Console.Write( "   " );
					Console.WriteLine( symbol );
				}
			}
		}

		private static void WriteError( string errorMessage )
		{
			ConsoleColor originalColor = Console.ForegroundColor;
			Console.ForegroundColor = ConsoleColor.DarkYellow;
			Console.WriteLine( errorMessage );
			Console.ForegroundColor = originalColor;
		}
	}
}
