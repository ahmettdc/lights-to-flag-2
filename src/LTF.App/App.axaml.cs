using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using LTF.App.Services;
using LTF.App.Session;
using LTF.App.ViewModels;

namespace LTF.App;

/// <summary>Root Avalonia application. Starts on the main menu and swaps to the in-game shell (M20).</summary>
public partial class App : Application
{
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Composition root (DI-lite): build the application root, which lands on the main menu and
            // swaps to the shell when a career is entered. Headless tests skip this block (no desktop
            // lifetime) and drive a RootViewModel directly. File I/O lives behind RootViewModel's flows.
            var catalog = CarsetCatalog.Discover();
            var notifications = new SampleNotificationSource();
            var root = new RootViewModel(catalog, notifications, quit: () => desktop.Shutdown());

            desktop.MainWindow = new MainWindow { DataContext = root };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
