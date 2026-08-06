using System.Collections.Generic;

namespace LTF.App.Localization;

/// <summary>
/// Default <see cref="ILocalizer"/>. English is the only pack in M19, so every look-up resolves against
/// <see cref="EnglishStrings"/>; a future Turkish pack will be selected here by
/// <c>CultureInfo.CurrentUICulture</c>, falling back to English. A missing key returns a visible
/// <c>!key!</c> sentinel so tests and eyeballs catch gaps. This never sets InvariantGlobalization — it
/// only reads culture for display (ROADMAP §4.2 invariant).
/// </summary>
public sealed class Localizer : ILocalizer
{
    private readonly IReadOnlyDictionary<string, string> _pack = EnglishStrings.Values;

    /// <summary>App-wide default instance, used by the <c>{loc:Tr}</c> markup extension.</summary>
    public static ILocalizer Current { get; set; } = new Localizer();

    public string this[string key] => Get(key);

    public string Get(string key) => _pack.TryGetValue(key, out var value) ? value : $"!{key}!";
}
