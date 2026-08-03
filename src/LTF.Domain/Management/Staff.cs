using LTF.Domain.Common;

namespace LTF.Domain.Management;

/// <summary>
/// A technical or trackside staff member (the mockup's Technical Director, Chief
/// Aerodynamicist, Chief Strategist, Race Engineer). Their skill feeds the R&D and
/// strategy layers (M14, M7); a vacant seat is modelled as the absence of a member.
/// </summary>
public sealed record Staff
{
    public required string Id { get; init; }
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public string FullName => $"{FirstName} {LastName}".Trim();

    public required StaffRole Role { get; init; }
    public required Rating Skill { get; init; }

    public string Nationality { get; init; } = "";
    public int Age { get; init; }

    /// <summary>Annual salary, in the carset's currency unit.</summary>
    public long Salary { get; init; }
}
