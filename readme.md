# Interfaces.SourceGenerator
Creates an interface of a class using source generator

## Usage
Add the attribute to the class you want to generate the interface for
```csharp
[GenerateInterface]
public class MyClass
{
	public string MyProperty { get; set; }
	public void MyMethod() { }
}
```

## Installation
Install the NuGet package [Interfaces.SourceGenerator](https://www.nuget.org/packages/Interfaces.SourceGenerator/)

## License
MIT