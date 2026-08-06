using LTF.App.Localization;
using LTF.App.Mvvm;
using LTF.App.Navigation;

namespace LTF.App.ViewModels.Screens;

/// <summary>
/// Stand-in for a screen not yet built. Shows the screen's (localized) title and which milestone lands
/// the real content. Every sidebar entry resolves to one of these in M19.
/// </summary>
public sealed class PlaceholderScreenViewModel : ViewModelBase
{
    public PlaceholderScreenViewModel(NavItem item)
    {
        Key = item.Key;
        Title = Localizer.Current.Get(item.LabelKey);
        Subtitle = $"Coming in {item.TargetMilestone}";
    }

    public NavKey Key { get; }

    public string Title { get; }

    public string Subtitle { get; }
}
