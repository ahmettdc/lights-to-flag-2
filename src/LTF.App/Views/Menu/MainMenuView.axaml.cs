using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace LTF.App.Views.Menu;

/// <summary>Renders a <see cref="LTF.App.ViewModels.Menu.MainMenuViewModel"/>.</summary>
public partial class MainMenuView : UserControl
{
    public MainMenuView() => AvaloniaXamlLoader.Load(this);
}
