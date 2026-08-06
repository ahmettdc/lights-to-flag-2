using LTF.App.Localization;
using LTF.App.Mvvm;

namespace LTF.App.ViewModels;

/// <summary>
/// The bottom status bar's data: a short live status line (left). The right-hand Continue is a static,
/// inert hint in M19 — the per-event Continue runner arrives in M21, and drives this line then.
/// </summary>
public sealed class StatusBarViewModel : ViewModelBase
{
    public string StatusText => Localizer.Current.Get(StringKeys.StatusReady);
}
