using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace LTF.App.Views.Quick;

/// <summary>Renders a <see cref="LTF.App.ViewModels.Quick.QuickRaceViewModel"/> — Quick Race setup + result.</summary>
public partial class QuickRaceView : UserControl
{
    public QuickRaceView() => AvaloniaXamlLoader.Load(this);
}
