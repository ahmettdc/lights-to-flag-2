// LTF.Tools — command-line utilities. File I/O is allowed here (unlike the engine layers);
// the tool reads carset files and hands their text to the I/O-free LTF.Content loader.

using System.Globalization;
using LTF.Career;
using LTF.Content;
using LTF.Domain;
using LTF.Simulation.Sweep;

if (args.Length == 0)
{
    PrintUsage();
    return 0;
}

switch (args[0])
{
    case "validate" when args.Length >= 2:
        return Validate(args[1]);

    case "validate":
        Console.Error.WriteLine("usage: ltf validate <carset-folder-or-json>");
        return 2;

    case "sweep" when args.Length >= 2:
        return Sweep(args);

    case "sweep":
        Console.Error.WriteLine("usage: ltf sweep <carset-folder-or-json> [seasons] [seed]");
        return 2;

    default:
        Console.Error.WriteLine($"Unknown command: {args[0]}");
        PrintUsage();
        return 2;
}

static int Validate(string path)
{
    var file = ResolveCarsetFile(path);
    if (file is null)
    {
        Console.Error.WriteLine($"No carset.json found at '{path}'.");
        return 2;
    }

    string json;
    try
    {
        json = File.ReadAllText(file);
    }
    catch (IOException ex)
    {
        Console.Error.WriteLine($"Could not read '{file}': {ex.Message}");
        return 2;
    }

    Carset carset;
    try
    {
        carset = CarsetLoader.LoadFromJson(json);
    }
    catch (CarsetValidationException ex)
    {
        Console.Error.WriteLine($"FAILED to load {file}:");
        Console.Error.WriteLine(ex.Message);
        return 1;
    }

    var issues = CarsetValidator.Validate(carset);
    foreach (var issue in issues)
    {
        var writer = issue.Severity == ValidationSeverity.Error ? Console.Error : Console.Out;
        writer.WriteLine(issue.ToString());
    }

    var errors = issues.Count(i => i.Severity == ValidationSeverity.Error);
    var warnings = issues.Count - errors;

    Console.WriteLine(
        $"{carset.Name}: {carset.Teams.Count} teams, {carset.Drivers.Count} drivers, " +
        $"{carset.Circuits.Count} circuits, {carset.Calendar.Count} rounds — " +
        $"{errors} error(s), {warnings} warning(s).");

    return errors > 0 ? 1 : 0;
}

static int Sweep(string[] args)
{
    var seasons = 100;
    var seed = 1;

    if (args.Length >= 3 && !int.TryParse(args[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out seasons))
    {
        Console.Error.WriteLine($"Invalid season count: '{args[2]}'.");
        return 2;
    }

    if (args.Length >= 4 && !int.TryParse(args[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out seed))
    {
        Console.Error.WriteLine($"Invalid seed: '{args[3]}'.");
        return 2;
    }

    if (seasons < 1)
    {
        Console.Error.WriteLine("Season count must be at least 1.");
        return 2;
    }

    var file = ResolveCarsetFile(args[1]);
    if (file is null)
    {
        Console.Error.WriteLine($"No carset.json found at '{args[1]}'.");
        return 2;
    }

    string json;
    try
    {
        json = File.ReadAllText(file);
    }
    catch (IOException ex)
    {
        Console.Error.WriteLine($"Could not read '{file}': {ex.Message}");
        return 2;
    }

    Carset carset;
    try
    {
        carset = CarsetLoader.LoadFromJson(json);
    }
    catch (CarsetValidationException ex)
    {
        Console.Error.WriteLine($"FAILED to load {file}:");
        Console.Error.WriteLine(ex.Message);
        return 1;
    }

    var report = BalanceSweep.Run(carset, seasons, seed);
    var names = carset.Drivers.ToDictionary(d => d.Id, d => $"{d.FirstName} {d.LastName}", StringComparer.Ordinal);

    Console.WriteLine(string.Create(CultureInfo.InvariantCulture,
        $"{carset.Name}: {report.Seasons} seasons x {report.Rounds} rounds = {report.Races} races (seed {seed})."));
    Console.WriteLine();
    Console.WriteLine($"{"Driver",-24} {"Titles",6} {"Wins",5} {"Points",8} {"DNF%",6}");
    foreach (var c in report.Competitors)
    {
        var name = names.TryGetValue(c.CompetitorId, out var n) ? n : c.CompetitorId;
        var dnf = c.Starts > 0 ? 100.0 * c.Retirements / c.Starts : 0.0;
        Console.WriteLine(string.Create(CultureInfo.InvariantCulture,
            $"{Clip(name, 24),-24} {c.Titles,6} {c.Wins,5} {c.Points,8} {dnf,5:0.0}%"));
    }

    Console.WriteLine();
    Console.WriteLine(string.Create(CultureInfo.InvariantCulture,
        $"Field: retirement rate {report.RetirementRate * 100.0:0.0}%, " +
        $"{report.SafetyCarsPerRace:0.00} safety cars/race, " +
        $"{report.AveragePitStopsPerCar:0.00} pit stops/car."));

    // Economy summary (M13) — only when the carset actually configures an economy.
    var economyRules = carset.Rules.Economy;
    if (economyRules.PrizeMoney.Count > 0 || economyRules.TvIncome > 0)
    {
        var economy = EconomySweep.Run(carset, seasons, seed);
        var teamNames = carset.Teams.ToDictionary(t => t.Id, t => t.Name, StringComparer.Ordinal);

        Console.WriteLine();
        Console.WriteLine($"{"Team",-24} {"Balance",16} {"Low",16} {"Debt",5}");
        foreach (var t in economy.Teams)
        {
            var name = teamNames.TryGetValue(t.TeamId, out var n) ? n : t.TeamId;
            Console.WriteLine(string.Create(CultureInfo.InvariantCulture,
                $"{Clip(name, 24),-24} {t.FinalBalance,16:N0} {t.MinBalance,16:N0} {t.SeasonsInDebt,5}"));
        }

        Console.WriteLine();
        Console.WriteLine(string.Create(CultureInfo.InvariantCulture,
            $"Economy: {economy.BankruptTeams} team(s) in the red after {economy.Seasons} seasons, " +
            $"richest {economy.MaxFinalBalance:N0}, poorest {economy.MinFinalBalance:N0}."));
    }

    // R&D summary (M14) — only when the carset actually ships a tech tree.
    if (carset.TechTree.Nodes.Count > 0)
    {
        var research = ResearchSweep.Run(carset, seasons, seed);
        var teamNames = carset.Teams.ToDictionary(t => t.Id, t => t.Name, StringComparer.Ordinal);

        Console.WriteLine();
        Console.WriteLine($"{"Team",-24} {"Start",6} {"Final",6} {"Nodes",6}");
        foreach (var t in research.Teams)
        {
            var name = teamNames.TryGetValue(t.TeamId, out var n) ? n : t.TeamId;
            Console.WriteLine(string.Create(CultureInfo.InvariantCulture,
                $"{Clip(name, 24),-24} {t.StartOverall,6} {t.FinalOverall,6} {t.NodesApproved,6}"));
        }

        Console.WriteLine();
        Console.WriteLine(string.Create(CultureInfo.InvariantCulture,
            $"R&D: {research.TotalNodesApproved} node(s) approved across the field over {research.Seasons} seasons, " +
            $"biggest car gain +{research.MaxOverallGain}."));
    }

    // Component summary (M15) — grid penalties a season's component allocation produces.
    var componentPenalties = ComponentPenalties.ForSeason(carset);
    var penaltyRounds = componentPenalties.Count(p => p.Count > 0);
    if (penaltyRounds > 0)
    {
        var placesLost = componentPenalties.Sum(p => p.Values.Sum());
        Console.WriteLine();
        Console.WriteLine(string.Create(CultureInfo.InvariantCulture,
            $"Components: {penaltyRounds} round(s) carry grid penalties, {placesLost} places lost to over-allocation."));
    }

    // Team-Principal summary (M17) — only when the carset names a player team and ships boards.
    if (carset.PlayerTeamId.Length > 0 && carset.Boards.Count > 0)
    {
        var boss = BossCareerSweep.Run(carset, seasons, seed);
        var teamNames = carset.Teams.ToDictionary(t => t.Id, t => t.Name, StringComparer.Ordinal);
        var playerName = teamNames.TryGetValue(boss.PlayerTeamId, out var pn) ? pn : boss.PlayerTeamId;

        Console.WriteLine();
        Console.WriteLine($"{"Season",6} {"Confidence",10} {"Firing",6} {"Passed",6} {"Signed",6}");
        foreach (var s in boss.PlayerBoard)
        {
            Console.WriteLine(string.Create(CultureInfo.InvariantCulture,
                $"{s.Season + 1,6} {s.BoardConfidence,10} {s.FiringRisk,6} {s.ProposalsPassed,6} {s.ContractsSigned,6}"));
        }

        Console.WriteLine();
        Console.WriteLine(string.Create(CultureInfo.InvariantCulture,
            $"Board: {Clip(playerName, 24)} final confidence {boss.FinalBoardConfidence}, " +
            $"ever near the sack: {(boss.EverAtRisk ? "yes" : "no")} over {boss.Seasons} seasons."));
    }

    // World summary (M18) — only when the carset develops drivers (ages, retires, transfers).
    if (carset.Rules.DriverDevelopment.IsActive)
    {
        var world = WorldSweep.Run(carset, seasons, seed);
        Console.WriteLine();
        Console.WriteLine(string.Create(CultureInfo.InvariantCulture,
            $"World: {world.Retirements} retirement(s), {world.Debuts} debut(s) over {world.Seasons} seasons; " +
            $"ages {world.YoungestAge}–{world.OldestAge}, seats {(world.AllSeatsFilled ? "all filled" : "SHORT")}."));
    }

    return 0;
}

static string Clip(string s, int max) => s.Length <= max ? s : s[..(max - 1)] + "…";

static string? ResolveCarsetFile(string path)
{
    if (File.Exists(path))
    {
        return path;
    }

    var candidate = Path.Combine(path, "carset.json");
    return File.Exists(candidate) ? candidate : null;
}

static void PrintUsage()
{
    Console.WriteLine("LTF.Tools — Lights to Flag 2 command-line utilities");
    Console.WriteLine();
    Console.WriteLine("Usage: ltf <command> [options]");
    Console.WriteLine();
    Console.WriteLine("Commands:");
    Console.WriteLine("  validate <carset-folder-or-json>          Load a carset and report problems");
    Console.WriteLine("  sweep <carset-folder-or-json> [seasons] [seed]   Simulate seasons and report balance");
}
