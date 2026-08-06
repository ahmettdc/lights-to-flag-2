using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace LTF.App.Views.Screens;

/// <summary>Renders a <see cref="LTF.App.ViewModels.Screens.PlaceholderScreenViewModel"/>.</summary>
public partial class PlaceholderScreenView : UserControl
{
    public PlaceholderScreenView() => AvaloniaXamlLoader.Load(this);
}
