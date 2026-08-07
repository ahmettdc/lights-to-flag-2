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

    // Main menu.
    public const string MenuTagline = "menu.tagline";
    public const string MenuNewCareer = "menu.newCareer";
    public const string MenuContinue = "menu.continue";
    public const string MenuLoadGame = "menu.loadGame";
    public const string MenuQuickRace = "menu.quickRace";
    public const string MenuSettings = "menu.settings";
    public const string MenuQuit = "menu.quit";

    // New-career wizard.
    public const string NewCareerTitle = "newcareer.title";
    public const string NewCareerBack = "newcareer.back";
    public const string NewCareerNext = "newcareer.next";
    public const string NewCareerStart = "newcareer.start";
    public const string NewCareerConfirmHeading = "newcareer.confirmHeading";
    public const string NewCareerStepCarset = "newcareer.stepCarset";
    public const string NewCareerStepTeam = "newcareer.stepTeam";
    public const string NewCareerStepBoard = "newcareer.stepBoard";
    public const string NewCareerStepConfirm = "newcareer.stepConfirm";

    // Board-objective negotiation.
    public const string BoardHeading = "board.heading";
    public const string BoardAmbitionCautious = "board.ambition.cautious";
    public const string BoardAmbitionBalanced = "board.ambition.balanced";
    public const string BoardAmbitionAggressive = "board.ambition.aggressive";
    public const string BoardReactionAccepted = "board.reaction.accepted";
    public const string BoardReactionCountered = "board.reaction.countered";
    public const string BoardReactionRejected = "board.reaction.rejected";

    // Load game.
    public const string LoadGameTitle = "loadgame.title";
    public const string LoadGameEmpty = "loadgame.empty";
    public const string LoadGameLoad = "loadgame.load";
    public const string LoadGameDelete = "loadgame.delete";
    public const string LoadGameBack = "loadgame.back";

    // Settings.
    public const string SettingsTitle = "settings.title";
    public const string SettingsGameplay = "settings.gameplay";
    public const string SettingsAutosave = "settings.autosave";
    public const string SettingsDifficulty = "settings.difficulty";
    public const string SettingsRaceSpeed = "settings.raceSpeed";
    public const string SettingsAccessibility = "settings.accessibility";
    public const string SettingsInterfaceScale = "settings.interfaceScale";
    public const string SettingsTextSize = "settings.textSize";
    public const string SettingsReduceMotion = "settings.reduceMotion";
    public const string SettingsHighContrast = "settings.highContrast";
    public const string SettingsAudio = "settings.audio";
    public const string SettingsMaster = "settings.master";
    public const string SettingsMusic = "settings.music";
    public const string SettingsSfx = "settings.sfx";
    public const string SettingsSave = "settings.save";
    public const string SettingsBack = "settings.back";
    public const string SettingsReset = "settings.reset";

    // Quick Race.
    public const string QuickRaceTitle = "quickrace.title";
    public const string QuickRaceCarset = "quickrace.carset";
    public const string QuickRaceCircuit = "quickrace.circuit";
    public const string QuickRaceRun = "quickrace.run";
    public const string QuickRaceBack = "quickrace.back";

    // In-shell screens (M21).
    public const string StandingsConstructors = "standings.constructors";
    public const string StandingsDrivers = "standings.drivers";
    public const string CalendarFixture = "calendar.fixture";
    public const string CalendarTrackProfile = "calendar.trackProfile";
    public const string DriversSquadAndProfiles = "drivers.squadAndProfiles";
    public const string DriversSquadList = "drivers.squadList";
    public const string DriversCoreAttributes = "drivers.coreAttributes";
    public const string DriversProfileDetails = "drivers.profileDetails";
    public const string DatabaseTabDrivers = "database.tabDrivers";
    public const string DatabaseTabJuniors = "database.tabJuniors";
    public const string DatabaseTabTeams = "database.tabTeams";
    public const string DatabaseColName = "database.colName";
    public const string DatabaseColNat = "database.colNat";
    public const string DatabaseColAge = "database.colAge";
    public const string DatabaseColTeam = "database.colTeam";
    public const string DatabaseColOvr = "database.colOvr";
    public const string DatabaseColPot = "database.colPot";
}
