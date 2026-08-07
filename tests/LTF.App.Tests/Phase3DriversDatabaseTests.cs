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
/// The Drivers and Database in-shell screens (M21b): the Drivers screen lists the player squad and shows
/// the selected driver's profile; the Database browser tabs across drivers/juniors/teams and drives a
/// shared detail panel. Both resolve to their views headless and register through the shell.
/// </summary>
public class Phase3DriversDatabaseTests
{
    private static Carset Flagship() => SessionLoader.LoadFlagship().Carset;

    // --- Drivers ---

    [Fact]
    public void Drivers_squad_lists_the_player_race_drivers_and_selects_the_first()
    {
        var carset = Flagship();
        var team = carset.PlayerTeam();
        Assert.NotNull(team); // the flagship designates a player team

        var vm = new DriversViewModel(carset);

        Assert.True(vm.HasSquad);
        foreach (var id in team!.DriverIds)
        {
            var name = carset.Drivers.First(d => d.Id == id).FullName;
            Assert.Contains(vm.Squad, r => r.Name == name);
        }

        Assert.Same(vm.Squad[0], vm.SelectedDriver);
        Assert.NotNull(vm.Profile);
        Assert.True(vm.Profile!.HasAttributes);
        Assert.Equal(6, vm.Profile.Attributes.Count);
    }

    [Fact]
    public void Drivers_selecting_a_driver_swaps_the_profile()
    {
        var vm = new DriversViewModel(Flagship());
        Assert.True(vm.Squad.Count >= 2);

        var firstName = vm.Profile!.Name;
        vm.SelectedDriver = vm.Squad[1];

        Assert.NotEqual(firstName, vm.Profile!.Name);
        Assert.Equal(vm.Squad[1].Name.ToUpperInvariant(), vm.Profile.Name);
    }

    [AvaloniaFact]
    public void Drivers_view_resolves_and_shows_the_detail_panel()
    {
        var host = new ContentControl { Content = new DriversViewModel(Flagship()) };
        var window = new Window { Content = host };

        window.Show();
        Dispatcher.UIThread.RunJobs();

        Assert.Single(host.GetVisualDescendants().OfType<DriversView>());
        Assert.Single(host.GetVisualDescendants().OfType<EntityDetailView>());
    }

    // --- Database ---

    [Fact]
    public void Database_default_tab_lists_every_driver_with_a_driver_detail()
    {
        var carset = Flagship();
        var vm = new DatabaseViewModel(carset);

        Assert.Equal(DatabaseTab.Drivers, vm.SelectedTab);
        Assert.Equal(carset.Drivers.Count, vm.Rows.Count);
        Assert.NotNull(vm.SelectedRow);
        Assert.NotNull(vm.Detail);
        Assert.True(vm.Detail!.HasAttributes);
    }

    [Fact]
    public void Database_teams_tab_rebuilds_rows_and_a_team_detail_without_attributes()
    {
        var carset = Flagship();
        var vm = new DatabaseViewModel(carset);

        vm.SelectTabCommand.Execute(DatabaseTab.Teams);

        Assert.True(vm.IsTeamsTab);
        Assert.Equal(carset.Teams.Count, vm.Rows.Count);
        Assert.NotNull(vm.Detail);
        Assert.False(vm.Detail!.HasAttributes);
    }

    [Fact]
    public void Database_juniors_tab_lists_the_reserve_pool()
    {
        var carset = Flagship();
        var vm = new DatabaseViewModel(carset);

        vm.SelectTabCommand.Execute(DatabaseTab.Juniors);

        Assert.True(vm.IsJuniorsTab);
        Assert.Equal(carset.Reserves.Count, vm.Rows.Count);
    }

    [AvaloniaFact]
    public void Database_view_resolves_from_its_view_model()
    {
        var host = new ContentControl { Content = new DatabaseViewModel(Flagship()) };
        var window = new Window { Content = host };

        window.Show();
        Dispatcher.UIThread.RunJobs();

        Assert.Single(host.GetVisualDescendants().OfType<DatabaseView>());
    }

    // --- Registration through the shell ---

    [Fact]
    public void Entering_a_career_registers_the_drivers_and_database_screens()
    {
        var catalog = CarsetCatalog.Discover();
        var dir = Directory.CreateTempSubdirectory().FullName;
        var saves = new SaveStore(catalog, dir);
        var settings = new SettingsStore(Path.Combine(dir, "settings.json"));
        var root = new RootViewModel(new AppServices(catalog, saves, settings, new SampleNotificationSource()));

        root.EnterCareer(SessionLoader.LoadFlagship());
        var shell = (ShellViewModel)root.Content!;

        shell.Navigation.Navigate(NavKey.Drivers);
        Assert.IsType<DriversViewModel>(shell.Navigation.CurrentScreen);

        shell.Navigation.Navigate(NavKey.Database);
        Assert.IsType<DatabaseViewModel>(shell.Navigation.CurrentScreen);
    }
}
