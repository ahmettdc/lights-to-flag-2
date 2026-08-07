using System.Collections.Generic;
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
using LTF.Domain.Common;
using LTF.Domain.Management;
using Xunit;

namespace LTF.App.Tests;

/// <summary>
/// The staff screen (M22f): projects the player team's technical staff and the free-agent pool, lets the
/// player hire and release (StaffLedger, landed on the season-start carset and persisted via the new
/// CareerState staff capture), resolves to its view headless, and registers through the shell.
/// </summary>
public class Phase4StaffTests
{
    private static Staff Member(string id, StaffRole role, int skill, long salary) => new()
    {
        Id = id, FirstName = id, LastName = "Eng", Role = role, Skill = new Rating(skill), Salary = salary,
    };

    private static Carset WithStaff(Carset carset, IReadOnlyList<Staff> squad, IReadOnlyList<Staff> pool)
    {
        var teams = carset.Teams
            .Select(t => string.CompareOrdinal(t.Id, carset.PlayerTeamId) == 0 ? t with { Staff = squad } : t)
            .ToList();
        return carset with { Teams = teams, StaffPool = pool };
    }

    private static Carset StaffedCarset() => WithStaff(
        SessionLoader.LoadFlagship().Carset,
        [Member("s1", StaffRole.TechnicalDirector, 85, 4_000_000)],
        [Member("s2", StaffRole.ChiefStrategist, 70, 1_500_000)]);

    private static ShellSession StaffedSession()
    {
        var s = SessionLoader.LoadFlagship();
        return new ShellSession(WithStaff(
            s.Carset,
            [Member("s1", StaffRole.TechnicalDirector, 85, 4_000_000)],
            [Member("s2", StaffRole.ChiefStrategist, 70, 1_500_000)]), s.Clock, s.Seed);
    }

    [Fact]
    public void Staff_projects_the_squad_and_the_pool()
    {
        var vm = new StaffViewModel(StaffedCarset());

        Assert.True(vm.HasTeam);
        var squad = Assert.Single(vm.Squad);
        Assert.Equal("s1 Eng", squad.Name);
        Assert.Equal(85, squad.Skill);
        var free = Assert.Single(vm.Pool);
        Assert.Equal("s2 Eng", free.Name);
    }

    [Fact]
    public void The_row_actions_invoke_the_hire_and_release_callbacks()
    {
        string? released = null;
        string? hired = null;
        var vm = new StaffViewModel(StaffedCarset(), hire: id => hired = id, release: id => released = id);

        vm.Squad.Single().Action!.Execute(null);
        Assert.Equal("s1", released);
        vm.Pool.Single().Action!.Execute(null);
        Assert.Equal("s2", hired);
    }

    [Fact]
    public void A_read_only_staff_screen_has_no_actions()
    {
        var vm = new StaffViewModel(StaffedCarset()); // no callbacks

        Assert.All(vm.Squad, r => Assert.False(r.CanAct));
        Assert.All(vm.Pool, r => Assert.Null(r.Action));
    }

    [Fact]
    public void Hiring_through_the_shell_moves_the_agent_and_persists()
    {
        var catalog = CarsetCatalog.Discover();
        var dir = Directory.CreateTempSubdirectory().FullName;
        var saves = new SaveStore(catalog, dir);
        var settings = new SettingsStore(Path.Combine(dir, "settings.json"));
        var root = new RootViewModel(new AppServices(catalog, saves, settings, new CareerNotificationSource()));

        root.EnterCareer(StaffedSession());
        var shell = (ShellViewModel)root.Content!;
        shell.Navigation.Navigate(NavKey.Staff);
        var vm = (StaffViewModel)shell.Navigation.CurrentScreen!;

        vm.Pool.Single().Action!.Execute(null); // hire s2

        var after = (StaffViewModel)shell.Navigation.CurrentScreen!;
        Assert.Equal(2, after.Squad.Count); // s1 + the hired s2
        Assert.Empty(after.Pool);

        var reloaded = saves.Load(saves.MostRecent()!);
        Assert.Contains(reloaded.Carset.PlayerTeam()!.Staff, s => s.Id == "s2");
    }

    [AvaloniaFact]
    public void Staff_view_resolves_from_its_view_model()
    {
        var host = new ContentControl { Content = new StaffViewModel(StaffedCarset()) };
        var window = new Window { Content = host };

        window.Show();
        Dispatcher.UIThread.RunJobs();

        Assert.Single(host.GetVisualDescendants().OfType<StaffView>());
    }

    [Fact]
    public void Entering_a_career_registers_the_staff_screen()
    {
        var catalog = CarsetCatalog.Discover();
        var dir = Directory.CreateTempSubdirectory().FullName;
        var saves = new SaveStore(catalog, dir);
        var settings = new SettingsStore(Path.Combine(dir, "settings.json"));
        var root = new RootViewModel(new AppServices(catalog, saves, settings, new CareerNotificationSource()));

        root.EnterCareer(SessionLoader.LoadFlagship());
        var shell = (ShellViewModel)root.Content!;

        shell.Navigation.Navigate(NavKey.Staff);
        Assert.IsType<StaffViewModel>(shell.Navigation.CurrentScreen);
    }
}
