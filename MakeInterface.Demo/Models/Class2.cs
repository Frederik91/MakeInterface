using MakeInterface;
using MakeInterface.Demo.Implementations;

namespace MakeInterface.Demo.Models;

[GenerateInterface]
public class Class2 : IClass2
{
    public IClass1? Class1 { get; }

    public string Name { get; set; }

    public string FullName() => $"{Name} {Name}";
}
