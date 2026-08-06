using System.Collections.Generic;
using LTF.Content;

namespace LTF.App.ViewModels.Menu;

/// <summary>An immutable team choice in the new-career flow, wrapping a carset team option.</summary>
public sealed class TeamOptionViewModel
{
    public TeamOptionViewModel(CarsetTeamOption option) => _option = option;

    private readonly CarsetTeamOption _option;

    public string Id => _option.Id;

    public string Name => _option.Name;

    public string ShortName => _option.ShortName;

    public IReadOnlyList<string> DriverList => _option.Drivers;

    /// <summary>The team's driver line-up as one comma-joined line for the option card.</summary>
    public string Drivers => string.Join(", ", _option.Drivers);
}
