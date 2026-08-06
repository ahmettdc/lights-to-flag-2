using System;
using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using LTF.App.Navigation;
using LTF.App.ViewModels.Screens;

namespace LTF.App.Services;

/// <summary>
/// Default navigation for the shell's content region. Real screens register a view-model factory via
/// <see cref="Register"/> (Settings in M20; more in M21/M22); any key without one resolves to a
/// <see cref="PlaceholderScreenViewModel"/>. Raises change notifications so the sidebar tracks the active row.
/// </summary>
public sealed partial class NavigationService : ObservableObject, INavigationService
{
    private readonly Dictionary<NavKey, Func<object>> _factories = new();

    [ObservableProperty]
    private NavKey _currentKey;

    [ObservableProperty]
    private object? _currentScreen;

    /// <summary>Register the real view-model factory for a key, replacing its placeholder.</summary>
    public void Register(NavKey key, Func<object> factory) => _factories[key] = factory;

    public void Navigate(NavKey key)
    {
        var item = NavRegistry.Items.First(i => i.Key == key);
        CurrentKey = key;
        CurrentScreen = _factories.TryGetValue(key, out var factory)
            ? factory()
            : new PlaceholderScreenViewModel(item);
    }
}
