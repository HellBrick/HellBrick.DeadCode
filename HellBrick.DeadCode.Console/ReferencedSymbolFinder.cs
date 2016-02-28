using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace HellBrick.DeadCode
{
	internal class ReferencedSymbolFinder : CSharpSyntaxWalker
	{
		private readonly SemanticModel _semanticModel;
		private readonly HashSet<ISymbol> _referencedSymbols = new HashSet<ISymbol>();

		public ReferencedSymbolFinder( SemanticModel semanticModel )
			: base( SyntaxWalkerDepth.Node )
		{
			_semanticModel = semanticModel;
		}

		public IReadOnlyCollection<ISymbol> ReferencedSymbols => _referencedSymbols;

		public override void DefaultVisit( SyntaxNode node )
		{
			if ( node?.ToString().Contains( "GetHost" ) == true )
			{
			}

			ISymbol symbol = _semanticModel.GetSymbolInfo( node ).Symbol;
			TryAdd( symbol );

			ITypeSymbol returnTypeSymbol = ( symbol as IPropertySymbol )?.Type ?? ( symbol as IMethodSymbol )?.ReturnType;
			TryAdd( returnTypeSymbol );

			ImmutableArray<ITypeSymbol> genericArgumentTypes = ( returnTypeSymbol as INamedTypeSymbol )?.TypeArguments ?? ImmutableArray<ITypeSymbol>.Empty;
			foreach ( ITypeSymbol genericArgumentType in genericArgumentTypes )
				TryAdd( genericArgumentType );

			base.DefaultVisit( node );
		}

		private void TryAdd( ISymbol symbol )
		{
			ISymbol definition = TryGetDefinition( symbol );
			if ( definition != null )
				_referencedSymbols.Add( definition );
		}

		private ISymbol TryGetDefinition( ISymbol symbol )
		{
			symbol = symbol?.OriginalDefinition;

			IMethodSymbol methodSymbol = symbol as IMethodSymbol;
			if ( methodSymbol?.ReducedFrom != null )
				return methodSymbol.ReducedFrom;

			return symbol;
		}
	}
}
