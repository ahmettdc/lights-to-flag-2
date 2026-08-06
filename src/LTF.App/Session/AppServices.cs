using LTF.App.Services;

namespace LTF.App.Session;

/// <summary>
/// The composition-root services the application root owns — the carset catalog, the save store and the
/// notification source. Bundled so <see cref="LTF.App.ViewModels.RootViewModel"/>'s constructor stays stable
/// as later phases add stores (settings in M20e).
/// </summary>
public sealed record AppServices(CarsetCatalog Catalog, SaveStore Saves, INotificationSource Notifications);
