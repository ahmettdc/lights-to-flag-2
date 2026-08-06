using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace LTF.App.Views.Menu;

/// <summary>Renders a <see cref="LTF.App.ViewModels.Menu.LoadGameViewModel"/> — the load-game slot list.</summary>
public partial class LoadGameView : UserControl
{
    public LoadGameView() => AvaloniaXamlLoader.Load(this);
}
