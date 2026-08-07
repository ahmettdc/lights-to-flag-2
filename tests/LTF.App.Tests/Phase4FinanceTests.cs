using System;
using System.IO;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using LTF.App.Navigation;
using LTF.App.Services;
using LTF.App.Session;
using LTF.App.Settings;
using LTF.App.ViewModels;
using LTF.App.ViewModels.Screens;
using LTF.App.Views.Screens;
using LTF.Career;
using LTF.Domain;
using LTF.Domain.Management;
using LTF.Domain.Racing;
using Xunit;

namespace LTF.App.Tests;

/// <summary>
/// The finance screen (M22b): projects the player team's money and its live credit profile (ADR-0029) —
/// balance, income, cost cap, the outstanding loans with each one's next straight-line instalment and
/// arrears, and the credit score/rate/limit — resolves to its view via the app DataTemplate headless, and
/// registers through the shell so navigating to Finance yields the real view-model.
/// </summary>
public class Phase4FinanceTests
{
    private static Carset Flagship() => SessionLoader.LoadFlagship().Carset;

    private static Carset WithPlayer(Carset carset, Func<Team, Team> transform)
    {
        var teams = carset.Teams
            .Select(t => string.CompareOrdinal(t.Id, carset.PlayerTeamId) == 0 ? transform(t) : t)
            .ToList();
        return carset with { Teams = teams };
    }

    private static Standings Empty(Carset carset) => ChampionshipStandings.Empty(carset);

    [Fact]
    public void Finance_projects_the_player_finances_and_credit()
    {
        var carset = Flagship();
        var vm = new FinanceViewModel(carset, Empty(carset));

        Assert.True(vm.HasTeam);
        Assert.True(vm.NoLoans);                               // a fresh career carries no debt
        Assert.InRange(vm.CreditScore, 0, 100);
        Assert.StartsWith("$", vm.BalanceText.TrimStart('-')); // a formatted money string
        Assert.False(vm.BankActive);                           // the flagship ships no bank block
        Assert.True(vm.NoBank);
    }

    [Fact]
    public void Finance_lists_loans_with_next_instalment_and_flags_arrears()
    {
        var clean = new Loan
        {
            Id = "loan-1", Lender = "Series Bank", Principal = 20_000_000, AnnualRatePercent = 10,
            TermSeasons = 5, SeasonsRemaining = 5, OutstandingBalance = 20_000_000,
        };
        var arrears = new Loan
        {
            Id = "loan-2", Principal = 10_000_000, AnnualRatePercent = 12,
            TermSeasons = 3, SeasonsRemaining = 2, OutstandingBalance = 10_000_000, MissedPayments = 2,
        };
        var carset = WithPlayer(Flagship(), t => t with { Finances = t.Finances with { Loans = [clean, arrears] } });
        var vm = new FinanceViewModel(carset, Empty(carset));

        Assert.True(vm.HasLoans);
        Assert.Equal(2, vm.Loans.Count);
        Assert.True(vm.InArrears);
        // Straight-line instalment: interest 2M + principal 20M/5 = 4M → 6M.
        Assert.Equal("$6M", vm.Loans[0].Instalment);
        Assert.False(vm.Loans[0].InArrears);
        Assert.True(vm.Loans[1].InArrears);
        Assert.Equal(2, vm.Loans[1].MissedPayments);
    }

    [Fact]
    public void Finance_derives_a_live_credit_limit_when_the_bank_is_active()
    {
        var carset = Flagship();
        carset = carset with { Rules = carset.Rules with { Bank = new BankRules
        {
            BaseRatePercent = 6, MaxRiskPremiumPercent = 24, MaxLoanToRevenuePercent = 200, LatePenaltyPercent = 10,
        } } };
        carset = WithPlayer(carset, t => t with { Finances = t.Finances with { PrizeMoney = 50_000_000, SponsorIncome = 10_000_000 } });

        var vm = new FinanceViewModel(carset, Empty(carset));

        Assert.True(vm.BankActive);
        Assert.EndsWith("%", vm.OfferedRateText);
        Assert.NotEqual("$0", vm.CreditLimitText); // revenue + active bank → a real limit
    }

    [AvaloniaFact]
    public void Finance_view_resolves_from_its_view_model()
    {
        var carset = Flagship();
        var host = new ContentControl { Content = new FinanceViewModel(carset, Empty(carset)) };
        var window = new Window { Content = host };

        window.Show();
        Dispatcher.UIThread.RunJobs();

        Assert.Single(host.GetVisualDescendants().OfType<FinanceView>());
    }

    [Fact]
    public void Entering_a_career_registers_the_finance_screen()
    {
        var catalog = CarsetCatalog.Discover();
        var dir = Directory.CreateTempSubdirectory().FullName;
        var saves = new SaveStore(catalog, dir);
        var settings = new SettingsStore(Path.Combine(dir, "settings.json"));
        var root = new RootViewModel(new AppServices(catalog, saves, settings, new CareerNotificationSource()));

        root.EnterCareer(SessionLoader.LoadFlagship());
        var shell = (ShellViewModel)root.Content!;

        shell.Navigation.Navigate(NavKey.Finance);
        Assert.IsType<FinanceViewModel>(shell.Navigation.CurrentScreen);
    }
}
