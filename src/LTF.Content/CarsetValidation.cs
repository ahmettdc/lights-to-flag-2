namespace LTF.Content;

/// <summary>Thrown when a carset cannot be loaded because it is structurally invalid.</summary>
public sealed class CarsetValidationException : Exception
{
    public CarsetValidationException(string message) : base(message) { }
}

/// <summary>Severity of a semantic validation finding.</summary>
public enum ValidationSeverity
{
    Warning,
    Error,
}

/// <summary>A single semantic validation finding against a loaded carset.</summary>
public sealed record ValidationIssue(ValidationSeverity Severity, string Message)
{
    public override string ToString() => $"{Severity.ToString().ToUpperInvariant()}: {Message}";
}
