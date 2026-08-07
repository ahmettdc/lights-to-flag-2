using System.IO;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using LTF.App.Navigation;
using LTF.App.Services;
using LTF.App.Session;
using LTF.App.Settings;
using LTF.App.ViewModels;
using LTF.App.ViewModels.Screens;
using LTF.App.Views.Screens;
using LTF.Domain;
using Xunit;

namespace LTF.App.Tests;

/// <summary>
/// The board &amp; sponsors screen (M22g): projects the player board's six pressure metrics, firing risk,
/// members and objectives plus the sponsor book, resolves to its view headless, and registers through the
/// shell. The flagship names a player team with a board, so the board panel is populated.
/// </summary>
public class Phase4BoardSponsorsTests
{
    private static Carset Flagship() => SessionLoader.LoadFlagship().Carset;

    [Fact]
    public void Board_projects_the_metrics_members_and_objectives()
    {
        var carset = Flagship();
        var vm = new BoardSponsorsViewModel(carset);

        Assert.True(vm.HasTeam);
        Assert.True(vm.HasBoard);                          // the flagship ships a player board
        Assert.Equal(6, vm.Metrics.Count);
        Assert.All(vm.Metrics, m => Assert.InRange(m.Value, 0, 100));
        Assert.InRange(vm.FiringRisk, 0, 100);
        Assert.False(string.IsNullOrWhiteSpace(vm.OwnershipText));
        Assert.True(vm.HasObjectives);
    }

    [AvaloniaFact]
    public void Board_view_resolves_from_its_view_model()
    {
        var host = new ContentControl { Content = new BoardSponsorsViewModel(Flagship()) };
        var window = new Window { Content = host };

        window.Show();
        Dispatcher.UIThread.RunJobs();

        Assert.Single(host.GetVisualDescendants().OfType<BoardSponsorsView>());
    }

    [Fact]
    public void Entering_a_career_registers_the_board_screen()
    {
        var catalog = CarsetCatalog.Discover();
        var dir = Directory.CreateTempSubdirectory().FullName;
        var saves = new SaveStore(catalog, dir);
        var settings = new SettingsStore(Path.Combine(dir, "settings.json"));
        var root = new RootViewModel(new AppServices(catalog, saves, settings, new CareerNotificationSource()));

        root.EnterCareer(SessionLoader.LoadFlagship());
        var shell = (ShellViewModel)root.Content!;

        shell.Navigation.Navigate(NavKey.BoardSponsors);
        Assert.IsType<BoardSponsorsViewModel>(shell.Navigation.CurrentScreen);
    }
}
