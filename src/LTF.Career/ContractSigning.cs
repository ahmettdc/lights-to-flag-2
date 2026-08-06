using LTF.Domain;
using LTF.Domain.Common;
using LTF.Domain.Management;

namespace LTF.Career;

/// <summary>A contract offer a team makes a driver (M17): the salary and terms proposed.</summary>
public sealed record ContractOffer
{
    public required string DriverId { get; init; }
    public required string TeamId { get; init; }
    public required long SalaryPerSeason { get; init; }
    public int SeasonsRemaining { get; init; } = 1;
    public long SigningBonus { get; init; }
    public ContractClauses Clauses { get; init; } = new();
}

/// <summary>The outcome of an offer (M17): the (possibly unchanged) carset, whether the driver signed,
/// and the salary they were holding out for.</summary>
public sealed record SigningResult
{
    public required Carset Carset { get; init; }
    public required bool Signed { get; init; }
    public required long ExpectedSalary { get; init; }
}

/// <summary>
/// Signs a driver to an offer (M17) — the missing writer that turns a <see cref="ContractNegotiation"/>
/// verdict into a contract on the carset. If the driver accepts the salary, a new driver contract is
/// appended to <see cref="Carset.Contracts"/>; a rejected offer, or an offer to a driver the carset
/// doesn't know, leaves the carset untouched (a no-op, like <see cref="StaffLedger"/>). Pure and
/// deterministic — the same carset and offer always reach the same result.
/// </summary>
public static class ContractSigning
{
    /// <summary>Offer a driver the terms and, if they accept, add the contract to the carset.</summary>
    public static SigningResult Sign(Carset carset, ContractOffer offer)
    {
        if (!KnowsDriver(carset, offer.DriverId))
        {
            return new SigningResult { Carset = carset, Signed = false, ExpectedSalary = 0 };
        }

        var verdict = ContractNegotiation.Evaluate(carset, offer.DriverId, offer.SalaryPerSeason);
        if (!verdict.Accepted)
        {
            return new SigningResult { Carset = carset, Signed = false, ExpectedSalary = verdict.ExpectedSalary };
        }

        var contract = new Contract
        {
            Kind = ContractKind.Driver,
            PartyId = offer.DriverId,
            TeamId = offer.TeamId,
            SalaryPerSeason = offer.SalaryPerSeason,
            SeasonsRemaining = offer.SeasonsRemaining,
            SigningBonus = offer.SigningBonus,
            Clauses = offer.Clauses,
        };

        var contracts = new List<Contract>(carset.Contracts) { contract };
        return new SigningResult
        {
            Carset = carset with { Contracts = contracts },
            Signed = true,
            ExpectedSalary = verdict.ExpectedSalary,
        };
    }

    private static bool KnowsDriver(Carset carset, string driverId)
    {
        foreach (var driver in carset.Drivers)
        {
            if (string.CompareOrdinal(driver.Id, driverId) == 0)
            {
                return true;
            }
        }

        return false;
    }
}
