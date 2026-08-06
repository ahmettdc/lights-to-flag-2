using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace LTF.App.Views.Settings;

/// <summary>Renders a <see cref="LTF.App.ViewModels.Settings.SettingsViewModel"/>.</summary>
public partial class SettingsView : UserControl
{
    public SettingsView() => AvaloniaXamlLoader.Load(this);
}
