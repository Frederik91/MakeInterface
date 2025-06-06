# MakeInterface
Creates an interface of a class using source generator

[![.NET](https://github.com/Frederik91/MakeInterface/actions/workflows/dotnet.yml/badge.svg)](https://github.com/Frederik91/MakeInterface/actions/workflows/dotnet.yml)

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
Install the NuGet package [MakeInterface](https://www.nuget.org/packages/MakeInterface.Generator/)

The required `GenerateInterface` attribute is automatically provided by the source generator, so no additional package reference is needed.


## License
MIT
## Release process
- Pushes to the `master` branch publish prerelease packages to GitHub Packages using versions like `0.1.0-ci.<run number>`.
- Tagging the repository with `v<version>` automatically publishes that version to NuGet.org if `NUGET_API_KEY` is configured.
