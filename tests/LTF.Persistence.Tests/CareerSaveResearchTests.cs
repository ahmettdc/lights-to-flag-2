using System.Linq;
using LTF.Career;
using LTF.Domain;
using LTF.Domain.Common;
using LTF.Domain.Management;
using LTF.Domain.Racing;
using LTF.Domain.Rnd;
using Xunit;

namespace LTF.Persistence.Tests;

public class CareerSaveResearchTests
{
    private static CareerState Sample() => new()
    {
        Seed = 14,
        Date = new DateOnly(2025, 12, 3),
        Research =
        [
            new TeamResearchRecord
            {
                TeamId = "alpha",
                Aerodynamics = 64, Chassis = 58, PowerUnit = 71, TyreGentleness = 55, Reliability = 80,
                DesignOffice = 5, WindTunnel = 4, Cfd = 5, CompositeManufacturing = 3, MechanicalWorkshop = 4,
                QualityControl = 2, Simulator = 5, Dyno = 3, PitCrewCentre = 4, DataCentre = 2,
                RegulationReadiness = 40, AeroLean = 30, PowertrainLean = -20,
                UnlockedNodeIds = ["aero1", "aero2"],
                ActiveProjects =
                [
                    new DevelopmentProject
                    {
                        NodeId = "chassis1", State = ValidationState.DataReview, TargetAxis = CarAxis.MechanicalGrip,
                        EstimatedGainMin = 8, EstimatedGainMax = 12, Confidence = 70, CorrelationPercent = 85,
                        Progress = 40, RetriesLeft = 2,
                    },
                ],
            },
            new TeamResearchRecord { TeamId = "bravo" }, // no progress → defaults
        ],
    };

    [Fact]
    public void A_round_trip_preserves_team_research()
    {
        var loaded = CareerStore.Deserialize(CareerStore.Serialize(Sample()));

        var alpha = loaded.Research.Single(r => r.TeamId == "alpha");
        Assert.Equal(64, alpha.Aerodynamics);
        Assert.Equal(71, alpha.PowerUnit);
        Assert.Equal(55, alpha.TyreGentleness);
        Assert.Equal(5, alpha.DesignOffice);
        Assert.Equal(2, alpha.DataCentre);
        Assert.Equal(40, alpha.RegulationReadiness);
        Assert.Equal(30, alpha.AeroLean);
        Assert.Equal(-20, alpha.PowertrainLean);
        Assert.Equal(2, alpha.UnlockedNodeIds.Count);
        Assert.Contains("aero1", alpha.UnlockedNodeIds);
        Assert.Contains("aero2", alpha.UnlockedNodeIds);

        var project = alpha.ActiveProjects.Single();
        Assert.Equal("chassis1", project.NodeId);
        Assert.Equal(ValidationState.DataReview, project.State);
        Assert.Equal(CarAxis.MechanicalGrip, project.TargetAxis);
        Assert.Equal(85, project.CorrelationPercent);
        Assert.Equal(2, project.RetriesLeft);

        var bravo = loaded.Research.Single(r => r.TeamId == "bravo");
        Assert.Empty(bravo.UnlockedNodeIds); // a record without progress round-trips as empty
        Assert.Empty(bravo.ActiveProjects);
    }

    [Fact]
    public void Serialization_stays_byte_stable_with_research()
    {
        var once = CareerStore.Serialize(Sample());
        var twice = CareerStore.Serialize(CareerStore.Deserialize(once));

        Assert.Equal(once, twice);
    }

    [Fact]
    public void Capture_then_restore_carries_research_back_to_the_carset()
    {
        var played = DevelopedCarset(); // alpha carries evolved car, facilities and research

        var state = CareerState.Capture(played, new DateOnly(2025, 9, 1), 7);

        // A fresh copy with the team's R&D progress wiped back to defaults.
        var fresh = played with
        {
            Teams =
            [
                played.Teams[0] with
                {
                    Car = Flat(50), Facilities = Facilities.Default, Research = ResearchState.Empty,
                },
            ],
        };
        Assert.Equal(50, fresh.Teams[0].Car.Aerodynamics.Value);   // sanity: really wiped
        Assert.Empty(fresh.Teams[0].Research.UnlockedNodeIds);

        var resumed = state.RestoreInto(fresh);
        var alpha = resumed.Teams[0];

        Assert.Equal(64, alpha.Car.Aerodynamics.Value);
        Assert.Equal(80, alpha.Car.Reliability.Value);
        Assert.Equal(5, alpha.Facilities.DesignOffice.Value);
        Assert.Equal(2, alpha.Facilities.DataCentre.Value);
        Assert.Contains("aero1", alpha.Research.UnlockedNodeIds);
        Assert.Equal(40, alpha.Research.RegulationReadiness);
        Assert.Equal(30, alpha.Research.Concept.AeroLean);
        Assert.Equal("chassis1", alpha.Research.ActiveProjects.Single().NodeId);
    }

    [Fact]
    public void A_carset_without_a_tech_tree_captures_no_research()
    {
        // The R&D snapshot is only worth storing once a series runs a tech tree (byte-identity gate).
        var noTree = DevelopedCarset() with { TechTree = TechTree.Empty };

        var state = CareerState.Capture(noTree, new DateOnly(2025, 9, 1), 7);

        Assert.Empty(state.Research);
    }

    private static Carset DevelopedCarset() => new()
    {
        Id = "mini",
        Name = "Mini",
        Rules = new RulesSet { SeriesName = "S", Points = new PointsScheme { RacePoints = [25, 18] } },
        TechTree = new TechTree
        {
            Departments = [new Department { Id = "aero", Name = "Aero" }],
            Nodes = [new TechNode { Id = "aero1", DepartmentId = "aero", Category = CarAxis.AeroLowSpeed }],
        },
        Teams =
        [
            new Team
            {
                Id = "alpha", Name = "Alpha", DriverIds = ["d1"],
                Car = new Car
                {
                    Aerodynamics = new(64), Chassis = new(58), PowerUnit = new(71),
                    TyreGentleness = new(55), Reliability = new(80),
                },
                Facilities = new Facilities
                {
                    DesignOffice = new(5), WindTunnel = new(4), Cfd = new(5), CompositeManufacturing = new(3),
                    MechanicalWorkshop = new(4), QualityControl = new(2), Simulator = new(5), Dyno = new(3),
                    PitCrewCentre = new(4), DataCentre = new(2),
                },
                Research = new ResearchState
                {
                    UnlockedNodeIds = ["aero1"],
                    ActiveProjects =
                    [
                        new DevelopmentProject
                        {
                            NodeId = "chassis1", State = ValidationState.InManufacture,
                            TargetAxis = CarAxis.MechanicalGrip, EstimatedGainMin = 8, EstimatedGainMax = 12,
                            Progress = 40, RetriesLeft = 2,
                        },
                    ],
                    Concept = new ConceptDirection { AeroLean = 30, PowertrainLean = -20 },
                    RegulationReadiness = 40,
                },
            },
        ],
        Drivers =
        [
            new Driver { Id = "d1", FirstName = "Ada", LastName = "One", Age = 24, Attributes = Attrs() },
        ],
        Circuits = [],
        Calendar = [],
    };

    private static Car Flat(int v) => new()
    {
        Aerodynamics = new(v), Chassis = new(v), PowerUnit = new(v), TyreGentleness = new(v), Reliability = new(v),
    };

    private static DriverAttributes Attrs() => new()
    {
        Pace = new(70), Racecraft = new(70), Consistency = new(70),
        TyreManagement = new(70), WetWeather = new(70), Feedback = new(70),
    };
}
