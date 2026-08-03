using LTF.Domain.Common;

namespace LTF.Domain.Racing;

/// <summary>
/// A specific tyre a series brings to an event — a compound family plus its spec code
/// (the mockup's "SOFT C4", "MEDIUM C3", "HARD C2"). Grip and durability are on the
/// shared 1–100 scale; the tyre wear model (M4) consumes them.
/// </summary>
public sealed record TyreSpec
{
    public required TyreCompound Compound { get; init; }

    /// <summary>Marketing/spec code, e.g. "C4".</summary>
    public string Code { get; init; } = "";

    /// <summary>Peak grip when fresh.</summary>
    public required Rating Grip { get; init; }

    /// <summary>Resistance to wear; higher lasts longer.</summary>
    public required Rating Durability { get; init; }
}
