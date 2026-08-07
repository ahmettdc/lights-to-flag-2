using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace LTF.App.Views.Screens;

/// <summary>Renders a <see cref="LTF.App.ViewModels.Screens.DriversViewModel"/> (M21).</summary>
public partial class DriversView : UserControl
{
    public DriversView() => AvaloniaXamlLoader.Load(this);
}
