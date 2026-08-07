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
/// The cars &amp; power-unit screen (M22e): projects the five car rating axes with the pace-weighted overall
/// and the component pool, resolves to its view headless, and registers through the shell.
/// </summary>
public class Phase4CarsPowerUnitTests
{
    private static Carset Flagship() => SessionLoader.LoadFlagship().Carset;

    [Fact]
    public void Cars_projects_the_five_axes_and_the_overall()
    {
        var carset = Flagship();
        var vm = new CarsPowerUnitViewModel(carset);

        Assert.True(vm.HasTeam);
        Assert.Equal(5, vm.Ratings.Count);
        Assert.All(vm.Ratings, r => Assert.InRange(r.Value, 1, 100));
        Assert.All(vm.Ratings, r => Assert.False(string.IsNullOrWhiteSpace(r.Name)));
        Assert.InRange(vm.Overall, 1, 100);
    }

    [AvaloniaFact]
    public void Cars_view_resolves_from_its_view_model()
    {
        var host = new ContentControl { Content = new CarsPowerUnitViewModel(Flagship()) };
        var window = new Window { Content = host };

        window.Show();
        Dispatcher.UIThread.RunJobs();

        Assert.Single(host.GetVisualDescendants().OfType<CarsPowerUnitView>());
    }

    [Fact]
    public void Entering_a_career_registers_the_cars_screen()
    {
        var catalog = CarsetCatalog.Discover();
        var dir = Directory.CreateTempSubdirectory().FullName;
        var saves = new SaveStore(catalog, dir);
        var settings = new SettingsStore(Path.Combine(dir, "settings.json"));
        var root = new RootViewModel(new AppServices(catalog, saves, settings, new CareerNotificationSource()));

        root.EnterCareer(SessionLoader.LoadFlagship());
        var shell = (ShellViewModel)root.Content!;

        shell.Navigation.Navigate(NavKey.CarsPowerUnit);
        Assert.IsType<CarsPowerUnitViewModel>(shell.Navigation.CurrentScreen);
    }
}
