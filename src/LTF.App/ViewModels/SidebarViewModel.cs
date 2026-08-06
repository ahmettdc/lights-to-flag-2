using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using LTF.App.Localization;
using LTF.App.Mvvm;
using LTF.App.Navigation;
using LTF.App.Services;

namespace LTF.App.ViewModels;

/// <summary>
/// The sidebar: the nav registry split into its three groups, with the active row tracked from the
/// navigation service. Selecting a row navigates; navigating (from anywhere) re-marks the active row.
/// </summary>
public sealed class SidebarViewModel : ViewModelBase
{
    private readonly INavigationService _navigation;
    private readonly IReadOnlyList<SidebarItemViewModel> _all;

    public SidebarViewModel(INavigationService navigation, Action<NavKey> dispatch)
    {
        _navigation = navigation;

        _all = NavRegistry.Items
            .Select(item => new SidebarItemViewModel(item, Localizer.Current.Get(item.LabelKey), () => dispatch(item.Key)))
            .ToList();

        MainItems = _all.Where(i => i.Item.Section == NavSection.Navigation).ToList();
        RaceDayItems = _all.Where(i => i.Item.Section == NavSection.RaceDay).ToList();
        SystemItems = _all.Where(i => i.Item.Section == NavSection.System).ToList();

        navigation.PropertyChanged += OnNavigationChanged;
        UpdateActive();
    }

    public IReadOnlyList<SidebarItemViewModel> MainItems { get; }

    public IReadOnlyList<SidebarItemViewModel> RaceDayItems { get; }

    public IReadOnlyList<SidebarItemViewModel> SystemItems { get; }

    public string NavigationLabel => Localizer.Current.Get(StringKeys.SectionNavigation);

    public string RaceDayLabel => Localizer.Current.Get(StringKeys.SectionRaceDay);

    private void OnNavigationChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(INavigationService.CurrentKey))
        {
            UpdateActive();
        }
    }

    private void UpdateActive()
    {
        foreach (var item in _all)
        {
            item.IsActive = item.Key == _navigation.CurrentKey;
        }
    }
}
