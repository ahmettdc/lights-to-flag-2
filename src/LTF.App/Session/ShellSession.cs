using System;
using LTF.Career;
using LTF.Domain;

namespace LTF.App.Session;

/// <summary>
/// The in-progress career the shell holds: the (evolving) carset, the FM-style <see cref="CareerClock"/>,
/// and the seed. There is no engine "session" type yet — a live career is implicitly this triple. Driven by
/// the menu in M20 (new career, load, Continue).
/// </summary>
public sealed record ShellSession(Carset Carset, CareerClock Clock, int Seed)
{
    /// <summary>Open a session on a carset, starting the clock the day before the first event (new career).</summary>
    public static ShellSession FromCarset(Carset carset, int seed) =>
        new(carset, CareerClock.Start(carset), seed);

    /// <summary>Resume a loaded career: keep the carset-built calendar but restore the saved date, so a
    /// mid-season save doesn't reset to the season start.</summary>
    public static ShellSession Resume(Carset carset, int seed, DateOnly date) =>
        new(carset, CareerClock.Start(carset) with { Date = date }, seed);
}
