using LTF.Domain.Racing;

namespace LTF.App.ViewModels.Quick;

/// <summary>A circuit choice in the Quick Race setup, wrapping a carset circuit and its grid index.</summary>
public sealed class CircuitOptionViewModel
{
    public CircuitOptionViewModel(Circuit circuit, int index)
    {
        Index = index;
        Name = circuit.Name;
        Country = circuit.Country;
    }

    /// <summary>The circuit's index in the carset's circuit list (what the sim runner takes).</summary>
    public int Index { get; }

    public string Name { get; }

    public string Country { get; }
}
