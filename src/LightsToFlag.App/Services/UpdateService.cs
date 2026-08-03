using System;
using System.IO;
using System.Threading.Tasks;
using Velopack;
using Velopack.Sources;

namespace LightsToFlag.App.Services;

/// <summary>
/// Checks GitHub Releases for a newer build and, if found, downloads it and
/// restarts into it. The repository is private, so the GitHub source needs a
/// read token — it is read from the environment or a local file rather than being
/// baked into the binary. With no token (or when running uninstalled, e.g. from a
/// dev build) the check is skipped silently.
/// </summary>
public sealed class UpdateService
{
    private const string RepoUrl = "https://github.com/ahmettdc/lights-to-flag-2";
    private const string TokenEnvVar = "LTF_UPDATE_TOKEN";
    private const string TokenFileName = "update-token.txt";

    public async Task CheckAndApplyAsync()
    {
        try
        {
            var token = ResolveToken();
            if (string.IsNullOrWhiteSpace(token))
            {
                return; // no credentials → nothing to do
            }

            var manager = new UpdateManager(new GithubSource(RepoUrl, token, prerelease: false));
            if (!manager.IsInstalled)
            {
                return; // running from a dev build, not an installed app
            }

            var updates = await manager.CheckForUpdatesAsync().ConfigureAwait(false);
            if (updates is null)
            {
                return; // already up to date
            }

            await manager.DownloadUpdatesAsync(updates).ConfigureAwait(false);
            manager.ApplyUpdatesAndRestart(updates);
        }
        catch
        {
            // Never let an update failure crash the game; try again next launch.
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

    private static string AppDataDir() => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "LightsToFlag");
}
