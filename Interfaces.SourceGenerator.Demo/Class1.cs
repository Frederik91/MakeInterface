using Interfaces.SourceGenerator.Contracts.Attributes;
using Interfaces.SourceGenerator.Demo.Models;

namespace Interfaces.SourceGenerator.Demo;

[GenerateInterface]
public class Class1 : ClassBase, IClass1
{
    public int PublicProperty { get; set; }
    public int ReadOnlyProperty { get; }
    internal int Internalproperty { get; set; }
    private int PrivateProperty { get; set; }

    public void Test() { }

    public void WithArgs(string test) { }

    public void WithGenerics<T>(string test) { }

    public void WithConstrainedGenerics<T>(T test) where T : Test { }

    public string WithReturnType(string test) => test;

    private void PrivateMethod() { }

    internal void InternalMethod() { }
}