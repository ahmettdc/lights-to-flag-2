using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using LTF.App.Services;
using LTF.App.Session;
using LTF.App.ViewModels;

namespace LTF.App;

/// <summary>Root Avalonia application. Builds the shell over the flagship carset on desktop.</summary>
public partial class App : Application
{
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Composition root (DI-lite): load the session, wire the services, build the shell VM.
            // File I/O lives at this boundary; headless tests skip this block (no desktop lifetime)
            // and build the shell from an injected in-memory snapshot instead.
            var session = SessionLoader.LoadFlagship();
            var snapshot = new SessionSnapshot(session);
            var navigation = new NavigationService();
            var notifications = new SampleNotificationSource();
            var shell = new ShellViewModel(navigation, snapshot, notifications);

            desktop.MainWindow = new MainWindow { DataContext = shell };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
