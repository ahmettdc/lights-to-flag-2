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
using LTF.Domain.Racing;

namespace LTF.App.ViewModels.Screens;

/// <summary>Which slice of the carset the database browser is showing (M21).</summary>
public enum DatabaseTab
{
    Drivers,
    Juniors,
    Teams,
}

/// <summary>
/// The database screen (M21): a read-only reference browser over the carset — race drivers, the junior /
/// reserve pool, and teams — with a detail panel for the selected row. Wage, scouting and transfer actions
/// are M22, so this is browse-only (the STAFF tab is M22 data too, and omitted).
/// </summary>
public sealed partial class DatabaseViewModel : ViewModelBase
{
    private readonly Carset _carset;
    private readonly Dictionary<string, string> _teamName = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> _teamOfDriver = new(StringComparer.Ordinal);
    private readonly Dictionary<string, IBrush> _teamAccent = new(StringComparer.Ordinal);
    private readonly Dictionary<string, int> _teamAverage = new(StringComparer.Ordinal);

    public DatabaseViewModel(Carset carset)
    {
        _carset = carset;

        var driverById = carset.Drivers.ToDictionary(d => d.Id, StringComparer.Ordinal);
        for (var i = 0; i < carset.Teams.Count; i++)
        {
            var team = carset.Teams[i];
            _teamName[team.Id] = team.Name;
            _teamAccent[team.Id] = ScreenBrushes.TeamAccent(i);

            var overalls = team.DriverIds
                .Where(driverById.ContainsKey)
                .Select(id => driverById[id].Attributes.Overall)
                .ToList();
            _teamAverage[team.Id] = overalls.Count > 0 ? (int)Math.Round(overalls.Average()) : 0;

            foreach (var id in team.DriverIds)
            {
                _teamOfDriver[id] = team.Id;
            }
        }

        Rows = BuildRows(DatabaseTab.Drivers);
        _selectedRow = Rows.FirstOrDefault();
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsDriversTab))]
    [NotifyPropertyChangedFor(nameof(IsJuniorsTab))]
    [NotifyPropertyChangedFor(nameof(IsTeamsTab))]
    private DatabaseTab _selectedTab;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Detail))]
    private DbRowViewModel? _selectedRow;

    public IReadOnlyList<DbRowViewModel> Rows { get; private set; }

    public EntityDetailViewModel? Detail => SelectedRow?.Detail;

    public bool IsDriversTab => SelectedTab == DatabaseTab.Drivers;

    public bool IsJuniorsTab => SelectedTab == DatabaseTab.Juniors;

    public bool IsTeamsTab => SelectedTab == DatabaseTab.Teams;

    [RelayCommand]
    private void SelectTab(DatabaseTab tab) => SelectedTab = tab;

    partial void OnSelectedTabChanged(DatabaseTab value)
    {
        Rows = BuildRows(value);
        OnPropertyChanged(nameof(Rows));
        SelectedRow = Rows.FirstOrDefault();
    }

    private IReadOnlyList<DbRowViewModel> BuildRows(DatabaseTab tab) => tab switch
    {
        DatabaseTab.Teams => _carset.Teams
            .Select(t => DbRowViewModel.ForTeam(
                t,
                _teamAccent.GetValueOrDefault(t.Id, ScreenBrushes.Faint),
                _teamAverage.GetValueOrDefault(t.Id, 0)))
            .ToList(),
        DatabaseTab.Juniors => _carset.Reserves
            .Select(d => DbRowViewModel.ForDriver(d, "Reserve", ScreenBrushes.Faint))
            .ToList(),
        _ => _carset.Drivers
            .Select(DriverRow)
            .ToList(),
    };

    private DbRowViewModel DriverRow(Driver driver)
    {
        var teamId = _teamOfDriver.GetValueOrDefault(driver.Id, "");
        var teamLabel = teamId.Length > 0 ? _teamName.GetValueOrDefault(teamId, "") : "Free agent";
        var accent = teamId.Length > 0 ? _teamAccent.GetValueOrDefault(teamId, ScreenBrushes.Faint) : ScreenBrushes.Faint;
        return DbRowViewModel.ForDriver(driver, teamLabel, accent);
    }
}

/// <summary>One row in the database table (M21), with a prebuilt detail for the panel.</summary>
public sealed class DbRowViewModel
{
    private DbRowViewModel(string name, string nationality, string age, string team, string overall, IBrush overallBrush, string potential, IBrush accent, EntityDetailViewModel detail)
    {
        Name = name;
        Nationality = nationality;
        Age = age;
        Team = team;
        Overall = overall;
        OverallBrush = overallBrush;
        Potential = potential;
        Accent = accent;
        Detail = detail;
    }

    public string Name { get; }

    public string Nationality { get; }

    public string Age { get; }

    public string Team { get; }

    public string Overall { get; }

    public IBrush OverallBrush { get; }

    public string Potential { get; }

    public IBrush Accent { get; }

    public EntityDetailViewModel Detail { get; }

    public static DbRowViewModel ForDriver(Driver driver, string teamLabel, IBrush accent) => new(
        driver.FullName,
        Blank(driver.Nationality),
        driver.Age.ToString(CultureInfo.InvariantCulture),
        Blank(teamLabel),
        driver.Attributes.Overall.ToString(CultureInfo.InvariantCulture),
        StatusColorConverter.Classify(driver.Attributes.Overall),
        driver.Potential > 0 ? driver.Potential.ToString(CultureInfo.InvariantCulture) : "—",
        accent,
        EntityDetailViewModel.ForDriver(driver, teamLabel));

    public static DbRowViewModel ForTeam(Team team, IBrush accent, int averageOverall) => new(
        team.Name,
        Blank(team.Nationality),
        "—",
        Blank(team.Base),
        averageOverall > 0 ? averageOverall.ToString(CultureInfo.InvariantCulture) : "—",
        StatusColorConverter.Classify(averageOverall),
        "—",
        accent,
        EntityDetailViewModel.ForTeam(team, team.DriverIds.Count, averageOverall));

    private static string Blank(string value) => string.IsNullOrWhiteSpace(value) ? "—" : value;
}
