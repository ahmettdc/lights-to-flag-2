using LTF.Career;
using LTF.Domain;

namespace LTF.App.Session;

/// <summary>
/// The in-progress career the shell holds: the (evolving) carset, the FM-style <see cref="CareerClock"/>,
/// and the seed. There is no engine "session" type yet — a live career is implicitly this triple. Read-only
/// in M19; M20/M21 will drive it (new career, load, Continue).
/// </summary>
public sealed record ShellSession(Carset Carset, CareerClock Clock, int Seed)
{
    /// <summary>Open a session on a carset, starting the clock the day before the first event.</summary>
    public static ShellSession FromCarset(Carset carset, int seed) =>
        new(carset, CareerClock.Start(carset), seed);
}
