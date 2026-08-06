using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LTF.App.Localization;
using LTF.App.Mvvm;
using LTF.App.Services;
using LTF.App.Session;
using LTF.Domain.Management;

namespace LTF.App.ViewModels.Menu;

/// <summary>The steps of the new-career wizard: pick a carset, pick the team, negotiate the board's
/// objectives, then confirm and start.</summary>
public enum NewCareerStep
{
    Carset,
    Team,
    Board,
    Confirm,
}

/// <summary>
/// The new Team-Principal career flow (M20b/M20c): pick a carset, pick the team to run, negotiate the
/// board's objectives, confirm and start. Selecting a carset repopulates the team list and pre-selects the
/// carset's designated player team; entering the board step opens an interactive negotiation for the chosen
/// team's board (a neutral board is synthesised for a team the carset ships none for). Start hands the built
/// <see cref="ShellSession"/> — with the negotiated objectives — to the shell via <see cref="IAppShellController"/>.
/// </summary>
public sealed partial class NewCareerViewModel : ViewModelBase
{
    private readonly IAppShellController _host;

    public NewCareerViewModel(IAppShellController host, CarsetCatalog catalog)
    {
        _host = host;
        Carsets = new ObservableCollection<CarsetOptionViewModel>(
            catalog.Descriptors.Select(d => new CarsetOptionViewModel(d)));
        SelectedCarset = Carsets.FirstOrDefault();
    }

    public ObservableCollection<CarsetOptionViewModel> Carsets { get; }

    public ObservableCollection<TeamOptionViewModel> Teams { get; } = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsCarsetStep), nameof(IsTeamStep), nameof(IsBoardStep),
        nameof(IsConfirmStep), nameof(StepLabel), nameof(CanGoNext))]
    [NotifyCanExecuteChangedFor(nameof(NextCommand))]
    private NewCareerStep _step = NewCareerStep.Carset;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectedCarsetName), nameof(CanGoNext))]
    [NotifyCanExecuteChangedFor(nameof(NextCommand), nameof(StartCommand))]
    private CarsetOptionViewModel? _selectedCarset;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectedTeamName), nameof(CanGoNext))]
    [NotifyCanExecuteChangedFor(nameof(NextCommand), nameof(StartCommand))]
    private TeamOptionViewModel? _selectedTeam;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanGoNext))]
    [NotifyCanExecuteChangedFor(nameof(NextCommand))]
    private BoardNegotiationViewModel? _board;

    public bool IsCarsetStep => Step == NewCareerStep.Carset;

    public bool IsTeamStep => Step == NewCareerStep.Team;

    public bool IsBoardStep => Step == NewCareerStep.Board;

    public bool IsConfirmStep => Step == NewCareerStep.Confirm;

    public string StepLabel => Localizer.Current.Get(Step switch
    {
        NewCareerStep.Carset => StringKeys.NewCareerStepCarset,
        NewCareerStep.Team => StringKeys.NewCareerStepTeam,
        NewCareerStep.Board => StringKeys.NewCareerStepBoard,
        _ => StringKeys.NewCareerStepConfirm,
    });

    public string SelectedCarsetName => SelectedCarset?.Name ?? "";

    public string SelectedTeamName => SelectedTeam?.Name ?? "";

    /// <summary>Whether the current step is satisfied enough to advance.</summary>
    public bool CanGoNext => Step switch
    {
        NewCareerStep.Carset => SelectedCarset is not null,
        NewCareerStep.Team => SelectedTeam is not null,
        NewCareerStep.Board => Board is not null,
        _ => false,
    };

    private bool CanStart => SelectedCarset is not null && SelectedTeam is not null;

    partial void OnSelectedCarsetChanged(CarsetOptionViewModel? value)
    {
        Teams.Clear();
        if (value is null)
        {
            SelectedTeam = null;
            return;
        }

        foreach (var team in value.Descriptor.Summary.Teams)
        {
            Teams.Add(new TeamOptionViewModel(team));
        }

        // Pre-select the carset's designated player team, else the first on the grid.
        var defaultId = value.Descriptor.Summary.DefaultPlayerTeamId;
        SelectedTeam = Teams.FirstOrDefault(t => t.Id == defaultId) ?? Teams.FirstOrDefault();
    }

    [RelayCommand(CanExecute = nameof(CanGoNext))]
    private void Next()
    {
        switch (Step)
        {
            case NewCareerStep.Carset:
                Step = NewCareerStep.Team;
                break;
            case NewCareerStep.Team:
                OpenBoardNegotiation();
                Step = NewCareerStep.Board;
                break;
            case NewCareerStep.Board:
                Step = NewCareerStep.Confirm;
                break;
        }
    }

    [RelayCommand]
    private void Back()
    {
        switch (Step)
        {
            case NewCareerStep.Carset:
                _host.ShowMainMenu();
                break;
            case NewCareerStep.Team:
                Step = NewCareerStep.Carset;
                break;
            case NewCareerStep.Board:
                Step = NewCareerStep.Team;
                break;
            case NewCareerStep.Confirm:
                Step = NewCareerStep.Board;
                break;
        }
    }

    [RelayCommand(CanExecute = nameof(CanStart))]
    private void Start()
    {
        var carset = SelectedCarset!.Descriptor.Carset;
        var objectives = Board?.Agreed ?? [];
        var session = NewCareerService.Create(carset, SelectedTeam!.Id, objectives);
        _host.EnterCareer(session);
    }

    // Build the negotiation for the chosen team's board, synthesising a neutral board if the carset ships
    // none for that team (so the board step is always meaningful).
    private void OpenBoardNegotiation()
    {
        var carset = SelectedCarset!.Descriptor.Carset;
        var teamId = SelectedTeam!.Id;
        var board = carset.Boards.FirstOrDefault(b => b.TeamId == teamId) ?? new TeamBoard { TeamId = teamId };
        Board = new BoardNegotiationViewModel(board, SessionLoader.DefaultSeed);
    }
}
