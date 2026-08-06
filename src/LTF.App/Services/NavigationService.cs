using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using LTF.App.Navigation;
using LTF.App.ViewModels.Screens;

namespace LTF.App.Services;

/// <summary>
/// Default navigation. In M19 every entry resolves to a <see cref="PlaceholderScreenViewModel"/>;
/// M21/M22 will register real screen factories here without changing the shell chrome.
/// </summary>
public sealed partial class NavigationService : ObservableObject, INavigationService
{
    [ObservableProperty]
    private NavKey _currentKey;

    [ObservableProperty]
    private object? _currentScreen;

    public void Navigate(NavKey key)
    {
        var item = NavRegistry.Items.First(i => i.Key == key);
        CurrentKey = key;
        CurrentScreen = new PlaceholderScreenViewModel(item);
    }
}
