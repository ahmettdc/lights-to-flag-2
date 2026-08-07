using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using LTF.App.Mvvm;
using LTF.App.Session;
using LTF.Domain.Racing;

namespace LTF.App.ViewModels.Screens;

/// <summary>
/// The records &amp; statistics screen (M24): career profiles, this-season statistics, all-time record boards and
/// the hall of fame, over a career's accumulated history. A read-only projection over the live career — the driver
/// and team career tallies (accumulated at each season rollover) and the persisted season archive — so a Continue
/// rebuilds it. No engine data is touched, so the golden race is untouched.
/// </summary>
public sealed partial class RecordsViewModel : ViewModelBase
{
    public RecordsViewModel(LiveCareer live)
    {
        var carset = live.Current;

        var teamName = new Dictionary<string, string>(StringComparer.Ordinal);
        var teamOfDriver = new Dictionary<string, string>(StringComparer.Ordinal);
        var teamAccent = new Dictionary<string, IBrush>(StringComparer.Ordinal);
        for (var i = 0; i < carset.Teams.Count; i++)
        {
            var team = carset.Teams[i];
            teamName[team.Id] = team.Name;
            teamAccent[team.Id] = ScreenBrushes.TeamAccent(i);
            foreach (var id in team.DriverIds)
            {
                teamOfDriver[id] = team.Id;
            }
        }

        // Career profiles (PROFILES): every driver's accumulated record, ranked by career points then wins.
        Profiles = carset.Drivers
            .OrderByDescending(d => d.Career.Points)
            .ThenByDescending(d => d.Career.Wins)
            .ThenBy(d => d.FullName, StringComparer.Ordinal)
            .Select((d, i) =>
            {
                var teamId = teamOfDriver.GetValueOrDefault(d.Id, "");
                var teamLabel = teamId.Length > 0 ? teamName.GetValueOrDefault(teamId, "") : "Free agent";
                var accent = teamId.Length > 0
                    ? teamAccent.GetValueOrDefault(teamId, ScreenBrushes.Faint)
                    : ScreenBrushes.Faint;
                return new CareerProfileRowViewModel(i + 1, d, teamLabel, accent);
            })
            .ToList();
        _selectedProfile = Profiles.FirstOrDefault();
    }

    /// <summary>Every driver's career record, ranked (PROFILES tab).</summary>
    public IReadOnlyList<CareerProfileRowViewModel> Profiles { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ProfileDetail))]
    private CareerProfileRowViewModel? _selectedProfile;

    /// <summary>The full profile card for the selected driver (the shared driver/team detail panel).</summary>
    public EntityDetailViewModel? ProfileDetail => SelectedProfile?.Detail;
}

/// <summary>One driver's row in the career-profiles table (M24): rank, name, team accent, the headline career
/// tallies, and the profile card the detail panel shows when the row is selected.</summary>
public sealed class CareerProfileRowViewModel
{
    public CareerProfileRowViewModel(int rank, Driver driver, string teamLabel, IBrush accent)
    {
        Rank = rank.ToString(CultureInfo.InvariantCulture);
        Name = driver.FullName;
        Accent = accent;
        Races = driver.Career.Races.ToString(CultureInfo.InvariantCulture);
        Wins = driver.Career.Wins.ToString(CultureInfo.InvariantCulture);
        Podiums = driver.Career.Podiums.ToString(CultureInfo.InvariantCulture);
        Poles = driver.Career.Poles.ToString(CultureInfo.InvariantCulture);
        Titles = driver.Career.Championships.ToString(CultureInfo.InvariantCulture);
        Points = driver.Career.Points.ToString("0", CultureInfo.InvariantCulture);
        Detail = EntityDetailViewModel.ForDriver(driver, teamLabel);
    }

    public string Rank { get; }
    public string Name { get; }
    public IBrush Accent { get; }
    public string Races { get; }
    public string Wins { get; }
    public string Podiums { get; }
    public string Poles { get; }
    public string Titles { get; }
    public string Points { get; }
    public EntityDetailViewModel Detail { get; }
}
