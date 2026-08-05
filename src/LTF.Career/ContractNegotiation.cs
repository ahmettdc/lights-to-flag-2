using LTF.Domain;
using LTF.Domain.Racing;

namespace LTF.Career;

/// <summary>The verdict on a contract offer (M12).</summary>
public enum NegotiationOutcome
{
    Accepted,
    Rejected,
}

/// <summary>The outcome of evaluating a contract offer (M12): whether the driver signs, and the
/// salary they were holding out for (useful for a UI to show how close an offer was).</summary>
public sealed record NegotiationResult
{
    public required NegotiationOutcome Outcome { get; init; }
    public required long ExpectedSalary { get; init; }

    public bool Accepted => Outcome == NegotiationOutcome.Accepted;
}

/// <summary>
/// Evaluates a contract offer the way a driver would (ADR-0013: "negotiation reads the relationship").
/// The asking price rises with the driver's reputation and falls with their loyalty and with a warm
/// bond to their teammate — a driver at ease in the garage re-signs for less, one feuding with their
/// teammate holds out for more. Team-Principal-only and fully deterministic (threshold arithmetic, no
/// RNG): the same carset and offer always reach the same verdict.
/// </summary>
public static class ContractNegotiation
{
    private const long BaseSalary = 5_000_000;
    private const long MinSalary = 500_000;
    private const long ReputationSalaryStep = 100_000;
    private const long LoyaltyStep = 20_000;
    private const long AffinityStep = 10_000;

    /// <summary>Would the driver accept this yearly salary? Returns the verdict and the salary they
    /// expected.</summary>
    public static NegotiationResult Evaluate(Carset carset, string driverId, long offeredSalary)
    {
        var driver = FindDriver(carset, driverId);
        var expected = ExpectedSalary(carset, driver);
        return new NegotiationResult
        {
            Outcome = offeredSalary >= expected ? NegotiationOutcome.Accepted : NegotiationOutcome.Rejected,
            ExpectedSalary = expected,
        };
    }

    private static long ExpectedSalary(Carset carset, Driver driver)
    {
        var reputationDemand = BaseSalary + (driver.Reputation.Value - 50) * ReputationSalaryStep;
        var loyaltyDiscount = (driver.Personality.Loyalty.Value - 50) * LoyaltyStep;
        var affinityDiscount = TeammateAffinity(carset, driver.Id) * AffinityStep;

        var expected = reputationDemand - loyaltyDiscount - affinityDiscount;
        return Math.Max(expected, MinSalary);
    }

    private static int TeammateAffinity(Carset carset, string driverId)
    {
        var teammate = TeammateOf(carset, driverId);
        if (teammate is null)
        {
            return 0;
        }

        return carset.Relationships.Between(driverId, teammate)?.Affinity.Value ?? 0;
    }

    private static string? TeammateOf(Carset carset, string driverId)
    {
        foreach (var team in carset.Teams)
        {
            if (!team.DriverIds.Contains(driverId, StringComparer.Ordinal))
            {
                continue;
            }

            foreach (var id in team.DriverIds)
            {
                if (!string.Equals(id, driverId, StringComparison.Ordinal))
                {
                    return id;
                }
            }
        }

        return null;
    }

    private static Driver FindDriver(Carset carset, string driverId)
    {
        foreach (var d in carset.Drivers)
        {
            if (string.Equals(d.Id, driverId, StringComparison.Ordinal))
            {
                return d;
            }
        }

        throw new ArgumentException($"unknown driver '{driverId}'.", nameof(driverId));
    }
}
