# MakeInterface
Generate interfaces for your classes at compile time using a simple attribute.
Generate interfaces for your classes at compile time using a simple attribute.

[![.NET](https://github.com/Frederik91/MakeInterface/actions/workflows/dotnet.yml/badge.svg)](https://github.com/Frederik91/MakeInterface/actions/workflows/dotnet.yml)

MakeInterface is a [C# source generator](https://learn.microsoft.com/dotnet/csharp/roslyn-sdk/source-generators-overview) that produces an `I{ClassName}` interface for any class marked with `GenerateInterface`.  The generator analyses the public members of the class and writes the matching interface into your project's build output.

This is particularly useful when you simply need an interface to facilitate unit testing or dependency injection.

MakeInterface is a [C# source generator](https://learn.microsoft.com/dotnet/csharp/roslyn-sdk/source-generators-overview) that produces an `I{ClassName}` interface for any class marked with `GenerateInterface`.  The generator analyses the public members of the class and writes the matching interface into your project's build output.

This is particularly useful when you simply need an interface to facilitate unit testing or dependency injection.

## Usage
1. Install the NuGet package (see [Installation](#installation)).
2. Add the attribute to the class you want an interface for.
3. Build your project. The interface will appear in your `obj` folder and be part of the compilation.
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

Need to omit a member? Use the `Exclude` property to provide a list of member names:
```csharp
[GenerateInterface(Exclude = new[] { nameof(MyMethod) })]
public class MyClass
{
        public string MyProperty { get; set; }
        public void MyMethod() { }
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
        public string MyProperty { get; set; }
        public void MyMethod() { }
}
```

```csharp
[GenerateInterface]
public partial class MailService
{
    [InterfaceInclude]          // will appear in IMailService
    public Task SendAsync() { /*...*/ }

    public void Flush() { }   // will NOT appear because opt-in is active
}
```

If a class has **no** `[InterfaceInclude]` attributes, the generator keeps its original
"include everything" behaviour.

## When should I generate interfaces?
Generating interfaces works well when you only need an interface so the class can be mocked in unit tests or injected into other components.  In that scenario your class is typically the single implementation and keeping the interface in sync manually becomes boilerplate.  Let the generator do the work for you.

If you maintain many implementations of the same interface or the interface needs to diverge from the class surface, consider writing the interface yourself.  Manually created interfaces give you more control over its shape and versioning.

## Installation
Install the NuGet package [MakeInterface](https://www.nuget.org/packages/MakeInterface.Generator/):

```bash
dotnet add package MakeInterface.Generator
```

The `GenerateInterface` attribute is included in the package and will be available after the build without adding any extra references.

## Versioning
This repository uses [GitVersion](https://gitversion.net/) in **Continuous Deployment** mode.
Every build calculates a deterministic SemVer 2.0 version from the Git history.
Local builds and CI therefore produce identical package and assembly versions.


## License
MIT
## Release process
- Pushes to `master` publish prerelease packages to GitHub Packages using the version calculated by GitVersion.
- Tagging the repository publishes the tagged version to NuGet.org when `NUGET_API_KEY` is configured.
