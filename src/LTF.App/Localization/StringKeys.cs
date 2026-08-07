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
    public const string PaddockSummaryStatus = "paddock.summaryStatus";
    public const string PaddockCurrentEvents = "paddock.currentEvents";
    public const string PaddockWccPosition = "paddock.wccPosition";
    public const string PaddockBoardConfidence = "paddock.boardConfidence";
    public const string PaddockNextSession = "paddock.nextSession";
    public const string PaddockNextEvent = "paddock.nextEvent";
    public const string PaddockGoToRaceWeekend = "paddock.goToRaceWeekend";
    public const string PaddockCriticalDecisions = "paddock.criticalDecisions";
    public const string PaddockHighPriority = "paddock.highPriority";
    public const string PaddockPendingItems = "paddock.pendingItems";
    public const string PaddockDialogue = "paddock.dialogue";
    public const string PaddockDialogueEmpty = "paddock.dialogueEmpty";
    public const string PaddockDialogueEmptyBody = "paddock.dialogueEmptyBody";

    // Finance screen (M22).
    public const string FinanceFinances = "finance.finances";
    public const string FinanceBalance = "finance.balance";
    public const string FinanceSeasonBudget = "finance.seasonBudget";
    public const string FinancePrizeMoney = "finance.prizeMoney";
    public const string FinanceSponsorIncome = "finance.sponsorIncome";
    public const string FinanceCostCap = "finance.costCap";
    public const string FinanceTotalDebt = "finance.totalDebt";
    public const string FinanceCredit = "finance.credit";
    public const string FinanceCreditScore = "finance.creditScore";
    public const string FinanceOfferedRate = "finance.offeredRate";
    public const string FinanceCreditLimit = "finance.creditLimit";
    public const string FinanceHeadroom = "finance.headroom";
    public const string FinanceNoBank = "finance.noBank";
    public const string FinanceLoans = "finance.loans";
    public const string FinanceNoLoans = "finance.noLoans";
    public const string FinanceNoLoansBody = "finance.noLoansBody";
    public const string FinanceColLender = "finance.colLender";
    public const string FinanceColOutstanding = "finance.colOutstanding";
    public const string FinanceColRate = "finance.colRate";
    public const string FinanceColLeft = "finance.colLeft";
    public const string FinanceColNext = "finance.colNext";
    public const string FinanceSponsors = "finance.sponsors";
    public const string FinanceNoSponsors = "finance.noSponsors";
    public const string FinanceTakeLoan = "finance.takeLoan";
    public const string FinanceBorrowAmount = "finance.borrowAmount";
    public const string FinanceBorrowTerm = "finance.borrowTerm";
    public const string FinanceBorrowButton = "finance.borrowButton";

    // R&D & Facilities screen (M22).
    public const string RndFacilities = "rnd.facilities";
    public const string RndResearch = "rnd.research";
    public const string RndReadiness = "rnd.readiness";
    public const string RndConceptAero = "rnd.conceptAero";
    public const string RndConceptPowertrain = "rnd.conceptPowertrain";
    public const string RndNodesUnlocked = "rnd.nodesUnlocked";
    public const string RndProjects = "rnd.projects";
    public const string RndNoProjects = "rnd.noProjects";
    public const string RndNoProjectsBody = "rnd.noProjectsBody";
    public const string RndSteerConcept = "rnd.steerConcept";
    public const string RndApplyConcept = "rnd.applyConcept";
    public const string RndFreezes = "rnd.freezes";

    public const string RaceWeekendNoRace = "raceWeekend.noRace";
    public const string RaceWeekendTower = "raceWeekend.tower";
    public const string RaceWeekendEvents = "raceWeekend.events";
    public const string RaceWeekendClassification = "raceWeekend.classification";

    // Cars & Power Unit screen (M22).
    public const string CarsPerformance = "cars.performance";
    public const string CarsOverall = "cars.overall";
    public const string CarsEngineSupplier = "cars.engineSupplier";
    public const string CarsComponents = "cars.components";
    public const string CarsColReliability = "cars.colReliability";
    public const string CarsColUsage = "cars.colUsage";
    public const string CarsColWear = "cars.colWear";
    public const string CarsNoComponents = "cars.noComponents";
    public const string CarsNoComponentsBody = "cars.noComponentsBody";

    // Board & Sponsors screen (M22).
    public const string BoardBoard = "board.board";
    public const string BoardOwnership = "board.ownership";
    public const string BoardFiringRisk = "board.firingRisk";
    public const string BoardMembers = "board.members";
    public const string BoardObjectives = "board.objectives";
    public const string BoardNoBoard = "board.noBoard";
    public const string BoardNoBoardBody = "board.noBoardBody";

    // Staff screen (M22).
    public const string StaffSquad = "staff.squad";
    public const string StaffFreeAgents = "staff.freeAgents";
    public const string StaffColRole = "staff.colRole";
    public const string StaffColSkill = "staff.colSkill";
    public const string StaffColSalary = "staff.colSalary";
    public const string StaffNoSquad = "staff.noSquad";
    public const string StaffNoPool = "staff.noPool";
}
