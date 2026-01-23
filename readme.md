# MakeInterface
Generate interfaces for your classes at compile time using a simple attribute.

[![.NET](https://github.com/Frederik91/MakeInterface/actions/workflows/dotnet.yml/badge.svg)](https://github.com/Frederik91/MakeInterface/actions/workflows/dotnet.yml)

MakeInterface is a [C# source generator](https://learn.microsoft.com/dotnet/csharp/roslyn-sdk/source-generators-overview) that produces an `I{ClassName}` interface for any class marked with `[GenerateInterface]`. The generator inspects public instance members and writes the matching interface into your project's build output.

This is useful when you need interfaces for unit tests or dependency injection without maintaining them manually.

## Usage
1. Install the NuGet package (see [Installation](#installation)).
2. Add the attribute to the class you want an interface for.
3. Build your project. The interface will appear in your `obj` folder and be part of the compilation.

```csharp
using MakeInterface;

[GenerateInterface]
public class MyClass
{
    public string MyProperty { get; set; }
    public void MyMethod() { }
}
```

Generated output:

```csharp
public partial interface IMyClass
{
    string MyProperty { get; set; }
    void MyMethod();
}
```

## Member rules
- Only instance members are considered; static members are ignored.
- Only public members are included.
- For properties, only public accessors are emitted (private/protected/internal/file setters are omitted).
- Expression-bodied and initialized properties are converted to getter-only interface members.
- If any member has `[InterfaceInclude]`, only members with that attribute are included.
- For partial classes, only members on the annotated declaration are considered.

## Excluding members
Use the `Exclude` property to provide a list of member names to skip:

```csharp
[GenerateInterface(Exclude = new[] { "MyMethod" })]
public class MyClass
{
    public string MyProperty { get; set; }
    public void MyMethod() { }
}
```

Generated output:

```csharp
public partial interface IMyClass
{
    string MyProperty { get; set; }
}
```

## Interface inheritance
If your class implements interfaces, the generated interface inherits those interfaces and does not duplicate their members.

If your class inherits a base class annotated with `[GenerateInterface]`, the generated interface inherits the base interface.

## Opt-in members with InterfaceInclude
If any member is marked with `[InterfaceInclude]`, only those members are included:

```csharp
[GenerateInterface]
public class MailService
{
    [InterfaceInclude]
    public Task SendAsync() { /* ... */ }

    public void Flush() { }
}
```

Generated output:

```csharp
public partial interface IMailService
{
    Task SendAsync();
}
```

## ObservableProperty support
Fields with `[ObservableProperty]` generate matching properties in the interface (using PascalCase naming):

```csharp
[GenerateInterface]
public class ViewModel
{
    [ObservableProperty]
    private string? _title;
}
```

Generated output:

```csharp
public partial interface IViewModel
{
    string? Title { get; set; }
}
```

## RelayCommand support
Methods with `[RelayCommand]` generate command properties in the interface. The generator chooses `IRelayCommand` or `IAsyncRelayCommand` based on the method signature, removes an `Async` suffix from the command name, and ignores `CancellationToken` parameters when building the generic command type.

```csharp
[GenerateInterface]
public class ViewModel
{
    [RelayCommand]
    private Task SaveAsync() { return Task.CompletedTask; }

    [RelayCommand]
    private void Reset() { }
}
```

Generated output:

```csharp
public partial interface IViewModel
{
    global::CommunityToolkit.Mvvm.Input.IAsyncRelayCommand SaveCommand { get; }
    global::CommunityToolkit.Mvvm.Input.IRelayCommand ResetCommand { get; }
}
```

## When should I generate interfaces?
Generating interfaces works well when you only need an interface so the class can be mocked in unit tests or injected into other components. In that scenario your class is typically the single implementation and keeping the interface in sync manually becomes boilerplate. Let the generator do the work for you.

If you maintain many implementations of the same interface or the interface needs to diverge from the class surface, consider writing the interface yourself. Manually created interfaces give you more control over its shape and versioning.

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
