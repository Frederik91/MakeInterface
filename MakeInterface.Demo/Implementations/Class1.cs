using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MakeInterface;
using MakeInterface.Demo.Models;

namespace MakeInterface.Demo.Implementations;

[GenerateInterface]
public partial class Class1 : ClassBase, IClass1, IDisposable
{
    public int PublicProperty { get; set; }
    public int ReadOnlyProperty { get; }
    internal int Internalproperty { get; set; }
    private int PrivateProperty { get; set; }

    [ObservableProperty]
    string? _generatedProperty;

    [RelayCommand]
    private void CommandMethod() { }

    [RelayCommand]
    private void CommandMethod3(string test) { }

    [RelayCommand]
    private Task CommandMethod2() { return Task.CompletedTask; }

    public void Test() { }

    public void WithArgs(string test) { }

    public void WithGenerics<T>(string test) { }

    public void WithConstrainedGenerics<T>(T test) where T : Test { }

    public string WithReturnType(string test) => test;

    private void PrivateMethod() { }

    internal void InternalMethod() { }

    public void Dispose()
    {
        throw new NotImplementedException();
    }
}