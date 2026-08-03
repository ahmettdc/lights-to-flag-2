namespace LightsToFlag.Tests;

/// <summary>Helpers for locating the carset test data copied next to the test binaries.</summary>
internal static class TestCarsets
{
    public const string F1_2019 = "F1 2019";

    public static string Path(string carsetName)
    {
        var path = System.IO.Path.Combine(AppContext.BaseDirectory, "carsets", carsetName);
        if (!Directory.Exists(path))
        {
            throw new DirectoryNotFoundException(
                $"Test carset '{carsetName}' not found at {path}. " +
                "Ensure the carsets/ folder is copied to the test output (see LightsToFlag.Tests.csproj).");
        }

        return path;
    }
}
