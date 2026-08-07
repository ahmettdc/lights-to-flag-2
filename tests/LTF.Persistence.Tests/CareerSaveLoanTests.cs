using System.Linq;
using LTF.Career;
using LTF.Domain.Management;
using Xunit;

namespace LTF.Persistence.Tests;

public class CareerSaveLoanTests
{
    private static CareerState WithLoans() => new()
    {
        Seed = 21,
        Date = new DateOnly(2027, 6, 1),
        Teams =
        [
            new TeamHistoryRecord
            {
                TeamId = "alpha",
                Finances = new Finances
                {
                    Balance = 40_000_000,
                    Loans =
                    [
                        new Loan
                        {
                            Id = "loan-1", Lender = "Series Bank", Principal = 30_000_000, AnnualRatePercent = 9,
                            TermSeasons = 5, SeasonsRemaining = 3, OutstandingBalance = 18_000_000, MissedPayments = 1,
                        },
                        new Loan
                        {
                            Id = "loan-2", Principal = 10_000_000, AnnualRatePercent = 14,
                            TermSeasons = 3, SeasonsRemaining = 3, OutstandingBalance = 10_000_000,
                        },
                    ],
                },
            },
        ],
    };

    [Fact]
    public void A_round_trip_preserves_every_loan()
    {
        var loaded = CareerStore.Deserialize(CareerStore.Serialize(WithLoans()));

        var finances = loaded.Teams.Single(t => t.TeamId == "alpha").Finances;
        Assert.Equal(28_000_000L, finances.TotalDebt); // 18M + 10M

        var first = finances.Loans.Single(l => l.Id == "loan-1");
        Assert.Equal("Series Bank", first.Lender);
        Assert.Equal(30_000_000L, first.Principal);
        Assert.Equal(9, first.AnnualRatePercent);   // the frozen rate survives
        Assert.Equal(5, first.TermSeasons);
        Assert.Equal(3, first.SeasonsRemaining);
        Assert.Equal(18_000_000L, first.OutstandingBalance);
        Assert.Equal(1, first.MissedPayments);

        var second = finances.Loans.Single(l => l.Id == "loan-2");
        Assert.Equal(14, second.AnnualRatePercent);
        Assert.Equal(0, second.MissedPayments);
    }

    [Fact]
    public void Serialization_stays_byte_stable_with_loans()
    {
        var once = CareerStore.Serialize(WithLoans());
        var twice = CareerStore.Serialize(CareerStore.Deserialize(once));

        Assert.Equal(once, twice);
    }
}
