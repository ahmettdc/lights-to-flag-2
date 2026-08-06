using System.Linq;
using System.Reflection;
using LTF.App.Localization;
using Xunit;

namespace LTF.App.Tests;

/// <summary>
/// The localization seam: English resolves today, and every declared key has a value (no gaps), so a
/// future Turkish pack can be checked against the same key set. Pure logic — no headless app needed.
/// </summary>
public class LocalizerTests
{
    private readonly ILocalizer _loc = new Localizer();

    [Fact]
    public void Known_key_returns_english_value() =>
        Assert.Equal("DRIVERS", _loc.Get(StringKeys.NavDrivers));

    [Fact]
    public void Indexer_matches_get() =>
        Assert.Equal(_loc.Get(StringKeys.NavFinance), _loc[StringKeys.NavFinance]);

    [Fact]
    public void Unknown_key_returns_visible_sentinel() =>
        Assert.Equal("!nope.missing!", _loc.Get("nope.missing"));

    [Fact]
    public void Every_defined_string_key_has_an_english_value()
    {
        var keys = typeof(StringKeys)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.IsLiteral && f.FieldType == typeof(string))
            .Select(f => (string)f.GetValue(null)!);

        foreach (var key in keys)
            Assert.False(_loc.Get(key).StartsWith('!'), $"no English string for key '{key}'");
    }
}
