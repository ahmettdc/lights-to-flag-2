using System.IO;
using System.Linq;
using LTF.App.Navigation;
using LTF.App.Services;
using LTF.App.Session;
using LTF.App.Settings;
using LTF.App.ViewModels;
using LTF.App.ViewModels.Screens;
using LTF.Career;
using LTF.Domain;
using LTF.Domain.Racing;
using Xunit;

namespace LTF.App.Tests;

/// <summary>
/// Taking a loan (M22c) — the first in-shell player mutation. The finance screen's borrow command runs the
/// callback with the entered amount/term, the amount is validated against the live headroom, and driving it
/// through the shell lands the loan on the season-start carset (the save base) so it shows immediately and
/// survives a save/reload.
/// </summary>
public class Phase4BorrowTests
{
    // The flagship with an active bank and the player given revenue, so there is real credit headroom.
    private static Carset WithBank(Carset carset)
    {
        carset = carset with { Rules = carset.Rules with { Bank = new BankRules
        {
            BaseRatePercent = 6, MaxRiskPremiumPercent = 24, MaxLoanToRevenuePercent = 200, LatePenaltyPercent = 10,
        } } };
        var teams = carset.Teams
            .Select(t => string.CompareOrdinal(t.Id, carset.PlayerTeamId) == 0
                ? t with { Finances = t.Finances with { PrizeMoney = 80_000_000, SponsorIncome = 20_000_000 } }
                : t)
            .ToList();
        return carset with { Teams = teams };
    }

    private static Carset BankCarset() => WithBank(SessionLoader.LoadFlagship().Carset);

    private static ShellSession BankSession()
    {
        var s = SessionLoader.LoadFlagship();
        return new ShellSession(WithBank(s.Carset), s.Clock, s.Seed);
    }

    private static Standings Empty(Carset carset) => ChampionshipStandings.Empty(carset);

    [Fact]
    public void The_borrow_command_invokes_the_callback_with_the_entered_amount_and_term()
    {
        var carset = BankCarset();
        long amount = 0;
        var term = 0;
        var called = false;
        var vm = new FinanceViewModel(carset, Empty(carset), (a, t) => { amount = a; term = t; called = true; });

        Assert.True(vm.CanOfferBorrow);
        vm.BorrowAmount = 20_000_000;
        Assert.True(vm.CanBorrowNow);

        vm.BorrowCommand.Execute(null);

        Assert.True(called);
        Assert.Equal(20_000_000, amount);
        Assert.Equal(5, term); // the default term
    }

    [Fact]
    public void The_draw_is_validated_against_the_headroom()
    {
        var carset = BankCarset();
        var vm = new FinanceViewModel(carset, Empty(carset), (_, _) => { });

        Assert.False(vm.CanBorrowNow);                       // nothing entered yet
        vm.BorrowAmount = 5_000_000;
        Assert.True(vm.CanBorrowNow);                        // within the headroom
        vm.BorrowAmount = vm.HeadroomValue + 1_000_000;      // beyond the limit
        Assert.False(vm.CanBorrowNow);
    }

    [Fact]
    public void A_read_only_finance_screen_offers_no_borrow_panel()
    {
        var carset = BankCarset();
        var vm = new FinanceViewModel(carset, Empty(carset)); // no borrow callback

        Assert.False(vm.CanOfferBorrow);
        Assert.False(vm.CanBorrowNow);
    }

    [Fact]
    public void Borrowing_through_the_shell_lands_a_loan_that_shows_and_persists()
    {
        var catalog = CarsetCatalog.Discover();
        var dir = Directory.CreateTempSubdirectory().FullName;
        var saves = new SaveStore(catalog, dir);
        var settings = new SettingsStore(Path.Combine(dir, "settings.json"));
        var root = new RootViewModel(new AppServices(catalog, saves, settings, new CareerNotificationSource()));

        root.EnterCareer(BankSession());
        var shell = (ShellViewModel)root.Content!;

        shell.Navigation.Navigate(NavKey.Finance);
        var vm = (FinanceViewModel)shell.Navigation.CurrentScreen!;
        Assert.True(vm.CanOfferBorrow);
        Assert.True(vm.NoLoans);

        vm.BorrowAmount = 20_000_000;
        vm.BorrowCommand.Execute(null);

        // The mutation rebuilds the open screen from the new state — the fresh Finance VM shows the loan.
        var after = (FinanceViewModel)shell.Navigation.CurrentScreen!;
        Assert.True(after.HasLoans);

        // It landed on the season-start carset and autosaved, so a reload carries the loan.
        var reloaded = saves.Load(saves.MostRecent()!);
        Assert.NotEmpty(reloaded.Carset.PlayerTeam()!.Finances.Loans);
    }
}
