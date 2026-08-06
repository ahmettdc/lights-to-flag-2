using LTF.App.Session;

namespace LTF.App.Services;

/// <summary>
/// The seam menu- and shell-level view-models call to drive the top-level view switch, so no view-model
/// ever touches a <c>Window</c>. Implemented by <see cref="LTF.App.ViewModels.RootViewModel"/>. Menu
/// screens request a sibling screen (<see cref="ShowNewCareer"/> / <see cref="ShowLoadGame"/> / …) or hand
/// back a built career (<see cref="EnterCareer"/>); the in-game shell requests <see cref="ExitToMenu"/>.
/// <see cref="Quit"/> ends the application. M20a wires the main menu + the menu↔shell switch; later M20
/// phases fill in the remaining screens.
/// </summary>
public interface IAppShellController
{
    /// <summary>Show the main menu (the app's landing screen, and the target of Exit to Menu).</summary>
    void ShowMainMenu();

    /// <summary>Begin a new career (the carset/team/board wizard lands in M20b).</summary>
    void ShowNewCareer();

    /// <summary>Resume the most recent save (the real save store lands in M20d).</summary>
    void ContinueCareer();

    /// <summary>Show the load-game slot list (lands in M20d).</summary>
    void ShowLoadGame();

    /// <summary>Show the Quick Race setup (lands in M20g).</summary>
    void ShowQuickRace();

    /// <summary>Show the settings screen (lands in M20e).</summary>
    void ShowSettings();

    /// <summary>Swap the window content to the in-game shell over the given session.</summary>
    void EnterCareer(ShellSession session);

    /// <summary>Drop the in-game shell and return to the main menu.</summary>
    void ExitToMenu();

    /// <summary>End the application.</summary>
    void Quit();
}
