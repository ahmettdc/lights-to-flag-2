using LTF.App.Session;

namespace LTF.App.ViewModels.Menu;

/// <summary>An immutable carset choice in the new-career flow, wrapping a discovered descriptor.</summary>
public sealed class CarsetOptionViewModel
{
    public CarsetOptionViewModel(CarsetDescriptor descriptor) => Descriptor = descriptor;

    public CarsetDescriptor Descriptor { get; }

    public string Id => Descriptor.Summary.Id;

    public string Name => Descriptor.Summary.Name;

    public string Subtitle => Descriptor.Summary.Subtitle;
}
