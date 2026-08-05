using LTF.Domain.Common;

namespace LTF.Domain.Racing;

/// <summary>
/// A person's disposition (ADR-0013), on the shared 1–100 <see cref="Rating"/> scale. It drives how
/// relationships form and shift and how hard someone bargains: a high-<see cref="Ego"/> driver reacts
/// bigger to a slight, a high-<see cref="Loyalty"/> one re-signs cheaply. Static talent that sits
/// alongside <see cref="Driver.Attributes"/>.
/// </summary>
public sealed record Personality
{
    /// <summary>Pride and sensitivity to slights — amplifies relationship swings.</summary>
    public required Rating Ego { get; init; }

    /// <summary>Attachment to a team — softens negotiation demands.</summary>
    public required Rating Loyalty { get; init; }

    /// <summary>Composure under pressure — dampens volatile reactions.</summary>
    public required Rating Temperament { get; init; }

    /// <summary>Drive to win and climb — sharpens rivalry and demands.</summary>
    public required Rating Ambition { get; init; }

    /// <summary>A balanced, unremarkable disposition (all 50) — the default for a driver with no
    /// personality data.</summary>
    public static Personality Neutral { get; } = new()
    {
        Ego = new(50),
        Loyalty = new(50),
        Temperament = new(50),
        Ambition = new(50),
    };
}
