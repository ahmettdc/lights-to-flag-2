using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace LightsToFlag.App.Services;

/// <summary>
/// Turns otherwise-silent crashes into something diagnosable: writes the full
/// exception to <c>%AppData%/LightsToFlag/crash.log</c> and shows a message box.
/// Without this an unhandled exception just closes the window with no clue why.
/// </summary>
public static class CrashLogger
{
    private static readonly string LogPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "LightsToFlag", "crash.log");

    /// <summary>Hook the three places an unhandled exception can surface in a WPF app.</summary>
    public static void Install(Application app)
    {
        app.DispatcherUnhandledException += OnDispatcher;
        AppDomain.CurrentDomain.UnhandledException += OnAppDomain;
        TaskScheduler.UnobservedTaskException += OnUnobservedTask;
    }

    private static void OnDispatcher(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        Report(e.Exception, "Dispatcher");
        // Keep the app alive so the user can read the message and report it.
        e.Handled = true;
    }

    private static void OnAppDomain(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
        {
            Report(ex, "AppDomain");
        }
    }

    private static void OnUnobservedTask(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        Report(e.Exception, "Task");
        e.SetObserved();
    }

    private static void Report(Exception ex, string source)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(LogPath)!);
            var sb = new StringBuilder();
            sb.AppendLine("==== " + DateTime.Now.ToString("u") + " [" + source + "] ====");
            sb.AppendLine(ex.ToString());
            sb.AppendLine();
            File.AppendAllText(LogPath, sb.ToString());
        }
        catch
        {
            // Logging must never itself crash the crash handler.
        }

        try
        {
            MessageBox.Show(
                ex.Message + "\n\nDetails were written to:\n" + LogPath,
                "Lights to Flag 2 — unexpected error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        catch
        {
            // Ignore if we cannot show UI (e.g. during shutdown).
        }
    }
}
