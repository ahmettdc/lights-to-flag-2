using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace LTF.App.Shell;

/// <summary>The permanent left navigation rail. DataContext is a <see cref="ViewModels.SidebarViewModel"/>.</summary>
public partial class Sidebar : UserControl
{
    public Sidebar() => AvaloniaXamlLoader.Load(this);
}
