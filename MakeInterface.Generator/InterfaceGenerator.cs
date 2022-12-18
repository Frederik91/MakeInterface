using MakeInterface.Generator.Attributes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Diagnostics;
using System.Text;

namespace MakeInterface.Generator;

[Generator]
public class InterfaceGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var classProvider = context.SyntaxProvider
                           .CreateSyntaxProvider((node, _) =>
                           {
                               if (node is not ClassDeclarationSyntax classDeclarationSyntax)
                                   return false;
                               
                               foreach (var attributeList in classDeclarationSyntax.AttributeLists)
                               {
                                   foreach (var attribute in attributeList.Attributes)
                                   {
                                       if (attribute.Name.ToString() == nameof(GenerateInterfaceAttribute) || attribute.Name.ToString() + "Attribute" == nameof(GenerateInterfaceAttribute))
                                           return true;
                                   }
                               }
                               return false;
                           },
                           (ctx, _) =>
                           {
                               return ((ctx.SemanticModel), (ClassDeclarationSyntax)ctx.Node);
                           });

        context.RegisterSourceOutput(classProvider, Generate);
    }

    private void Generate(SourceProductionContext ctx, (SemanticModel, ClassDeclarationSyntax) cds)
    {
        var model = cds.Item1;
        var classSyntax = cds.Item2;

        var classSymbol = model.GetDeclaredSymbol(classSyntax);
        if (classSymbol is null)
            return;
        
        var interfaceName = "I" + classSymbol.Name;
        var source = GenerateInterface(classSymbol, interfaceName, new Dictionary<string, string>());
        var sourceText = source.ToFullString();
        ctx.AddSource($"{interfaceName}.g.cs", SourceText.From(sourceText, Encoding.UTF8));
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
}
