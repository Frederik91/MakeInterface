using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MakeInterface.Generator.Extensions;
internal static class SemanticModelExtensions
{
    public static bool IsSameType(this SemanticModel semanticModel, BaseTypeSyntax baseTypeSyntax, ITypeSymbol typeSymbol)
    {
        // Get the symbol for the parsed type syntax.
        var baseTypeSymbol = semanticModel.GetSymbolInfo(baseTypeSyntax.Type).Symbol;
        if (baseTypeSymbol is null)
            return false;

        // Get the full namespace of the symbol.
        var fullBaseTypeSymbol = baseTypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var fullTypeSymbol = typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        // Check if the parsed type syntax represents the same type as the type symbol.
        return fullBaseTypeSymbol == fullTypeSymbol;
    }
}
