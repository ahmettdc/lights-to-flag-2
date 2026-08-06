namespace LTF.App.Localization;

/// <summary>
/// Look-up of user-facing strings by key. English is the only pack in M19; the interface exists so a
/// future Turkish pack (ROADMAP M19: "yerelleştirme altyapısı") slots in behind it — selected by
/// culture — with no View changes. The game's UI language is English (ROADMAP Faz 3).
/// </summary>
public interface ILocalizer
{
    /// <summary>Localized string for <paramref name="key"/>, or a visible sentinel if the key is unknown.</summary>
    string this[string key] { get; }

    /// <inheritdoc cref="this"/>
    string Get(string key);
}
