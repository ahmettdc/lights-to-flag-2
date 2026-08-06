using System;
using System.IO;
using LTF.App.Session;
using Xunit;

namespace LTF.App.Tests;

/// <summary>
/// Save slots over CareerStore (M20d): saving a career makes it discoverable, a load round-trips the team
/// and — crucially — the saved date (not the season-start default), and deleting removes it. Each store
/// points at a fresh temp folder so tests never touch real app-data.
/// </summary>
public class SaveStoreTests
{
    private static SaveStore Fresh() =>
        new(CarsetCatalog.Discover(), Directory.CreateTempSubdirectory().FullName);

    [Fact]
    public void Empty_store_reports_no_saves()
    {
        var store = Fresh();

        Assert.False(store.HasAnySave());
        Assert.Null(store.MostRecent());
        Assert.Empty(store.List());
    }

    [Fact]
    public void Saving_a_career_makes_it_discoverable_with_its_team_name()
    {
        var store = Fresh();
        store.Save(SessionLoader.LoadFlagship());

        var slot = Assert.Single(store.List());
        Assert.Equal("global-prix", slot.CarsetId);
        Assert.NotEmpty(slot.TeamName);
        Assert.True(store.HasAnySave());
    }

    [Fact]
    public void Save_then_load_preserves_the_team_and_the_saved_date()
    {
        var store = Fresh();
        var midSeason = new DateOnly(2027, 6, 1);
        var session = SessionLoader.LoadFlagship();
        session = session with { Clock = session.Clock with { Date = midSeason } };

        store.Save(session);
        var loaded = store.Load(store.MostRecent()!);

        Assert.Equal("talon", loaded.Carset.PlayerTeamId);
        Assert.Equal(midSeason, loaded.Clock.Date); // restored, not reset to the season start
        Assert.Equal(session.Seed, loaded.Seed);
    }

    [Fact]
    public void Deleting_a_save_removes_it()
    {
        var store = Fresh();
        store.Save(SessionLoader.LoadFlagship());

        store.Delete(store.MostRecent()!);

        Assert.False(store.HasAnySave());
    }
}
