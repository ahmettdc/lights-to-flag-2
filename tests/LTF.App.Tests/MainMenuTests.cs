using LTF.App.Services;
using LTF.App.Session;
using LTF.App.ViewModels.Menu;
using Xunit;

namespace LTF.App.Tests;

/// <summary>
/// The main menu (M20a): every command delegates to the <see cref="IAppShellController"/>, and Continue is
/// gated on whether a career exists to resume. Pure view-model logic — no headless app needed.
/// </summary>
public class MainMenuTests
{
    /// <summary>Records which controller method each command invoked.</summary>
    private sealed class StubShell : IAppShellController
    {
        public int NewCareer;
        public int Continue;
        public int LoadGame;
        public int QuickRace;
        public int Settings;
        public int MainMenu;
        public int Enter;
        public int Exit;
        public int Quits;

        public void ShowMainMenu() => MainMenu++;
        public void ShowNewCareer() => NewCareer++;
        public void ContinueCareer() => Continue++;
        public void ShowLoadGame() => LoadGame++;
        public void ShowQuickRace() => QuickRace++;
        public void ShowSettings() => Settings++;
        public void EnterCareer(ShellSession session) => Enter++;
        public void ExitToMenu() => Exit++;
        public void Quit() => Quits++;
    }

    [Fact]
    public void Each_command_delegates_to_its_host_method()
    {
        var host = new StubShell();
        var menu = new MainMenuViewModel(host, canContinue: true);

        menu.NewCareerCommand.Execute(null);
        menu.ContinueCommand.Execute(null);
        menu.LoadGameCommand.Execute(null);
        menu.QuickRaceCommand.Execute(null);
        menu.SettingsCommand.Execute(null);
        menu.QuitCommand.Execute(null);

        Assert.Equal(1, host.NewCareer);
        Assert.Equal(1, host.Continue);
        Assert.Equal(1, host.LoadGame);
        Assert.Equal(1, host.QuickRace);
        Assert.Equal(1, host.Settings);
        Assert.Equal(1, host.Quits);
    }

    [Fact]
    public void Continue_is_disabled_without_a_save() =>
        // CanExecute false greys out the button in the UI (Execute itself is unguarded by design).
        Assert.False(new MainMenuViewModel(new StubShell(), canContinue: false).ContinueCommand.CanExecute(null));

    [Fact]
    public void Continue_is_enabled_with_a_save() =>
        Assert.True(new MainMenuViewModel(new StubShell(), canContinue: true).ContinueCommand.CanExecute(null));
}
