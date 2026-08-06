using LTF.Domain.Common;
using LTF.Domain.Racing;
using LTF.Simulation;

namespace LTF.Career;

/// <summary>
/// Generates a fictional young driver to fill a seat the transfer market cannot fill from the existing pool
/// (M18 / ADR-0017). Names, nationality, age (18–22), attributes and a hidden <see cref="Driver.Potential"/>
/// ceiling above the rookie's current level are all drawn from a seeded stream forked by the requested id,
/// so the same seed and id always produce the same rookie and distinct ids diverge. Fully deterministic and
/// I/O-free — the name parts are in-code tables, never a data file. Content is invented (ADR-0003): no real
/// people.
/// </summary>
public static class RookieGenerator
{
    private static readonly string[] FirstParts =
        ["mar", "lo", "en", "ka", "ri", "to", "va", "le", "ni", "sa", "dro", "fen", "cas", "bel", "tor", "mi", "rael", "ro", "dan", "su"];

    private static readonly string[] LastParts =
        ["ven", "kov", "ton", "sar", "ell", "mann", "ric", "dal", "son", "strom", "ini", "well", "berg", "court", "ova", "ade", "lund", "sky", "ner", "ez"];

    private static readonly string[] Nationalities =
        ["GB", "IT", "DE", "FR", "BR", "JP", "US", "ES", "AU", "NL", "FI", "CA", "MX", "BE", "DK"];

    public static Driver Generate(int seed, string id)
    {
        var rng = new DeterministicRandom(seed).Fork(Salt(id));

        var first = Title(FirstParts[rng.NextInt(0, FirstParts.Length)] + FirstParts[rng.NextInt(0, FirstParts.Length)]);
        var last = Title(LastParts[rng.NextInt(0, LastParts.Length)] + LastParts[rng.NextInt(0, LastParts.Length)]);
        var nationality = Nationalities[rng.NextInt(0, Nationalities.Length)];
        var age = rng.NextInt(18, 23);

        var mean = rng.NextInt(56, 68);
        var attributes = new DriverAttributes
        {
            Pace = Attribute(rng, mean),
            Racecraft = Attribute(rng, mean),
            Consistency = Attribute(rng, mean),
            TyreManagement = Attribute(rng, mean),
            WetWeather = Attribute(rng, mean),
            Feedback = Attribute(rng, mean),
        };

        // A rookie has upside: a potential ceiling several points above where they start today.
        var potential = Math.Min(Rating.Max, attributes.Overall + rng.NextInt(6, 26));

        return new Driver
        {
            Id = id,
            FirstName = first,
            LastName = last,
            Age = age,
            Nationality = nationality,
            Attributes = attributes,
            Potential = potential,
        };
    }

    private static Rating Attribute(IRandom rng, int mean) => Rating.Clamped(mean + rng.NextInt(-4, 5));

    private static string Title(string part) => char.ToUpperInvariant(part[0]) + part[1..];

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
