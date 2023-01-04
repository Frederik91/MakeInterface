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
        if (classSyntax.BaseList is null)
            return interfaceDeclaration;

        var baseTypes = classSyntax.BaseList.Types;

        if (baseTypes.Any() != true)
            return interfaceDeclaration;

        var baseClass = baseTypes.FirstOrDefault(x => !IsInterface(x, semanticModel));

        if (baseClass is not null)
        {
            baseTypes = AddInheritedInterfaces(semanticModel, baseTypes, baseClass);
        }

        var interfaces = baseTypes
            .Select(x => semanticModel.GetSymbolInfo(x.Type).Symbol)
            .OfType<ITypeSymbol>()
            .Where(x => x.TypeKind == TypeKind.Interface && x.Name != interfaceDeclaration.Identifier.Text)
            .ToArray();
        

        if (!interfaces.Any())
            return interfaceDeclaration;

        var basesToAdd = baseTypes.Where(x => interfaces.Any(y => semanticModel.IsSameType(x, y))).ToArray();
        interfaceDeclaration = interfaceDeclaration.AddBaseListTypes(basesToAdd);
        foreach (var @interface in interfaces)
        {
            var interfaceImplementationSyntax = basesToAdd.FirstOrDefault(x => semanticModel.IsSameType(x, @interface));
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

    private static SeparatedSyntaxList<BaseTypeSyntax> AddInheritedInterfaces(SemanticModel semanticModel, SeparatedSyntaxList<BaseTypeSyntax> baseTypes,
        BaseTypeSyntax baseClass)
    {
        baseTypes = baseTypes.Remove(baseClass);
        // Get the symbol for the interface
        var classSymbol = semanticModel.GetDeclaredSymbol(baseClass);
        if (classSymbol is null)
            return baseTypes;

        // Get the syntax node for the interface declaration
        if (classSymbol.DeclaringSyntaxReferences[0].GetSyntax() is not ClassDeclarationSyntax classDeclarationSyntax)
            return AddBaseTypesBySymbol(semanticModel, baseClass, baseTypes);
        
        if (classDeclarationSyntax.BaseList is null)
            return baseTypes;

        var baseInterfaces = classDeclarationSyntax.BaseList.Types.Where(x => IsInterface(x, semanticModel));
        foreach (var baseInterface in baseInterfaces)
        {
            baseTypes = baseTypes.Add(baseInterface);
        }

        return baseTypes;
    }

    private static SeparatedSyntaxList<BaseTypeSyntax> AddBaseTypesBySymbol(SemanticModel semanticModel, BaseTypeSyntax baseClass, SeparatedSyntaxList<BaseTypeSyntax> baseTypes)
    {
        // Get the symbol for the base class
        var baseClassSymbol = semanticModel.GetDeclaredSymbol(baseClass) as INamedTypeSymbol;
        if (baseClassSymbol is null)
            return baseTypes;

        // Get the list of inherited interfaces
        IEnumerable<INamedTypeSymbol> inheritedInterfaces = baseClassSymbol.AllInterfaces;

        // Create a list of base type syntax objects for the inherited interfaces
        IEnumerable<BaseTypeSyntax> baseTypeSyntaxList = inheritedInterfaces
            .Select(interfaceSymbol => SyntaxFactory.SimpleBaseType(
                SyntaxFactory.ParseTypeName(interfaceSymbol.Name)
            ));
        
        return baseTypes.AddRange(baseTypeSyntaxList);
    }

    private static bool IsInterface(BaseTypeSyntax baseTypeSyntax, SemanticModel semanticModel)
    {
        // Get the type syntax from the base type syntax
        var typeSyntax = baseTypeSyntax.Type;

        // Check if the type syntax is an interface declaration syntax
        if (typeSyntax.IsKind(SyntaxKind.InterfaceDeclaration))
        {
            return true;
        }

        // If the type syntax is not an interface declaration syntax,
        // it could be a type defined in another assembly.
        // In this case, we can fall back to using the ISymbol interface
        // to check the TypeKind of the type.
        return semanticModel.GetDeclaredSymbol(baseTypeSyntax) is INamedTypeSymbol { TypeKind: TypeKind.Interface };
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
