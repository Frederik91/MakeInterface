using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Text;

namespace MakeInterface.Generator;
internal sealed class ClassAttributeReceiver : ISyntaxContextReceiver
{
    private readonly string _expectedAttribute;
    public ClassAttributeReceiver(string expectedAttribute) => _expectedAttribute = expectedAttribute;

    public List<INamedTypeSymbol> Classes { get; } = new();

    public void OnVisitSyntaxNode(GeneratorSyntaxContext context)
    {
        if (context.Node is not ClassDeclarationSyntax classDeclarationSyntax)
            return;

        if (!HasAttribute(classDeclarationSyntax))
            return;

        var classSymbol = context.SemanticModel.GetDeclaredSymbol(classDeclarationSyntax) as INamedTypeSymbol;
        if (classSymbol == null)        
            return;       


        Classes.Add(classSymbol);
    }
    
    private bool HasAttribute(ClassDeclarationSyntax classDeclarationSyntax)
    {
        foreach (var attributeList in classDeclarationSyntax.AttributeLists)
        {
            foreach (var attribute in attributeList.Attributes)
            {
                if (attribute.Name.ToString() == _expectedAttribute || attribute.Name.ToString() + "Attribute" == _expectedAttribute)
                    return true;
            }
        }
        return false;
    }
}