# MakeInterface
Generate interfaces for your classes at compile time using a simple attribute.

[![.NET](https://github.com/Frederik91/MakeInterface/actions/workflows/dotnet.yml/badge.svg)](https://github.com/Frederik91/MakeInterface/actions/workflows/dotnet.yml)

MakeInterface is a [C# source generator](https://learn.microsoft.com/dotnet/csharp/roslyn-sdk/source-generators-overview) that produces an `I{ClassName}` interface for any class marked with `GenerateInterface`.  The generator analyses the public members of the class and writes the matching interface into your project's build output.

This is particularly useful when you simply need an interface to facilitate unit testing or dependency injection.

## Usage
1. Install the NuGet package (see [Installation](#installation)).
2. Add the attribute to the class you want an interface for.
3. Build your project. The interface will appear in your `obj` folder and be part of the compilation.
```csharp
[GenerateInterface]
public class MyClass
{
        public string MyProperty { get; set; }
        public void MyMethod() { }
}
```

Need to omit a member? Use the `Exclude` property to provide a list of member names:
```csharp
[GenerateInterface(Exclude = new[] { nameof(MyMethod) })]
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

## CommunityToolkit.MVVM integration
If you use [CommunityToolkit.MVVM](https://learn.microsoft.com/dotnet/communitytoolkit/mvvm/overview),
methods marked with `RelayCommand` will have their generated command properties
reflected in the interface as `IRelayCommand` or `IAsyncRelayCommand` members.

```csharp
public partial class MyViewModel
{
    [RelayCommand]
    private async Task LoadAsync() => await Task.CompletedTask;
}
```

This results in the interface containing:

```csharp
IAsyncRelayCommand LoadCommand { get; }
```

## When should I generate interfaces?
Generating interfaces works well when you only need an interface so the class can be mocked in unit tests or injected into other components.  In that scenario your class is typically the single implementation and keeping the interface in sync manually becomes boilerplate.  Let the generator do the work for you.

If you maintain many implementations of the same interface or the interface needs to diverge from the class surface, consider writing the interface yourself.  Manually created interfaces give you more control over its shape and versioning.

## Installation
Install the NuGet package [MakeInterface](https://www.nuget.org/packages/MakeInterface.Generator/):

```bash
dotnet add package MakeInterface.Generator
```

The `GenerateInterface` attribute is included in the package and will be available after the build without adding any extra references.


## License
MIT
## Release process
- Pushes to the `master` branch publish prerelease packages to GitHub Packages using versions like `0.1.0-ci.<run number>`.
- Tagging the repository with `v<version>` automatically publishes that version to NuGet.org if `NUGET_API_KEY` is configured.
