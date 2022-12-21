using Microsoft.CodeAnalysis;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Text;
using System.Text;
using MakeInterface.Generator;

namespace MakeInterface.Tests;

[UsesVerify]
public class InterfaceGeneratorTests
{
    private static readonly string _header = """
        // <auto-generated/>
        #pragma warning disable
        #nullable enable
        
        """;

    private readonly ImmutableArray<string> references = AppDomain.CurrentDomain
    .GetAssemblies()
    .Where(assembly => !assembly.IsDynamic)
    .Select(assembly => assembly.Location)
    .ToImmutableArray();

    [Fact]
    public Task CreateInterface()
    {
        var source = """
#nullable enable
using MakeInterface.Tests.Models;
using MakeInterface.Contracts.Attributes;
using System.Collections.Generic;
namespace MakeInterface.Tests
{
    [GenerateInterface]
    public partial class Class1 : BaseClass
    {
        public void Method1() { }
        public TestModel Test() { return new TestModel(); }
        public void Test2<T>(T data) { }
        public void Test3<T>(T data) where T : TestModel { }
        public string? Property1 { get; set; }
        public List<ITestModel?>? TestCollection() { return new List<ITestModel?>(); }
        public void OutMethod(out string data) { data = string.Empty; }
        public void RefMethod(ref string data) {  }
        public void DefaultNullMethod(string? data = default) {  }
        public void DefaultMethod(int data = default) {  }

        public override string AbstractProperty { get; set; }
        public override void AbstractMethod { get; set; }
        public override string VirtualProperty { get; set; }
        public override void VirtualMethod() { }
        
        public async Task AsyncMethod() { }

        public string PropertyWithDefault => string.Empty;
        public string PropertyWithDefault2 { get; } = string.Empty;
    }

    public partial class Class1 
    {
        public void Method2() { }
    }

    public abstract class BaseClass 
    {
        public abstract string AbstractProperty { get; set; }
        public abstract void AbstractMethod() { }
        public virtual string VirtualProperty { get; set; }
        public virtual void VirtualMethod() { }
    }    
}

namespace MakeInterface.Tests.Models
{
    [GenerateInterface]
    public class TestModel : ITestModel
    {
        
    }
}
""";

        return TestHelper.Verify(source, "Class1.cs");
    }

    [Fact]
    public Task NamedModel()
    {
        var source = """
using NewName = MakeInterface.Tests.Models.NamedModel;
namespace MakeInterface.Tests
{
    [GenerateInterface]
    public partial class Class1 : BaseClass
    {
        public NewName GetNewName { get; }
        public NewName SetNewName(NewName name) { return name; }
    }  
}

namespace MakeInterface.Tests.Models
{
    public class NamedModel
    {
        
    }
}
""";

        return TestHelper.Verify(source);
    }

    [Fact]
    public Task NotPublicSetter()
    {
        var source = """
namespace MakeInterface.Tests
{
    [GenerateInterface]
    public class Class1
    {
        public string Private1 { get; private set; }
        public string Protected1 { get; protected set; }
        public string File1 { get; file set; }
        public string Internal1 { get; internal set; }
        
        public string Private2 { get; private set; } = "Test";
        public string Protected2 { get; protected set; } = "Test";
        public string File2 { get; file set; } = "Test";
        public string Internal2 { get; internal set; } = "Test";
    }  
}
""";

        return TestHelper.Verify(source);
    }

    [Fact]
    public Task ObservableProperty()
    {
        var source = """
namespace MakeInterface.Tests
{
    [GenerateInterface]
    public class Class1
    {
        [ObservableProperty]
        string? _generatedProperty;
    }  
}
""";

        return TestHelper.Verify(source);
    }

    [Fact]
    public Task RelayCommand()
    {
        var source = """
namespace MakeInterface.Tests
{
    [GenerateInterface]
    public class Class1
    {
        private bool CanExecuteTest() => true;
    
        [RelayCommand]
        private void Test() { }
        
        [RelayCommand(CanExecute = nameof(CanExecuteTest))]
        private void Test2() { }
        
        [RelayCommand]
        private Task Test3() { return Task.CompletedTask; }
        
        [RelayCommand]
        private async Task Test4() { await Task.CompletedTask; }
        
        [RelayCommand]
        private Task<string> Test5() { return Task.FromResult(string.Empty); }

        [RelayCommand]
        private async Task<string> Test6() { return Task.FromResult(string.Empty); }
    }  
}
""";

        return TestHelper.Verify(source);
    }
}