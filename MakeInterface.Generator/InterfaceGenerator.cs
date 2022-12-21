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
                               if (node is not CompilationUnitSyntax compilationUnitSyntax)
                                   return false;

                               foreach (var namespaceDeclarationSyntax in compilationUnitSyntax.Members.OfType<BaseNamespaceDeclarationSyntax>())
                               {
                                   foreach (var classDeclarationSyntax in namespaceDeclarationSyntax.Members.OfType<ClassDeclarationSyntax>())
                                   {
                                       if (ConstainsGenerateInterfaceAttribute(classDeclarationSyntax))
                                           return true;
                                   }
                               }

                               return false;
                           },
                           (ctx, _) =>
                           {
                               return (ctx.SemanticModel, (CompilationUnitSyntax)ctx.Node);
                           });

        context.RegisterSourceOutput(classProvider, Generate);
    }

    private bool ConstainsGenerateInterfaceAttribute(ClassDeclarationSyntax classDeclarationSyntax)
    {
        foreach (var attributeList in classDeclarationSyntax.AttributeLists)
        {
            foreach (var attribute in attributeList.Attributes)
            {
                if (attribute.Name.ToString() == nameof(GenerateInterfaceAttribute) || attribute.Name.ToString() + "Attribute" == nameof(GenerateInterfaceAttribute))
                    return true;
            }
        }
        return false;
    }

    private void Generate(SourceProductionContext ctx, (SemanticModel, CompilationUnitSyntax) input)
    {
        try
        {
            var semanticModel = input.Item1;
            var compilationUnitSyntax = input.Item2;
            var trivia = SyntaxCreator.CreateTrivia();
            var namespaces = SyntaxFactory.List<MemberDeclarationSyntax>();
            foreach (var namepaceSyntax in compilationUnitSyntax.Members.OfType<BaseNamespaceDeclarationSyntax>())
            {
                var interfaces = SyntaxFactory.List<MemberDeclarationSyntax>();
                foreach (var classDeclarationSyntax in namepaceSyntax.Members.OfType<ClassDeclarationSyntax>())
                {
                    if (!ConstainsGenerateInterfaceAttribute(classDeclarationSyntax))
                        continue;

                    var interfaceName = "I" + classDeclarationSyntax.Identifier.Text;
                    var fileName = "I" + Path.GetFileNameWithoutExtension(compilationUnitSyntax.SyntaxTree.FilePath);

                    var interfaceSyntax = GenerateInterface(interfaceName, classDeclarationSyntax, semanticModel);
                    if (interfaceSyntax is null)
                    {
                        var diagnostic = Diagnostic.Create(new DiagnosticDescriptor("MI002", "MakeInterface", $"Failed to create interface '{interfaceName}'", "MakeInterface", DiagnosticSeverity.Error, true), Location.None);
                        ctx.ReportDiagnostic(diagnostic);
                        continue;
                    }
                    interfaces = interfaces.Add(interfaceSyntax);
                }

                var namespaceSyntax = namepaceSyntax
                    .WithMembers(SyntaxFactory.List<MemberDeclarationSyntax>())
                    .WithNamespaceKeyword(trivia)
                    .WithMembers(interfaces);
                namespaces = namespaces.Add(namespaceSyntax);
            }

            var compilationUnit = SyntaxFactory.CompilationUnit()
                .WithUsings(compilationUnitSyntax.Usings)
                .WithMembers(namespaces)
                .NormalizeWhitespace();


            var sourceText = compilationUnit.ToFullString();

            ctx.AddSource($"I{Path.GetFileNameWithoutExtension(compilationUnitSyntax.SyntaxTree.FilePath)}.g.cs", SourceText.From(sourceText, Encoding.UTF8));
        }
        catch (Exception e)
        {
            var diagnostic = Diagnostic.Create(new DiagnosticDescriptor("MI001", "MakeInterface", e.ToString(), "MakeInterface", DiagnosticSeverity.Error, true), Location.None);
            ctx.ReportDiagnostic(diagnostic);
        }

    }

    private InterfaceDeclarationSyntax? GenerateInterface(string interfaceName, ClassDeclarationSyntax classSyntax, SemanticModel semanticModel)
    {
        var members = new List<MemberDeclarationSyntax>();

        foreach (var memberSyntax in classSyntax.Members)
        {
            if (IsNotValidInterfaceNamber(memberSyntax.Modifiers))
                continue;

            var publicModifier = memberSyntax.Modifiers.FirstOrDefault(x => x.IsKind(SyntaxKind.PublicKeyword));
            if (memberSyntax is FieldDeclarationSyntax fieldDeclarationSyntax && ContainsAttributeWithName(fieldDeclarationSyntax.AttributeLists, "ObservableProperty"))
            {
                var name = fieldDeclarationSyntax.Declaration.Variables.First().Identifier.Text;
                name = GetObservablePropertyName(name);
                // Create property from field
                var propertyDeclarationSyntax = SyntaxFactory.PropertyDeclaration(fieldDeclarationSyntax.Declaration.Type, name);
                propertyDeclarationSyntax = propertyDeclarationSyntax
                    .WithModifiers(SyntaxFactory.TokenList(publicModifier))
                    .WithAccessorList(SyntaxFactory.AccessorList(SyntaxFactory.List(new[]
                    {
                        SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                            .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)),
                        SyntaxFactory.AccessorDeclaration(SyntaxKind.SetAccessorDeclaration)
                            .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)),
                    })));
                members.Add(propertyDeclarationSyntax);
            }
            else if (memberSyntax is MethodDeclarationSyntax relayCommandMethod && ContainsAttributeWithName(memberSyntax.AttributeLists, "RelayCommand"))
                members.Add(CreateRelayCommand(relayCommandMethod));
            else if (!memberSyntax.Modifiers.IsPublic())
                continue;
            else if (memberSyntax is MethodDeclarationSyntax methodSyntax)
            {
                var modifiers = methodSyntax.Modifiers.Remove(publicModifier);
                var asyncModifier = modifiers.FirstOrDefault(x => x.IsKind(SyntaxKind.AsyncKeyword));
                modifiers = modifiers.Remove(asyncModifier);

                var newMethod = methodSyntax
                    .WithBody(null)
                    .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken))
                    .WithModifiers(modifiers);

                members.Add(newMethod);
            }
            else if (memberSyntax is PropertyDeclarationSyntax propertySyntax)
            {
                var newProperty = propertySyntax
                    .WithModifiers(propertySyntax.Modifiers.Remove(publicModifier));


                if (newProperty.ExpressionBody is not null)
                {
                    var getAccessor = SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                                .WithSemicolonToken(
                                    SyntaxFactory.Token(SyntaxKind.SemicolonToken));

                    newProperty = newProperty
                        .WithExpressionBody(null)
                        .WithAccessorList(SyntaxFactory.AccessorList(SyntaxFactory.SingletonList(getAccessor)))
                        .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.None));
                }
                else if (newProperty.Initializer != null)
                {
                    newProperty = newProperty
                        .WithInitializer(null)
                        .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.None));
                }
                if (newProperty.AccessorList is not null)
                {
                    var newAccessors = SyntaxFactory.List<AccessorDeclarationSyntax>();
                    foreach (var accessor in newProperty.AccessorList.Accessors)
                    {
                        if (accessor.Modifiers.Any(x => x.IsKind(SyntaxKind.PrivateKeyword) || x.IsKind(SyntaxKind.ProtectedKeyword) || x.IsKind(SyntaxKind.InternalKeyword) || x.IsKind(SyntaxKind.FileKeyword)))
                            continue;

                        var newAccessor = accessor
                            .WithBody(null)
                            .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));

                        newAccessors = newAccessors.Add(newAccessor);
                    }
                    newProperty = newProperty.WithAccessorList(SyntaxFactory.AccessorList(newAccessors));
                }

                newProperty = newProperty.WithTrailingTrivia(SyntaxTriviaList.Empty).WithLeadingTrivia(SyntaxTriviaList.Empty);
                members.Add(newProperty);
            }
        }

        var interfaceDeclaration = SyntaxFactory.InterfaceDeclaration(interfaceName)
                                .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword), SyntaxFactory.Token(SyntaxKind.PartialKeyword)))
                                .WithMembers(SyntaxFactory.List(members));

        interfaceDeclaration = AddInterfaces(interfaceDeclaration, classSyntax, semanticModel);

        return interfaceDeclaration;
    }

    private InterfaceDeclarationSyntax AddInterfaces(InterfaceDeclarationSyntax interfaceDeclaration, ClassDeclarationSyntax classSyntax, SemanticModel semanticModel)
    {
        var baseInterfaces = classSyntax.BaseList?.Types
            .OfType<SimpleBaseTypeSyntax>()
            .ToArray();

        if (baseInterfaces?.Any() != true)
            return interfaceDeclaration;

        var interfaces = baseInterfaces
            .Select(x => semanticModel.GetSymbolInfo(x.Type).Symbol)
            .OfType<ITypeSymbol>()
            .Where(x => x.TypeKind == TypeKind.Interface)
            .ToArray();

        if (!interfaces.Any())
            return interfaceDeclaration;

        baseInterfaces = baseInterfaces.Where(x => interfaces.Any(y => y.Name == x.Type.ToString())).ToArray();
        interfaceDeclaration = interfaceDeclaration.AddBaseListTypes(baseInterfaces);
        foreach (var @interface in interfaces)
        {
            var interfaceImplementationSyntax = baseInterfaces.FirstOrDefault(x => x.Type.ToString() == @interface.Name);
            if (interfaceImplementationSyntax is null)
                continue;

            // Get members from interface
            var baseInterfaceMembers = @interface.GetMembers().Select(x => x.Name);

            // Get members that matches interfaceMembers
            var membersToRemove = interfaceDeclaration.Members
                .Where(member => baseInterfaceMembers.Contains(member.GetName()))
                .ToList();

            // Remove the members from the interface declaration.
            var newInterface = interfaceDeclaration.RemoveNodes(membersToRemove, SyntaxRemoveOptions.KeepNoTrivia);
            if (newInterface is not null)
                interfaceDeclaration = newInterface;
        }

        // Add the base interfaces to the interface declaration's base list.
        return interfaceDeclaration;
    }

    private MemberDeclarationSyntax CreateRelayCommand(MethodDeclarationSyntax methodSyntax)
    {
        var isAsync = MethodIsAsync(methodSyntax);
        var returnType = SyntaxFactory.ParseTypeName("global::CommunityToolkit.Mvvm.Input." + (isAsync ? "IAsyncRelayCommand" : "IRelayCommand"));
        var methodName = methodSyntax.Identifier.Text;

        // remove Async from the end of the name if it exists
        if (methodName.EndsWith("Async", StringComparison.OrdinalIgnoreCase))
            methodName = methodName.Substring(0, methodName.Length - 5);

        var commandName = methodName + "Command";
        var comment = SyntaxFactory.Comment($"// This property was generated because of the RelayCommand attribute applied to the '{methodSyntax.Identifier.Text}' method. See https://aka.ms/CommunityToolkit.MVVM");
        var leadingTrivia = SyntaxFactory.TriviaList(comment);

        return SyntaxFactory.PropertyDeclaration(returnType, commandName)
            .WithLeadingTrivia(leadingTrivia)
            .WithAccessorList(SyntaxFactory.AccessorList(SyntaxFactory.List(new[]
            {
                SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                    .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)),
            })));
    }

    private bool MethodIsAsync(MethodDeclarationSyntax methodSyntax)
    {
        if (methodSyntax.Modifiers.Any(x => x.IsKind(SyntaxKind.AsyncKeyword)))
            return true;

        return methodSyntax.ReturnType is GenericNameSyntax genericName && genericName.Identifier.Text == "Task" || methodSyntax.ReturnType.ToString() == "Task";
    }

    private static bool IsNotValidInterfaceNamber(ISymbol member)
    {
        return !member.IsDefinition || member.IsStatic || member.IsImplicitlyDeclared || member.IsOverride;
    }

    private static bool IsNotValidInterfaceNamber(SyntaxTokenList tokenList)
    {
        return tokenList.IsStatic() || tokenList.IsOverride();
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

    private string GetObservablePropertyName(string fieldName)
    {
        if (fieldName.StartsWith("_"))
            fieldName = fieldName.Substring(1);

        return ConvertToPascalCase(fieldName);
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

    private bool ContainsAttributeWithName(SyntaxList<AttributeListSyntax> attributeLists, string attrubuteName)
    {
        foreach (var attributeList in attributeLists)
        {
            foreach (var attribute in attributeList.Attributes)
            {
                if (attribute.Name is null)
                    continue;

                if (attribute.Name.ToString() == attrubuteName || attribute.Name.ToString() == attrubuteName + "Attribute")
                    return true;
            }
        }
        return false;
    }
}
