using CommunityToolkit.Mvvm.ComponentModel;

namespace LightsToFlag.App.ViewModels;

/// <summary>Hosts the active full-screen page shown inside the shell window.</summary>
public partial class ShellViewModel : ObservableObject
{
    [ObservableProperty]
    private object? _currentPage;
}
