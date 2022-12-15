using Interfaces.SourceGenerator.Demo.Models;

namespace Interfaces.SourceGenerator.Demo;
public class Class1 : ClassBase
{
    public int PublicProperty { get; set; }
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