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
/// The R&amp;D and facilities screen (M22d): projects the ten facilities and their levels plus the player
/// team's research state (regulation readiness, concept lean, active projects, nodes unlocked), resolves to
/// its view headless, and registers through the shell.
/// </summary>
public class Phase4RndFacilitiesTests
{
    private static Carset Flagship() => SessionLoader.LoadFlagship().Carset;

    [Fact]
    public void Rnd_projects_the_ten_facilities_and_research_state()
    {
        var carset = Flagship();
        var vm = new RndFacilitiesViewModel(carset);

        Assert.True(vm.HasTeam);
        Assert.Equal(10, vm.Facilities.Count);
        Assert.All(vm.Facilities, f => Assert.InRange(f.Level, 1, 5));
        Assert.All(vm.Facilities, f => Assert.False(string.IsNullOrWhiteSpace(f.Name)));
        Assert.InRange(vm.RegulationReadiness, 0, 100);
        Assert.EndsWith("%", vm.ReadinessText);
        Assert.Contains("/", vm.NodesText); // "unlocked / total"
        Assert.False(string.IsNullOrWhiteSpace(vm.AeroLeanText));
    }

    [AvaloniaFact]
    public void Rnd_view_resolves_from_its_view_model()
    {
        var host = new ContentControl { Content = new RndFacilitiesViewModel(Flagship()) };
        var window = new Window { Content = host };

        window.Show();
        Dispatcher.UIThread.RunJobs();

        Assert.Single(host.GetVisualDescendants().OfType<RndFacilitiesView>());
    }

    [Fact]
    public void Entering_a_career_registers_the_rnd_screen()
    {
        var catalog = CarsetCatalog.Discover();
        var dir = Directory.CreateTempSubdirectory().FullName;
        var saves = new SaveStore(catalog, dir);
        var settings = new SettingsStore(Path.Combine(dir, "settings.json"));
        var root = new RootViewModel(new AppServices(catalog, saves, settings, new CareerNotificationSource()));

        root.EnterCareer(SessionLoader.LoadFlagship());
        var shell = (ShellViewModel)root.Content!;

        shell.Navigation.Navigate(NavKey.RndFacilities);
        Assert.IsType<RndFacilitiesViewModel>(shell.Navigation.CurrentScreen);
    }
}
