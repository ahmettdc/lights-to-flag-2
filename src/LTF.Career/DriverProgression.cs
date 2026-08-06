using LTF.Domain;
using LTF.Domain.Common;
using LTF.Domain.Racing;
using LTF.Simulation;

namespace LTF.Career;

/// <summary>
/// Ages the field and moves each driver along their development curve at season rollover (M18 / ADR-0015).
/// A driver below their peak with headroom grows toward their hidden <see cref="Driver.Potential"/>; a
/// driver past their peak declines, with the physical attributes (Pace, Consistency) fading faster than the
/// experience attributes (Racecraft, TyreManagement, Feedback, WetWeather) — the "cunning veteran" holding
/// on. Through the peak band a driver simply holds their level. Gated behind
/// <see cref="DriverDevelopmentRules.IsActive"/>: a carset that authors no development curve is returned
/// untouched, so aging is inert until content opts in. Deterministic — each driver evolves from its own
/// seeded stream (the season seed forked by an ordinal hash of the driver id); no wall-clock, no shared RNG.
/// </summary>
public static class DriverProgression
{
    public static Carset Advance(Carset carset, int seed)
    {
        var rules = carset.Rules.DriverDevelopment;
        if (!rules.IsActive)
        {
            return carset;
        }

        var root = new DeterministicRandom(seed);
        var drivers = carset.Drivers
            .Select(d => Develop(d, rules, root.Fork(Salt(d.Id))))
            .ToList();

        return carset with { Drivers = drivers };
    }

    private static Driver Develop(Driver driver, DriverDevelopmentRules rules, IRandom rng)
    {
        var age = driver.Age + 1;
        var attributes = driver.Attributes;

        if (age < rules.PeakAgeStart && driver.Potential > attributes.Overall)
        {
            attributes = Grow(attributes, driver.Potential, rules, rng);
        }
        else if (age > rules.PeakAgeEnd)
        {
            attributes = Decline(attributes, rules, age - rules.PeakAgeEnd);
        }

        return driver with { Age = age, Attributes = attributes };
    }

    // Close a fraction of the gap to potential, applied to every attribute (so the headline rating rises by
    // that fraction), with an optional seeded spread. Growth self-tapers as the headroom shrinks.
    private static DriverAttributes Grow(
        DriverAttributes attributes, int potential, DriverDevelopmentRules rules, IRandom rng)
    {
        var headroom = potential - attributes.Overall;
        var noise = rng.NextGaussian() * rules.DevelopmentSpread;
        var gain = (int)Math.Round(
            rules.GrowthPerSeason * headroom * (1.0 + noise), MidpointRounding.AwayFromZero);
        if (gain <= 0)
        {
            return attributes;
        }

        return attributes with
        {
            Pace = Bump(attributes.Pace, gain),
            Racecraft = Bump(attributes.Racecraft, gain),
            Consistency = Bump(attributes.Consistency, gain),
            TyreManagement = Bump(attributes.TyreManagement, gain),
            WetWeather = Bump(attributes.WetWeather, gain),
            Feedback = Bump(attributes.Feedback, gain),
        };
    }

    // Decline past the peak: physical attributes fade faster than experience ones. The per-season step is
    // the difference of the cumulative rounded loss, so a fractional rate accrues correctly over the years
    // (e.g. 0.5/season loses a point every second season) without carrying any state between seasons.
    private static DriverAttributes Decline(DriverAttributes attributes, DriverDevelopmentRules rules, int yearsPast)
    {
        var physical = Step(rules.PhysicalDeclinePerSeason, yearsPast);
        var experience = Step(rules.ExperienceDeclinePerSeason, yearsPast);
        if (physical == 0 && experience == 0)
        {
            return attributes;
        }

        return attributes with
        {
            Pace = Bump(attributes.Pace, -physical),
            Consistency = Bump(attributes.Consistency, -physical),
            Racecraft = Bump(attributes.Racecraft, -experience),
            TyreManagement = Bump(attributes.TyreManagement, -experience),
            WetWeather = Bump(attributes.WetWeather, -experience),
            Feedback = Bump(attributes.Feedback, -experience),
        };
    }

    // Points lost this season for a decline rate: the cumulative rounded loss minus last season's, so a
    // fractional rate accumulates exactly and deterministically (Math.Round is platform-stable).
    private static int Step(double rate, int yearsPast) =>
        (int)Math.Round(rate * yearsPast, MidpointRounding.AwayFromZero)
        - (int)Math.Round(rate * (yearsPast - 1), MidpointRounding.AwayFromZero);

    private static Rating Bump(Rating rating, int delta) => Rating.Clamped(rating.Value + delta);

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
