using LTF.Domain;
using LTF.Domain.Common;

namespace LTF.Content;

/// <summary>
/// Semantic validation of a loaded <see cref="Carset"/>: cross-references, duplicates and
/// sanity checks that structural parsing (in <see cref="CarsetLoader"/>) cannot catch.
/// Returns findings rather than throwing, so a tool can report them all at once.
/// </summary>
public static class CarsetValidator
{
    public static IReadOnlyList<ValidationIssue> Validate(Carset carset)
    {
        var issues = new List<ValidationIssue>();
        void Error(string m) => issues.Add(new ValidationIssue(ValidationSeverity.Error, m));
        void Warn(string m) => issues.Add(new ValidationIssue(ValidationSeverity.Warning, m));

        // Unique ids.
        var driverIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var d in carset.Drivers)
        {
            if (!driverIds.Add(d.Id))
            {
                Error($"duplicate driver id '{d.Id}'");
            }
        }

        var circuitIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var c in carset.Circuits)
        {
            if (!circuitIds.Add(c.Id))
            {
                Error($"duplicate circuit id '{c.Id}'");
            }

            if (c.Laps <= 0)
            {
                Error($"circuit '{c.Id}' must have a positive lap count");
            }

            if (c.LapDistanceKm <= 0 || c.BaseLapTimeSeconds <= 0)
            {
                Error($"circuit '{c.Id}' must have positive lap distance and base lap time");
            }
        }

        var teamIds = new HashSet<string>(StringComparer.Ordinal);
        var seatedDrivers = new HashSet<string>(StringComparer.Ordinal);
        foreach (var t in carset.Teams)
        {
            if (!teamIds.Add(t.Id))
            {
                Error($"duplicate team id '{t.Id}'");
            }

            if (t.DriverIds.Count == 0)
            {
                Error($"team '{t.Id}' has no drivers");
            }
            else if (t.DriverIds.Count != 2)
            {
                Warn($"team '{t.Id}' has {t.DriverIds.Count} drivers (2 is typical)");
            }

            foreach (var id in t.DriverIds)
            {
                if (!driverIds.Contains(id))
                {
                    Error($"team '{t.Id}' references unknown driver '{id}'");
                }

                if (!seatedDrivers.Add(id))
                {
                    Error($"driver '{id}' is seated in more than one team");
                }
            }
        }

        foreach (var d in carset.Drivers)
        {
            if (!seatedDrivers.Contains(d.Id))
            {
                Warn($"driver '{d.Id}' is not seated in any team");
            }
        }

        // Calendar.
        var rounds = new HashSet<int>();
        foreach (var r in carset.Calendar)
        {
            if (!circuitIds.Contains(r.CircuitId))
            {
                Error($"calendar round {r.Round} references unknown circuit '{r.CircuitId}'");
            }

            if (!rounds.Add(r.Round))
            {
                Error($"duplicate calendar round number {r.Round}");
            }
        }

        for (var n = 1; n <= carset.Calendar.Count; n++)
        {
            if (!rounds.Contains(n))
            {
                Warn($"calendar is missing round {n} (rounds are not contiguous from 1)");
            }
        }

        if (carset.Rules.Points.RacePoints.Count == 0)
        {
            Error("rules.points.racePoints must list at least one score");
        }

        // Regulations (ADR-0018): the era parsed structurally in the loader; here sanity-check the
        // 2026 energy/aero coefficients.
        var reg = carset.Regulations;
        if (reg.EnergyRegenPerLap < 0 || reg.ManualOverrideEnergyCost < 0 || reg.ManualOverrideBoost < 0
            || reg.DeRatingPenaltySeconds < 0 || reg.LowDragLapGainSeconds < 0
            || reg.HighDownforceLapGainSeconds < 0 || reg.LowDragTopSpeedKph < 0)
        {
            Error("regulations energy/aero coefficients must be non-negative");
        }

        if (reg.DeRatingThreshold < 0 || reg.DeRatingThreshold > 1)
        {
            Error("regulations deRatingThreshold must be within 0..1");
        }

        // Economy (M13): every payout, cost and penalty coefficient must be non-negative.
        var economy = carset.Rules.Economy;
        if (economy.TvIncome < 0 || economy.OperatingCostPerRace < 0 || economy.CrashCostPerIncident < 0
            || economy.CostCapFinePercent < 0 || economy.CostCapPointsPerOverage < 0
            || economy.PrizeMoney.Any(p => p < 0))
        {
            Error("economy values must be non-negative");
        }

        // Sponsors and staff (M13): fees, bonuses and salaries must be non-negative.
        foreach (var team in carset.Teams)
        {
            foreach (var sponsor in team.Sponsors)
            {
                if (sponsor.PerRaceFee < 0 || sponsor.PerPointBonus < 0 || sponsor.ObjectiveBonus < 0)
                {
                    Error($"sponsor '{sponsor.Id}' on team '{team.Id}' has a negative fee or bonus");
                }
            }

            foreach (var member in team.Staff)
            {
                if (member.Salary < 0)
                {
                    Error($"staff '{member.Id}' on team '{team.Id}' has a negative salary");
                }
            }
        }

        // Contracts (M12): party and team must resolve; terms are non-negative.
        foreach (var contract in carset.Contracts)
        {
            if (!teamIds.Contains(contract.TeamId))
            {
                Error($"contract for '{contract.PartyId}' references unknown team '{contract.TeamId}'");
            }

            if (contract.Kind == ContractKind.Driver && !driverIds.Contains(contract.PartyId))
            {
                Error($"driver contract references unknown driver '{contract.PartyId}'");
            }

            if (contract.SalaryPerSeason < 0)
            {
                Error($"contract for '{contract.PartyId}' has a negative salary");
            }

            if (contract.SeasonsRemaining < 0)
            {
                Error($"contract for '{contract.PartyId}' has negative seasons remaining");
            }
        }

        // R&D tech tree (M14): node graph integrity, per-team references and tuning sanity.
        var tree = carset.TechTree;
        var departmentIds = new HashSet<string>(tree.Departments.Select(d => d.Id), StringComparer.Ordinal);
        var nodeIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var node in tree.Nodes)
        {
            if (!nodeIds.Add(node.Id))
            {
                Error($"duplicate tech node id '{node.Id}'");
            }

            if (!departmentIds.Contains(node.DepartmentId))
            {
                Error($"tech node '{node.Id}' references unknown department '{node.DepartmentId}'");
            }

            if (node.Cost < 0 || node.Quota < 0 || node.GainMin < 0 || node.GainMax < 0)
            {
                Error($"tech node '{node.Id}' has a negative cost, quota or gain");
            }

            if (node.GainMin > node.GainMax)
            {
                Error($"tech node '{node.Id}' has gainMin above gainMax");
            }

            if (node.Confidence is < 0 or > 100 || node.Correlation is < 0 or > 100)
            {
                Error($"tech node '{node.Id}' confidence and correlation must be within 0..100");
            }
        }

        foreach (var node in tree.Nodes)
        {
            foreach (var prereq in node.Prerequisites)
            {
                if (!nodeIds.Contains(prereq))
                {
                    Error($"tech node '{node.Id}' requires unknown node '{prereq}'");
                }
            }
        }

        foreach (var team in carset.Teams)
        {
            foreach (var unlocked in team.Research.UnlockedNodeIds)
            {
                if (!nodeIds.Contains(unlocked))
                {
                    Error($"team '{team.Id}' has unlocked unknown node '{unlocked}'");
                }
            }

            foreach (var project in team.Research.ActiveProjects)
            {
                if (!nodeIds.Contains(project.NodeId))
                {
                    Error($"team '{team.Id}' is developing unknown node '{project.NodeId}'");
                }
            }
        }

        // Staff pool (M14): unique ids and non-negative salaries.
        var poolIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var member in carset.StaffPool)
        {
            if (!poolIds.Add(member.Id))
            {
                Error($"duplicate staff-pool id '{member.Id}'");
            }

            if (member.Salary < 0)
            {
                Error($"staff-pool member '{member.Id}' has a negative salary");
            }
        }

        // Research rules (M14): tuning coefficients must be non-negative.
        var research = carset.Rules.Research;
        if (research.BaseProgressPerSeason < 0 || research.StepProgress < 0 || research.FacilityWeight < 0
            || research.StaffWeight < 0 || research.CorrelationBaseline < 0 || research.QuotaPerFacilityLevel < 0
            || research.MaxRetries < 0 || research.RealizationSpread < 0 || research.ReadinessGainPerSeason < 0)
        {
            Error("research rules coefficients must be non-negative");
        }

        // Component allocation (M15): sane quotas, life and reliability influence.
        foreach (var (kind, alloc) in carset.Rules.ComponentAllocation)
        {
            if (alloc < 1)
            {
                Error($"component allocation for '{kind}' must be at least 1");
            }
        }

        foreach (var (kind, life) in carset.Rules.ComponentLifeRounds)
        {
            if (life < 1)
            {
                Error($"component life for '{kind}' must be at least 1 round");
            }
        }

        if (carset.Rules.ComponentReliabilityWearInfluence < 0)
        {
            Error("componentReliabilityWearInfluence must be non-negative");
        }

        return issues;
    }
}
