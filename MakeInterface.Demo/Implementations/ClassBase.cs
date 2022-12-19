using CommunityToolkit.Mvvm.ComponentModel;

namespace MakeInterface.Demo.Implementations;

public class ClassBase : ObservableObject
{
    public int PropertyInBase { get; set; }
    public void MethodInBase() { }
}