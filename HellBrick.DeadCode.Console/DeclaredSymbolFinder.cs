using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace HellBrick.DeadCode
{
	internal class DeclaredSymbolFinder : SymbolVisitor
	{
		private readonly IAssemblySymbol _assembly;
		private readonly HashSet<ISymbol> _symbols = new HashSet<ISymbol>();

		public DeclaredSymbolFinder( IAssemblySymbol assembly )
		{
			_assembly = assembly;
		}

		public IReadOnlyCollection<ISymbol> DeclaredSymbols => _symbols;

		public override void DefaultVisit( ISymbol symbol )
		{
			if ( symbol.ContainingAssembly != _assembly || symbol.IsImplicitlyDeclared )
				return;

			if ( _symbols.Add( symbol ) )
				base.DefaultVisit( symbol );
		}

		public override void VisitNamespace( INamespaceSymbol symbol )
		{
			VisitNamespaceOrType( symbol );
			base.VisitNamespace( symbol );
		}

		public override void VisitNamedType( INamedTypeSymbol symbol )
		{
			VisitNamespaceOrType( symbol );
			base.VisitNamedType( symbol );
		}

		private void VisitNamespaceOrType( INamespaceOrTypeSymbol symbol )
		{
			foreach ( ISymbol member in symbol.GetMembers() )
				Visit( member );
		}

		public override void VisitMethod( IMethodSymbol symbol )
		{
			bool isIgnored =
				symbol.MethodKind == MethodKind.PropertyGet ||
				symbol.MethodKind == MethodKind.PropertySet ||
				symbol.IsOverride ||
				symbol.MetadataName == ".cctor" ||
				ImplementsInterface( symbol );

			if ( !isIgnored )
				base.VisitMethod( symbol );
		}

		public override void VisitProperty( IPropertySymbol symbol )
		{
			if ( !ImplementsInterface( symbol ) && !symbol.IsOverride )
				base.VisitProperty( symbol );
		}

		private bool ImplementsInterface( ISymbol symbol )
		{
			var matches =
				from @interface in symbol.ContainingType.AllInterfaces
				from interfaceMember in @interface.GetMembers()
				let implementation = symbol.ContainingType.FindImplementationForInterfaceMember( interfaceMember )
				where symbol == implementation
				select 0;

			return matches.Any();
		}
	}
}
