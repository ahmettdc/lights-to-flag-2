using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace LTF.App.Views;

/// <summary>Hosts the application root — the single content region swapped by
/// <see cref="LTF.App.ViewModels.RootViewModel"/> between the menu layer and the in-game shell.</summary>
public partial class RootView : UserControl
{
    public RootView() => AvaloniaXamlLoader.Load(this);
}
