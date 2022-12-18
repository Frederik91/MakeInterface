using CommunityToolkit.Mvvm.ComponentModel;
using MakeInterface.Contracts.Attributes;
using MakeInterface.Demo.Models;

namespace MakeInterface.Demo;

[GenerateInterface]
public partial class Class1 : ClassBase, IClass1
{
    public int PublicProperty2 { get; set; }
    public int PublicProperty { get; set; }
    public int ReadOnlyProperty { get; }
    internal int Internalproperty { get; set; }
    private int PrivateProperty { get; set; }

    [ObservableProperty]
    string? _generatedProperty;
    
    public void Test() { }

    public void WithArgs(string test) { }

    public void WithGenerics<T>(string test) { }

    public void WithConstrainedGenerics<T>(T test) where T : Test { }

    public string WithReturnType(string test) => test;

    private void PrivateMethod() { }

    internal void InternalMethod() { }
}