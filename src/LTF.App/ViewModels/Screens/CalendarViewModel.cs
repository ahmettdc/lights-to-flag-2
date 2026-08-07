using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia.Media;
using LTF.App.Mvvm;
using LTF.Career;
using LTF.Domain;
using LTF.Domain.Racing;

namespace LTF.App.ViewModels.Screens;

/// <summary>
/// The calendar screen (M21): the season's rounds with done/next/upcoming state read off the career clock,
/// plus a track profile for the next race. Read-only; a Continue that advances the date rebuilds it (M21d).
/// Weather forecast, logistics and corner counts have no domain model yet, so the profile shows only what
/// the circuit card actually carries (ADR-0027 deferral).
/// </summary>
public sealed class CalendarViewModel : ViewModelBase
{
    public CalendarViewModel(Carset carset, CareerClock clock)
    {
        var today = clock.Date;
        var circuits = carset.Circuits.ToDictionary(c => c.Id, StringComparer.Ordinal);
        var rounds = carset.Calendar.OrderBy(r => r.Round).ToList();

        // The "next" round is the earliest one not yet in the past (Date >= today); null once the season's done.
        var next = rounds.FirstOrDefault(r => r.Date >= today);

        Rounds = rounds
            .Select(r =>
            {
                var name = circuits.TryGetValue(r.CircuitId, out var circuit) ? circuit.Name : r.CircuitId;
                var status = r.Date < today ? RoundStatus.Done
                    : next is not null && r.Round == next.Round ? RoundStatus.Next
                    : RoundStatus.Upcoming;
                return new CalendarRoundRowViewModel(r.Round, name.ToUpperInvariant(), FormatDate(r.Date), status);
            })
            .ToList();

        SeasonLabel = rounds.Count > 0 ? $"{rounds[0].Date.Year} SEASON" : "—";

        if (next is not null && circuits.TryGetValue(next.CircuitId, out var nextCircuit))
        {
            HasNextRace = true;
            NextRaceHeading = $"NEXT RACE · R{next.Round:00} {nextCircuit.Name.ToUpperInvariant()}";
            NextRaceSubtitle = nextCircuit.Country.ToUpperInvariant();
            Profile = BuildProfile(nextCircuit, next.IsSprint);
        }
        else
        {
            NextRaceHeading = "SEASON COMPLETE";
            NextRaceSubtitle = "";
            Profile = [];
        }
    }

    public IReadOnlyList<CalendarRoundRowViewModel> Rounds { get; }

    public string SeasonLabel { get; }

    public bool HasNextRace { get; }

    public string NextRaceHeading { get; }

    public string NextRaceSubtitle { get; }

    public IReadOnlyList<ProfileStatViewModel> Profile { get; }

    private static IReadOnlyList<ProfileStatViewModel> BuildProfile(Circuit c, bool isSprint) =>
        new[]
        {
            new ProfileStatViewModel("Length", $"{c.LapDistanceKm.ToString("0.000", CultureInfo.InvariantCulture)} km", ScreenBrushes.Primary),
            new ProfileStatViewModel("Race laps", c.Laps.ToString(CultureInfo.InvariantCulture), ScreenBrushes.Primary),
            new ProfileStatViewModel("Overtaking", EaseLabel(c.Overtaking), EaseBrush(c.Overtaking)),
            new ProfileStatViewModel("Tyre wear", LevelLabel(c.TyreStress), RiskBrush(c.TyreStress)),
            new ProfileStatViewModel("Rain sensitivity", LevelLabel(c.WeatherVariability), RiskBrush(c.WeatherVariability)),
            new ProfileStatViewModel("Weekend format", isSprint ? "Sprint" : "Standard", ScreenBrushes.Primary),
        };

    private static string FormatDate(DateOnly date) => date.ToString("dd MMM", CultureInfo.InvariantCulture);

    // Higher Overtaking rating = easier to pass → greener.
    private static string EaseLabel(int rating) => rating >= 66 ? "Easy" : rating >= 40 ? "Medium" : "Hard";

    private static IBrush EaseBrush(int rating) => rating >= 66 ? ScreenBrushes.Good : rating >= 40 ? ScreenBrushes.Warn : ScreenBrushes.Bad;

    // Higher stress / variability = a bigger risk → redder.
    private static string LevelLabel(int rating) => rating >= 66 ? "High" : rating >= 40 ? "Medium" : "Low";

    private static IBrush RiskBrush(int rating) => rating >= 66 ? ScreenBrushes.Bad : rating >= 40 ? ScreenBrushes.Warn : ScreenBrushes.Good;
}

/// <summary>Where a round sits relative to the career clock (M21).</summary>
public enum RoundStatus
{
    Done,
    Next,
    Upcoming,
}

/// <summary>One fixture row of the calendar (M21).</summary>
public sealed record CalendarRoundRowViewModel(int Round, string Circuit, string DateText, RoundStatus Status)
{
    public string RoundLabel => $"R{Round:00}";

    public string StatusLabel => Status switch
    {
        RoundStatus.Done => "DONE",
        RoundStatus.Next => "NEXT",
        _ => "",
    };

    public IBrush StatusBrush => Status switch
    {
        RoundStatus.Done => ScreenBrushes.Faint,
        RoundStatus.Next => ScreenBrushes.Bad,
        _ => ScreenBrushes.Dim,
    };

    public IBrush CircuitBrush => Status == RoundStatus.Next ? ScreenBrushes.Primary : ScreenBrushes.Secondary;
}

/// <summary>One label/value line in the next-race track profile (M21).</summary>
public sealed record ProfileStatViewModel(string Label, string Value, IBrush ValueBrush);
