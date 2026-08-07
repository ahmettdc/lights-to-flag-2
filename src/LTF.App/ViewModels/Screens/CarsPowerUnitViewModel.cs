using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia.Media;
using LTF.App.Converters;
using LTF.App.Mvvm;
using LTF.Domain;

namespace LTF.App.ViewModels.Screens;

/// <summary>
/// The cars &amp; power-unit screen (M22e): the player team's car performance across the five rating axes
/// (with the pace-weighted overall and the engine supplier) and its life-limited component pool (each
/// component's reliability, season usage against the allocation, and current wear). Read-only projection
/// over the current carset; a Continue rebuilds it.
/// </summary>
public sealed class CarsPowerUnitViewModel : ViewModelBase
{
    public CarsPowerUnitViewModel(Carset carset)
    {
        var team = carset.PlayerTeam();
        HasTeam = team is not null;
        var car = team?.Car;

        Ratings =
        [
            Rating("Aerodynamics", car?.Aerodynamics.Value ?? 0),
            Rating("Chassis", car?.Chassis.Value ?? 0),
            Rating("Power Unit", car?.PowerUnit.Value ?? 0),
            Rating("Tyre Gentleness", car?.TyreGentleness.Value ?? 0),
            Rating("Reliability", car?.Reliability.Value ?? 0),
        ];

        Overall = car?.Overall ?? 0;
        OverallBrush = StatusColorConverter.Classify(Overall);
        EngineSupplier = car?.EngineSupplier ?? "";
        HasEngineSupplier = EngineSupplier.Length > 0;

        var allocation = carset.Rules.ComponentAllocation;
        Components = (team?.Components ?? [])
            .Select(c =>
            {
                var quota = allocation.TryGetValue(c.Kind, out var q) ? q : 0;
                var wear = (int)Math.Round(Math.Clamp(c.CurrentWear, 0.0, 1.0) * 100);
                return new ComponentRowViewModel(
                    c.Kind.ToString(),
                    c.Reliability.Value,
                    quota > 0
                        ? string.Create(CultureInfo.InvariantCulture, $"{c.UnitsUsed}/{quota}")
                        : string.Create(CultureInfo.InvariantCulture, $"{c.UnitsUsed}"),
                    wear,
                    StatusColorConverter.Classify(100 - wear));
            })
            .ToList();
        HasComponents = Components.Count > 0;
    }

    public bool HasTeam { get; }

    public IReadOnlyList<CarRatingRowViewModel> Ratings { get; }

    public int Overall { get; }

    public IBrush OverallBrush { get; }

    public string EngineSupplier { get; }

    public bool HasEngineSupplier { get; }

    public IReadOnlyList<ComponentRowViewModel> Components { get; }

    public bool HasComponents { get; }

    public bool NoComponents => !HasComponents;

    private static CarRatingRowViewModel Rating(string name, int value) =>
        new(name, value, 96.0 * Math.Clamp(value, 0, 100) / 100.0, StatusColorConverter.Classify(value));
}

/// <summary>One car-performance axis on the cars screen (M22e).</summary>
public sealed record CarRatingRowViewModel(string Name, int Value, double BarWidth, IBrush Brush);

/// <summary>One life-limited component on the cars screen (M22e).</summary>
public sealed record ComponentRowViewModel(string Kind, int Reliability, string Usage, int WearPercent, IBrush WearBrush);
