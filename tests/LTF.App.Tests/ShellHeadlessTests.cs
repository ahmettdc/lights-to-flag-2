using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using LTF.App.Navigation;
using LTF.App.Services;
using LTF.App.Session;
using LTF.App.Shell;
using LTF.App.ViewModels;
using LTF.App.ViewModels.Screens;
using LTF.App.Views.Menu;
using LTF.App.Views.Screens;
using Xunit;

namespace LTF.App.Tests;

/// <summary>
/// The assembled navigation shell (M19l): the four permanent regions (top bar, sidebar, status bar,
/// content) render headless on all three CI platforms, the content region starts on the Paddock Hub and
/// follows every nav entry, and the inbox popover toggles from the top bar and closes on navigation. The
/// M0 skeleton window this class used to drive (M19a) is gone — MainWindow now hosts the real shell.
/// </summary>
public class ShellHeadlessTests
{
    /// <summary>A minimal in-memory session so the shell tests never touch the flagship file on disk.</summary>
    private sealed class StubSession : ISessionSnapshot
    {
        public bool HasSession => true;
        public DateOnly Date => new(2027, 5, 14);
        public bool HasPlayerTeam => true;
        public string TeamName => "Test Team";
        public string TeamBadge => "TST";
        public bool HasCap => true;
        public long CapRoom => 12_000_000;
        public bool HasBoard => true;
        public int BoardConfidence => 74;
    }

    private static ShellViewModel MakeShell() =>
        new(new NavigationService(), new StubSession(), new SampleNotificationSource());

    [AvaloniaFact]
    public void Shell_renders_the_four_regions_headless()
    {
        var view = new ShellView { DataContext = MakeShell() };
        var window = new Window { Content = view };

        window.Show();
        Dispatcher.UIThread.RunJobs();

        Assert.Single(view.GetVisualDescendants().OfType<TopBar>());
        Assert.Single(view.GetVisualDescendants().OfType<Sidebar>());
        Assert.Single(view.GetVisualDescendants().OfType<StatusBar>());
        Assert.Single(view.GetVisualDescendants().OfType<PlaceholderScreenView>());
    }

    [AvaloniaFact]
    public void Main_window_starts_on_the_menu_then_hosts_the_shell_on_entering_a_career()
    {
        var root = new RootViewModel(CarsetCatalog.Discover(), new SampleNotificationSource());
        var window = new global::LTF.App.MainWindow { DataContext = root };

        window.Show();
        Dispatcher.UIThread.RunJobs();

        // The app lands on the main menu.
        Assert.True(window.IsVisible);
        Assert.Single(window.GetVisualDescendants().OfType<MainMenuView>());

        // Entering a career swaps the window content to the in-game shell.
        root.EnterCareer(SessionLoader.LoadFlagship());
        Dispatcher.UIThread.RunJobs();

        Assert.Single(window.GetVisualDescendants().OfType<ShellView>());
        Assert.Single(window.GetVisualDescendants().OfType<TopBar>());
    }

    [Fact]
    public void Content_region_starts_on_the_paddock_hub()
    {
        var shell = MakeShell();

        Assert.Equal(NavKey.PaddockHub, shell.Navigation.CurrentKey);
        Assert.IsType<PlaceholderScreenViewModel>(shell.Navigation.CurrentScreen);
    }

    [Fact]
    public void Every_nav_entry_navigates_the_content_region()
    {
        var shell = MakeShell();
        var items = shell.Sidebar.MainItems
            .Concat(shell.Sidebar.RaceDayItems)
            .Concat(shell.Sidebar.SystemItems)
            .ToList();

        Assert.Equal(13, items.Count);
        foreach (var item in items)
        {
            item.SelectCommand.Execute(null);
            Assert.Equal(item.Key, shell.Navigation.CurrentKey);
        }
    }

    [Fact]
    public void Inbox_toggles_open_and_closed_from_the_top_bar()
    {
        var shell = MakeShell();
        Assert.False(shell.IsInboxOpen);

        shell.TopBar.ToggleInboxCommand.Execute(null);
        Assert.True(shell.IsInboxOpen);

        shell.TopBar.ToggleInboxCommand.Execute(null);
        Assert.False(shell.IsInboxOpen);
    }

    [Fact]
    public void Opening_a_notification_closes_the_inbox_and_navigates()
    {
        var shell = MakeShell();
        shell.TopBar.ToggleInboxCommand.Execute(null);
        Assert.True(shell.IsInboxOpen);

        var item = shell.Inbox.Items.First(i => i.DeepLink == NavKey.Finance);
        item.OpenCommand.Execute(null);

        Assert.False(shell.IsInboxOpen);
        Assert.Equal(NavKey.Finance, shell.Navigation.CurrentKey);
    }
}
