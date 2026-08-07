using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace LTF.App.Views.Screens;

/// <summary>Renders a <see cref="LTF.App.ViewModels.Screens.PaddockHubViewModel"/> (M21).</summary>
public partial class PaddockHubView : UserControl
{
    public PaddockHubView() => AvaloniaXamlLoader.Load(this);
}
