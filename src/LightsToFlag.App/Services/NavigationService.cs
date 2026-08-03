using System;
using LightsToFlag.App.ViewModels;

namespace LightsToFlag.App.Services;

/// <summary>Container-backed navigation that drives <see cref="ShellViewModel.CurrentPage"/>.</summary>
public sealed class NavigationService : INavigationService
{
    private readonly IServiceProvider _services;
    private readonly ShellViewModel _shell;

    public NavigationService(IServiceProvider services, ShellViewModel shell)
    {
        _services = services;
        _shell = shell;
    }

    public void NavigateTo<TViewModel>() where TViewModel : class
    {
        var page = _services.GetService(typeof(TViewModel))
                   ?? throw new InvalidOperationException($"View model {typeof(TViewModel).Name} is not registered.");
        _shell.CurrentPage = page;
    }

    public void Navigate(object pageViewModel) => _shell.CurrentPage = pageViewModel;
}
