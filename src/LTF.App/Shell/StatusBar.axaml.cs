using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace LTF.App.Shell;

/// <summary>The permanent bottom status bar. DataContext is a <see cref="ViewModels.StatusBarViewModel"/>.</summary>
public partial class StatusBar : UserControl
{
    public StatusBar() => AvaloniaXamlLoader.Load(this);
}
