using LightsToFlag.Core.Domain;

namespace LightsToFlag.Core.Data;

/// <summary>Loads a <see cref="Carset"/> from a folder on disk.</summary>
public interface ICarsetLoader
{
    /// <summary>
    /// Load the carset stored in <paramref name="carsetFolder"/>.
    /// </summary>
    /// <exception cref="CarsetValidationException">The carset is missing or malformed.</exception>
    Carset Load(string carsetFolder);
}
