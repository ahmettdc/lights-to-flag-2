namespace LightsToFlag.Core.Domain;

/// <summary>
/// Series regulations for a carset (see <c>Rules.txt</c>, schema in
/// <c>Carsetmaker/rulesdesc.txt</c>). The record is variable-width: class names,
/// qualifying-session minutes and the two points tables are length-prefixed.
/// </summary>
public sealed record RulesSet
{
    public required string GoverningBody { get; init; }
    public string SeriesName { get; init; } = "";
    public string ChampionshipName { get; init; } = "";
    public string EventTitle { get; init; } = "";
    public CarType CarType { get; init; } = CarType.Single;

    public int DriverCount { get; init; }
    public int SpareCount { get; init; }
    public int CircuitCount { get; init; }
    public int TyresPerCompound { get; init; }
    public int DriversDisplayed { get; init; }
    public int ClassCount { get; init; } = 1;
    public IReadOnlyList<string> ClassNames { get; init; } = Array.Empty<string>();

    public int RetirementAge { get; init; }
    public bool RefuellingAllowed { get; init; }
    public bool TyreChangesAllowed { get; init; }
    public bool QualifyingOnRaceFuel { get; init; }
    public bool QualifyingOnRaceTyre { get; init; }
    public bool BothDryCompoundsRequired { get; init; }
    public bool RaceInRain { get; init; }
    public bool SharedTeamPitBox { get; init; }
    public int PitLaneClosesAfterScLaps { get; init; }
    public int StopGoPenaltySeconds { get; init; }
    public bool FixedDriverNumbers { get; init; }

    public double PointsForPole { get; init; }
    public double PointsForFastestLap { get; init; }
    public double PointsForLeadingLap { get; init; }
    public double PointsForLeadingMostLaps { get; init; }

    public bool KnockoutQualifying { get; init; }
    public bool SingleLapQualifying { get; init; }
    public int QualifyingMaxLapsPerSession { get; init; }
    public int QualifyingSessionCount { get; init; }
    public IReadOnlyList<int> QualifyingSessionMinutes { get; init; } = Array.Empty<int>();
    public bool RollingStart { get; init; }

    /// <summary>Primary points table: points awarded for 1st, 2nd, … downwards.</summary>
    public IReadOnlyList<double> PrimaryPoints { get; init; } = Array.Empty<double>();

    /// <summary>Secondary points table (empty when the series uses a single format).</summary>
    public IReadOnlyList<double> SecondaryPoints { get; init; } = Array.Empty<double>();

    public int EnginesPerSeason { get; init; }
    public int ChassisPerSeason { get; init; }
    public int BallastIncreaseForPodium { get; init; }
    public int BallastReductionOtherwise { get; init; }
    public int MaxBallast { get; init; }

    public int ChaseRound { get; init; }
    public int ChaseDriverCount { get; init; }
    public double ChasePoints { get; init; }
    public bool DriversUnlapUnderSafetyCar { get; init; }
}
