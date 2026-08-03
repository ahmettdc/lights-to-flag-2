namespace LightsToFlag.Core.Data;

/// <summary>
/// Thrown when a carset on disk is malformed or internally inconsistent
/// (missing file, wrong record count, unparseable field, …).
/// </summary>
public sealed class CarsetValidationException : Exception
{
    public CarsetValidationException(string message) : base(message)
    {
    }

    public CarsetValidationException(string message, Exception inner) : base(message, inner)
    {
    }
}
