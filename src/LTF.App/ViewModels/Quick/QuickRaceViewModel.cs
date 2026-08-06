using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LTF.App.Mvvm;
using LTF.App.Services;
using LTF.App.Session;
using LTF.App.ViewModels.Menu;

namespace LTF.App.ViewModels.Quick;

/// <summary>
/// Quick Race (M20g): a one-off race outside a career. Pick a carset and a circuit, run it, and read the
/// result — then race another. Reuses the deterministic engine via <see cref="QuickRaceService"/>; the seed
/// is fixed for reproducibility (a custom seed is a later nicety). <see cref="Result"/> null = setup screen,
/// set = result screen.
/// </summary>
public sealed partial class QuickRaceViewModel : ViewModelBase
{
    private readonly IAppShellController _host;

    public QuickRaceViewModel(IAppShellController host, CarsetCatalog catalog)
    {
        _host = host;
        Carsets = new ObservableCollection<CarsetOptionViewModel>(
            catalog.Descriptors.Select(d => new CarsetOptionViewModel(d)));
        SelectedCarset = Carsets.FirstOrDefault();
    }

    public ObservableCollection<CarsetOptionViewModel> Carsets { get; }

    public ObservableCollection<CircuitOptionViewModel> Circuits { get; } = new();

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RunCommand))]
    private CarsetOptionViewModel? _selectedCarset;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RunCommand))]
    private CircuitOptionViewModel? _selectedCircuit;

    [ObservableProperty]
    private int _seed = SessionLoader.DefaultSeed;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsSetup))]
    private QuickRaceResult? _result;

    public bool IsSetup => Result is null;

    private bool CanRun => SelectedCarset is not null && SelectedCircuit is not null;

    partial void OnSelectedCarsetChanged(CarsetOptionViewModel? value)
    {
        Circuits.Clear();
        if (value is null)
        {
            SelectedCircuit = null;
            return;
        }

        var circuits = value.Descriptor.Carset.Circuits;
        for (var i = 0; i < circuits.Count; i++)
        {
            Circuits.Add(new CircuitOptionViewModel(circuits[i], i));
        }

        SelectedCircuit = Circuits.FirstOrDefault();
    }

    [RelayCommand(CanExecute = nameof(CanRun))]
    private void Run() =>
        Result = QuickRaceService.Run(SelectedCarset!.Descriptor.Carset, SelectedCircuit!.Index, Seed);

    [RelayCommand]
    private void Back()
    {
        // From the result, go back to the setup; from the setup, back to the main menu.
        if (Result is not null)
        {
            Result = null;
        }
        else
        {
            _host.ShowMainMenu();
        }
    }
}
