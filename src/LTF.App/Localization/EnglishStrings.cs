using System.Collections.Generic;

namespace LTF.App.Localization;

/// <summary>
/// The English string pack — the game's UI language (ROADMAP Faz 3). One entry per key in
/// <see cref="StringKeys"/>; the coverage test proves there are no gaps. A Turkish pack of the same
/// shape can be added later without touching any View (ADR-0026 / ROADMAP M19).
/// </summary>
public static class EnglishStrings
{
    public static readonly IReadOnlyDictionary<string, string> Values = new Dictionary<string, string>
    {
        [StringKeys.NavPaddockHub] = "PADDOCK HUB",
        [StringKeys.NavDrivers] = "DRIVERS",
        [StringKeys.NavRndFacilities] = "R&D & FAC.",
        [StringKeys.NavCarsPowerUnit] = "CARS & PU",
        [StringKeys.NavStaff] = "STAFF",
        [StringKeys.NavFinance] = "FINANCE",
        [StringKeys.NavBoardSponsors] = "BOARD & SPON.",
        [StringKeys.NavStandings] = "STANDINGS",
        [StringKeys.NavCalendar] = "CALENDAR",
        [StringKeys.NavDatabase] = "DATABASE",
        [StringKeys.NavRaceWeekend] = "RACE WEEKEND",
        [StringKeys.NavSettings] = "SETTINGS",
        [StringKeys.NavExitToMenu] = "EXIT TO MENU",

        [StringKeys.SectionNavigation] = "NAVIGATION",
        [StringKeys.SectionRaceDay] = "RACE DAY",

        [StringKeys.TopContinue] = "CONTINUE",
        [StringKeys.TopInbox] = "INBOX",
        [StringKeys.TopSearchPlaceholder] = "Search…",
        [StringKeys.TopCapRoom] = "CAP ROOM",
        [StringKeys.TopBoardConf] = "BOARD CONF",

        [StringKeys.StatusReady] = "Ready.",

        [StringKeys.StateEmptyTitle] = "Nothing here yet",
        [StringKeys.StateLoadingTitle] = "Loading…",
        [StringKeys.StateErrorTitle] = "Something went wrong",
        [StringKeys.StateErrorRetry] = "Retry",

        [StringKeys.DialogConfirm] = "Confirm",
        [StringKeys.DialogCancel] = "Cancel",
        [StringKeys.DialogExitTitle] = "Exit to menu?",
        [StringKeys.DialogExitBody] = "Any unsaved progress will be lost.",

        [StringKeys.MenuTagline] = "TEAM PRINCIPAL",
        [StringKeys.MenuNewCareer] = "NEW CAREER",
        [StringKeys.MenuContinue] = "CONTINUE CAREER",
        [StringKeys.MenuLoadGame] = "LOAD GAME",
        [StringKeys.MenuQuickRace] = "QUICK RACE",
        [StringKeys.MenuSettings] = "SETTINGS",
        [StringKeys.MenuQuit] = "QUIT",

        [StringKeys.NewCareerTitle] = "NEW CAREER",
        [StringKeys.NewCareerBack] = "BACK",
        [StringKeys.NewCareerNext] = "NEXT",
        [StringKeys.NewCareerStart] = "START CAREER",
        [StringKeys.NewCareerConfirmHeading] = "YOU'RE ABOUT TO TAKE CHARGE OF",
        [StringKeys.NewCareerStepCarset] = "STEP 1 OF 4 · CHOOSE A SERIES",
        [StringKeys.NewCareerStepTeam] = "STEP 2 OF 4 · CHOOSE YOUR TEAM",
        [StringKeys.NewCareerStepBoard] = "STEP 3 OF 4 · BOARD OBJECTIVES",
        [StringKeys.NewCareerStepConfirm] = "STEP 4 OF 4 · CONFIRM",

        [StringKeys.BoardHeading] = "The board sets your mandate. Push back with your ambition, and they'll answer.",
        [StringKeys.BoardAmbitionCautious] = "CAUTIOUS",
        [StringKeys.BoardAmbitionBalanced] = "BALANCED",
        [StringKeys.BoardAmbitionAggressive] = "AGGRESSIVE",
        [StringKeys.BoardReactionAccepted] = "The board agrees.",
        [StringKeys.BoardReactionCountered] = "The board counters.",
        [StringKeys.BoardReactionRejected] = "The board holds firm.",

        [StringKeys.LoadGameTitle] = "LOAD GAME",
        [StringKeys.LoadGameEmpty] = "No saved careers yet.",
        [StringKeys.LoadGameLoad] = "LOAD",
        [StringKeys.LoadGameDelete] = "DELETE",
        [StringKeys.LoadGameBack] = "BACK",
    };
}
