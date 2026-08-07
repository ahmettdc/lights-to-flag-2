using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LTF.App.Converters;
using LTF.App.Mvvm;
using LTF.Career;
using LTF.Domain;
using LTF.Domain.Management;

namespace LTF.App.ViewModels.Screens;

/// <summary>
/// The finance screen (M22b/M22c): the player team's money and its standing with the series' bank
/// (ADR-0029) — balance, budget, income and cost cap; the live credit profile (score, tier, offered rate,
/// limit, headroom); the outstanding loans with each one's next straight-line instalment and any arrears;
/// and the sponsor book. Read-only projection over the current carset + standings; a Continue rebuilds it
/// (M21d). The credit profile is derived on demand (never persisted), so it tracks the world. When the bank
/// is active and there is headroom, the borrow panel (M22c) draws a loan through the supplied callback — the
/// first in-shell player mutation — landing it on the season-start carset.
/// </summary>
public sealed partial class FinanceViewModel : ViewModelBase
{
    private readonly Action<long, int>? _borrow;
    private readonly long _headroomAmount;
    private readonly int _offeredRate;

    public FinanceViewModel(Carset carset, Standings standings, Action<long, int>? borrow = null)
    {
        _borrow = borrow;

        var team = carset.PlayerTeam();
        HasTeam = team is not null;

        var finances = team?.Finances ?? new Finances();
        BalanceText = Money(finances.Balance);
        BalanceBrush = finances.Balance < 0 ? ScreenBrushes.Bad : ScreenBrushes.Primary;
        SeasonBudgetText = Money(finances.SeasonBudget);
        PrizeMoneyText = Money(finances.PrizeMoney);
        SponsorIncomeText = Money(finances.SponsorIncome);
        HasCostCap = finances.HasCostCap;
        CostCapText = HasCostCap ? Money(finances.CostCap) : "—";
        TotalDebtText = Money(finances.TotalDebt);

        var board = team is not null
            ? carset.Boards.FirstOrDefault(b => string.CompareOrdinal(b.TeamId, carset.PlayerTeamId) == 0)
            : null;
        var profile = team is not null
            ? CreditProfile.For(team, standings, board, carset.Rules.Bank, carset.Rules.Economy)
            : new CreditProfile { Score = 0, Tier = CreditTier.Poor, OfferedRatePercent = 0, CreditLimit = 0, TotalDebt = 0 };

        BankActive = carset.Rules.Bank.IsActive;
        CreditScore = profile.Score;
        CreditScoreBrush = StatusColorConverter.Classify(profile.Score);
        CreditTierText = profile.Tier.ToString().ToUpperInvariant();
        OfferedRateText = string.Create(CultureInfo.InvariantCulture, $"{profile.OfferedRatePercent}%");
        CreditLimitText = Money(profile.CreditLimit);
        HeadroomText = Money(profile.Headroom);

        Loans = finances.Loans
            .Select(l => new LoanRowViewModel(
                l.Lender.Length > 0 ? l.Lender : "Series Bank",
                Money(l.OutstandingBalance),
                string.Create(CultureInfo.InvariantCulture, $"{l.AnnualRatePercent}%"),
                l.SeasonsRemaining,
                Money(Instalment(l)),
                l.MissedPayments,
                l.MissedPayments > 0,
                l.MissedPayments > 0 ? ScreenBrushes.Bad : ScreenBrushes.Dim))
            .ToList();
        HasLoans = Loans.Count > 0;
        InArrears = finances.Loans.Any(l => l.MissedPayments > 0);

        Sponsors = (team?.Sponsors ?? [])
            .Select((s, i) => new SponsorRowViewModel(
                s.Name,
                s.Tier.ToString().ToUpperInvariant(),
                Money(s.PerRaceFee),
                ScreenBrushes.TeamAccent(i)))
            .ToList();
        HasSponsors = Sponsors.Count > 0;

        // Borrow panel (M22c): offered only when the bank is active and there is headroom to draw against.
        _headroomAmount = profile.Headroom;
        _offeredRate = profile.OfferedRatePercent;
        CanOfferBorrow = _borrow is not null && BankActive && _headroomAmount > 0;
        UpdateBorrowSummary();
    }

    public bool HasTeam { get; }

    public string BalanceText { get; }

    public IBrush BalanceBrush { get; }

    public string SeasonBudgetText { get; }

    public string PrizeMoneyText { get; }

    public string SponsorIncomeText { get; }

    public bool HasCostCap { get; }

    public string CostCapText { get; }

    public string TotalDebtText { get; }

    public bool BankActive { get; }

    /// <summary>True when the series runs no bank — the credit panel shows the no-credit note instead.</summary>
    public bool NoBank => !BankActive;

    public int CreditScore { get; }

    public IBrush CreditScoreBrush { get; }

    public string CreditTierText { get; }

    public string OfferedRateText { get; }

    public string CreditLimitText { get; }

    public string HeadroomText { get; }

    public IReadOnlyList<LoanRowViewModel> Loans { get; }

    public bool HasLoans { get; }

    /// <summary>True when there is no debt — the loans panel shows its empty state.</summary>
    public bool NoLoans => !HasLoans;

    public bool InArrears { get; }

    public IReadOnlyList<SponsorRowViewModel> Sponsors { get; }

    public bool HasSponsors { get; }

    /// <summary>True when the team has no sponsors — the sponsor panel shows its empty state.</summary>
    public bool NoSponsors => !HasSponsors;

    // --- Borrow panel (M22c) ---

    /// <summary>Whether the borrow panel is offered (active bank + headroom + a wired callback).</summary>
    public bool CanOfferBorrow { get; }

    /// <summary>The most that can be drawn now, as a slider maximum.</summary>
    public double HeadroomValue => _headroomAmount;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(BorrowCommand))]
    private double _borrowAmount;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(BorrowCommand))]
    private double _borrowTermValue = 5;

    /// <summary>The whole-season term the slider currently sits on.</summary>
    public int BorrowTerm => (int)BorrowTermValue;

    [ObservableProperty]
    private string _borrowSummaryText = "";

    /// <summary>Whether the entered draw is valid to take right now.</summary>
    public bool CanBorrowNow
    {
        get
        {
            var amount = (long)BorrowAmount;
            return _borrow is not null && BankActive && amount > 0 && amount <= _headroomAmount && BorrowTerm > 0;
        }
    }

    [RelayCommand(CanExecute = nameof(CanBorrowNow))]
    private void Borrow() => _borrow?.Invoke((long)BorrowAmount, BorrowTerm);

    partial void OnBorrowAmountChanged(double value) => UpdateBorrowSummary();

    partial void OnBorrowTermValueChanged(double value)
    {
        OnPropertyChanged(nameof(BorrowTerm));
        UpdateBorrowSummary();
    }

    // A preview of the first-season instalment for the entered amount/term at the current offered rate.
    private void UpdateBorrowSummary()
    {
        var amount = (long)BorrowAmount;
        if (amount <= 0 || BorrowTerm <= 0)
        {
            BorrowSummaryText = "";
            return;
        }

        var interest = amount * _offeredRate / 100;
        var principal = BorrowTerm > 1 ? amount / BorrowTerm : amount;
        var instalment = interest + principal;
        BorrowSummaryText = string.Create(
            CultureInfo.InvariantCulture,
            $"{Money(amount)} over {BorrowTerm} seasons · ≈ {Money(instalment)} / season");
    }

    /// <summary>Next season's straight-line instalment for a loan — interest at its frozen rate plus the
    /// principal share (the final/overdue season demands the balance). Mirrors <see cref="BankLedger"/>.</summary>
    private static long Instalment(Loan loan)
    {
        var interest = loan.OutstandingBalance * loan.AnnualRatePercent / 100;
        var principal = loan.SeasonsRemaining > 1
            ? loan.OutstandingBalance / loan.SeasonsRemaining
            : loan.OutstandingBalance;
        return interest + principal;
    }

    private static string Money(long value)
    {
        var sign = value < 0 ? "-" : "";
        var abs = Math.Abs(value);
        if (abs >= 1_000_000)
        {
            return string.Create(CultureInfo.InvariantCulture, $"{sign}${abs / 1_000_000.0:0.#}M");
        }

        if (abs >= 1_000)
        {
            return string.Create(CultureInfo.InvariantCulture, $"{sign}${abs / 1_000.0:0}k");
        }

        return string.Create(CultureInfo.InvariantCulture, $"{sign}${abs}");
    }
}

/// <summary>One outstanding loan on the finance screen (M22b).</summary>
public sealed record LoanRowViewModel(
    string Lender, string Outstanding, string Rate, int SeasonsRemaining, string Instalment,
    int MissedPayments, bool InArrears, IBrush StatusBrush);

/// <summary>One sponsor on the finance screen (M22b).</summary>
public sealed record SponsorRowViewModel(string Name, string Tier, string Fee, IBrush Accent);
