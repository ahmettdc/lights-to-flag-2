using System.Linq;
using LTF.App.Services;
using LTF.App.Session;
using LTF.App.ViewModels.Menu;
using Xunit;

namespace LTF.App.Tests;

/// <summary>
/// The new-career flow (M20b): step gating, the carset→team cascade, and producing a valid session that
/// designates the chosen team and seeds board objectives. Uses the real bundled carsets via the catalog.
/// </summary>
public class NewCareerFlowTests
{
    /// <summary>Captures the session the wizard hands to the shell.</summary>
    private sealed class CapturingShell : IAppShellController
    {
        public ShellSession? Entered;
        public int MainMenu;

        public void ShowMainMenu() => MainMenu++;
        public void ShowNewCareer() { }
        public void ContinueCareer() { }
        public void ShowLoadGame() { }
        public void ShowQuickRace() { }
        public void ShowSettings() { }
        public void EnterCareer(ShellSession session) => Entered = session;
        public void ExitToMenu() { }
        public void Quit() { }
    }

    private static CarsetCatalog Catalog() => CarsetCatalog.Discover();

    [Fact]
    public void Wizard_starts_on_the_carset_step_with_a_default_selection()
    {
        var wizard = new NewCareerViewModel(new CapturingShell(), Catalog());

        Assert.Equal(NewCareerStep.Carset, wizard.Step);
        Assert.NotNull(wizard.SelectedCarset);
        Assert.True(wizard.CanGoNext);
    }

    [Fact]
    public void Choosing_a_carset_populates_teams_and_preselects_the_default()
    {
        var wizard = new NewCareerViewModel(new CapturingShell(), Catalog());

        wizard.SelectedCarset = wizard.Carsets.First(c => c.Id == "global-prix");

        Assert.NotEmpty(wizard.Teams);
        Assert.Equal("talon", wizard.SelectedTeam!.Id);
    }

    [Fact]
    public void Next_advances_carset_team_board_confirm()
    {
        var wizard = new NewCareerViewModel(new CapturingShell(), Catalog());
        wizard.SelectedCarset = wizard.Carsets.First(c => c.Id == "global-prix");

        wizard.NextCommand.Execute(null);
        Assert.Equal(NewCareerStep.Team, wizard.Step);

        wizard.NextCommand.Execute(null);
        Assert.Equal(NewCareerStep.Board, wizard.Step);
        Assert.NotNull(wizard.Board);
        Assert.NotEmpty(wizard.Board!.Objectives);

        wizard.NextCommand.Execute(null);
        Assert.Equal(NewCareerStep.Confirm, wizard.Step);
    }

    [Fact]
    public void Back_from_the_first_step_returns_to_the_main_menu()
    {
        var host = new CapturingShell();
        var wizard = new NewCareerViewModel(host, Catalog());

        wizard.BackCommand.Execute(null);

        Assert.Equal(1, host.MainMenu);
    }

    [Fact]
    public void Cannot_advance_without_a_carset_selected()
    {
        var wizard = new NewCareerViewModel(new CapturingShell(), Catalog());

        wizard.SelectedCarset = null;

        Assert.False(wizard.CanGoNext);
        Assert.False(wizard.NextCommand.CanExecute(null));
    }

    [Fact]
    public void Start_enters_a_career_running_the_chosen_team()
    {
        var host = new CapturingShell();
        var wizard = new NewCareerViewModel(host, Catalog());
        wizard.SelectedCarset = wizard.Carsets.First(c => c.Id == "global-prix");
        wizard.SelectedTeam = wizard.Teams.First(t => t.Id == "talon");

        wizard.StartCommand.Execute(null);

        Assert.NotNull(host.Entered);
        Assert.Equal("talon", host.Entered!.Carset.PlayerTeam()!.Id);
    }

    [Fact]
    public void New_career_service_designates_the_team_and_seeds_board_objectives()
    {
        var flagship = Catalog().Find("global-prix")!.Carset;

        var session = NewCareerService.Create(flagship, "talon", []);

        Assert.Equal("talon", session.Carset.PlayerTeamId);
        Assert.NotNull(session.Carset.PlayerTeam());
        var board = session.Carset.Boards.FirstOrDefault(b => b.TeamId == "talon");
        Assert.NotNull(board);
        Assert.NotEmpty(board!.Objectives);
    }
}
