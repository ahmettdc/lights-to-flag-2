using System.Linq;
using LTF.Career;
using LTF.Domain;
using LTF.Domain.Common;
using LTF.Domain.Management;
using LTF.Domain.Racing;
using Xunit;

namespace LTF.Persistence.Tests;

public class CareerSaveFinancesTests
{
    private static CareerState Sample() => new()
    {
        Seed = 11,
        Date = new DateOnly(2025, 11, 2),
        Teams =
        [
            new TeamHistoryRecord
            {
                TeamId = "alpha",
                ChampionshipsWon = 1,
                RaceWins = 9,
                Finances = new Finances
                {
                    Balance = 120_000_000, PrizeMoney = 45_000_000, SponsorIncome = 18_000_000, CostCap = 135_000_000,
                },
            },
            new TeamHistoryRecord { TeamId = "bravo", RaceWins = 1 }, // no finances → default
        ],
    };

    [Fact]
    public void A_round_trip_preserves_team_finances()
    {
        var loaded = CareerStore.Deserialize(CareerStore.Serialize(Sample()));

        var alpha = loaded.Teams.Single(t => t.TeamId == "alpha");
        Assert.Equal(120_000_000L, alpha.Finances.Balance);
        Assert.Equal(45_000_000L, alpha.Finances.PrizeMoney);
        Assert.Equal(18_000_000L, alpha.Finances.SponsorIncome);
        Assert.Equal(135_000_000L, alpha.Finances.CostCap);

        var bravo = loaded.Teams.Single(t => t.TeamId == "bravo");
        Assert.Equal(new Finances(), bravo.Finances); // a record without finances round-trips as empty
    }

    [Fact]
    public void Serialization_stays_byte_stable_with_finances()
    {
        var once = CareerStore.Serialize(Sample());
        var twice = CareerStore.Serialize(CareerStore.Deserialize(once));

        Assert.Equal(once, twice);
    }

    [Fact]
    public void Capture_then_restore_carries_finances_back_to_the_carset()
    {
        var played = MiniCarset(); // alpha carries finances

        var state = CareerState.Capture(played, new DateOnly(2025, 9, 1), 7);

        // A fresh copy with the team's finances wiped.
        var fresh = played with { Teams = [played.Teams[0] with { Finances = new Finances() }] };
        Assert.Equal(0L, fresh.Teams[0].Finances.Balance); // sanity: really wiped

        var resumed = state.RestoreInto(fresh);

        Assert.Equal(120_000_000L, resumed.Teams[0].Finances.Balance);
        Assert.Equal(135_000_000L, resumed.Teams[0].Finances.CostCap);
    }

    private static Carset MiniCarset() => new()
    {
        Id = "mini",
        Name = "Mini",
        Rules = new RulesSet { SeriesName = "S", Points = new PointsScheme { RacePoints = [25, 18] } },
        Teams =
        [
            new Team
            {
                Id = "alpha", Name = "Alpha", Car = Flat(70), DriverIds = ["d1"],
                Finances = new Finances { Balance = 120_000_000, CostCap = 135_000_000 },
            },
        ],
        Drivers =
        [
            new Driver { Id = "d1", FirstName = "Ada", LastName = "One", Age = 24, Attributes = Attrs() },
        ],
        Circuits = [],
        Calendar = [],
    };

    private static Car Flat(int v) => new()
    {
        Aerodynamics = new(v), Chassis = new(v), PowerUnit = new(v), TyreGentleness = new(v), Reliability = new(v),
    };

    private static DriverAttributes Attrs() => new()
    {
        Pace = new(70), Racecraft = new(70), Consistency = new(70),
        TyreManagement = new(70), WetWeather = new(70), Feedback = new(70),
    };
}
