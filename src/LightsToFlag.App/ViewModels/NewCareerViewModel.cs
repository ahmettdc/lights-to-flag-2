using System.Collections.Generic;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LightsToFlag.App.Services;
using LightsToFlag.Core.Domain;

namespace LightsToFlag.App.ViewModels;

/// <summary>A driver the player can choose to control.</summary>
public sealed record DriverPick(string Id, string Name, string Team, string Nationality);

/// <summary>New-career setup: pick a carset, pick a driver to control, choose a seed.</summary>
public partial class NewCareerViewModel : ObservableObject
{
    private readonly INavigationService _nav;
    private readonly GameSession _session;
    private Carset? _previewCarset;

    public NewCareerViewModel(INavigationService nav, GameSession session)
    {
        _nav = nav;
        _session = session;

        foreach (var carset in _session.AvailableCarsets())
        {
            Carsets.Add(carset);
        }

        SelectedCarset = Carsets.Count > 0 ? Carsets[0] : null;
        Seed = 2024;
    }

    public ObservableCollection<CarsetInfo> Carsets { get; } = new();
    public ObservableCollection<DriverPick> Drivers { get; } = new();

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartCommand))]
    private CarsetInfo? _selectedCarset;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartCommand))]
    private DriverPick? _selectedDriver;

    [ObservableProperty]
    private int _seed;

    [ObservableProperty]
    private string? _error;

    partial void OnSelectedCarsetChanged(CarsetInfo? value)
    {
        Drivers.Clear();
        SelectedDriver = null;
        Error = null;
        if (value is null)
        {
            return;
        }

        try
        {
            _previewCarset = _session.LoadCarset(value);
            for (var i = 0; i < _previewCarset.Drivers.Count; i++)
            {
                var d = _previewCarset.Drivers[i];
                var teamIndex = System.Math.Clamp(d.TeamNumber - 1, 0, System.Math.Max(0, _previewCarset.Teams.Count - 1));
                var team = _previewCarset.Teams.Count > 0 ? _previewCarset.Teams[teamIndex].Name : "";
                Drivers.Add(new DriverPick($"D{i + 1}", d.FullName, team, d.Nationality));
            }

            SelectedDriver = Drivers.Count > 0 ? Drivers[0] : null;
        }
        catch (System.Exception ex)
        {
            Error = "Could not read carset: " + ex.Message;
        }
    }

    private bool CanStart() => SelectedCarset is not null && SelectedDriver is not null;

    [RelayCommand(CanExecute = nameof(CanStart))]
    private void Start()
    {
        if (SelectedCarset is null || SelectedDriver is null)
        {
            return;
        }

        try
        {
            _session.StartNewCareer(SelectedCarset, SelectedDriver.Id, Seed);
            _nav.NavigateTo<CareerViewModel>();
        }
        catch (System.Exception ex)
        {
            Error = "Could not start career: " + ex.Message;
        }
    }

    [RelayCommand]
    private void Back() => _nav.NavigateTo<MainMenuViewModel>();
}
