using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MakeInterface.Generator.Extensions;
internal static class MemberDeclarationSyntaxExtensions
{
    public static string? GetName(this MemberDeclarationSyntax syntax)
    {
        return syntax switch
        {
            PropertyDeclarationSyntax property => property.Identifier.Text,
            MethodDeclarationSyntax method => method.Identifier.Text,
            FieldDeclarationSyntax field => field.Declaration.Variables.First().Identifier.Text,
            _ => null
        };
    }
}
