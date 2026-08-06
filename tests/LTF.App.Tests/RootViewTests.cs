using System.IO;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using LTF.App.Services;
using LTF.App.Session;
using LTF.App.Settings;
using LTF.App.Shell;
using LTF.App.ViewModels;
using LTF.App.ViewModels.Menu;
using LTF.App.ViewModels.Settings;
using LTF.App.Views;
using LTF.App.Views.Menu;
using Xunit;

namespace LTF.App.Tests;

/// <summary>
/// The application root view-switch (M20a): the root lands on the main menu, entering a career swaps the
/// content to the in-game shell, and Exit to Menu swaps back — each resolved to its view via the App
/// DataTemplates, headless on all three CI platforms.
/// </summary>
public class RootViewTests
{
    private static RootViewModel MakeRoot()
    {
        var catalog = CarsetCatalog.Discover();
        var dir = Directory.CreateTempSubdirectory().FullName;
        var saves = new SaveStore(catalog, dir);
        var settings = new SettingsStore(Path.Combine(dir, "settings.json"));
        return new RootViewModel(new AppServices(catalog, saves, settings, new SampleNotificationSource()));
    }

    [Fact]
    public void Continue_becomes_available_after_a_career_is_entered()
    {
        var root = MakeRoot();
        Assert.False(((MainMenuViewModel)root.Content!).CanContinue);

        root.EnterCareer(SessionLoader.LoadFlagship());
        root.ExitToMenu();

        Assert.True(((MainMenuViewModel)root.Content!).CanContinue);
    }

    [Fact]
    public void Settings_opens_and_returns_to_the_menu()
    {
        var root = MakeRoot();

        root.ShowSettings();
        Assert.IsType<SettingsViewModel>(root.Content);

        ((SettingsViewModel)root.Content!).BackCommand.Execute(null);
        Assert.IsType<MainMenuViewModel>(root.Content);
    }

    [Fact]
    public void Root_starts_on_the_main_menu() =>
        Assert.IsType<MainMenuViewModel>(MakeRoot().Content);

    [Fact]
    public void Enter_career_then_exit_swaps_content_between_shell_and_menu()
    {
        var root = MakeRoot();

        root.EnterCareer(SessionLoader.LoadFlagship());
        Assert.IsType<ShellViewModel>(root.Content);

        root.ExitToMenu();
        Assert.IsType<MainMenuViewModel>(root.Content);
    }

    [AvaloniaFact]
    public void Root_view_resolves_the_menu_then_the_shell()
    {
        var root = MakeRoot();
        var view = new RootView { DataContext = root };
        var window = new Window { Content = view };

        window.Show();
        Dispatcher.UIThread.RunJobs();

        Assert.Single(view.GetVisualDescendants().OfType<MainMenuView>());

        root.EnterCareer(SessionLoader.LoadFlagship());
        Dispatcher.UIThread.RunJobs();

        Assert.Single(view.GetVisualDescendants().OfType<ShellView>());
        Assert.Empty(view.GetVisualDescendants().OfType<MainMenuView>());
    }
}
