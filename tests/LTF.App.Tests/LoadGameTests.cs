using System.IO;
using LTF.App.Services;
using LTF.App.Session;
using LTF.App.ViewModels.Menu;
using Xunit;

namespace LTF.App.Tests;

/// <summary>The load-game screen (M20d): lists saved careers, loads or deletes the selection, and goes back.</summary>
public class LoadGameTests
{
    private sealed class StubShell : IAppShellController
    {
        public ShellSession? Entered;
        public int MainMenu;

        public void ShowMainMenu() => MainMenu++;
        public void ShowNewCareer() { }
        public void ContinueCareer() { }
        public void ShowLoadGame() { }
        public void ShowQuickRace() { }
        public void ShowSettings() { }
        public void EnterCareer(ShellSession session) => Entered = session;
        public void ExitToMenu() { }
        public void Quit() { }
    }

    private static SaveStore FreshStore() =>
        new(CarsetCatalog.Discover(), Directory.CreateTempSubdirectory().FullName);

    [Fact]
    public void Empty_when_there_are_no_saves()
    {
        var vm = new LoadGameViewModel(new StubShell(), FreshStore());

        Assert.True(vm.IsEmpty);
        Assert.Null(vm.SelectedSlot);
        Assert.False(vm.LoadCommand.CanExecute(null));
    }

    [Fact]
    public void Lists_and_loads_a_saved_career()
    {
        var store = FreshStore();
        store.Save(SessionLoader.LoadFlagship());
        var host = new StubShell();
        var vm = new LoadGameViewModel(host, store);

        Assert.False(vm.IsEmpty);
        Assert.NotNull(vm.SelectedSlot);

        vm.LoadCommand.Execute(null);

        Assert.NotNull(host.Entered);
        Assert.Equal("talon", host.Entered!.Carset.PlayerTeamId);
    }

    [Fact]
    public void Deleting_the_last_save_empties_the_list()
    {
        var store = FreshStore();
        store.Save(SessionLoader.LoadFlagship());
        var vm = new LoadGameViewModel(new StubShell(), store);

        vm.DeleteCommand.Execute(null);

        Assert.True(vm.IsEmpty);
        Assert.Null(vm.SelectedSlot);
    }

    [Fact]
    public void Back_returns_to_the_main_menu()
    {
        var host = new StubShell();
        var vm = new LoadGameViewModel(host, FreshStore());

        vm.BackCommand.Execute(null);

        Assert.Equal(1, host.MainMenu);
    }
}
