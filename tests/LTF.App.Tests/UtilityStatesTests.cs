using System.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using CommunityToolkit.Mvvm.Input;
using LTF.App.Controls;
using LTF.App.Services;
using Xunit;

namespace LTF.App.Tests;

/// <summary>
/// The utility states + toast (uieksikler item 10): each renders headless, the error state wires and
/// fires its Retry command and hides it when absent, and the toast service reaches the overlay layer.
/// </summary>
public class UtilityStatesTests
{
    private static Window Host(Control content)
    {
        var window = new Window { Content = content };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        return window;
    }

    [AvaloniaFact]
    public void Empty_state_shows_its_title()
    {
        var empty = new EmptyState { Title = "Nothing here", Message = "add one" };
        Host(empty);

        var texts = empty.GetVisualDescendants().OfType<TextBlock>().Select(t => t.Text).ToList();
        Assert.Contains("Nothing here", texts);
    }

    [AvaloniaFact]
    public void Loading_state_defaults_to_loading_title()
    {
        var loading = new LoadingState();
        Host(loading);

        Assert.Equal("Loading…", loading.Title);
    }

    [AvaloniaFact]
    public void Error_state_retry_button_binds_and_fires()
    {
        var fired = false;
        var error = new ErrorState
        {
            Title = "Oops",
            Message = "bad",
            RetryCommand = new RelayCommand(() => fired = true),
        };
        Host(error);

        var button = error.GetVisualDescendants().OfType<Button>().FirstOrDefault();
        Assert.NotNull(button);
        Assert.True(button!.IsVisible);
        Assert.Same(error.RetryCommand, button.Command);

        button.Command!.Execute(null);
        Assert.True(fired);
    }

    [AvaloniaFact]
    public void Error_state_hides_retry_when_no_command()
    {
        var error = new ErrorState { Title = "Oops" };
        Host(error);

        var button = error.GetVisualDescendants().OfType<Button>().FirstOrDefault();
        Assert.True(button is null || !button.IsVisible);
    }

    [AvaloniaFact]
    public void Toast_service_adds_a_toast_to_the_overlay()
    {
        var window = Host(new TextBlock { Text = "x" });

        new ToastService().Show(window, "Saved", ToastTone.Success);
        Dispatcher.UIThread.RunJobs();

        var layer = OverlayLayer.GetOverlayLayer(window);
        Assert.NotNull(layer);
        Assert.Contains(layer!.Children, c => c is Toast);
    }
}
