using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LightsToFlag.App.Services;

namespace LightsToFlag.App.ViewModels;

/// <summary>Brand / credits page.</summary>
public partial class AboutViewModel : ObservableObject
{
    private readonly INavigationService _nav;

    public AboutViewModel(INavigationService nav) => _nav = nav;

    public string Tagline => "Motorsport Manager";

    public string Blurb =>
        "Lights to Flag 2 — a ground-up rewrite. A deterministic race engine drives a "
        + "single-player management career: practice, qualifying, live-timed races, "
        + "championship standings and season-to-season contracts.";

    public string Credits =>
        "Fonts: Saira Condensed, Chakra Petch, Archivo (Google Fonts, OFL).";

    [RelayCommand]
    private void Back() => _nav.NavigateTo<MainMenuViewModel>();
}
