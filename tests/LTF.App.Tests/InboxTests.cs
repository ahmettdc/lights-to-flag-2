using System.Linq;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using LTF.App.Navigation;
using LTF.App.Services;
using LTF.App.ViewModels;
using LTF.App.Views.Notifications;
using Xunit;

namespace LTF.App.Tests;

/// <summary>
/// The notification centre: unread count tracks unread items, opening a row navigates its deep link and
/// marks it read (decrementing the count), and the popover renders one card per notification headless.
/// </summary>
public class InboxTests
{
    private static InboxViewModel MakeInbox(NavigationService navigation) =>
        new(new SampleNotificationSource(), navigation);

    [Fact]
    public void Unread_count_starts_at_the_item_count()
    {
        var inbox = MakeInbox(new NavigationService());

        Assert.True(inbox.HasItems);
        Assert.Equal(inbox.Items.Count, inbox.UnreadCount);
    }

    [Fact]
    public void Opening_a_notification_navigates_and_marks_it_read()
    {
        var nav = new NavigationService();
        var inbox = MakeInbox(nav);
        var item = inbox.Items.First(i => i.DeepLink == NavKey.Finance);
        var before = inbox.UnreadCount;

        item.OpenCommand.Execute(null);

        Assert.True(item.IsRead);
        Assert.Equal(NavKey.Finance, nav.CurrentKey);
        Assert.Equal(before - 1, inbox.UnreadCount);
    }

    [AvaloniaFact]
    public void Popover_renders_one_card_per_notification()
    {
        var inbox = MakeInbox(new NavigationService());
        var popover = new InboxPopover { DataContext = inbox };
        var window = new Window { Content = popover };

        window.Show();
        Dispatcher.UIThread.RunJobs();

        var cards = popover.GetVisualDescendants()
            .OfType<Button>()
            .Where(b => b.Classes.Contains("ghost"))
            .ToList();

        Assert.Equal(inbox.Items.Count, cards.Count);
    }
}
