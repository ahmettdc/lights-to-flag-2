using System;
using System.Collections.Generic;
using System.Linq;
using LTF.Domain;
using LTF.Simulation;
using LTF.Simulation.Racing;

namespace LTF.App.Session;

/// <summary>One classified competitor in a Quick Race result (ids already joined to names).</summary>
public sealed record QuickRaceRow(int Position, string Driver, string Team, string Status, double GapToLeader, int Points)
{
    /// <summary>Gap to the leader, or a dash for the winner.</summary>
    public string GapText => Position <= 1 ? "—" : $"+{GapToLeader:0.000}";

    /// <summary>Points scored, blank when none.</summary>
    public string PointsText => Points > 0 ? Points.ToString() : "";
}

/// <summary>A finished Quick Race, ready for the results table.</summary>
public sealed record QuickRaceResult(string CircuitName, string? WinnerName, IReadOnlyList<QuickRaceRow> Rows);

/// <summary>
/// Runs a one-off race outside a career (M20g): build the entry list, run the deterministic
/// <see cref="RaceSimulator"/> on a chosen circuit + seed, and join the id-only classification back to driver
/// and team names for display. No new engine — the very same deterministic race a career round runs.
/// </summary>
public static class QuickRaceService
{
    public static QuickRaceResult Run(Carset carset, int circuitIndex, int seed)
    {
        var circuit = carset.Circuits[circuitIndex];
        var grid = EntryList.Build(carset);
        var result = RaceSimulator.Run(circuit, grid, carset.Rules, carset.Balance, seed);

        var driverName = carset.Drivers.ToDictionary(d => d.Id, d => d.FullName, StringComparer.Ordinal);
        var teamOfDriver = grid.ToDictionary(c => c.Id, c => c.TeamId, StringComparer.Ordinal);
        var teamName = carset.Teams.ToDictionary(t => t.Id, t => t.Name, StringComparer.Ordinal);

        string Name(string id) => driverName.GetValueOrDefault(id, id);
        string Team(string id) => teamName.GetValueOrDefault(teamOfDriver.GetValueOrDefault(id, ""), "");

        var rows = result.Classification
            .Select(e => new QuickRaceRow(
                e.Position, Name(e.CompetitorId), Team(e.CompetitorId), e.Status.ToString(), e.GapToLeader, e.Points))
            .ToList();

        var winner = result.WinnerId is null ? null : Name(result.WinnerId);
        return new QuickRaceResult(circuit.Name, winner, rows);
    }
}
