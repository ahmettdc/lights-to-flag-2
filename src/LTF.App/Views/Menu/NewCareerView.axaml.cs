using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace LTF.App.Views.Menu;

/// <summary>Renders a <see cref="LTF.App.ViewModels.Menu.NewCareerViewModel"/> — the new-career wizard.</summary>
public partial class NewCareerView : UserControl
{
    public NewCareerView() => AvaloniaXamlLoader.Load(this);
}
