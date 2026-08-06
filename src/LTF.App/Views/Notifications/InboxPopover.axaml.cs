using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace LTF.App.Views.Notifications;

/// <summary>The notification centre dropdown. DataContext is an <see cref="ViewModels.InboxViewModel"/>.</summary>
public partial class InboxPopover : UserControl
{
    public InboxPopover() => AvaloniaXamlLoader.Load(this);
}
