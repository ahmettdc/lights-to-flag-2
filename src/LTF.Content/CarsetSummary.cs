using System;
using System.Collections.Generic;
using System.Linq;
using LTF.Domain;

namespace LTF.Content;

/// <summary>
/// A lightweight, I/O-free projection of a <see cref="Carset"/> for the new-career / carset-select UI:
/// enough to list carsets and their teams (and each team's drivers) without exposing the whole aggregate.
/// Produced by <see cref="From"/> from an already-loaded carset — discovery and file I/O live in LTF.App.
/// </summary>
public sealed record CarsetSummary(
    string Id,
    string Name,
    string Subtitle,
    string DefaultPlayerTeamId,
    IReadOnlyList<CarsetTeamOption> Teams)
{
    /// <summary>Project a loaded carset into its summary. Teams keep carset order; drivers resolve to names.</summary>
    public static CarsetSummary From(Carset carset)
    {
        var driversById = carset.Drivers.ToDictionary(d => d.Id, StringComparer.Ordinal);

        var teams = carset.Teams
            .Select(t => new CarsetTeamOption(
                t.Id,
                t.Name,
                t.ShortName,
                t.Class,
                t.DriverIds
                    .Select(id => driversById.TryGetValue(id, out var d) ? d.FullName : id)
                    .ToList()))
            .ToList();

        var subtitle = $"{carset.Teams.Count} teams · {carset.Drivers.Count} drivers";
        return new CarsetSummary(carset.Id, carset.Name, subtitle, carset.PlayerTeamId, teams);
    }
}

/// <summary>A team the player can choose to run, projected from a carset's team.</summary>
public sealed record CarsetTeamOption(
    string Id,
    string Name,
    string ShortName,
    string Class,
    IReadOnlyList<string> Drivers);
