using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Media;
using LTF.App.Mvvm;
using LTF.Career;
using LTF.Domain;

namespace LTF.App.ViewModels.Screens;

/// <summary>
/// The standings screen (M21): the constructors' and drivers' championship tables, projected from a
/// <see cref="Standings"/> with ids resolved to names off the carset. Read-only — a Continue that runs a
/// round rebuilds it from the live career (M21d). Before a wheel turns every line sits on zero
/// (<see cref="ChampionshipStandings.Empty"/>). Team accents come from the carset's team order.
/// </summary>
public sealed class StandingsViewModel : ViewModelBase
{
    public StandingsViewModel(Carset carset, Standings standings)
    {
        var teamName = new Dictionary<string, string>(StringComparer.Ordinal);
        var teamAccent = new Dictionary<string, IBrush>(StringComparer.Ordinal);
        for (var i = 0; i < carset.Teams.Count; i++)
        {
            var team = carset.Teams[i];
            teamName[team.Id] = team.Name;
            teamAccent[team.Id] = ScreenBrushes.TeamAccent(i);
        }

        var teamOfDriver = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var team in carset.Teams)
        {
            foreach (var driverId in team.DriverIds)
            {
                teamOfDriver[driverId] = team.Id;
            }
        }

        var driverName = carset.Drivers.ToDictionary(d => d.Id, d => d.FullName, StringComparer.Ordinal);
        var maxTeamPoints = standings.Constructors.Count > 0 ? standings.Constructors.Max(c => c.Points) : 0;

        Constructors = standings.Constructors
            .Select(c => new ConstructorRowViewModel(
                c.Position,
                teamName.GetValueOrDefault(c.TeamId, c.TeamId),
                c.Points,
                BarWidth(c.Points, maxTeamPoints),
                teamAccent.GetValueOrDefault(c.TeamId, ScreenBrushes.Faint)))
            .ToList();

        Drivers = standings.Drivers
            .Select(d =>
            {
                var teamId = teamOfDriver.GetValueOrDefault(d.DriverId, "");
                return new DriverStandingRowViewModel(
                    d.Position,
                    driverName.GetValueOrDefault(d.DriverId, d.DriverId),
                    teamId.Length > 0 ? teamName.GetValueOrDefault(teamId, "") : "",
                    d.Points,
                    teamId.Length > 0 ? teamAccent.GetValueOrDefault(teamId, ScreenBrushes.Faint) : ScreenBrushes.Faint);
            })
            .ToList();
    }

    public IReadOnlyList<ConstructorRowViewModel> Constructors { get; }

    public IReadOnlyList<DriverStandingRowViewModel> Drivers { get; }

    public int DriverCount => Drivers.Count;

    /// <summary>Fill width (px) for a points bar within a fixed 96px track, leader-relative.</summary>
    private static double BarWidth(int points, int max) => max <= 0 ? 0 : 96.0 * points / max;
}

/// <summary>One row of the constructors' championship table (M21).</summary>
public sealed record ConstructorRowViewModel(int Position, string Team, int Points, double BarWidth, IBrush Accent);

/// <summary>One row of the drivers' championship table (M21).</summary>
public sealed record DriverStandingRowViewModel(int Position, string Name, string Team, int Points, IBrush Accent);
