namespace LightsToFlag.Core.Domain;

/// <summary>
/// A reserve / rookie driver in the spare pool. Carsets author these with the
/// abbreviated schema in <c>Carsetmaker/rookiedesc.txt</c>
/// (<c>First Name_Last Name_Age_nationality_Bias</c>) rather than the full
/// 24-field driver record, so they are modelled separately from
/// <see cref="DriverRating"/>.
/// </summary>
public sealed record RookieRating
{
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public string FullName => $"{FirstName} {LastName}".Trim();

    public int Age { get; init; }
    public string Nationality { get; init; } = "";

    /// <summary>Optional authoring hint (free-text/number as authored); often blank.</summary>
    public string Bias { get; init; } = "";
}
