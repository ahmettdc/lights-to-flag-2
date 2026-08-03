namespace LightsToFlag.App.Services;

/// <summary>Swaps the shell's active full-screen page.</summary>
public interface INavigationService
{
    /// <summary>Resolve <typeparamref name="TViewModel"/> from the container and show it.</summary>
    void NavigateTo<TViewModel>() where TViewModel : class;

    /// <summary>Show an already-constructed page view model.</summary>
    void Navigate(object pageViewModel);
}
