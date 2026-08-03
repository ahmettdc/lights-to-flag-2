using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LightsToFlag.App.Services;
using LightsToFlag.Core.Career;
using LightsToFlag.Core.Domain;

namespace LightsToFlag.App.ViewModels;

public sealed record DriverRow(int Position, string Name, string Team, double Points, int Wins, bool IsPlayer);
public sealed record ConstructorRow(int Position, string Team, double Points, int Wins);
public sealed record RoundRow(int Round, string Circuit, string Status, string Winner);
public sealed record OfferRow(int TeamNumber, string Team);

/// <summary>The career hub: dashboard, championship standings, calendar and seat offers.</summary>
public partial class CareerViewModel : ObservableObject
{
    private readonly GameSession _session;
    private readonly INavigationService _nav;

    public CareerViewModel(GameSession session, INavigationService nav)
    {
        _session = session;
        _nav = nav;
        Refresh();
    }

    public ObservableCollection<DriverRow> DriverStandings { get; } = new();
    public ObservableCollection<ConstructorRow> ConstructorStandings { get; } = new();
    public ObservableCollection<RoundRow> Calendar { get; } = new();
    public ObservableCollection<OfferRow> Offers { get; } = new();

    [ObservableProperty] private string _section = "dashboard";
    [ObservableProperty] private string _seasonLabel = "";
    [ObservableProperty] private string _playerName = "";
    [ObservableProperty] private string _playerTeam = "";
    [ObservableProperty] private string _championshipStanding = "";
    [ObservableProperty] private string _nextRaceCircuit = "";
    [ObservableProperty] private string _nextRaceLabel = "";
    [ObservableProperty] private bool _seasonComplete;
    [ObservableProperty] private bool _hasOffers;
    [ObservableProperty] private string? _status;

    private void Refresh()
    {
        var carset = _session.Carset;
        var career = _session.Career;
        if (carset is null || career is null)
        {
            return;
        }

        var engine = _session.Engine;
        SeasonLabel = $"Season {career.SeasonIndex + 1}";
        SeasonComplete = engine.IsSeasonComplete(career, carset);

        var driverStandings = engine.DriverStandings(career, carset);
        var constructorStandings = engine.ConstructorStandings(career, carset);

        DriverStandings.Clear();
        foreach (var s in driverStandings)
        {
            DriverStandings.Add(new DriverRow(
                s.Position, s.DriverName, s.TeamName, s.Points, s.Wins, s.CompetitorId == career.PlayerId));
        }

        ConstructorStandings.Clear();
        foreach (var s in constructorStandings)
        {
            ConstructorStandings.Add(new ConstructorRow(s.Position, s.TeamName, s.Points, s.Wins));
        }

        BuildCalendar(carset, career);

        Offers.Clear();
        foreach (var offer in career.PendingOffers)
        {
            Offers.Add(new OfferRow(offer.TeamNumber, offer.TeamName));
        }

        HasOffers = Offers.Count > 0;

        var player = driverStandings.FirstOrDefault(s => s.CompetitorId == career.PlayerId);
        PlayerName = player?.DriverName ?? "Spectator";
        PlayerTeam = player?.TeamName ?? "";
        ChampionshipStanding = player is not null ? $"P{player.Position} · {player.Points:0} pts" : "—";

        if (SeasonComplete)
        {
            NextRaceCircuit = "Season complete";
            NextRaceLabel = "Review the standings, then start next season.";
        }
        else
        {
            var circuit = engine.NextCircuit(career, carset);
            NextRaceCircuit = circuit.Name;
            NextRaceLabel = $"Round {engine.NextRoundIndex(career) + 1} of {carset.Circuits.Count} · {circuit.Venue}";
        }
    }

    private void BuildCalendar(Carset carset, CareerState career)
    {
        Calendar.Clear();
        var done = career.CompletedRounds.ToDictionary(r => r.RoundIndex);
        var nextIndex = _session.Engine.NextRoundIndex(career);
        for (var i = 0; i < carset.Circuits.Count; i++)
        {
            var circuit = carset.Circuits[i];
            string status;
            var winner = "";
            if (done.TryGetValue(i, out var result))
            {
                status = "Done";
                winner = NameOf(career, result.WinnerId);
            }
            else
            {
                status = i == nextIndex ? "Next" : "Upcoming";
            }

            Calendar.Add(new RoundRow(i + 1, circuit.Name, status, winner));
        }
    }

    private static string NameOf(CareerState career, string competitorId) =>
        career.Entrants.FirstOrDefault(e => e.Id == competitorId)?.Driver.FullName ?? competitorId;

    [RelayCommand] private void ShowDashboard() => Section = "dashboard";
    [RelayCommand] private void ShowStandings() => Section = "standings";
    [RelayCommand] private void ShowCalendar() => Section = "calendar";
    [RelayCommand] private void ShowOffers() => Section = "offers";

    [RelayCommand]
    private void GoToRace()
    {
        if (!SeasonComplete)
        {
            _nav.NavigateTo<RaceWeekendViewModel>();
        }
    }

    [RelayCommand]
    private void AdvanceSeason()
    {
        if (_session is { Carset: { } carset, Career: { } career } && SeasonComplete)
        {
            _session.Career = _session.Engine.AdvanceToNextSeason(career, carset);
            Refresh();
            Section = Offers.Count > 0 ? "offers" : "dashboard";
        }
    }

    [RelayCommand]
    private void AcceptOffer(OfferRow? offer)
    {
        if (offer is null || _session.Career is not { } career)
        {
            return;
        }

        _session.Career = _session.Engine.AcceptOffer(career, new TeamOffer { TeamNumber = offer.TeamNumber, TeamName = offer.Team });
        Refresh();
        Status = $"Signed for {offer.Team}.";
        Section = "dashboard";
    }

    [RelayCommand]
    private void Save()
    {
        _session.Save("career");
        Status = "Career saved.";
    }

    [RelayCommand]
    private void BackToMenu() => _nav.NavigateTo<MainMenuViewModel>();
}
