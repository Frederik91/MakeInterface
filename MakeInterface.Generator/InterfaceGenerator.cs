using MakeInterface.Generator.Attributes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

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
        var source = GenerateInterface(classSymbol, interfaceName, classSyntax);
        var sourceText = source.ToFullString();
        ctx.AddSource($"{interfaceName}.g.cs", SourceText.From(sourceText, Encoding.UTF8));
    }

    private CompilationUnitSyntax GenerateInterface(INamedTypeSymbol classSymbol, string interfaceName, ClassDeclarationSyntax classSyntax)
    {


        var members = new List<MemberDeclarationSyntax>();
        foreach (var member in classSymbol.GetMembers())
        {
            if (!member.IsDefinition || member.IsStatic || member.IsImplicitlyDeclared || member.IsOverride)
                continue;

            if (member is IFieldSymbol fieldSymbol && member.DeclaredAccessibility == Accessibility.Private && ContainsAttributeWithName(member, "ObservableProperty"))
            {
                var name = GetObservablePropertyName(member);
                var propertySyntax = SyntaxCreator.CreatePropertyFromField(fieldSymbol, name);
                members.Add(propertySyntax);
            }
            else if (member.DeclaredAccessibility != Accessibility.Public)
                continue;
            else if (member is IMethodSymbol methodSymbol)
            {
                if (methodSymbol.MethodKind != MethodKind.Ordinary)
                    continue;

                var methodSyntax = SyntaxCreator.CreateMethod(methodSymbol);
                members.Add(methodSyntax);
            }
            else if (member is IPropertySymbol propertySymbol)
            {
                var propertySyntax = SyntaxCreator.CreateProperty(propertySymbol);
                members.Add(propertySyntax);
            }
        }

        var interfaceSyntax = SyntaxFactory.InterfaceDeclaration(interfaceName)
                                .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword), SyntaxFactory.Token(SyntaxKind.PartialKeyword)))
                                .WithMembers(SyntaxFactory.List(members));


        var trivia = SyntaxCreator.CreateTrivia();

        var namespaceSyntax = SyntaxFactory.FileScopedNamespaceDeclaration(SyntaxFactory.IdentifierName(classSymbol.ContainingNamespace.ToDisplayString())).NormalizeWhitespace();
        namespaceSyntax = namespaceSyntax.WithNamespaceKeyword(trivia);
        namespaceSyntax = namespaceSyntax.WithMembers(SyntaxFactory.SingletonList<MemberDeclarationSyntax>(interfaceSyntax));

        var usingsSyntax = CreateUsingSyntax(classSyntax);

        return SyntaxFactory.CompilationUnit()            
            .WithUsings(usingsSyntax)
            .WithMembers(SyntaxFactory.SingletonList<MemberDeclarationSyntax>(namespaceSyntax))
            .NormalizeWhitespace();
    }

    private SyntaxList<UsingDirectiveSyntax> CreateUsingSyntax(ClassDeclarationSyntax classSyntax)
    {
        var usingDirectives = new SyntaxList<UsingDirectiveSyntax>();

        var parentNode = classSyntax.Parent?.Parent;
        if (parentNode is null)
            return new SyntaxList<UsingDirectiveSyntax>();

        foreach (UsingDirectiveSyntax usingDirective in parentNode.ChildNodes().OfType<UsingDirectiveSyntax>())
        {
            usingDirectives = usingDirectives.Add(usingDirective);
        }
        return usingDirectives;
    }

    private string GetObservablePropertyName(ISymbol member)
    {
        var name = member.Name;
        if (name.StartsWith("_"))
            name = name.Substring(1);

        return ConvertToPascalCase(name);
    }

    private string ConvertToPascalCase(string camelCase)
    {
        // Split the string by capital letters
        var words = Regex.Split(camelCase, @"(?<!^)(?=[A-Z])");

        // Capitalize the first letter of each word and join them back together
        return string.Join("", words.Select(w => w.Substring(0, 1).ToUpper() + w.Substring(1)));
    }

    private bool ContainsAttributeWithName(ISymbol member, string attrubuteName)
    {
        foreach (var attribute in member.GetAttributes())
        {
            if (attribute.AttributeClass is null)
                continue;

            if (attribute.AttributeClass.Name == attrubuteName || attribute.AttributeClass.Name == attrubuteName + "Attribute")
                return true;
        }
        return false;
    }
}
