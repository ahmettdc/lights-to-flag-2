using System.Text.Json;
using System.Text.Json.Serialization;
using LightsToFlag.Core.Career;

namespace LightsToFlag.Core.Saves;

/// <summary>
/// Serializes <see cref="CareerState"/> to and from a clean, human-readable JSON
/// document. Only the career state is persisted; the immutable carset is
/// referenced by <see cref="CareerState.CarsetName"/> and reloaded on open.
/// </summary>
public static class CareerSaveSerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() },
    };

    public static string Serialize(CareerState state) => JsonSerializer.Serialize(state, Options);

    public static CareerState Deserialize(string json)
    {
        var state = JsonSerializer.Deserialize<CareerState>(json, Options);
        if (state is null)
        {
            throw new InvalidDataException("Save file did not contain a career state.");
        }

        return state;
    }
}
