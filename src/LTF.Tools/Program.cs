// LTF.Tools — command-line utilities. File I/O is allowed here (unlike the engine layers);
// the tool reads carset files and hands their text to the I/O-free LTF.Content loader.

using LTF.Content;
using LTF.Domain;

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
    Console.WriteLine("  validate <carset-folder-or-json>   Load a carset and report problems");
    Console.WriteLine("  (sweep arrives in M10)");
}
