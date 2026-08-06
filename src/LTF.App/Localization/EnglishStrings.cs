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
    };
}
