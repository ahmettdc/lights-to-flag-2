using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace LTF.App.Views.Screens;

/// <summary>Renders a <see cref="LTF.App.ViewModels.Screens.DatabaseViewModel"/> (M21).</summary>
public partial class DatabaseView : UserControl
{
    public DatabaseView() => AvaloniaXamlLoader.Load(this);
}
