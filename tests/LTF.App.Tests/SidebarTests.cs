using System.Linq;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using LTF.App.Navigation;
using LTF.App.Services;
using LTF.App.Shell;
using LTF.App.ViewModels;
using Xunit;

namespace LTF.App.Tests;

/// <summary>
/// The sidebar: entries are grouped by section in order, selecting a row navigates and marks it active,
/// and the view renders all 13 nav buttons headless.
/// </summary>
public class SidebarTests
{
    [Fact]
    public void Groups_items_by_section_in_order()
    {
        var vm = new SidebarViewModel(new NavigationService());

        Assert.Equal(10, vm.MainItems.Count);
        Assert.Equal(NavKey.PaddockHub, vm.MainItems[0].Key);
        Assert.Equal(NavKey.Database, vm.MainItems[^1].Key);
        Assert.Equal(new[] { NavKey.RaceWeekend }, vm.RaceDayItems.Select(i => i.Key));
        Assert.Equal(new[] { NavKey.Settings, NavKey.ExitToMenu }, vm.SystemItems.Select(i => i.Key));
        Assert.Equal("PADDOCK HUB", vm.MainItems[0].Label);
    }

    [Fact]
    public void Selecting_a_row_navigates_and_marks_it_active()
    {
        var nav = new NavigationService();
        var vm = new SidebarViewModel(nav);
        var finance = vm.MainItems.First(i => i.Key == NavKey.Finance);
        var drivers = vm.MainItems.First(i => i.Key == NavKey.Drivers);

        finance.SelectCommand.Execute(null);

        Assert.Equal(NavKey.Finance, nav.CurrentKey);
        Assert.True(finance.IsActive);
        Assert.False(drivers.IsActive);
    }

    [AvaloniaFact]
    public void View_renders_all_nav_buttons()
    {
        var vm = new SidebarViewModel(new NavigationService());
        var sidebar = new Sidebar { DataContext = vm };
        var window = new Window { Content = sidebar };

        window.Show();
        Dispatcher.UIThread.RunJobs();

        var navButtons = sidebar.GetVisualDescendants()
            .OfType<Button>()
            .Where(b => b.Classes.Contains("navitem"))
            .ToList();

        Assert.Equal(13, navButtons.Count);
    }
}
