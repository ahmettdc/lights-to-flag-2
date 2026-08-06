namespace LTF.App.Navigation;

/// <summary>
/// A single sidebar entry: its target, the localization key for its label, its group, and the milestone
/// that fills the screen in (shown on the placeholder until then).
/// </summary>
public sealed record NavItem(NavKey Key, string LabelKey, NavSection Section, string TargetMilestone);
