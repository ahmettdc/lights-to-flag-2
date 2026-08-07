using System.Collections.Generic;
using LTF.App.Localization;

namespace LTF.App.Navigation;

/// <summary>
/// The canonical, ordered list of sidebar entries. Order and grouping mirror the mockup
/// (design/mockups/ui.dc.html): the NAVIGATION block, then RACE DAY, then bottom-pinned system actions.
/// </summary>
public static class NavRegistry
{
    public static readonly IReadOnlyList<NavItem> Items = new[]
    {
        new NavItem(NavKey.PaddockHub, StringKeys.NavPaddockHub, NavSection.Navigation, "M21"),
        new NavItem(NavKey.Drivers, StringKeys.NavDrivers, NavSection.Navigation, "M21"),
        new NavItem(NavKey.RndFacilities, StringKeys.NavRndFacilities, NavSection.Navigation, "M22"),
        new NavItem(NavKey.CarsPowerUnit, StringKeys.NavCarsPowerUnit, NavSection.Navigation, "M22"),
        new NavItem(NavKey.Staff, StringKeys.NavStaff, NavSection.Navigation, "M22"),
        new NavItem(NavKey.Finance, StringKeys.NavFinance, NavSection.Navigation, "M22"),
        new NavItem(NavKey.BoardSponsors, StringKeys.NavBoardSponsors, NavSection.Navigation, "M22"),
        new NavItem(NavKey.Standings, StringKeys.NavStandings, NavSection.Navigation, "M21"),
        new NavItem(NavKey.Calendar, StringKeys.NavCalendar, NavSection.Navigation, "M21"),
        new NavItem(NavKey.Database, StringKeys.NavDatabase, NavSection.Navigation, "M21"),
        new NavItem(NavKey.Records, StringKeys.NavRecords, NavSection.Navigation, "M24"),
        new NavItem(NavKey.RaceWeekend, StringKeys.NavRaceWeekend, NavSection.RaceDay, "M23"),
        new NavItem(NavKey.Settings, StringKeys.NavSettings, NavSection.System, "M20"),
        new NavItem(NavKey.ExitToMenu, StringKeys.NavExitToMenu, NavSection.System, "M20"),
    };
}
