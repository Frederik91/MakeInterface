using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MakeInterface.Generator;
internal static class MemberDeclarationSyntaxExtensions
{
    public static string GetName(this MemberDeclarationSyntax syntax)
    {
        return syntax switch
        {
            PropertyDeclarationSyntax property => property.Identifier.Text,
            MethodDeclarationSyntax method => method.Identifier.Text,
            _ => throw new NotImplementedException(),
        };
    }
}
