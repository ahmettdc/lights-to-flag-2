namespace LTF.Simulation.Racing;

/// <summary>
/// Pure grid-order transforms applied before <see cref="RaceSimulator.Run"/> (M9g). The race reads
/// only the order of the competitor list it is handed, so reordering it here — reversing it, a
/// partial reverse, or dropping cars down the grid for penalties — is all it takes to run a
/// reverse-grid race or a penalised grid, with no change to the simulator. The season layer (M11)
/// computes component-allocation grid penalties and calls <see cref="WithPenalties"/> here.
/// </summary>
public static class GridOrder
{
    /// <summary>The field in reverse — the last starter becomes first.</summary>
    public static IReadOnlyList<Competitor> Reversed(IReadOnlyList<Competitor> grid)
    {
        var order = new List<Competitor>(grid);
        order.Reverse();
        return order;
    }

    /// <summary>The front <paramref name="count"/> cars reversed, the rest left in order — a partial
    /// reverse grid. A count of 0 or less reverses nothing; a count at or beyond the field reverses
    /// the whole grid.</summary>
    public static IReadOnlyList<Competitor> PartiallyReversed(IReadOnlyList<Competitor> grid, int count)
    {
        var n = Math.Clamp(count, 0, grid.Count);
        var order = new List<Competitor>(grid.Count);
        for (var i = n - 1; i >= 0; i--)
        {
            order.Add(grid[i]);
        }

        for (var i = n; i < grid.Count; i++)
        {
            order.Add(grid[i]);
        }

        return order;
    }

    /// <summary>The grid with per-driver grid penalties applied: each named car drops that many
    /// places, and a car it is dropped past keeps its slot. The primitive the season layer (M11)
    /// uses for component-allocation grid penalties.</summary>
    public static IReadOnlyList<Competitor> WithPenalties(
        IReadOnlyList<Competitor> grid, IReadOnlyDictionary<string, int> placesLost)
    {
        var ranked = new List<(Competitor Car, int Key, int Penalty, int Index)>(grid.Count);
        for (var i = 0; i < grid.Count; i++)
        {
            var penalty = placesLost.TryGetValue(grid[i].Id, out var p) ? Math.Max(0, p) : 0;
            ranked.Add((grid[i], i + penalty, penalty, i));
        }

        // Sort by the penalised slot; on a tie the car that didn't move (smaller penalty) keeps the
        // place, then the original order breaks any remaining tie — a stable, deterministic result.
        ranked.Sort((a, b) =>
        {
            var byKey = a.Key.CompareTo(b.Key);
            if (byKey != 0)
            {
                return byKey;
            }

            var byPenalty = a.Penalty.CompareTo(b.Penalty);
            return byPenalty != 0 ? byPenalty : a.Index.CompareTo(b.Index);
        });

        return ranked.Select(r => r.Car).ToList();
    }
}
