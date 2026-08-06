using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LTF.App.Mvvm;
using LTF.App.Navigation;
using LTF.App.Notifications;

namespace LTF.App.ViewModels;

/// <summary>One inbox row. Tracks its own read state and, when opened, marks itself read and jumps to its
/// deep link (via a callback so it needs no direct navigation dependency).</summary>
public sealed partial class NotificationViewModel : ViewModelBase
{
    private readonly Action<NavKey?> _navigate;

    public NotificationViewModel(Notification model, Action<NavKey?> navigate)
    {
        Model = model;
        _navigate = navigate;
    }

    public Notification Model { get; }

    public string Title => Model.Title;

    public string Body => Model.Body;

    public NotificationSeverity Severity => Model.Severity;

    public string CategoryLabel => Model.Category.ToString().ToUpperInvariant();

    public NavKey? DeepLink => Model.DeepLink;

    [ObservableProperty]
    private bool _isRead;

    [RelayCommand]
    private void Open()
    {
        IsRead = true;
        _navigate(Model.DeepLink);
    }
}
