using System.Linq;
using LTF.Career;
using LTF.Domain.Common;
using Xunit;

namespace LTF.Persistence.Tests;

public class CareerSaveStaffTests
{
    private static CareerState WithStaff() => new()
    {
        Seed = 31,
        Date = new DateOnly(2027, 3, 1),
        Teams = [new TeamHistoryRecord { TeamId = "alpha" }],
        Staff =
        [
            new TeamStaffRecord
            {
                TeamId = "alpha",
                Staff =
                [
                    new StaffRecord
                    {
                        Id = "s1", FirstName = "Tia", LastName = "Vega", Role = StaffRole.TechnicalDirector,
                        Skill = 88, Nationality = "GB", Age = 47, Salary = 4_000_000,
                    },
                ],
            },
        ],
        StaffPool =
        [
            new StaffRecord
            {
                Id = "s2", FirstName = "Rae", LastName = "Otto", Role = StaffRole.ChiefStrategist,
                Skill = 72, Salary = 1_500_000,
            },
        ],
    };

    [Fact]
    public void A_round_trip_preserves_staff_and_the_pool()
    {
        var loaded = CareerStore.Deserialize(CareerStore.Serialize(WithStaff()));

        var member = loaded.Staff.Single(t => t.TeamId == "alpha").Staff.Single();
        Assert.Equal("s1", member.Id);
        Assert.Equal("Tia", member.FirstName);
        Assert.Equal(StaffRole.TechnicalDirector, member.Role);
        Assert.Equal(88, member.Skill);
        Assert.Equal(47, member.Age);
        Assert.Equal(4_000_000L, member.Salary);

        var free = loaded.StaffPool.Single();
        Assert.Equal("s2", free.Id);
        Assert.Equal(StaffRole.ChiefStrategist, free.Role);
        Assert.Equal(1_500_000L, free.Salary);
    }

    [Fact]
    public void Serialization_stays_byte_stable_with_staff()
    {
        var once = CareerStore.Serialize(WithStaff());
        var twice = CareerStore.Serialize(CareerStore.Deserialize(once));

        Assert.Equal(once, twice);
    }
}
