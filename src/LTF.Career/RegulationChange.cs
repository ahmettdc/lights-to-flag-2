using LTF.Domain;
using LTF.Domain.Common;
using LTF.Domain.Management;
using LTF.Domain.Racing;
using LTF.Domain.Rnd;

namespace LTF.Career;

/// <summary>
/// Applies the regulation changes the teams have voted through, at season rollover (M18 / ADR-0010) — the
/// step M17 deferred (it only recorded the ballot). A proposal that passes (re-resolved deterministically
/// via <see cref="RegulationBallot"/>) sets every team back on the car rating it favours, in proportion to
/// how unprepared the team is: <c>(100 − RegulationReadiness) / 100 × Magnitude × RegulationUnreadinessPenalty</c>.
/// A well-prepared team (high readiness) barely moves; an unprepared one drops hard. This is the first and
/// only reader of the <see cref="ResearchState.RegulationReadiness"/> accumulator. With no proposals, or the
/// penalty coefficient left at 0, the carset is returned untouched — inert until a carset opts in.
/// Deterministic; no wall-clock.
/// </summary>
public static class RegulationChange
{
    public static Carset Apply(Carset carset, int seed)
    {
        if (carset.RegulationProposals.Count == 0)
        {
            return carset;
        }

        // Re-resolve every proposal deterministically. The passed ones drive both the development-freeze regime
        // (freeze proposals) and the car-rating setback for the unprepared (magnitude proposals).
        var passed = carset.RegulationProposals
            .Where(p => RegulationBallot.Resolve(carset, p, seed).Passed)
            .ToList();

        // Development freezes (Ri4): the coming season's freeze list is exactly the passed freeze proposals'
        // axes/modes — recomputed each boundary, so an unpassed (or repealed) freeze lapses. Only rewrites the
        // regulations when the list actually changes, so a freeze-free ballot stays byte-identical.
        var freezes = passed
            .Where(p => p.FreezeMode is not null)
            .SelectMany(p => p.FreezeAxes.Select(a => new AxisFreeze { Axis = a, Mode = p.FreezeMode!.Value }))
            .ToList();
        var next = SameFreezes(carset.Regulations.DevelopmentFreezes, freezes)
            ? carset
            : carset with { Regulations = carset.Regulations with { DevelopmentFreezes = freezes } };

        // Car-rating setback for the unprepared (M18): only when the series runs a penalty and something passed.
        var penaltyCoeff = carset.Rules.RegulationUnreadinessPenalty;
        if (penaltyCoeff <= 0.0 || passed.Count == 0)
        {
            return next;
        }

        var teams = next.Teams.Select(t => SetBack(t, passed, penaltyCoeff)).ToList();
        return next with { Teams = teams };
    }

    private static bool SameFreezes(IReadOnlyList<AxisFreeze> a, IReadOnlyList<AxisFreeze> b) =>
        a.Count == b.Count && a.SequenceEqual(b);

    private static Team SetBack(Team team, IReadOnlyList<RegulationProposal> passed, double penaltyCoeff)
    {
        var unreadiness = (100 - team.Research.RegulationReadiness) / 100.0;
        var car = team.Car;
        foreach (var proposal in passed)
        {
            var drop = (int)Math.Round(
                unreadiness * proposal.Magnitude * penaltyCoeff, MidpointRounding.AwayFromZero);
            if (drop > 0)
            {
                car = Drop(car, proposal.FavoredAxis, drop);
            }
        }

        return car == team.Car ? team : team with { Car = car };
    }

    private static Car Drop(Car car, CarAxis axis, int amount) => CarAxisMap.TargetOf(axis) switch
    {
        CarRatingTarget.Aerodynamics => car with { Aerodynamics = Bump(car.Aerodynamics, -amount) },
        CarRatingTarget.Chassis => car with { Chassis = Bump(car.Chassis, -amount) },
        CarRatingTarget.PowerUnit => car with { PowerUnit = Bump(car.PowerUnit, -amount) },
        CarRatingTarget.TyreGentleness => car with { TyreGentleness = Bump(car.TyreGentleness, -amount) },
        CarRatingTarget.Reliability => car with { Reliability = Bump(car.Reliability, -amount) },
        _ => car, // PitOperations — no live car rating to move
    };

    private static Rating Bump(Rating rating, int delta) => Rating.Clamped(rating.Value + delta);
}
