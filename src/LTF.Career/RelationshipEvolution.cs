using LTF.Domain;
using LTF.Domain.Common;
using LTF.Simulation.Racing;

namespace LTF.Career;

/// <summary>
/// Moves paddock relationships as a season is raced (ADR-0013). It reads the race event log — the
/// participant ids the simulator already records — and turns teammate contact into a soured bond and
/// lost morale for both drivers. The intensity is personality-driven: a higher-ego pair feuds harder.
/// Pure, deterministic arithmetic (no RNG): the same season always evolves to the same graph, and a
/// season with no teammate contact leaves the carset untouched. The Simulation layer stays unaware of
/// relationships — the signal is only the collision's two ids, read here in the Career layer.
/// </summary>
public static class RelationshipEvolution
{
    private const int TeammateCollisionAffinityDrop = 12;
    private const int TeammateCollisionMoraleDrop = 6;

    /// <summary>Return the carset with relationships and morale evolved by the season's teammate contact.</summary>
    public static Carset Apply(Carset carset, SeasonResult season)
    {
        var teamOfDriver = BuildTeamMap(carset);
        var graph = carset.Relationships;
        var moraleDrop = new Dictionary<string, int>(StringComparer.Ordinal);

        foreach (var round in season.Rounds)
        {
            foreach (var e in round.Events)
            {
                if (e.Kind != RaceEventKind.Collision || e.OtherCompetitorId is not { } other)
                {
                    continue;
                }

                var one = e.CompetitorId;
                if (!teamOfDriver.TryGetValue(one, out var teamOne)
                    || !teamOfDriver.TryGetValue(other, out var teamOther)
                    || !string.Equals(teamOne, teamOther, StringComparison.Ordinal))
                {
                    continue; // only teammate contact evolves the bond in M12
                }

                graph = graph.Shifted(one, other, -AffinityDrop(carset, one, other));
                moraleDrop[one] = moraleDrop.GetValueOrDefault(one) + TeammateCollisionMoraleDrop;
                moraleDrop[other] = moraleDrop.GetValueOrDefault(other) + TeammateCollisionMoraleDrop;
            }
        }

        var drivers = carset.Drivers
            .Select(d => moraleDrop.TryGetValue(d.Id, out var md)
                ? d with { Morale = Rating.Clamped(d.Morale.Value - md) }
                : d)
            .ToList();

        return carset with { Drivers = drivers, Relationships = graph };
    }

    // Higher ego → a sharper feud: ego 50 gives the base drop, 100 gives 1.5×, 1 gives ~0.5×.
    private static int AffinityDrop(Carset carset, string aId, string bId)
    {
        var maxEgo = Math.Max(EgoOf(carset, aId), EgoOf(carset, bId));
        return TeammateCollisionAffinityDrop * (100 + (maxEgo - 50)) / 100;
    }

    private static int EgoOf(Carset carset, string driverId)
    {
        foreach (var d in carset.Drivers)
        {
            if (d.Id == driverId)
            {
                return d.Personality.Ego.Value;
            }
        }

        return 50;
    }

    private static Dictionary<string, string> BuildTeamMap(Carset carset)
    {
        var map = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var team in carset.Teams)
        {
            foreach (var id in team.DriverIds)
            {
                map[id] = team.Id;
            }
        }

        return map;
    }
}
