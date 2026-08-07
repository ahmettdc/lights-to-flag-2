using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace LTF.App.Views.Screens;

/// <summary>Renders a <see cref="LTF.App.ViewModels.Screens.CalendarViewModel"/> (M21).</summary>
public partial class CalendarView : UserControl
{
    public CalendarView() => AvaloniaXamlLoader.Load(this);
}
