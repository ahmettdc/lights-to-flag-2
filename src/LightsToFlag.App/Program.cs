using System;
using Velopack;

namespace LightsToFlag.App;

/// <summary>
/// Explicit entry point. Velopack must run before anything else so it can handle
/// its install / update / uninstall hooks; only then do we start WPF.
/// </summary>
public static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        VelopackApp.Build().Run();

        var app = new App();
        app.InitializeComponent();
        app.Run();
    }
}
