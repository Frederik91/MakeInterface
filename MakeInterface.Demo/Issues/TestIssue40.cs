using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MakeInterface;

namespace TestDemo.Issues;

[GenerateInterface]
internal partial class ViewModel : ObservableObject, IViewModel
{
    [RelayCommand]
    private Task DoStuff(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
