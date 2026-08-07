using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace LTF.App.Views.Screens;

/// <summary>Renders an <see cref="LTF.App.ViewModels.Screens.EntityDetailViewModel"/> (M21) — the shared
/// driver/team detail panel used by the Drivers and Database screens.</summary>
public partial class EntityDetailView : UserControl
{
    public EntityDetailView() => AvaloniaXamlLoader.Load(this);
}
