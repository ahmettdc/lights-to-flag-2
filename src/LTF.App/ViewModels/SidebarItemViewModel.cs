using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LTF.App.Mvvm;
using LTF.App.Navigation;

namespace LTF.App.ViewModels;

/// <summary>
/// One sidebar row. Holds its <see cref="Label"/>, tracks whether it is the active screen
/// (<see cref="IsActive"/>, driven by the sidebar), and navigates to its target when selected.
/// </summary>
public sealed partial class SidebarItemViewModel : ViewModelBase
{
    private readonly Action _select;

    public SidebarItemViewModel(NavItem item, string label, Action select)
    {
        Item = item;
        Label = label;
        _select = select;
    }

    public NavItem Item { get; }

    public string Label { get; }

    public NavKey Key => Item.Key;

    [ObservableProperty]
    private bool _isActive;

    [RelayCommand]
    private void Select() => _select();
}
