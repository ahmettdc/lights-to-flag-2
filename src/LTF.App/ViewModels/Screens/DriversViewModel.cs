using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using LTF.App.Converters;
using LTF.App.Mvvm;
using LTF.Domain;
using LTF.Domain.Racing;

namespace LTF.App.ViewModels.Screens;

/// <summary>
/// The drivers screen (M21): the player team's race drivers plus the reserve/academy pool as a squad list,
/// and the selected driver's profile (six ratings + biographical facts). Read-only. Contract/relationship
/// panels are transfer-market data (M22), so the profile shows only what the domain carries (ADR-0027).
/// </summary>
public sealed partial class DriversViewModel : ViewModelBase
{
    public DriversViewModel(Carset carset)
    {
        var driverById = carset.Drivers.ToDictionary(d => d.Id, StringComparer.Ordinal);
        var squad = new List<DriverSquadRowViewModel>();

        var playerTeam = carset.PlayerTeam();
        if (playerTeam is not null)
        {
            var accent = TeamAccentFor(carset, playerTeam.Id);
            foreach (var id in playerTeam.DriverIds)
            {
                if (driverById.TryGetValue(id, out var driver))
                {
                    squad.Add(new DriverSquadRowViewModel(driver, playerTeam.Name, "Race", accent));
                }
            }
        }

        foreach (var reserve in carset.Reserves)
        {
            squad.Add(new DriverSquadRowViewModel(reserve, "Reserve", "Academy", ScreenBrushes.Faint));
        }

        Squad = squad;
        HasSquad = squad.Count > 0;
        _selectedDriver = squad.FirstOrDefault();
    }

    public IReadOnlyList<DriverSquadRowViewModel> Squad { get; }

    public bool HasSquad { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Profile))]
    private DriverSquadRowViewModel? _selectedDriver;

    public EntityDetailViewModel? Profile =>
        SelectedDriver is null ? null : EntityDetailViewModel.ForDriver(SelectedDriver.Driver, SelectedDriver.TeamLabel);

    private static IBrush TeamAccentFor(Carset carset, string teamId)
    {
        for (var i = 0; i < carset.Teams.Count; i++)
        {
            if (string.CompareOrdinal(carset.Teams[i].Id, teamId) == 0)
            {
                return ScreenBrushes.TeamAccent(i);
            }
        }

        return ScreenBrushes.Faint;
    }
}

/// <summary>One row in the squad list (M21): a driver with their seat/pool category.</summary>
public sealed class DriverSquadRowViewModel
{
    public DriverSquadRowViewModel(Driver driver, string teamLabel, string category, IBrush accent)
    {
        Driver = driver;
        TeamLabel = teamLabel;
        Category = category;
        Accent = accent;
    }

    public Driver Driver { get; }

    public string TeamLabel { get; }

    public string Category { get; }

    public IBrush Accent { get; }

    public string NumberLabel => Driver.Number > 0 ? Driver.Number.ToString(CultureInfo.InvariantCulture) : "—";

    public string Name => Driver.FullName;

    public string Subtitle => $"{Blank(Driver.Nationality)} · {Driver.Age} · {Category}";

    public int Overall => Driver.Attributes.Overall;

    public IBrush OverallBrush => StatusColorConverter.Classify(Overall);

    private static string Blank(string value) => string.IsNullOrWhiteSpace(value) ? "—" : value;
}
