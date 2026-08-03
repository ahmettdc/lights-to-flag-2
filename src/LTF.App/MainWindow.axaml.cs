using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace LTF.App;

/// <summary>Placeholder shell window. Replaced by the real navigation shell in M19.</summary>
public partial class MainWindow : Window
{
    public MainWindow() => AvaloniaXamlLoader.Load(this);
}
