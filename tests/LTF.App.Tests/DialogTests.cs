using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using LTF.App.Controls;
using LTF.App.Services;
using Xunit;

namespace LTF.App.Tests;

/// <summary>
/// The confirmation dialog flow: DialogService shows a dialog in the overlay, the Confirm/Cancel buttons
/// resolve the awaited result true/false, and the dialog is removed afterwards. Headless on all 3 OSes.
/// </summary>
public class DialogTests
{
    private static Window Host()
    {
        var window = new Window { Content = new TextBlock { Text = "x" } };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        return window;
    }

    private static Button TemplateButton(ConfirmationDialog dialog, string name) =>
        dialog.GetVisualDescendants().OfType<Button>().Single(b => b.Name == name);

    [AvaloniaFact]
    public async Task Confirm_button_resolves_true_and_removes_the_dialog()
    {
        var window = Host();
        var task = new DialogService().Confirm(window, "Exit?", "Are you sure?");
        Dispatcher.UIThread.RunJobs();

        var layer = OverlayLayer.GetOverlayLayer(window)!;
        var dialog = layer.Children.OfType<ConfirmationDialog>().Single();
        TemplateButton(dialog, "PART_Confirm").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Dispatcher.UIThread.RunJobs();

        Assert.True(await task);
        Assert.DoesNotContain(layer.Children, c => c is ConfirmationDialog);
    }

    [AvaloniaFact]
    public async Task Cancel_button_resolves_false()
    {
        var window = Host();
        var task = new DialogService().Confirm(window, "Exit?", "Are you sure?");
        Dispatcher.UIThread.RunJobs();

        var dialog = OverlayLayer.GetOverlayLayer(window)!.Children.OfType<ConfirmationDialog>().Single();
        TemplateButton(dialog, "PART_Cancel").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Dispatcher.UIThread.RunJobs();

        Assert.False(await task);
    }
}
