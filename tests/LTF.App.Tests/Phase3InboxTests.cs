using System.IO;
using System.Linq;
using LTF.App.Navigation;
using LTF.App.Notifications;
using LTF.App.Services;
using LTF.App.Session;
using LTF.App.Settings;
using LTF.App.ViewModels;
using LTF.Domain.Common;
using LTF.Domain.Management;
using Xunit;

namespace LTF.App.Tests;

/// <summary>
/// The dated inbox + Rev-15 halt (M21e): Continue raises dated news per event, a contract deadline is an
/// action-required item that pauses Continue until acknowledged (without skipping any round), and the shell
/// inbox accumulates the feed as the career advances.
/// </summary>
public class Phase3InboxTests
{
    // A flagship career with one expiring driver contract, so the career calendar carries a contract-deadline
    // event (the shipped carsets have none) — the trigger for the Rev-15 halt.
    private static ShellSession WithExpiringContract()
    {
        var flagship = SessionLoader.LoadFlagship();
        var team = flagship.Carset.PlayerTeam()!;
        var expiring = new Contract
        {
            Kind = ContractKind.Driver,
            PartyId = team.DriverIds[0],
            TeamId = team.Id,
            SalaryPerSeason = 5_000_000,
            SeasonsRemaining = 1,
        };
        var carset = flagship.Carset with { Contracts = new[] { expiring } };
        return ShellSession.FromCarset(carset, flagship.Seed);
    }

    [Fact]
    public void A_race_continue_raises_a_dated_race_notification()
    {
        var live = new LiveCareer(SessionLoader.LoadFlagship());

        Notification? race = null;
        var guard = 0;
        while (race is null && !live.SeasonComplete && guard++ < 200)
        {
            live.Continue();
            race = live.LastStepNews.FirstOrDefault(n => n.Category == NotificationCategory.Race);
        }

        Assert.NotNull(race);
        Assert.Equal(live.Clock.Date, race!.Date); // dated to the race day
        Assert.False(race.RequiresAction);
    }

    [Fact]
    public void A_contract_deadline_raises_an_action_required_item_and_halts_continue()
    {
        var live = new LiveCareer(WithExpiringContract());

        var guard = 0;
        while (live.CanContinue && guard++ < 400)
        {
            live.Continue();
        }

        // Continue stopped short of the season end, paused on an action-required item.
        Assert.True(live.PendingAction);
        Assert.False(live.CanContinue);
        Assert.False(live.SeasonComplete);
        Assert.Contains(live.LastStepNews, n => n.RequiresAction && n.Category == NotificationCategory.Staff);

        // Acknowledging clears the pause so Continue can advance again.
        live.Acknowledge();
        Assert.True(live.CanContinue);
        Assert.False(live.PendingAction);
    }

    [Fact]
    public void Acknowledging_through_a_halt_still_runs_every_round()
    {
        var session = WithExpiringContract();
        var live = new LiveCareer(session);

        var guard = 0;
        while (!live.SeasonComplete && guard++ < 500)
        {
            if (live.PendingAction)
            {
                live.Acknowledge();
            }
            else
            {
                live.Continue();
            }
        }

        Assert.True(live.SeasonComplete);
        Assert.Equal(session.Carset.Calendar.Count, live.Results.Count); // the pause skipped nothing
    }

    [Fact]
    public void The_shell_inbox_starts_empty_and_accumulates_the_career_feed()
    {
        var catalog = CarsetCatalog.Discover();
        var dir = Directory.CreateTempSubdirectory().FullName;
        var saves = new SaveStore(catalog, dir);
        var settings = new SettingsStore(Path.Combine(dir, "settings.json"));
        var feed = new CareerNotificationSource();
        var root = new RootViewModel(new AppServices(catalog, saves, settings, feed));

        root.EnterCareer(SessionLoader.LoadFlagship());
        var shell = (ShellViewModel)root.Content!;

        Assert.Empty(shell.Inbox.Items); // a fresh career opens on an empty feed

        var guard = 0;
        while (shell.TopBar.CanContinue && guard++ < 500)
        {
            shell.TopBar.ContinueCommand.Execute(null);
        }

        Assert.NotEmpty(shell.Inbox.Items);
        Assert.Contains(shell.Inbox.Items, i => i.Model.Category == NotificationCategory.Race);
    }
}
