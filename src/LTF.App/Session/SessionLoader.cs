using System.IO;
using LTF.Content;

namespace LTF.App.Session;

/// <summary>
/// Loads the shell's starting session from a carset on disk. File I/O lives here at the app boundary
/// (like LTF.Tools), then hands the text to the I/O-free <see cref="CarsetLoader"/>. A fixed seed is used
/// in M19; M20's new-career flow will choose it.
/// </summary>
public static class SessionLoader
{
    /// <summary>The seed used until the new-career flow (M20) picks one.</summary>
    public const int DefaultSeed = 20260101;

    /// <summary>The flagship carset copied next to the app (see LTF.App.csproj).</summary>
    public static string FlagshipPath =>
        Path.Combine(AppContext.BaseDirectory, "carsets", "global-prix.json");

    public static ShellSession Load(string carsetPath, int seed = DefaultSeed)
    {
        var json = File.ReadAllText(carsetPath);
        var carset = CarsetLoader.LoadFromJson(json);
        return ShellSession.FromCarset(carset, seed);
    }

    public static ShellSession LoadFlagship(int seed = DefaultSeed) => Load(FlagshipPath, seed);
}
