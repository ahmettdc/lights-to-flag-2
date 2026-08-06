using System;
using System.Linq;
using LTF.Domain;
using LTF.Domain.Common;
using LTF.Domain.Management;
using LTF.Domain.Racing;
using LTF.Domain.Rnd;
using Xunit;

namespace LTF.Career.Tests;

public class BossCareerSweepTests
{
    private static Car Flat(int v) => new()
    {
        Aerodynamics = new(v), Chassis = new(v), PowerUnit = new(v), TyreGentleness = new(v), Reliability = new(v),
    };

    // alpha is the player; its board demands the constructors' title. Both teams get ample money, so only
    // the on-track result moves the board.
    private static Carset BoardCareerCarset(int alphaCar, int bravoCar)
    {
        var carset = CareerFixtures.SeasonCarset(rounds: 6) with { PlayerTeamId = "alpha" };
        var teams = carset.Teams
            .Select(t => t with
            {
                Car = Flat(t.Id == "alpha" ? alphaCar : bravoCar),
                Finances = new Finances { Balance = 500_000_000 },
            })
            .ToList();
        var board = new TeamBoard
        {
            TeamId = "alpha",
            Objectives = [new Objective { Kind = ObjectiveKind.ConstructorPosition, Target = 1 }],
        };
        return carset with { Teams = teams, Boards = [board] };
    }

    // A developing carset with no boards, no player and no proposals — the shape ResearchSweep runs on.
    private static Carset DevelopingCarset()
    {
        var carset = CareerFixtures.SeasonCarset(rounds: 4);
        var nodes = Enumerable.Range(1, 4)
            .Select(i => new TechNode
            {
                Id = $"aero{i}", DepartmentId = "aero", Category = CarAxis.AeroLowSpeed,
                Cost = 1_000_000, GainMin = 5, GainMax = 5, Correlation = 100,
                Prerequisites = i == 1 ? Array.Empty<string>() : new[] { $"aero{i - 1}" },
            })
            .ToList();
        var tree = new TechTree { Departments = [new Department { Id = "aero", Name = "Aero" }], Nodes = nodes };
        var rules = carset.Rules with
        {
            Research = new ResearchRules { BaseProgressPerSeason = 1000, StepProgress = 100, BaseActiveProjects = 1 },
        };
        var teams = carset.Teams
            .Select(t => t with { Car = Flat(50), Finances = new Finances { Balance = 200_000_000 } })
            .ToList();
        return carset with { TechTree = tree, Rules = rules, Teams = teams };
    }

    [Fact]
    public void A_dominant_player_keeps_the_board_onside()
    {
        var report = BossCareerSweep.Run(BoardCareerCarset(alphaCar: 90, bravoCar: 50), seasons: 4, seed: 7);

        Assert.True(report.FinalBoardConfidence > 50); // wins the title every year → confidence high
        Assert.False(report.EverAtRisk);
    }

    [Fact]
    public void A_failing_player_slides_toward_the_sack()
    {
        var report = BossCareerSweep.Run(BoardCareerCarset(alphaCar: 50, bravoCar: 90), seasons: 4, seed: 7);

        Assert.True(report.FinalBoardConfidence < 50); // never wins the title → confidence collapses
        Assert.True(report.EverAtRisk);                // firing risk climbs
    }

    [Fact]
    public void A_boss_policy_signs_the_drivers_it_offers()
    {
        var carset = BoardCareerCarset(alphaCar: 90, bravoCar: 50);
        var policy = new BossPolicy(
            offers: [new ContractOffer { DriverId = "d1", TeamId = "alpha", SalaryPerSeason = 10_000_000 }]);

        var report = BossCareerSweep.Run(carset, seasons: 1, seed: 7, policy);

        Assert.Equal(1, report.PlayerBoard.Single().ContractsSigned);
    }

    [Fact]
    public void A_boardless_boss_sweep_reproduces_the_research_sweep_car_trajectory()
    {
        var carset = DevelopingCarset();

        var boss = BossCareerSweep.Run(carset, seasons: 4, seed: 7);
        var research = ResearchSweep.Run(carset, seasons: 4, seed: 7);

        Assert.DoesNotContain(boss.PlayerBoard, r => r.BoardConfidence != 0); // no board → nothing tracked
        foreach (var team in research.Teams)
        {
            Assert.Equal(team.FinalOverall, boss.Teams.Single(t => t.TeamId == team.TeamId).FinalOverall);
        }
    }

    [Fact]
    public void The_boss_sweep_is_deterministic()
    {
        var carset = BoardCareerCarset(alphaCar: 60, bravoCar: 80);

        Assert.Equal(Key(BossCareerSweep.Run(carset, 4, 7)), Key(BossCareerSweep.Run(carset, 4, 7)));
    }

    private static string Key(BossCareerSweepReport r) =>
        $"{r.FinalBoardConfidence}:{r.EverAtRisk}:" +
        string.Join(",", r.PlayerBoard.Select(s => $"{s.Season}={s.BoardConfidence}/{s.FiringRisk}")) + "|" +
        string.Join(",", r.Teams.Select(t => $"{t.TeamId}={t.FinalOverall}/{t.FinalBalance}"));
}
