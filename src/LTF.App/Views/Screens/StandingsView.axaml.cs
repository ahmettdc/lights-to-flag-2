using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace LTF.App.Views.Screens;

/// <summary>Renders a <see cref="LTF.App.ViewModels.Screens.StandingsViewModel"/> (M21).</summary>
public partial class StandingsView : UserControl
{
    public StandingsView() => AvaloniaXamlLoader.Load(this);
}
