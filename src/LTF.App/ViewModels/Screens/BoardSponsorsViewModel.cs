using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Avalonia.Media;
using LTF.App.Converters;
using LTF.App.Mvvm;
using LTF.Domain;
using LTF.Domain.Common;
using LTF.Domain.Management;

namespace LTF.App.ViewModels.Screens;

/// <summary>
/// The board &amp; sponsors screen (M22g): the player team's board — its ownership, the six pressure metrics
/// (board confidence and the five pressures), the firing risk, the members and their confidence, and the
/// season objectives — plus the sponsor book. Read-only projection over the current carset; a Continue
/// rebuilds it, and the board now evolves live at each season boundary (M22a). A carset that ships no board
/// (an AI-run team) shows the empty state.
/// </summary>
public sealed class BoardSponsorsViewModel : ViewModelBase
{
    public BoardSponsorsViewModel(Carset carset)
    {
        var team = carset.PlayerTeam();
        HasTeam = team is not null;

        var board = team is not null
            ? carset.Boards.FirstOrDefault(b => string.CompareOrdinal(b.TeamId, carset.PlayerTeamId) == 0)
            : null;
        HasBoard = board is not null;

        if (board is not null)
        {
            OwnershipText = Spaced(board.Ownership.ToString());

            var m = board.Metrics;
            Metrics =
            [
                Metric("Board confidence", m.BoardConfidence.Value, higherIsBetter: true),
                Metric("Sporting pressure", m.SportingPressure.Value, higherIsBetter: false),
                Metric("Financial pressure", m.FinancialPressure.Value, higherIsBetter: false),
                Metric("Sponsor pressure", m.SponsorPressure.Value, higherIsBetter: false),
                Metric("Media pressure", m.MediaPressure.Value, higherIsBetter: false),
                Metric("Internal pressure", m.InternalPressure.Value, higherIsBetter: false),
            ];

            FiringRisk = board.FiringRisk.Value;
            FiringRiskText = string.Create(CultureInfo.InvariantCulture, $"{board.FiringRisk.Value}%");
            FiringRiskBrush = StatusColorConverter.Classify(100 - board.FiringRisk.Value);
            FiringRiskBarWidth = 96.0 * board.FiringRisk.Value / 100.0;

            Members = board.Members
                .Select(member => new BoardMemberRowViewModel(
                    member.Name.Length > 0 ? member.Name : member.Id,
                    member.ConfidenceInPlayer.Value,
                    StatusColorConverter.Classify(member.ConfidenceInPlayer.Value)))
                .ToList();

            Objectives = board.Objectives
                .Select(o => new ObjectiveRowViewModel(ObjectiveText(o.Kind, o.Target)))
                .ToList();
        }
        else
        {
            Metrics = [];
            Members = [];
            Objectives = [];
            OwnershipText = "";
            FiringRiskText = "—";
        }

        HasMembers = Members.Count > 0;
        HasObjectives = Objectives.Count > 0;

        Sponsors = (team?.Sponsors ?? [])
            .Select((s, i) => new SponsorRowViewModel(
                s.Name, s.Tier.ToString().ToUpperInvariant(), Money(s.PerRaceFee), ScreenBrushes.TeamAccent(i)))
            .ToList();
        HasSponsors = Sponsors.Count > 0;
    }

    public bool HasTeam { get; }

    public bool HasBoard { get; }

    public bool NoBoard => !HasBoard;

    public string OwnershipText { get; } = "";

    public IReadOnlyList<MetricRowViewModel> Metrics { get; }

    public int FiringRisk { get; }

    public string FiringRiskText { get; }

    public IBrush FiringRiskBrush { get; } = ScreenBrushes.Dim;

    public double FiringRiskBarWidth { get; }

    public IReadOnlyList<BoardMemberRowViewModel> Members { get; }

    public bool HasMembers { get; }

    public IReadOnlyList<ObjectiveRowViewModel> Objectives { get; }

    public bool HasObjectives { get; }

    public IReadOnlyList<SponsorRowViewModel> Sponsors { get; }

    public bool HasSponsors { get; }

    public bool NoSponsors => !HasSponsors;

    private static MetricRowViewModel Metric(string name, int value, bool higherIsBetter) =>
        new(name, value, 96.0 * value / 100.0, StatusColorConverter.Classify(higherIsBetter ? value : 100 - value));

    private static string ObjectiveText(ObjectiveKind kind, int target) => kind switch
    {
        ObjectiveKind.ConstructorPosition => string.Create(CultureInfo.InvariantCulture, $"Constructors' championship ≤ P{target}"),
        ObjectiveKind.DriverPosition => string.Create(CultureInfo.InvariantCulture, $"Best driver ≤ P{target}"),
        ObjectiveKind.ConstructorPoints => string.Create(CultureInfo.InvariantCulture, $"Score ≥ {target} points"),
        ObjectiveKind.FinancialResult => target <= 0
            ? "Finish the season in the black"
            : string.Create(CultureInfo.InvariantCulture, $"Balance ≥ {Money(target)}"),
        ObjectiveKind.RaceWins => string.Create(CultureInfo.InvariantCulture, $"Win ≥ {target} race(s)"),
        _ => string.Create(CultureInfo.InvariantCulture, $"{Spaced(kind.ToString())}: {target}"),
    };

    private static string Spaced(string pascal)
    {
        var sb = new StringBuilder(pascal.Length + 4);
        for (var i = 0; i < pascal.Length; i++)
        {
            if (i > 0 && char.IsUpper(pascal[i]))
            {
                sb.Append(' ');
            }

            sb.Append(pascal[i]);
        }

        return sb.ToString();
    }

    private static string Money(long value)
    {
        var abs = System.Math.Abs(value);
        var sign = value < 0 ? "-" : "";
        if (abs >= 1_000_000)
        {
            return string.Create(CultureInfo.InvariantCulture, $"{sign}${abs / 1_000_000.0:0.#}M");
        }

        return abs >= 1_000
            ? string.Create(CultureInfo.InvariantCulture, $"{sign}${abs / 1_000.0:0}k")
            : string.Create(CultureInfo.InvariantCulture, $"{sign}${abs}");
    }
}

/// <summary>One board pressure metric on the board screen (M22g).</summary>
public sealed record MetricRowViewModel(string Name, int Value, double BarWidth, IBrush Brush);

/// <summary>One board member on the board screen (M22g).</summary>
public sealed record BoardMemberRowViewModel(string Name, int Confidence, IBrush ConfidenceBrush);

/// <summary>One season objective on the board screen (M22g).</summary>
public sealed record ObjectiveRowViewModel(string Text);
