using LTF.App.Services;
using LTF.App.Settings;

namespace LTF.App.Session;

/// <summary>
/// The composition-root services the application root owns — the carset catalog, the save store, the
/// settings store and the notification source. Bundled so <see cref="LTF.App.ViewModels.RootViewModel"/>'s
/// constructor stays stable as phases add stores.
/// </summary>
public sealed record AppServices(
    CarsetCatalog Catalog,
    SaveStore Saves,
    ISettingsStore Settings,
    INotificationSource Notifications);
