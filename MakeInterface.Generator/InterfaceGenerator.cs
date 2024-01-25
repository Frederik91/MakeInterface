using MakeInterface.Generator.Attributes;
using MakeInterface.Generator.Extensions;
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

        var attribute = classSyntax.AttributeLists
            .SelectMany(x => x.Attributes)
            .FirstOrDefault(x => x.Name.ToString() == nameof(GenerateInterfaceAttribute) || x.Name.ToString() + "Attribute" == nameof(GenerateInterfaceAttribute));

        if (attribute is null)
            throw new Exception($"Class '{classSyntax.Identifier.Text}' does not have the '{nameof(GenerateInterfaceAttribute)}' attribute");

        var excludedMembers = GetExcludedMembers(attribute);
        var membersFromImplementedTypes = GetMembersDeclaredByInterfacesTypes(classSyntax, semanticModel);

        foreach (var memberSyntax in classSyntax.Members)
        {
            if (IsNotValidInterfaceNamber(memberSyntax.Modifiers))
                continue;

            var name = memberSyntax.GetName();
            if (name is null || excludedMembers.Contains(name))
                continue;

            if (membersFromImplementedTypes.Contains(name))
                continue;

            var publicModifier = memberSyntax.Modifiers.FirstOrDefault(x => x.IsKind(SyntaxKind.PublicKeyword));
            if (memberSyntax is FieldDeclarationSyntax fieldDeclarationSyntax && ContainsAttributeWithName(fieldDeclarationSyntax.AttributeLists, "ObservableProperty"))
            {
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

                var overrideModifier = modifiers.FirstOrDefault(x => x.IsKind(SyntaxKind.OverrideKeyword));
                modifiers = modifiers.Remove(overrideModifier);

                var newMethod = methodSyntax
                    .WithBody(null)
                    .WithExpressionBody(null)
                    .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken))
                    .WithModifiers(modifiers);

                members.Add(newMethod);
            }
            else if (memberSyntax is PropertyDeclarationSyntax propertySyntax)
            {
                var newProperty = propertySyntax
                    .WithModifiers(propertySyntax.Modifiers.Remove(publicModifier));

                var overrideModifier = newProperty.Modifiers.FirstOrDefault(x => x.IsKind(SyntaxKind.OverrideKeyword));
                newProperty = newProperty.WithModifiers(newProperty.Modifiers.Remove(overrideModifier));

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
                            .WithExpressionBody(null)
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

    private List<string> GetExcludedMembers(AttributeSyntax attribute)
    {
        if (attribute.ArgumentList is null)
            return [];

        var result = new List<string>();
        foreach (var argument in attribute.ArgumentList.Arguments)
        {
            if (argument.Expression is not ArrayCreationExpressionSyntax literalExpressionSyntax)
                continue;

            if (literalExpressionSyntax.Initializer is not InitializerExpressionSyntax initializerExpressionSyntax)
                continue;

            foreach (var expression in initializerExpressionSyntax.Expressions)
            {
                if (expression is not LiteralExpressionSyntax literalExpression)
                    continue;

                if (literalExpression.Token.Value is string value)
                    result.Add(value);
            }
        }
        return result;
    }

    private List<string> GetMembersDeclaredByInterfacesTypes(ClassDeclarationSyntax classSyntax, SemanticModel semanticModel)
    {
        var baseTypes = classSyntax.BaseList?.Types
            .OfType<SimpleBaseTypeSyntax>()
            .Select(x => semanticModel.GetSymbolInfo(x.Type).Symbol)
            .OfType<ITypeSymbol>();

        var result = new List<string>();
        foreach (var baseType in baseTypes ?? new List<ITypeSymbol>())
        {
            var interfaceMembers = GetMembersFromInterfaces(baseType);
            result.AddRange(interfaceMembers);
        }
        return result;
    }

    private List<string> GetMembersFromInterfaces(ITypeSymbol baseType)
    {
        var result = new List<string>();
        foreach (var interfaceType in baseType.AllInterfaces)
        {
            var interfaceMembers = GetMembersFromType(interfaceType);
            result.AddRange(interfaceMembers);
        }
        return result;
    }

    private static List<string> GetMembersFromType(ITypeSymbol baseType)
    {
        List<string> members = new();

        foreach (var member in baseType.GetMembers())
        {
            if (baseType.TypeKind == TypeKind.Class && IsNotValidInterfaceNamber(member))
                continue;

            if (member is IMethodSymbol methodSymbol && methodSymbol.MethodKind is MethodKind.PropertyGet or MethodKind.PropertySet)
                continue;

            members.Add(member.Name);
        }

        return members;
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
            .Where(x => x.TypeKind == TypeKind.Interface && x.Name != interfaceDeclaration.Identifier.Text)
            .ToArray();

        if (!interfaces.Any())
            return interfaceDeclaration;

        baseInterfaces = baseInterfaces.Where(x => interfaces.Any(y => semanticModel.IsSameType(x, y))).ToArray();
        interfaceDeclaration = interfaceDeclaration.AddBaseListTypes(baseInterfaces);
        foreach (var @interface in interfaces)
        {
            var interfaceImplementationSyntax = baseInterfaces.FirstOrDefault(x => semanticModel.IsSameType(x, @interface));
            if (interfaceImplementationSyntax is null)
                continue;

            // Get members from interface
            var baseInterfaceMembers = @interface.GetMembers().Select(x => x.Name);

            // Get members that matches interfaceMembers
            var membersToRemove = interfaceDeclaration.Members
                .Where(member => member.GetName() is { } name && baseInterfaceMembers.Contains(name))
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
        TypeSyntax returnType;
        if (methodSyntax.ParameterList.Parameters.Any())
        {
            var genericReturnType = SyntaxFactory.GenericName("global::CommunityToolkit.Mvvm.Input." + (isAsync ? "IAsyncRelayCommand" : "IRelayCommand"));
            // Get the list of type arguments for the generic name syntax.
            var typeArgumentList = genericReturnType.TypeArgumentList;

            typeArgumentList = typeArgumentList.AddArguments(methodSyntax.ParameterList.Parameters.Select(p => p.Type).OfType<TypeSyntax>().ToArray());
            returnType = genericReturnType.WithTypeArgumentList(typeArgumentList);
        }
        else
            returnType = SyntaxFactory.ParseTypeName("global::CommunityToolkit.Mvvm.Input." + (isAsync ? "IAsyncRelayCommand" : "IRelayCommand"));

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
        if (tokenList.IsStatic())
        {
            return true;
        }

        return false;
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
