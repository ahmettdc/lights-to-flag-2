using LTF.Domain;
using LTF.Domain.Management;

namespace LTF.Career;

/// <summary>
/// Advances contracts as seasons pass (M12): each contract's <see cref="Contract.SeasonsRemaining"/>
/// counts down at rollover, and a contract that has served its term is dropped, freeing the party to
/// negotiate a new one. Pure and deterministic — the same carset always advances to the same ledger.
/// </summary>
public static class ContractLedger
{
    /// <summary>Return the carset with every contract advanced one season: terms count down, and any
    /// contract whose term is now served is removed.</summary>
    public static Carset AdvanceSeason(Carset carset)
    {
        var next = new List<Contract>(carset.Contracts.Count);
        foreach (var contract in carset.Contracts)
        {
            var remaining = contract.SeasonsRemaining - 1;
            if (remaining > 0)
            {
                next.Add(contract with { SeasonsRemaining = remaining });
            }

            // remaining <= 0 → the term is served, so the contract lapses and is dropped.
        }

        return carset with { Contracts = next };
    }
}
