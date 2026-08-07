using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Media;
using LTF.App.Converters;
using LTF.App.Mvvm;
using LTF.Domain.Racing;

namespace LTF.App.ViewModels.Screens;

/// <summary>
/// A read-only detail panel for a driver or a team (M21), shared by the Drivers profile and the Database
/// browser. A header (name + subtitle), a list of key/value facts, and — for drivers — the six attribute
/// bars. Contract, salary, trust and scouting are transfer-market data (M22), so they are left out here;
/// this shows only what the domain carries today.
/// </summary>
public sealed class EntityDetailViewModel : ViewModelBase
{
    private EntityDetailViewModel(
        string name,
        string subtitle,
        IReadOnlyList<ProfileStatViewModel> facts,
        IReadOnlyList<AttributeBarViewModel> attributes)
    {
        Name = name;
        Subtitle = subtitle;
        Facts = facts;
        Attributes = attributes;
        HasAttributes = attributes.Count > 0;
    }

    public string Name { get; }

    public string Subtitle { get; }

    public IReadOnlyList<ProfileStatViewModel> Facts { get; }

    public IReadOnlyList<AttributeBarViewModel> Attributes { get; }

    public bool HasAttributes { get; }

    /// <summary>A driver profile: CA/PA subtitle, the six ratings as bars, and biographical facts.</summary>
    public static EntityDetailViewModel ForDriver(Driver driver, string teamLabel)
    {
        var potential = driver.Potential > 0 ? driver.Potential.ToString(CultureInfo.InvariantCulture) : "—";
        var subtitle = $"CA {driver.Attributes.Overall} · PA {potential}";

        var facts = new[]
        {
            Fact("Nationality", Blank(driver.Nationality)),
            Fact("Age", driver.Age.ToString(CultureInfo.InvariantCulture)),
            Fact("Number", driver.Number > 0 ? driver.Number.ToString(CultureInfo.InvariantCulture) : "—"),
            Fact("Team", Blank(teamLabel)),
            new ProfileStatViewModel("Morale", driver.Morale.Value.ToString(CultureInfo.InvariantCulture), StatusColorConverter.Classify(driver.Morale.Value)),
            Fact("Career wins", driver.Career.Wins.ToString(CultureInfo.InvariantCulture)),
        };

        var attributes = new[]
        {
            Attr("Pace", driver.Attributes.Pace),
            Attr("Racecraft", driver.Attributes.Racecraft),
            Attr("Consistency", driver.Attributes.Consistency),
            Attr("Tyre Mgmt", driver.Attributes.TyreManagement),
            Attr("Wet Weather", driver.Attributes.WetWeather),
            Attr("Feedback", driver.Attributes.Feedback),
        };

        return new EntityDetailViewModel(driver.FullName.ToUpperInvariant(), subtitle, facts, attributes);
    }

    /// <summary>A team card: base/nationality/principal and the honours record (no attribute bars).</summary>
    public static EntityDetailViewModel ForTeam(Team team, int driverCount, int averageOverall)
    {
        var facts = new[]
        {
            Fact("Base", Blank(team.Base)),
            Fact("Nationality", Blank(team.Nationality)),
            Fact("Principal", Blank(team.Principal)),
            Fact("Drivers", driverCount.ToString(CultureInfo.InvariantCulture)),
            new ProfileStatViewModel("Avg. rating", averageOverall > 0 ? averageOverall.ToString(CultureInfo.InvariantCulture) : "—", StatusColorConverter.Classify(averageOverall)),
            Fact("Championships", team.ChampionshipsWon.ToString(CultureInfo.InvariantCulture)),
        };

        return new EntityDetailViewModel(team.Name.ToUpperInvariant(), Blank(team.ShortName), facts, Array.Empty<AttributeBarViewModel>());
    }

    private static ProfileStatViewModel Fact(string label, string value) =>
        new(label, value, ScreenBrushes.Primary);

    private static AttributeBarViewModel Attr(string label, int value) =>
        new(label, value, StatusColorConverter.Classify(value));

    private static string Blank(string value) => string.IsNullOrWhiteSpace(value) ? "—" : value;
}

/// <summary>One 0–100 attribute rendered as a labelled bar (M21).</summary>
public sealed record AttributeBarViewModel(string Label, int Value, IBrush BarBrush);
