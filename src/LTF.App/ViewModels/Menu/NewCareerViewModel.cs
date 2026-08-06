using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LTF.App.Localization;
using LTF.App.Mvvm;
using LTF.App.Services;
using LTF.App.Session;

namespace LTF.App.ViewModels.Menu;

/// <summary>The steps of the new-career wizard. M20b covers carset → team → confirm; M20c inserts the
/// interactive board-objective negotiation between Team and Confirm.</summary>
public enum NewCareerStep
{
    Carset,
    Team,
    Confirm,
}

/// <summary>
/// The new Team-Principal career flow (M20b): pick a carset, pick the team to run, confirm and start.
/// Selecting a carset repopulates the team list and pre-selects the carset's designated player team.
/// Start hands the built <see cref="ShellSession"/> to the shell via <see cref="IAppShellController"/>.
/// Board objectives default to each board's ownership (via <c>BoardReview.SetObjectives</c>) here; M20c
/// makes that step an interactive negotiation.
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
    [NotifyPropertyChangedFor(nameof(IsCarsetStep), nameof(IsTeamStep), nameof(IsConfirmStep),
        nameof(StepLabel), nameof(CanGoNext))]
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

    public bool IsCarsetStep => Step == NewCareerStep.Carset;

    public bool IsTeamStep => Step == NewCareerStep.Team;

    public bool IsConfirmStep => Step == NewCareerStep.Confirm;

    public string StepLabel => Localizer.Current.Get(Step switch
    {
        NewCareerStep.Carset => StringKeys.NewCareerStepCarset,
        NewCareerStep.Team => StringKeys.NewCareerStepTeam,
        _ => StringKeys.NewCareerStepConfirm,
    });

    public string SelectedCarsetName => SelectedCarset?.Name ?? "";

    public string SelectedTeamName => SelectedTeam?.Name ?? "";

    /// <summary>Whether the current step is satisfied enough to advance.</summary>
    public bool CanGoNext => Step switch
    {
        NewCareerStep.Carset => SelectedCarset is not null,
        NewCareerStep.Team => SelectedTeam is not null,
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
        Step = Step switch
        {
            NewCareerStep.Carset => NewCareerStep.Team,
            NewCareerStep.Team => NewCareerStep.Confirm,
            _ => Step,
        };
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
            case NewCareerStep.Confirm:
                Step = NewCareerStep.Team;
                break;
        }
    }

    [RelayCommand(CanExecute = nameof(CanStart))]
    private void Start()
    {
        var carset = SelectedCarset!.Descriptor.Carset;
        var session = NewCareerService.Create(carset, SelectedTeam!.Id, []);
        _host.EnterCareer(session);
    }
}
