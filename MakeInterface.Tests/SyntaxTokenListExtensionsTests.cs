using Microsoft.CodeAnalysis.CSharp;
using MakeInterface.Generator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MakeInterface.Tests;
public class SyntaxTokenListExtensionsTests
{
    [Fact]
    public void IsStatic()
    {
        var tokenList = SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.StaticKeyword));
        Assert.True(tokenList.IsStatic());

        tokenList = SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword));
        Assert.False(tokenList.IsStatic());
    }

    [Fact]
    public void IsPublic()
    {
        var tokenList = SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword));
        Assert.True(tokenList.IsPublic());

        tokenList = SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.StaticKeyword));
        Assert.False(tokenList.IsPublic());
    }

    [Fact]
    public void IsAbstract()
    {
        var tokenList = SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.AbstractKeyword));
        Assert.True(tokenList.IsAbstract());

        tokenList = SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword));
        Assert.False(tokenList.IsAbstract());
    }

    [Fact]
    public void IsOverride()
    {
        var tokenList = SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.OverrideKeyword));
        Assert.True(tokenList.IsOverride());

        tokenList = SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword));
        Assert.False(tokenList.IsOverride());
    }
}
