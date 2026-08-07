using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using LTF.App.Mvvm;
using LTF.Domain;
using LTF.Domain.Management;

namespace LTF.App.ViewModels.Screens;

/// <summary>
/// The staff screen (M22f): the player team's technical staff and the free-agent pool. Read-only projection
/// of each member's role, skill and salary; when the hire/release callbacks are wired, each row carries its
/// own action so the player can release a team member to the pool or hire a free agent onto the team
/// (StaffLedger, applied to the season-start carset). A Continue rebuilds it.
/// </summary>
public sealed class StaffViewModel : ViewModelBase
{
    public StaffViewModel(Carset carset, Action<string>? hire = null, Action<string>? release = null)
    {
        var team = carset.PlayerTeam();
        HasTeam = team is not null;

        Squad = (team?.Staff ?? [])
            .Select(s => Row(s, "RELEASE", release is not null ? new RelayCommand(() => release(s.Id)) : null))
            .ToList();
        HasSquad = Squad.Count > 0;

        Pool = carset.StaffPool
            .Select(s => Row(s, "HIRE", hire is not null ? new RelayCommand(() => hire(s.Id)) : null))
            .ToList();
        HasPool = Pool.Count > 0;
    }

    public bool HasTeam { get; }

    public IReadOnlyList<StaffRowViewModel> Squad { get; }

    public bool HasSquad { get; }

    public bool NoSquad => !HasSquad;

    public IReadOnlyList<StaffRowViewModel> Pool { get; }

    public bool HasPool { get; }

    public bool NoPool => !HasPool;

    private static StaffRowViewModel Row(Staff s, string actionLabel, ICommand? action) => new(
        s.FullName,
        Spaced(s.Role.ToString()),
        s.Skill.Value,
        Money(s.Salary),
        actionLabel,
        action,
        action is not null);

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
        var abs = Math.Abs(value);
        return abs >= 1_000_000
            ? string.Create(CultureInfo.InvariantCulture, $"${abs / 1_000_000.0:0.#}M")
            : string.Create(CultureInfo.InvariantCulture, $"${abs / 1_000.0:0}k");
    }
}

/// <summary>One staff member on the staff screen (M22f), with an optional hire/release action.</summary>
public sealed record StaffRowViewModel(
    string Name, string Role, int Skill, string Salary, string ActionLabel, ICommand? Action, bool CanAct);
