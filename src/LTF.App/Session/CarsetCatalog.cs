using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LTF.Content;
using LTF.Domain;

namespace LTF.App.Session;

/// <summary>
/// Discovers the carsets bundled next to the app (mirrors <see cref="SessionLoader"/>: file I/O here, pure
/// parsing in <see cref="CarsetLoader"/>). Each carset's JSON is read and parsed once, then cached with its
/// <see cref="CarsetSummary"/>, so the new-career flow can list carsets, pick a team and start a session
/// with no re-parsing. Only a couple of carsets ship, so eager discovery is fine.
/// </summary>
public sealed class CarsetCatalog
{
    private CarsetCatalog(IReadOnlyList<CarsetDescriptor> descriptors) => Descriptors = descriptors;

    /// <summary>The carsets folder copied next to the app (see LTF.App.csproj).</summary>
    public static string CarsetsDir => Path.Combine(AppContext.BaseDirectory, "carsets");

    /// <summary>The discovered carsets, ordered by display name.</summary>
    public IReadOnlyList<CarsetDescriptor> Descriptors { get; }

    /// <summary>Read and parse every <c>carsets/*.json</c> next to the app.</summary>
    public static CarsetCatalog Discover() => DiscoverFrom(CarsetsDir);

    /// <summary>Discovery against an explicit directory (used by tests).</summary>
    public static CarsetCatalog DiscoverFrom(string dir)
    {
        var descriptors = new List<CarsetDescriptor>();
        if (Directory.Exists(dir))
        {
            foreach (var path in Directory.EnumerateFiles(dir, "*.json"))
            {
                var carset = CarsetLoader.LoadFromJson(File.ReadAllText(path));
                descriptors.Add(new CarsetDescriptor(CarsetSummary.From(carset), carset, path));
            }
        }

        descriptors.Sort((a, b) => string.CompareOrdinal(a.Summary.Name, b.Summary.Name));
        return new CarsetCatalog(descriptors);
    }

    /// <summary>The discovered carset for an id, or null if none matches.</summary>
    public CarsetDescriptor? Find(string carsetId) =>
        Descriptors.FirstOrDefault(d => string.CompareOrdinal(d.Summary.Id, carsetId) == 0);
}

/// <summary>A discovered carset: its UI summary, the fully-parsed carset, and the file it came from.</summary>
public sealed record CarsetDescriptor(CarsetSummary Summary, Carset Carset, string Path);
