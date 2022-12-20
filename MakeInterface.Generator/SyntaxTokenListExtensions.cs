using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MakeInterface.Generator;
internal static class SyntaxTokenListExtensions
{
    public static bool ContainsKeyword(this SyntaxTokenList tokenList, SyntaxKind keyword)
    {
        return tokenList.Any(t => t.IsKind(keyword));
    }

    public static bool IsStatic(this SyntaxTokenList list) => list.ContainsKeyword(SyntaxKind.StaticKeyword);
    public static bool IsPublic(this SyntaxTokenList list) => list.ContainsKeyword(SyntaxKind.PublicKeyword);
    public static bool IsAbstract(this SyntaxTokenList list) => list.ContainsKeyword(SyntaxKind.AbstractKeyword);
    public static bool IsOverride(this SyntaxTokenList list) => list.ContainsKeyword(SyntaxKind.OverrideKeyword);
}
