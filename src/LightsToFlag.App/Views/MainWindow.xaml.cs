using System.IO;
using System.Windows;
using LightsToFlag.Core.Data;

namespace LightsToFlag.App.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        ShowCarsetSummary();
    }

    /// <summary>
    /// Minimal M0 smoke check that the Core engine is referenced and can load a carset.
    /// Replaced by the real carset picker / new-career flow in M7.
    /// </summary>
    private void ShowCarsetSummary()
    {
        var carsetsRoot = Path.Combine(AppContext.BaseDirectory, "carsets");
        var f2019 = Path.Combine(carsetsRoot, "F1 2019");
        if (!Directory.Exists(f2019))
        {
            StatusText.Text = $"No bundled carset found under {carsetsRoot}.";
            return;
        }

        try
        {
            var loader = new LegacyTextCarsetLoader();
            var carset = loader.Load(f2019);
            StatusText.Text =
                $"Loaded carset '{carset.Name}': {carset.Drivers.Count} drivers, " +
                $"{carset.Teams.Count} teams, {carset.Circuits.Count} circuits.";
        }
        catch (Exception ex)
        {
            StatusText.Text = "Failed to load carset: " + ex.Message;
        }
    }
}
