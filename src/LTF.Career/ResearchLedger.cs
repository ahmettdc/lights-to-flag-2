using LTF.Domain;
using LTF.Domain.Common;
using LTF.Domain.Management;
using LTF.Domain.Racing;
using LTF.Domain.Rnd;

namespace LTF.Career;

/// <summary>
/// Advances every team's R&amp;D by one season (M14): each active development project accrues progress —
/// scaled by the team's facilities and staff — and walks the validation pipeline (ADR-0024); a project
/// that clears validation has its estimated gain applied permanently to the car (via
/// <see cref="CarAxisMap"/>), and the team auto-starts affordable available nodes to fill its free slots,
/// drawing their cost from its budget. The car changes only when a node is approved. Pure and
/// deterministic; with no tech tree or no development budget the carset is returned untouched.
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

        var developments = new List<TeamDevelopment>(carset.Teams.Count);
        var teams = new List<Team>(carset.Teams.Count);
        foreach (var team in carset.Teams)
        {
            var (developed, development) = Develop(carset.TechTree, rules, team);
            teams.Add(developed);
            developments.Add(development);
        }

        return new ResearchOutcome { Carset = carset with { Teams = teams }, Developments = developments };
    }

    private static (Team Team, TeamDevelopment Development) Develop(TechTree tree, ResearchRules rules, Team team)
    {
        var car = team.Car;
        var balance = team.Finances.Balance;
        var startBalance = balance;
        var unlocked = new HashSet<string>(team.Research.UnlockedNodeIds, StringComparer.Ordinal);
        var approved = new List<string>();

        var rate = Rate(rules, team);

        // 1. Advance existing projects; a project that clears validation is applied and unlocked.
        var active = new List<DevelopmentProject>();
        foreach (var project in team.Research.ActiveProjects)
        {
            var advanced = Advance(project, rate, rules);
            if (advanced.State == ValidationState.ApprovedForRace)
            {
                car = Apply(car, advanced);
                unlocked.Add(advanced.NodeId);
                approved.Add(advanced.NodeId);
            }
            else
            {
                active.Add(advanced);
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

            if (unlocked.Contains(node.Id) || activeIds.Contains(node.Id) || !AllMet(node, unlocked))
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

        var research = team.Research with { UnlockedNodeIds = unlocked.ToList(), ActiveProjects = active };
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

    private static DevelopmentProject Advance(DevelopmentProject project, int rate, ResearchRules rules)
    {
        var progress = project.Progress + rate;
        var state = project.State;
        while (IsDeveloping(state) && progress >= rules.StepProgress)
        {
            progress -= rules.StepProgress;
            state = Next(state);
        }

        var advanced = project with { Progress = progress, State = state };

        // M14h: reaching data review approves the project deterministically (validation lands in M14i).
        return state == ValidationState.DataReview
            ? advanced with { State = ValidationState.ApprovedForRace }
            : advanced;
    }

    private static Car Apply(Car car, DevelopmentProject project)
    {
        var gain = Realize(project);
        return CarAxisMap.TargetOf(project.TargetAxis) switch
        {
            CarRatingTarget.Aerodynamics => car with { Aerodynamics = Bump(car.Aerodynamics, gain) },
            CarRatingTarget.Chassis => car with { Chassis = Bump(car.Chassis, gain) },
            CarRatingTarget.PowerUnit => car with { PowerUnit = Bump(car.PowerUnit, gain) },
            CarRatingTarget.TyreGentleness => car with { TyreGentleness = Bump(car.TyreGentleness, gain) },
            CarRatingTarget.Reliability => car with { Reliability = Bump(car.Reliability, gain) },
            _ => car,
        };
    }

    // The realised gain — the estimate midpoint scaled by correlation. M14i adds seeded spread + a
    // pass/fail threshold; here it always lands at the deterministic estimate.
    private static int Realize(DevelopmentProject project)
    {
        var estimate = (project.EstimatedGainMin + project.EstimatedGainMax) / 2.0;
        return (int)Math.Round(estimate * (project.CorrelationPercent / 100.0));
    }

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
}
