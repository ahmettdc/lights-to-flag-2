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
using LTF.Domain.Racing;
using LTF.Domain.Rnd;
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

    // --- Concept steer (Ri2) ---

    [Fact]
    public void The_concept_sliders_project_the_saved_lean()
    {
        var carset = Flagship();
        var vm = new RndFacilitiesViewModel(carset, setConcept: (_, _) => { });

        var concept = carset.PlayerTeam()!.Research.Concept;
        Assert.Equal(concept.AeroLean, vm.AeroLean);
        Assert.Equal(concept.PowertrainLean, vm.PowertrainLean);
    }

    [Fact]
    public void Apply_concept_invokes_the_callback_with_the_slider_values()
    {
        (int Aero, int Powertrain)? applied = null;
        var vm = new RndFacilitiesViewModel(Flagship(), setConcept: (a, p) => applied = (a, p));

        Assert.True(vm.CanSteerConcept);
        vm.AeroLeanValue = 55;
        vm.PowertrainLeanValue = -30;
        vm.ApplyConceptCommand.Execute(null);

        Assert.Equal((55, -30), applied);
    }

    [Fact]
    public void A_read_only_rnd_screen_offers_no_concept_steer()
    {
        var vm = new RndFacilitiesViewModel(Flagship()); // no callback

        Assert.False(vm.CanSteerConcept);
        Assert.False(vm.ApplyConceptCommand.CanExecute(null));
    }

    [Fact]
    public void Steering_the_concept_through_the_shell_persists_it()
    {
        var catalog = CarsetCatalog.Discover();
        var dir = Directory.CreateTempSubdirectory().FullName;
        var saves = new SaveStore(catalog, dir);
        var settings = new SettingsStore(Path.Combine(dir, "settings.json"));
        var root = new RootViewModel(new AppServices(catalog, saves, settings, new CareerNotificationSource()));

        root.EnterCareer(SessionLoader.LoadFlagship());
        var shell = (ShellViewModel)root.Content!;
        shell.Navigation.Navigate(NavKey.RndFacilities);
        var vm = (RndFacilitiesViewModel)shell.Navigation.CurrentScreen!;

        Assert.True(vm.CanSteerConcept);
        vm.AeroLeanValue = 60;
        vm.PowertrainLeanValue = -40;
        vm.ApplyConceptCommand.Execute(null);

        var reloaded = saves.Load(saves.MostRecent()!);
        var concept = reloaded.Carset.PlayerTeam()!.Research.Concept;
        Assert.Equal(60, concept.AeroLean);
        Assert.Equal(-40, concept.PowertrainLean);
    }

    [Fact]
    public void Opposite_concepts_develop_the_car_differently_over_a_season()
    {
        var session = SessionLoader.LoadFlagship();
        var aeroCar = DevelopUnderConcept(session, aeroLean: 100, powertrainLean: -100);
        var powerCar = DevelopUnderConcept(session, aeroLean: -100, powertrainLean: 100);

        Assert.NotEqual(aeroCar, powerCar); // the concept lean steers which nodes the season develops
    }

    // --- FIA development-freeze badges (Ri5) ---

    [Fact]
    public void The_screen_projects_active_development_freezes()
    {
        var carset = Flagship();
        var frozen = carset with
        {
            Regulations = carset.Regulations with
            {
                DevelopmentFreezes =
                [
                    new AxisFreeze { Axis = CarAxis.PowerUnit, Mode = DevelopmentFreezeMode.Full },
                    new AxisFreeze { Axis = CarAxis.MechanicalGrip, Mode = DevelopmentFreezeMode.InSeasonOnly },
                ],
            },
        };

        var vm = new RndFacilitiesViewModel(frozen);

        Assert.True(vm.HasFreezes);
        Assert.Equal(2, vm.Freezes.Count);
        Assert.Contains(vm.Freezes, f => f.Mode.Contains("full", System.StringComparison.Ordinal));
        Assert.Contains(vm.Freezes, f => f.Mode.Contains("in-season", System.StringComparison.Ordinal));
    }

    [Fact]
    public void A_freeze_free_screen_shows_no_freezes()
    {
        Assert.False(new RndFacilitiesViewModel(Flagship()).HasFreezes);
    }

    private static Car DevelopUnderConcept(ShellSession session, int aeroLean, int powertrainLean)
    {
        var carset = session.Carset;
        var team = carset.PlayerTeam()!;
        var research = team.Research with
        {
            Concept = new ConceptDirection { AeroLean = aeroLean, PowertrainLean = powertrainLean },
        };
        var teams = carset.Teams
            .Select(t => string.CompareOrdinal(t.Id, team.Id) == 0 ? team with { Research = research } : t)
            .ToList();
        var live = new LiveCareer(new ShellSession(carset with { Teams = teams }, session.Clock, session.Seed));

        var guard = 0;
        while (!live.SeasonComplete && guard++ < 1000)
        {
            if (live.PendingAction)
            {
                live.Acknowledge();
            }
            else
            {
                live.Continue();
            }
        }

        return live.Current.PlayerTeam()!.Car;
    }
}
