using LTF.Domain;
using LTF.Domain.Common;
using LTF.Simulation;

namespace LTF.Career;

/// <summary>
/// Builds a season's entry list with each car's reliability adjusted for its team's quality-control
/// facility (M15 / ADR-0020). Facilities give no car points directly — this is the one place the career
/// layer turns a facility level into a rating, so the simulator stays untouched. At influence 0 (the
/// default) it is exactly <see cref="EntryList.Build"/>, so the season is byte-identical.
/// </summary>
public static class SeasonEntries
{
    private const int NeutralLevel = 3;

    public static IReadOnlyList<Competitor> Build(Carset carset)
    {
        var influence = carset.Rules.Research.QualityControlReliabilityInfluence;
        if (influence == 0.0)
        {
            return EntryList.Build(carset);
        }

        var teams = carset.Teams
            .Select(team =>
            {
                var shift = (int)Math.Round(influence * (team.Facilities.QualityControl.Value - NeutralLevel));
                return shift == 0
                    ? team
                    : team with
                    {
                        Car = team.Car with { Reliability = Rating.Clamped(team.Car.Reliability.Value + shift) },
                    };
            })
            .ToList();

        return EntryList.Build(carset with { Teams = teams });
    }
}
