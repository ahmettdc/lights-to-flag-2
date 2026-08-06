namespace LTF.App.Localization;

/// <summary>
/// XAML markup extension: <c>{loc:Tr nav.drivers}</c> (or <c>{loc:Tr Key=nav.drivers}</c>) resolves a
/// localized string via <see cref="Localizer.Current"/>. Avalonia recognises a markup extension by its
/// <c>ProvideValue</c> method — no base class required. Culture does not change at runtime in M19, so
/// one-shot resolution is sufficient; a future dynamic language switch would replace this with a binding.
/// </summary>
public sealed class TrExtension
{
    public TrExtension()
    {
    }

    public TrExtension(string key) => Key = key;

    /// <summary>The <see cref="StringKeys"/> value to resolve.</summary>
    public string Key { get; set; } = "";

    public object ProvideValue(IServiceProvider serviceProvider) => Localizer.Current.Get(Key);
}
