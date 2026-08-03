using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using Velopack;
using Velopack.Sources;

namespace LightsToFlag.App.Services;

public enum UpdateOutcome
{
    NoToken,
    NotInstalled,
    UpToDate,
    Updating,
    Failed,
}

public sealed record UpdateResult(UpdateOutcome Outcome, string Message);

/// <summary>
/// Checks GitHub Releases for a newer build and, if found, downloads it and
/// restarts into it. The repository is private, so the GitHub source needs a read
/// token — read from the environment or a local file, never baked into the binary.
/// Every step is written to <c>%AppData%/LightsToFlag/update.log</c> so a failed or
/// skipped update is diagnosable instead of silent.
/// </summary>
public sealed class UpdateService
{
    private const string RepoUrl = "https://github.com/ahmettdc/lights-to-flag-2";
    private const string TokenEnvVar = "LTF_UPDATE_TOKEN";
    private const string TokenFileName = "update-token.txt";

    private static readonly string LogPath = Path.Combine(AppDataDir(), "update.log");

    /// <summary>Startup check: apply silently if an update is found, log everything.</summary>
    public async Task CheckAndApplyAsync()
    {
        await CheckAsync(apply: true).ConfigureAwait(false);
    }

    /// <summary>User-initiated check from the menu: report the outcome in a dialog.</summary>
    public async Task CheckAndReportAsync()
    {
        var result = await CheckAsync(apply: true).ConfigureAwait(false);

        // If we're actually applying, the app restarts, so no dialog is shown.
        if (result.Outcome == UpdateOutcome.Updating)
        {
            return;
        }

        var image = result.Outcome == UpdateOutcome.Failed ? MessageBoxImage.Warning : MessageBoxImage.Information;
        MessageBox.Show(result.Message, "Lights to Flag 2 — Updates", MessageBoxButton.OK, image);
    }

    public async Task<UpdateResult> CheckAsync(bool apply)
    {
        Log("check start (v" + AppVersion() + ")");

        var token = ResolveToken();
        if (string.IsNullOrWhiteSpace(token))
        {
            Log("no token found");
            return new UpdateResult(UpdateOutcome.NoToken,
                "No update token found.\nAdd your GitHub token to:\n" +
                Path.Combine(AppDataDir(), TokenFileName));
        }

        try
        {
            var manager = new UpdateManager(new GithubSource(RepoUrl, token, prerelease: false));

            if (!manager.IsInstalled)
            {
                Log("not an installed build; skipping");
                return new UpdateResult(UpdateOutcome.NotInstalled,
                    "This is not an installed build, so it cannot self-update. Install via the Setup.exe first.");
            }

            Log("checking github releases…");
            var updates = await manager.CheckForUpdatesAsync().ConfigureAwait(false);
            if (updates is null)
            {
                Log("already up to date");
                return new UpdateResult(UpdateOutcome.UpToDate, "You're already on the latest version.");
            }

            var version = updates.TargetFullRelease?.Version?.ToString() ?? "?";
            Log("update available: " + version);

            if (apply)
            {
                Log("downloading…");
                await manager.DownloadUpdatesAsync(updates).ConfigureAwait(false);
                Log("download complete; applying and restarting");
                manager.ApplyUpdatesAndRestart(updates);
            }

            return new UpdateResult(UpdateOutcome.Updating,
                "Update " + version + " found — downloading now. The game will restart automatically.");
        }
        catch (Exception ex)
        {
            Log("ERROR: " + ex);
            return new UpdateResult(UpdateOutcome.Failed, "Update check failed:\n" + ex.Message);
        }
    }

    private static string? ResolveToken()
    {
        var fromEnv = Environment.GetEnvironmentVariable(TokenEnvVar);
        if (!string.IsNullOrWhiteSpace(fromEnv))
        {
            return fromEnv.Trim();
        }

        foreach (var dir in new[] { AppContext.BaseDirectory, AppDataDir() })
        {
            var path = Path.Combine(dir, TokenFileName);
            if (File.Exists(path))
            {
                var text = File.ReadAllText(path).Trim();
                if (text.Length > 0)
                {
                    return text;
                }
            }
        }

        return null;
    }

    private static void Log(string message)
    {
        try
        {
            Directory.CreateDirectory(AppDataDir());
            File.AppendAllText(LogPath, DateTime.Now.ToString("u") + "  " + message + Environment.NewLine);
        }
        catch
        {
            // logging must never crash the app
        }
    }

    private static string AppVersion() =>
        typeof(UpdateService).Assembly.GetName().Version?.ToString() ?? "?";

    private static string AppDataDir() => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "LightsToFlag");
}
