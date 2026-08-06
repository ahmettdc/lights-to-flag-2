using LTF.App.Services;
using LTF.App.Session;
using LTF.App.ViewModels.Quick;
using Xunit;

namespace LTF.App.Tests;

/// <summary>The Quick Race view-model (M20g): setup → run → result → back to setup, and back-from-setup to
/// the main menu.</summary>
public class QuickRaceTests
{
    private sealed class StubShell : IAppShellController
    {
        public int MainMenu;

        public void ShowMainMenu() => MainMenu++;
        public void ShowNewCareer() { }
        public void ContinueCareer() { }
        public void ShowLoadGame() { }
        public void ShowQuickRace() { }
        public void ShowSettings() { }
        public void EnterCareer(ShellSession session) { }
        public void ExitToMenu() { }
        public void Quit() { }
    }

    private static QuickRaceViewModel Make(StubShell host) => new(host, CarsetCatalog.Discover());

    [Fact]
    public void Starts_on_setup_with_a_carset_and_circuit_selected()
    {
        var vm = Make(new StubShell());

        Assert.True(vm.IsSetup);
        Assert.NotNull(vm.SelectedCarset);
        Assert.NotEmpty(vm.Circuits);
        Assert.NotNull(vm.SelectedCircuit);
        Assert.True(vm.RunCommand.CanExecute(null));
    }

    [Fact]
    public void Running_produces_a_result_then_back_returns_to_setup()
    {
        var vm = Make(new StubShell());

        vm.RunCommand.Execute(null);

        Assert.False(vm.IsSetup);
        Assert.NotNull(vm.Result);
        Assert.NotEmpty(vm.Result!.Rows);

        vm.BackCommand.Execute(null);
        Assert.True(vm.IsSetup);
    }

    [Fact]
    public void Back_from_setup_returns_to_the_main_menu()
    {
        var host = new StubShell();
        var vm = Make(host);

        vm.BackCommand.Execute(null);

        Assert.Equal(1, host.MainMenu);
    }
}
