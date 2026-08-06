using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using LTF.App.Navigation;
using LTF.App.Services;
using LTF.App.ViewModels.Screens;
using LTF.App.Views.Screens;
using Xunit;

namespace LTF.App.Tests;

/// <summary>
/// The navigation model + placeholder screen: the registry is complete and ordered, Navigate swaps the
/// current key + placeholder view-model, and the view-model resolves to its view via the app DataTemplate.
/// </summary>
public class NavigationTests
{
    [Fact]
    public void Registry_lists_every_nav_key_paddock_first_exit_last()
    {
        var keys = NavRegistry.Items.Select(i => i.Key).ToList();

        Assert.Equal(Enum.GetValues<NavKey>().Length, keys.Count);
        Assert.Equal(Enum.GetValues<NavKey>().Distinct().Count(), keys.Distinct().Count());
        Assert.Equal(NavKey.PaddockHub, keys[0]);
        Assert.Equal(NavKey.ExitToMenu, keys[^1]);
    }

    [Fact]
    public void Navigate_sets_the_current_key_and_a_placeholder_screen()
    {
        var nav = new NavigationService();

        nav.Navigate(NavKey.Finance);

        Assert.Equal(NavKey.Finance, nav.CurrentKey);
        var screen = Assert.IsType<PlaceholderScreenViewModel>(nav.CurrentScreen);
        Assert.Equal(NavKey.Finance, screen.Key);
        Assert.Equal("FINANCE", screen.Title);
    }

    [AvaloniaFact]
    public void Placeholder_view_model_resolves_to_its_view()
    {
        var vm = new PlaceholderScreenViewModel(NavRegistry.Items.First(i => i.Key == NavKey.Drivers));
        var host = new ContentControl { Content = vm };
        var window = new Window { Content = host };

        window.Show();
        Dispatcher.UIThread.RunJobs();

        var view = host.GetVisualDescendants().OfType<PlaceholderScreenView>().FirstOrDefault();
        Assert.NotNull(view);
    }
}
