using MakeInterface.Generator.Attributes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Diagnostics;
using System.Text;

namespace MakeInterface.Generator;

[Generator]
public class InterfaceGenerator : ISourceGenerator
{
    public void Execute(GeneratorExecutionContext context)
    {
        if (context.SyntaxContextReceiver is not ClassAttributeReceiver receiver)
            return;

        var interfaceNamespaceMap = receiver.Classes.ToDictionary(x => "I" + x.Name, x => x.ContainingNamespace.ToDisplayString() + ".I" + x.Name);

        foreach (var classSymbol in receiver.Classes)
        {
            
            var interfaceName = "I" + classSymbol.Name;
            var source = GenerateInterface(classSymbol, interfaceName, interfaceNamespaceMap);
            var sourceText = source.ToFullString();
            context.AddSource($"{interfaceName}", SourceText.From(sourceText, Encoding.UTF8));
        }
    }

    private CompilationUnitSyntax GenerateInterface(INamedTypeSymbol classSymbol, string interfaceName, Dictionary<string, string> interfaceNamespaceMap)
    {
        var trivia = SyntaxCreator.CreateTrivia();


        var members = new List<MemberDeclarationSyntax>();
        foreach (var member in classSymbol.GetMembers())
        {
            if (!member.IsDefinition || member.IsStatic || member.DeclaredAccessibility != Accessibility.Public)
                continue;

            if (member is IMethodSymbol methodSymbol)
            {
                if (methodSymbol.MethodKind != MethodKind.Ordinary)
                    continue;

                var methodSyntax = SyntaxCreator.CreateMethod(methodSymbol, interfaceNamespaceMap);
                members.Add(methodSyntax);
            }
            else if (member is IPropertySymbol propertySymbol)
            {
                var propertySyntax = SyntaxCreator.CreateProperty(propertySymbol, interfaceNamespaceMap);
                members.Add(propertySyntax);
            }
        }

        var interfaceSyntax = SyntaxFactory.InterfaceDeclaration(interfaceName)
                                .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword), SyntaxFactory.Token(SyntaxKind.PartialKeyword)))
                                .WithMembers(SyntaxFactory.List(members));


        var namespaceSyntax = SyntaxFactory.FileScopedNamespaceDeclaration(SyntaxFactory.IdentifierName(classSymbol.ContainingNamespace.ToDisplayString())).NormalizeWhitespace();
        namespaceSyntax = namespaceSyntax.WithNamespaceKeyword(trivia);
        namespaceSyntax = namespaceSyntax.WithMembers(SyntaxFactory.SingletonList<MemberDeclarationSyntax>(interfaceSyntax));


        return SyntaxFactory.CompilationUnit()
            .WithMembers(SyntaxFactory.SingletonList<MemberDeclarationSyntax>(namespaceSyntax))
            .NormalizeWhitespace();
    }

    public void Initialize(GeneratorInitializationContext context)
    {
        context.RegisterForSyntaxNotifications(() => new ClassAttributeReceiver(nameof(GenerateInterfaceAttribute)));
    }
}
