using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace LTF.App.Shell;

/// <summary>The permanent top bar. DataContext is a <see cref="ViewModels.TopBarViewModel"/>.</summary>
public partial class TopBar : UserControl
{
    public TopBar() => AvaloniaXamlLoader.Load(this);
}
