using System.Globalization;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using LTF.App.Services;
using LTF.App.Session;
using LTF.App.Shell;
using LTF.App.ViewModels;
using Xunit;

namespace LTF.App.Tests;

/// <summary>
/// The top bar formats the live session values (date, cap room, board confidence, team) and renders the
/// brand + team, headless. Continue is present but inert in M19.
/// </summary>
public class TopBarTests
{
    private static SessionSnapshot Flagship() => new(SessionLoader.LoadFlagship());

    [Fact]
    public void Formats_live_top_bar_values()
    {
        var snapshot = Flagship();
        var vm = new TopBarViewModel(snapshot, inboxCount: 5);

        Assert.Contains(snapshot.Date.Year.ToString(CultureInfo.InvariantCulture), vm.DateText);
        Assert.True(vm.HasBoard);
        Assert.Equal($"{snapshot.BoardConfidence}%", vm.BoardConfidenceText);
        Assert.Equal(snapshot.TeamName, vm.TeamName);
        Assert.Equal(snapshot.TeamBadge, vm.TeamBadge);
        Assert.True(vm.HasInbox);
        Assert.Equal(5, vm.InboxCount);
    }

    [Fact]
    public void Continue_command_is_present_but_inert()
    {
        var vm = new TopBarViewModel(Flagship());

        Assert.NotNull(vm.ContinueCommand);
        vm.ContinueCommand.Execute(null); // no-op, must not throw
        Assert.False(vm.HasInbox);
    }

    [AvaloniaFact]
    public void View_renders_brand_and_team()
    {
        var snapshot = Flagship();
        var topBar = new TopBar { DataContext = new TopBarViewModel(snapshot, 5) };
        var window = new Window { Content = topBar };

        window.Show();
        Dispatcher.UIThread.RunJobs();

        var texts = topBar.GetVisualDescendants().OfType<TextBlock>().Select(t => t.Text).ToList();
        Assert.Contains("LIGHTS TO FLAG", texts);
        Assert.Contains(snapshot.TeamName, texts);
    }
}
