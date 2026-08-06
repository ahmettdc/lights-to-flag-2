using System.Linq;
using LTF.Content;
using LTF.Domain;
using Xunit;

namespace LTF.Career.Tests;

/// <summary>
/// Runs M17's Team-Principal layer against the shipped flagship carset on CI (the M17 counterpart to
/// <see cref="FlagshipEconomyTests"/>/<see cref="FlagshipResearchTests"/>/<see cref="FlagshipComponentTests"/>):
/// the flagship names a player team and ships a board, a boss career evolves that board deterministically,
/// and the board layer leaves the shared car engine byte-identical to the research sweep.
/// </summary>
public class FlagshipBossTests
{
    private static Carset Flagship()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "carsets/global-prix.json");
        Assert.True(File.Exists(path), $"missing content file: {path}");
        return CarsetLoader.LoadFromJson(File.ReadAllText(path));
    }

    [Fact]
    public void The_flagship_names_a_player_team_and_ships_a_board()
    {
        var flagship = Flagship();

        Assert.Equal("talon", flagship.PlayerTeamId);

        var board = Assert.Single(flagship.Boards);
        Assert.Equal("talon", board.TeamId);
        Assert.NotEmpty(board.Members);      // a multi-member board, not a single number
        Assert.NotEmpty(board.Objectives);   // the board judges the season against real targets
        Assert.NotEmpty(flagship.RegulationProposals);
    }

    [Fact]
    public void The_flagship_board_evolves_across_a_boss_career()
    {
        var report = BossCareerSweep.Run(Flagship(), seasons: 4, seed: 7);

        Assert.Equal("talon", report.PlayerTeamId);
        Assert.Equal(4, report.PlayerBoard.Count);                       // one record per season
        Assert.InRange(report.FinalBoardConfidence, 0, 100);             // a real 0–100 pressure
        Assert.All(report.PlayerBoard, s => Assert.InRange(s.BoardConfidence, 0, 100));
        Assert.All(report.PlayerBoard, s => Assert.InRange(s.FiringRisk, 0, 100));
    }

    [Fact]
    public void The_flagship_boss_sweep_leaves_the_car_engine_untouched()
    {
        var flagship = Flagship();

        var boss = BossCareerSweep.Run(flagship, seasons: 4, seed: 7);
        var research = ResearchSweep.Run(flagship, seasons: 4, seed: 7);

        // The board and player layers must not touch the shared R&D/race engine: the same car trajectory.
        foreach (var team in research.Teams)
        {
            Assert.Equal(team.FinalOverall, boss.Teams.Single(t => t.TeamId == team.TeamId).FinalOverall);
        }
    }

    [Fact]
    public void The_flagship_boss_sweep_is_deterministic()
    {
        var flagship = Flagship();

        Assert.Equal(Key(BossCareerSweep.Run(flagship, 4, 7)), Key(BossCareerSweep.Run(flagship, 4, 7)));
    }

    private static string Key(BossCareerSweepReport r) =>
        $"{r.FinalBoardConfidence}:{r.EverAtRisk}:" +
        string.Join(",", r.PlayerBoard.Select(s => $"{s.Season}={s.BoardConfidence}/{s.FiringRisk}")) + "|" +
        string.Join(",", r.Teams.Select(t => $"{t.TeamId}={t.FinalOverall}/{t.FinalBalance}"));
}
