using CommunityToolkit.Mvvm.ComponentModel;

namespace MakeInterface.Demo;

public class ClassBase : ObservableObject
{
    public int PropertyInBase { get; set; }
    public void MethodInBase() { }
}