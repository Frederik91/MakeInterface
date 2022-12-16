# Interfaces.SourceGenerator
Creates an interface of a class using source generator

[![.NET](https://github.com/Frederik91/Interfaces.SourceGenerator/actions/workflows/dotnet.yml/badge.svg)](https://github.com/Frederik91/Interfaces.SourceGenerator/actions/workflows/dotnet.yml)

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

The generated interface will then be generated as IMyClass.g.cs
```csharp
public interface IMyClass
{
	string MyProperty { get; set; }
	void MyMethod();
}
```

You can then implement the interface in your class
```csharp
public class MyClass : IMyClass
{
	public string MyProperty { get; set; }
	public void MyMethod() { }
}
```

## Installation
Install the NuGet package [Interfaces.SourceGenerator](https://www.nuget.org/packages/Interfaces.SourceGenerator/)

## License
MIT