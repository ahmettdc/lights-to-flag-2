using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LTF.App.Converters;
using LTF.App.Mvvm;
using LTF.Domain;
using LTF.Domain.Management;
using LTF.Domain.Rnd;

namespace LTF.App.ViewModels.Screens;

/// <summary>
/// The R&amp;D and facilities screen (M22d): the player team's development infrastructure (the ten
/// facilities and their levels) and its research state (regulation readiness, the car-concept lean, the
/// in-flight development projects and how much of the tech tree is unlocked). Projects a read-only view of the
/// current carset (a Continue rebuilds it) and, from Ri2, lets the player <em>steer</em> the car concept: the
/// aero/powertrain lean sliders write <see cref="ConceptDirection"/> onto the season-start carset through the
/// supplied callback, and the live per-round R&amp;D (Model B) develops that season toward the chosen lean.
/// </summary>
public sealed partial class RndFacilitiesViewModel : ViewModelBase
{
    private readonly Action<int, int>? _setConcept;

    public RndFacilitiesViewModel(Carset carset, Action<int, int>? setConcept = null)
    {
        _setConcept = setConcept;

        var team = carset.PlayerTeam();
        HasTeam = team is not null;

        var facilities = team?.Facilities ?? LTF.Domain.Management.Facilities.Default;
        Facilities =
        [
            Facility("Design Office", facilities.DesignOffice),
            Facility("Wind Tunnel", facilities.WindTunnel),
            Facility("CFD", facilities.Cfd),
            Facility("Composite Mfg.", facilities.CompositeManufacturing),
            Facility("Mechanical Workshop", facilities.MechanicalWorkshop),
            Facility("Quality Control", facilities.QualityControl),
            Facility("Simulator", facilities.Simulator),
            Facility("Dyno", facilities.Dyno),
            Facility("Pit-Crew Centre", facilities.PitCrewCentre),
            Facility("Data Centre", facilities.DataCentre),
        ];

        var research = team?.Research ?? ResearchState.Empty;
        RegulationReadiness = research.RegulationReadiness;
        ReadinessText = string.Create(CultureInfo.InvariantCulture, $"{research.RegulationReadiness}%");
        ReadinessBrush = StatusColorConverter.Classify(research.RegulationReadiness);
        ReadinessBarWidth = 96.0 * research.RegulationReadiness / 100.0;

        AeroLeanText = LeanText(research.Concept.AeroLean, "Low drag", "High downforce");
        PowertrainLeanText = LeanText(research.Concept.PowertrainLean, "Efficiency", "Peak power");
        _aeroLeanValue = research.Concept.AeroLean;
        _powertrainLeanValue = research.Concept.PowertrainLean;
        CanSteerConcept = _setConcept is not null && HasTeam;

        var unlocked = research.UnlockedNodeIds.Count;
        var total = carset.TechTree.Nodes.Count;
        NodesText = string.Create(CultureInfo.InvariantCulture, $"{unlocked} / {total}");

        Projects = research.ActiveProjects
            .Select(p => new ProjectRowViewModel(
                Spaced(p.TargetAxis.ToString()),
                p.NodeId,
                Spaced(p.State.ToString()),
                p.Progress,
                96.0 * System.Math.Clamp(p.Progress, 0, 100) / 100.0,
                string.Create(CultureInfo.InvariantCulture, $"+{p.EstimatedGainMin}–{p.EstimatedGainMax}"),
                StateBrush(p.State)))
            .ToList();
        HasProjects = Projects.Count > 0;
    }

    public bool HasTeam { get; }

    public IReadOnlyList<FacilityRowViewModel> Facilities { get; }

    public int RegulationReadiness { get; }

    public string ReadinessText { get; }

    public IBrush ReadinessBrush { get; }

    public double ReadinessBarWidth { get; }

    public string AeroLeanText { get; }

    public string PowertrainLeanText { get; }

    public string NodesText { get; }

    public IReadOnlyList<ProjectRowViewModel> Projects { get; }

    public bool HasProjects { get; }

    public bool NoProjects => !HasProjects;

    // --- Concept steer (Ri2) ---

    /// <summary>Whether the concept-steer panel is offered (a wired callback + a player team).</summary>
    public bool CanSteerConcept { get; }

    [ObservableProperty]
    private double _aeroLeanValue;

    [ObservableProperty]
    private double _powertrainLeanValue;

    /// <summary>The aero lean the slider currently sits on (−100 low drag … +100 high downforce).</summary>
    public int AeroLean => (int)AeroLeanValue;

    /// <summary>The powertrain lean the slider currently sits on (−100 efficiency … +100 peak power).</summary>
    public int PowertrainLean => (int)PowertrainLeanValue;

    partial void OnAeroLeanValueChanged(double value) => OnPropertyChanged(nameof(AeroLean));

    partial void OnPowertrainLeanValueChanged(double value) => OnPropertyChanged(nameof(PowertrainLean));

    // Land the chosen lean on the season-start carset; the live R&D (Model B) then develops that season toward
    // it. Concept never feeds the race sim, so this is reconstruction-neutral (it re-steers the whole season).
    [RelayCommand(CanExecute = nameof(CanSteerConcept))]
    private void ApplyConcept() => _setConcept?.Invoke(AeroLean, PowertrainLean);

    private static FacilityRowViewModel Facility(string name, LTF.Domain.Common.FacilityLevel level) =>
        new(name, level.Value, 96.0 * level.Value / 5.0);

    // A signed lean (−100…+100) as a human label: the pole it leans toward, or "Balanced" near zero.
    private static string LeanText(int lean, string low, string high)
    {
        if (lean <= -20)
        {
            return string.Create(CultureInfo.InvariantCulture, $"{low} ({lean})");
        }

        if (lean >= 20)
        {
            return string.Create(CultureInfo.InvariantCulture, $"{high} (+{lean})");
        }

        return "Balanced";
    }

    // Split a PascalCase enum name into words for display (e.g. ReadyForTrackTest → "Ready For Track Test").
    private static string Spaced(string pascal)
    {
        var sb = new System.Text.StringBuilder(pascal.Length + 4);
        for (var i = 0; i < pascal.Length; i++)
        {
            if (i > 0 && char.IsUpper(pascal[i]))
            {
                sb.Append(' ');
            }

            sb.Append(pascal[i]);
        }

        return sb.ToString();
    }

    private static IBrush StateBrush(ValidationState state) => state switch
    {
        ValidationState.ApprovedForRace => ScreenBrushes.Good,
        ValidationState.Rework or ValidationState.Abandoned => ScreenBrushes.Bad,
        _ => ScreenBrushes.Warn,
    };
}

/// <summary>One facility row on the R&amp;D screen (M22d): name + level 1–5 with a bar.</summary>
public sealed record FacilityRowViewModel(string Name, int Level, double BarWidth);

/// <summary>One in-flight development project on the R&amp;D screen (M22d).</summary>
public sealed record ProjectRowViewModel(
    string Axis, string NodeId, string State, int Progress, double ProgressBarWidth, string Gain, IBrush StateBrush);
