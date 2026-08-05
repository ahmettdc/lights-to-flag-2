using LTF.Domain;
using LTF.Domain.Common;
using LTF.Domain.Management;
using LTF.Domain.Racing;
using LTF.Domain.Rnd;
using LTF.Simulation;

namespace LTF.Career;

/// <summary>
/// Advances every team's R&amp;D by one season (M14). Each active development project accrues progress —
/// scaled by the team's facilities and staff — and walks the validation pipeline (ADR-0024). When a
/// project reaches data review its gain is realised (the estimate scaled by correlation, with a seeded
/// spread): if it clears the approval bar it is <b>Approved for Race</b> and its gain is applied
/// permanently to the car (via <see cref="CarAxisMap"/>); otherwise it is reworked, or abandoned once its
/// retries run out. The car changes only on approval. Teams auto-start affordable, prerequisite-met nodes
/// to fill free slots, and accrue regulation readiness. Deterministic (seeded); with no tech tree or no
/// development budget the carset is returned untouched.
/// </summary>
public static class ResearchLedger
{
    public static ResearchOutcome DevelopSeason(Carset carset, int seed)
    {
        var rules = carset.Rules.Research;
        if (carset.TechTree.Nodes.Count == 0 || rules.BaseProgressPerSeason <= 0)
        {
            return new ResearchOutcome { Carset = carset };
        }

        var root = new DeterministicRandom(seed);
        var developments = new List<TeamDevelopment>(carset.Teams.Count);
        var teams = new List<Team>(carset.Teams.Count);
        foreach (var team in carset.Teams)
        {
            var (developed, development) = Develop(carset.TechTree, rules, team, root.Fork(Salt(team.Id)));
            teams.Add(developed);
            developments.Add(development);
        }

        return new ResearchOutcome { Carset = carset with { Teams = teams }, Developments = developments };
    }

    private static (Team Team, TeamDevelopment Development) Develop(
        TechTree tree, ResearchRules rules, Team team, IRandom rng)
    {
        var car = team.Car;
        var balance = team.Finances.Balance;
        var startBalance = balance;
        var unlocked = new HashSet<string>(team.Research.UnlockedNodeIds, StringComparer.Ordinal);
        var abandonedIds = new HashSet<string>(StringComparer.Ordinal);
        var approved = new List<string>();
        var abandoned = 0;

        var rate = Rate(rules, team);

        // 1. Advance existing projects; resolve those that reach validation.
        var active = new List<DevelopmentProject>();
        foreach (var project in team.Research.ActiveProjects)
        {
            var advanced = Advance(project, rate, rules);
            if (advanced.State != ValidationState.DataReview)
            {
                active.Add(advanced);
                continue;
            }

            var realised = Realize(advanced, rules, rng.Fork(Salt(advanced.NodeId)));
            if (realised >= advanced.EstimatedGainMin * rules.ApproveThreshold)
            {
                car = Apply(car, advanced, (int)Math.Round(realised));
                unlocked.Add(advanced.NodeId);
                approved.Add(advanced.NodeId);
            }
            else if (advanced.RetriesLeft > 0)
            {
                active.Add(advanced with
                {
                    State = ValidationState.InManufacture,
                    RetriesLeft = advanced.RetriesLeft - 1,
                    Progress = 0,
                });
            }
            else
            {
                abandoned++;
                abandonedIds.Add(advanced.NodeId);
            }
        }

        // 2. Fill free slots with affordable, prerequisite-met nodes (deterministic catalog order).
        var capacity = Math.Max(0, rules.BaseActiveProjects);
        var activeIds = new HashSet<string>(active.Select(p => p.NodeId), StringComparer.Ordinal);
        foreach (var node in tree.Nodes)
        {
            if (active.Count >= capacity)
            {
                break;
            }

            if (unlocked.Contains(node.Id) || activeIds.Contains(node.Id) || abandonedIds.Contains(node.Id)
                || !AllMet(node, unlocked))
            {
                continue;
            }

            var cost = Cost(node, rules);
            if (cost > balance)
            {
                continue;
            }

            balance -= cost;
            active.Add(Start(node, rules));
            activeIds.Add(node.Id);
        }

        var readiness = Math.Clamp(team.Research.RegulationReadiness + rules.ReadinessGainPerSeason, 0, 100);
        var research = team.Research with
        {
            UnlockedNodeIds = unlocked.ToList(),
            ActiveProjects = active,
            RegulationReadiness = readiness,
        };
        var newTeam = team with
        {
            Car = car,
            Finances = team.Finances with { Balance = balance },
            Research = research,
        };
        var development = new TeamDevelopment
        {
            TeamId = team.Id,
            BudgetSpent = startBalance - balance,
            NodesApproved = approved.Count,
            NodesAbandoned = abandoned,
            ApprovedNodeIds = approved,
        };
        return (newTeam, development);
    }

    // Progress per season, scaled up by how strong (and how heavily weighted) a team's facilities and staff are.
    private static int Rate(ResearchRules rules, Team team)
    {
        var factor = (1.0 + (rules.FacilityWeight * AverageFacility(team.Facilities)))
                     * (1.0 + (rules.StaffWeight * AverageStaff(team.Staff)));
        return (int)(rules.BaseProgressPerSeason * factor);
    }

    // Walk the pipeline as far as this season's progress carries it, stopping at data review (the gate).
    private static DevelopmentProject Advance(DevelopmentProject project, int rate, ResearchRules rules)
    {
        var progress = project.Progress + rate;
        var state = project.State;
        while (IsDeveloping(state) && progress >= rules.StepProgress)
        {
            progress -= rules.StepProgress;
            state = Next(state);
        }

        return project with { Progress = progress, State = state };
    }

    // The realised gain: the estimate midpoint, scaled by baseline × node correlation, with a seeded spread.
    private static double Realize(DevelopmentProject project, ResearchRules rules, IRandom rng)
    {
        var estimate = (project.EstimatedGainMin + project.EstimatedGainMax) / 2.0;
        var correlation = (rules.CorrelationBaseline / 100.0) * (project.CorrelationPercent / 100.0);
        var noise = rng.NextGaussian() * rules.RealizationSpread;
        return estimate * correlation * (1.0 + noise);
    }

    private static Car Apply(Car car, DevelopmentProject project, int gain) =>
        CarAxisMap.TargetOf(project.TargetAxis) switch
        {
            CarRatingTarget.Aerodynamics => car with { Aerodynamics = Bump(car.Aerodynamics, gain) },
            CarRatingTarget.Chassis => car with { Chassis = Bump(car.Chassis, gain) },
            CarRatingTarget.PowerUnit => car with { PowerUnit = Bump(car.PowerUnit, gain) },
            CarRatingTarget.TyreGentleness => car with { TyreGentleness = Bump(car.TyreGentleness, gain) },
            CarRatingTarget.Reliability => car with { Reliability = Bump(car.Reliability, gain) },
            _ => car,
        };

    private static Rating Bump(Rating rating, int gain) => Rating.Clamped(rating.Value + gain);

    private static bool AllMet(TechNode node, HashSet<string> unlocked)
    {
        foreach (var prerequisite in node.Prerequisites)
        {
            if (!unlocked.Contains(prerequisite))
            {
                return false;
            }
        }

        return true;
    }

    private static long Cost(TechNode node, ResearchRules rules) =>
        (long)(node.Cost * SizeMultiplier(node.Size, rules));

    private static double SizeMultiplier(NodeSize size, ResearchRules rules) => size switch
    {
        NodeSize.Minor => rules.MinorCostMultiplier,
        NodeSize.Major => rules.MajorCostMultiplier,
        NodeSize.Ultimate => rules.UltimateCostMultiplier,
        _ => 1.0,
    };

    private static DevelopmentProject Start(TechNode node, ResearchRules rules) => new()
    {
        NodeId = node.Id,
        State = ValidationState.InDesign,
        TargetAxis = node.Category,
        EstimatedGainMin = node.GainMin,
        EstimatedGainMax = node.GainMax,
        Confidence = node.Confidence,
        CorrelationPercent = node.Correlation,
        Progress = 0,
        RetriesLeft = rules.MaxRetries,
    };

    private static bool IsDeveloping(ValidationState state) =>
        state is ValidationState.InDesign or ValidationState.InManufacture
            or ValidationState.ReadyForTrackTest or ValidationState.FittedForPractice;

    private static ValidationState Next(ValidationState state) => state switch
    {
        ValidationState.InDesign => ValidationState.InManufacture,
        ValidationState.InManufacture => ValidationState.ReadyForTrackTest,
        ValidationState.ReadyForTrackTest => ValidationState.FittedForPractice,
        ValidationState.FittedForPractice => ValidationState.DataReview,
        _ => state,
    };

    private static double AverageFacility(Facilities f) =>
        (f.DesignOffice.Normalized + f.WindTunnel.Normalized + f.Cfd.Normalized + f.CompositeManufacturing.Normalized
         + f.MechanicalWorkshop.Normalized + f.QualityControl.Normalized + f.Simulator.Normalized + f.Dyno.Normalized
         + f.PitCrewCentre.Normalized + f.DataCentre.Normalized) / 10.0;

    private static double AverageStaff(IReadOnlyList<Staff> staff)
    {
        if (staff.Count == 0)
        {
            return 0.0;
        }

        var sum = 0.0;
        foreach (var member in staff)
        {
            sum += member.Skill.Normalized;
        }

        return sum / staff.Count;
    }

    // A stable, ordinal FNV-1a hash of an id → fork salt. Never String.GetHashCode (process-randomised).
    private static long Salt(string id)
    {
        unchecked
        {
            var hash = 1469598103934665603UL; // FNV-1a offset basis
            foreach (var c in id)
            {
                hash ^= c;
                hash *= 1099511628211UL; // FNV-1a prime
            }

            return (long)hash;
        }
    }
}
