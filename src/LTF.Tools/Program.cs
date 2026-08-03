// LTF.Tools — command-line utilities for content validation and balance sweeps.
// Commands are added as the milestones that need them land (validate: M2,
// sweep: M10). For now this is a routing skeleton.

if (args.Length == 0)
{
    Console.WriteLine("LTF.Tools — Lights to Flag 2 command-line utilities");
    Console.WriteLine();
    Console.WriteLine("Usage: ltf <command> [options]");
    Console.WriteLine();
    Console.WriteLine("Commands:");
    Console.WriteLine("  (none yet — 'validate' arrives in M2, 'sweep' in M10)");
    return 0;
}

Console.Error.WriteLine($"Unknown command: {args[0]}");
return 1;
