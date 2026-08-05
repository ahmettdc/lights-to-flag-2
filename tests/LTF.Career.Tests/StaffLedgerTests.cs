using System.Linq;
using LTF.Domain;
using LTF.Domain.Common;
using LTF.Domain.Management;
using Xunit;

namespace LTF.Career.Tests;

public class StaffLedgerTests
{
    private static Staff Member(string id) => new()
    {
        Id = id, FirstName = "S", LastName = id, Role = StaffRole.RaceEngineer, Skill = new(70), Salary = 1_000_000,
    };

    private static Carset WithPoolAndStaff()
    {
        var carset = CareerFixtures.SeasonCarset(rounds: 4);
        var teams = carset.Teams
            .Select(t => t.Id == "alpha" ? t with { Staff = [Member("m1")] } : t)
            .ToList();
        return carset with { Teams = teams, StaffPool = [Member("fa1")] };
    }

    [Fact]
    public void Hiring_moves_a_free_agent_onto_the_team()
    {
        var after = StaffLedger.Hire(WithPoolAndStaff(), "alpha", "fa1");

        Assert.Empty(after.StaffPool);
        Assert.Contains(after.Teams.Single(t => t.Id == "alpha").Staff, s => s.Id == "fa1");
    }

    [Fact]
    public void Releasing_returns_a_member_to_the_pool()
    {
        var after = StaffLedger.Release(WithPoolAndStaff(), "alpha", "m1");

        Assert.DoesNotContain(after.Teams.Single(t => t.Id == "alpha").Staff, s => s.Id == "m1");
        Assert.Contains(after.StaffPool, s => s.Id == "m1");
    }

    [Fact]
    public void Hiring_an_unknown_agent_is_a_no_op()
    {
        var after = StaffLedger.Hire(WithPoolAndStaff(), "alpha", "ghost");

        Assert.Single(after.StaffPool); // pool unchanged
        Assert.Equal("fa1", after.StaffPool.Single().Id);
    }
}
