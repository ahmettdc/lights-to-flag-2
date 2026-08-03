using LightsToFlag.Core.Domain;
using LightsToFlag.Core.Simulation;

namespace LightsToFlag.Core.Career;

/// <summary>
/// Drives a single-player career: builds the entry list, simulates rounds one at a
/// time (deterministically seeded per round), computes championship standings, and
/// rolls the career over to the next season (ageing, retirements, rookie promotions
/// and seat offers to the player). Pure and deterministic — no wall-clock, no shared RNG.
/// </summary>
public sealed class CareerEngine
{
    private const int RookieBaselineRating = 5;

    public CareerState Start(Carset carset, string? playerId, int seed)
    {
        var entrants = new List<SeasonEntrant>(carset.Drivers.Count);
        for (var i = 0; i < carset.Drivers.Count; i++)
        {
            entrants.Add(new SeasonEntrant
            {
                Id = $"D{i + 1}",
                Driver = carset.Drivers[i],
                TeamNumber = carset.Drivers[i].TeamNumber,
            });
        }

        return new CareerState
        {
            CarsetName = carset.Name,
            Seed = seed,
            PlayerId = playerId,
            SeasonIndex = 0,
            Entrants = entrants,
        };
    }

    public bool IsSeasonComplete(CareerState state, Carset carset) =>
        state.CompletedRounds.Count >= carset.Circuits.Count;

    public CareerState SimulateNextRound(CareerState state, Carset carset)
    {
        if (IsSeasonComplete(state, carset))
        {
            return state;
        }

        var roundIndex = state.CompletedRounds.Count;
        var circuit = carset.Circuits[roundIndex];
        var competitors = BuildCompetitors(state, carset);
        var rng = new SeededRandom(RoundSeed(state.Seed, state.SeasonIndex, roundIndex));

        var grid = QualifyingSimulator.Run(competitors, circuit, carset.Coefficients, carset.Rules, rng);
        var race = new RaceSimulator().Run(competitors, grid, circuit, carset.Coefficients, carset.Rules, rng);

        var entries = race.Entries
            .Select(e => new RoundEntry
            {
                CompetitorId = e.CompetitorId,
                Position = e.Position,
                Points = e.Points,
                Finished = e.Status == FinishStatus.Finished,
                FastestLap = e.FastestLap,
            })
            .ToList();

        var result = new RoundResult
        {
            RoundIndex = roundIndex,
            CircuitName = circuit.Name,
            PolePositionId = grid.PolePosition,
            Entries = entries,
        };

        return state with { CompletedRounds = state.CompletedRounds.Append(result).ToList() };
    }

    public CareerState SimulateWholeSeason(CareerState state, Carset carset)
    {
        var current = state;
        while (!IsSeasonComplete(current, carset))
        {
            current = SimulateNextRound(current, carset);
        }

        return current;
    }

    public IReadOnlyList<DriverStanding> DriverStandings(CareerState state, Carset carset)
    {
        var points = new Dictionary<string, double>();
        var wins = new Dictionary<string, int>();
        foreach (var entry in state.CompletedRounds.SelectMany(r => r.Entries))
        {
            points[entry.CompetitorId] = points.GetValueOrDefault(entry.CompetitorId) + entry.Points;
            if (entry is { Finished: true, Position: 1 })
            {
                wins[entry.CompetitorId] = wins.GetValueOrDefault(entry.CompetitorId) + 1;
            }
        }

        return state.Entrants
            .Select(e => new DriverStanding
            {
                CompetitorId = e.Id,
                DriverName = e.Driver.FullName,
                TeamName = TeamName(carset, e.TeamNumber),
                Points = points.GetValueOrDefault(e.Id),
                Wins = wins.GetValueOrDefault(e.Id),
            })
            .OrderByDescending(s => s.Points)
            .ThenByDescending(s => s.Wins)
            .ThenBy(s => s.CompetitorId, StringComparer.Ordinal)
            .Select((s, i) => s with { Position = i + 1 })
            .ToList();
    }

    public IReadOnlyList<ConstructorStanding> ConstructorStandings(CareerState state, Carset carset)
    {
        var teamOf = state.Entrants.ToDictionary(e => e.Id, e => e.TeamNumber);
        var points = new Dictionary<int, double>();
        var wins = new Dictionary<int, int>();
        foreach (var entry in state.CompletedRounds.SelectMany(r => r.Entries))
        {
            if (!teamOf.TryGetValue(entry.CompetitorId, out var team))
            {
                continue;
            }

            points[team] = points.GetValueOrDefault(team) + entry.Points;
            if (entry is { Finished: true, Position: 1 })
            {
                wins[team] = wins.GetValueOrDefault(team) + 1;
            }
        }

        return points.Keys
            .Select(team => new ConstructorStanding
            {
                TeamNumber = team,
                TeamName = TeamName(carset, team),
                Points = points.GetValueOrDefault(team),
                Wins = wins.GetValueOrDefault(team),
            })
            .OrderByDescending(s => s.Points)
            .ThenByDescending(s => s.Wins)
            .ThenBy(s => s.TeamNumber)
            .Select((s, i) => s with { Position = i + 1 })
            .ToList();
    }

    /// <summary>
    /// Close the current season and set up the next: archive the champions, age
    /// every driver, retire those past the retirement age (promoting a reserve into
    /// the seat), and generate seat offers to the player based on their finish.
    /// </summary>
    public CareerState AdvanceToNextSeason(CareerState state, Carset carset)
    {
        var driverStandings = DriverStandings(state, carset);
        var constructorStandings = ConstructorStandings(state, carset);

        var summary = new SeasonSummary
        {
            SeasonIndex = state.SeasonIndex,
            DriverChampionId = driverStandings.Count > 0 ? driverStandings[0].CompetitorId : "",
            DriverChampionName = driverStandings.Count > 0 ? driverStandings[0].DriverName : "",
            ConstructorChampionName = constructorStandings.Count > 0 ? constructorStandings[0].TeamName : "",
        };

        var reserves = new Queue<RookieRating>(carset.Reserves);
        var newEntrants = new List<SeasonEntrant>(state.Entrants.Count);
        foreach (var entrant in state.Entrants)
        {
            var aged = entrant.Driver with { Age = entrant.Driver.Age + 1 };
            var retires = carset.Rules.RetirementAge > 0
                && aged.Age > carset.Rules.RetirementAge
                && entrant.Id != state.PlayerId; // the player retires on their own terms

            if (retires && reserves.Count > 0)
            {
                var rookie = reserves.Dequeue();
                newEntrants.Add(entrant with { Driver = PromoteRookie(rookie, aged) });
            }
            else
            {
                newEntrants.Add(entrant with { Driver = aged });
            }
        }

        var offers = GeneratePlayerOffers(state, driverStandings, constructorStandings);

        return state with
        {
            SeasonIndex = state.SeasonIndex + 1,
            Entrants = newEntrants,
            CompletedRounds = Array.Empty<RoundResult>(),
            PendingOffers = offers,
            History = state.History.Append(summary).ToList(),
        };
    }

    private IReadOnlyList<TeamOffer> GeneratePlayerOffers(
        CareerState state,
        IReadOnlyList<DriverStanding> driverStandings,
        IReadOnlyList<ConstructorStanding> constructorStandings)
    {
        if (state.PlayerId is null || constructorStandings.Count == 0)
        {
            return Array.Empty<TeamOffer>();
        }

        var playerPos = driverStandings.FirstOrDefault(s => s.CompetitorId == state.PlayerId)?.Position
                        ?? driverStandings.Count;

        // Do well → the leading teams come knocking; do poorly → mid/back-of-grid seats.
        var take = playerPos <= 3 ? 3 : playerPos <= 8 ? 2 : 1;
        return constructorStandings
            .Take(Math.Min(take, constructorStandings.Count))
            .Select(c => new TeamOffer { TeamNumber = c.TeamNumber, TeamName = c.TeamName })
            .ToList();
    }

    private static DriverRating PromoteRookie(RookieRating rookie, DriverRating seat) => new()
    {
        FirstName = rookie.FirstName,
        LastName = rookie.LastName,
        Age = rookie.Age > 0 ? rookie.Age : 20,
        Nationality = rookie.Nationality,
        Number = seat.Number,
        TeamNumber = seat.TeamNumber,
        NumberWithinTeam = seat.NumberWithinTeam,
        Pace = RookieBaselineRating,
        Consistency = RookieBaselineRating,
        Concentration = RookieBaselineRating,
        WetWeather = RookieBaselineRating,
        Overtaking = RookieBaselineRating,
        Smoothness = RookieBaselineRating,
        Feedback = RookieBaselineRating,
        Teamwork = RookieBaselineRating,
        Qualifying = RookieBaselineRating,
    };

    private static IReadOnlyList<Competitor> BuildCompetitors(CareerState state, Carset carset)
    {
        return state.Entrants
            .Select(e =>
            {
                var teamIndex = Math.Clamp(e.TeamNumber - 1, 0, Math.Max(0, carset.Teams.Count - 1));
                var team = carset.Teams.Count > 0 ? carset.Teams[teamIndex] : new TeamRating { Name = "Privateer" };
                return new Competitor
                {
                    Id = e.Id,
                    Driver = e.Driver,
                    Car = team,
                    ClassIndex = Math.Max(0, team.Class - 1),
                };
            })
            .ToList();
    }

    private static string TeamName(Carset carset, int teamNumber)
    {
        var index = teamNumber - 1;
        return index >= 0 && index < carset.Teams.Count ? carset.Teams[index].Name : $"Team {teamNumber}";
    }

    /// <summary>Deterministic per-round seed derived from the career seed + season + round.</summary>
    private static int RoundSeed(int careerSeed, int season, int round)
    {
        unchecked
        {
            var hash = 17;
            hash = hash * 31 + careerSeed;
            hash = hash * 31 + season;
            hash = hash * 31 + round;
            return hash;
        }
    }
}
