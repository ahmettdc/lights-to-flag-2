using System;
using System.IO;
using System.Windows;
using LightsToFlag.App.Services;
using LightsToFlag.App.Shell;
using LightsToFlag.App.ViewModels;
using LightsToFlag.Core.Career;
using LightsToFlag.Core.Data;
using LightsToFlag.Core.Saves;
using Microsoft.Extensions.DependencyInjection;

namespace LightsToFlag.App;

/// <summary>WPF application entry point and dependency-injection composition root.</summary>
public partial class App : Application
{
    private IServiceProvider? _services;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var services = new ServiceCollection();

        var savesRoot = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "LightsToFlag", "Saves");

        // Core services
        services.AddSingleton<ICarsetLoader, LegacyTextCarsetLoader>();
        services.AddSingleton<ISaveStore>(_ => new FileSaveStore(savesRoot));
        services.AddSingleton<CareerEngine>();

        // App services
        services.AddSingleton<CarsetCatalog>();
        services.AddSingleton<GameSession>();
        services.AddSingleton<ShellViewModel>();
        services.AddSingleton<INavigationService, NavigationService>();
        services.AddSingleton<ShellWindow>();

        // Pages (fresh each navigation)
        services.AddTransient<MainMenuViewModel>();
        services.AddTransient<NewCareerViewModel>();
        services.AddTransient<LoadGameViewModel>();
        services.AddTransient<CareerViewModel>();
        services.AddTransient<RaceWeekendViewModel>();
        services.AddTransient<AboutViewModel>();

        _services = services.BuildServiceProvider();

        var shell = _services.GetRequiredService<ShellWindow>();
        shell.DataContext = _services.GetRequiredService<ShellViewModel>();

        _services.GetRequiredService<INavigationService>().NavigateTo<MainMenuViewModel>();
        shell.Show();
    }
}
