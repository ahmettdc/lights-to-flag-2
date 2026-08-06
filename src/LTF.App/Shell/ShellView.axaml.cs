using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace LTF.App.Shell;

/// <summary>The permanent navigation shell. DataContext is a <see cref="ViewModels.ShellViewModel"/>.</summary>
public partial class ShellView : UserControl
{
    public ShellView() => AvaloniaXamlLoader.Load(this);
}
