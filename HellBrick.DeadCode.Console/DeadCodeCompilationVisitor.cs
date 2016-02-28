using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace HellBrick.DeadCode
{
	public class DeadCodeCompilationVisitor
	{
		private HashSet<ISymbol> _declaredSymbols = new HashSet<ISymbol>();
		private HashSet<ISymbol> _referencedSymbols = new HashSet<ISymbol>();

		public void VisitCompilation( Compilation compilation )
		{
			DiscoverDeclaredSymbols( compilation );
			DiscoverReferencedSymbols( compilation );
		}

		private void DiscoverDeclaredSymbols( Compilation compilation )
		{
			DeclaredSymbolFinder discoverer = new DeclaredSymbolFinder( compilation.Assembly );
			discoverer.Visit( compilation.GlobalNamespace );
			foreach ( ISymbol discoveredSymbol in discoverer.DeclaredSymbols )
				_declaredSymbols.Add( discoveredSymbol );
		}

		private void DiscoverReferencedSymbols( Compilation compilation )
		{
			foreach ( SyntaxTree syntaxTree in compilation.SyntaxTrees )
			{
				SemanticModel semanticModel = compilation.GetSemanticModel( syntaxTree );
				ReferencedSymbolFinder discoverer = new ReferencedSymbolFinder( semanticModel );
				discoverer.Visit( syntaxTree.GetRoot() );

				foreach ( ISymbol referencedSymbol in discoverer.ReferencedSymbols )
					_referencedSymbols.Add( referencedSymbol );
			}
		}

		public IEnumerable<ISymbol> GetUnusedSymbols() => _declaredSymbols.Except( _referencedSymbols, SymbolComparer.Instance );

		private class SymbolComparer : IEqualityComparer<ISymbol>
		{
			public static SymbolComparer Instance { get; } = new SymbolComparer();

			public bool Equals( ISymbol x, ISymbol y ) => x.ToString() == y.ToString();
			public int GetHashCode( ISymbol obj ) => obj.ToString().GetHashCode();
		}
	}
}