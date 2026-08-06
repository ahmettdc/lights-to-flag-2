namespace LTF.App.Localization;

/// <summary>
/// Compile-checked keys for every user-facing string. Values live in the string packs
/// (<see cref="EnglishStrings"/>). Referencing keys as <c>const</c>s rather than raw literals means a
/// renamed or dropped key is a build break, and a coverage test can assert every key resolves.
/// </summary>
public static class StringKeys
{
    // Sidebar navigation (order matches design/mockups/ui.dc.html).
    public const string NavPaddockHub = "nav.paddockHub";
    public const string NavDrivers = "nav.drivers";
    public const string NavRndFacilities = "nav.rndFacilities";
    public const string NavCarsPowerUnit = "nav.carsPowerUnit";
    public const string NavStaff = "nav.staff";
    public const string NavFinance = "nav.finance";
    public const string NavBoardSponsors = "nav.boardSponsors";
    public const string NavStandings = "nav.standings";
    public const string NavCalendar = "nav.calendar";
    public const string NavDatabase = "nav.database";
    public const string NavRaceWeekend = "nav.raceWeekend";
    public const string NavSettings = "nav.settings";
    public const string NavExitToMenu = "nav.exitToMenu";

    // Sidebar section labels.
    public const string SectionNavigation = "section.navigation";
    public const string SectionRaceDay = "section.raceDay";

    // Top bar.
    public const string TopContinue = "top.continue";
    public const string TopInbox = "top.inbox";
    public const string TopSearchPlaceholder = "top.searchPlaceholder";
    public const string TopCapRoom = "top.capRoom";
    public const string TopBoardConf = "top.boardConf";

    // Status bar.
    public const string StatusReady = "status.ready";

    // Utility states.
    public const string StateEmptyTitle = "state.empty.title";
    public const string StateLoadingTitle = "state.loading.title";
    public const string StateErrorTitle = "state.error.title";
    public const string StateErrorRetry = "state.error.retry";

    // Dialogs.
    public const string DialogConfirm = "dialog.confirm";
    public const string DialogCancel = "dialog.cancel";
    public const string DialogExitTitle = "dialog.exit.title";
    public const string DialogExitBody = "dialog.exit.body";
}
