using LTF.Domain.Racing;
using LTF.Domain.Rnd;
using Xunit;

namespace LTF.Domain.Tests;

public class ResearchStateTests
{
    [Fact]
    public void An_empty_research_state_is_inert()
    {
        Assert.Empty(ResearchState.Empty.UnlockedNodeIds);
        Assert.Empty(ResearchState.Empty.ActiveProjects);
        Assert.Equal(0, ResearchState.Empty.RegulationReadiness);
        Assert.Equal(ConceptDirection.Neutral, ResearchState.Empty.Concept);
    }

    [Fact]
    public void A_neutral_concept_leans_nowhere()
    {
        Assert.Equal(0, ConceptDirection.Neutral.AeroLean);
        Assert.Equal(0, ConceptDirection.Neutral.PowertrainLean);
    }

    [Fact]
    public void A_new_project_starts_in_design()
    {
        var project = new DevelopmentProject { NodeId = "n1" };

        Assert.Equal(ValidationState.InDesign, project.State);
    }

    [Fact]
    public void Default_research_rules_do_no_development()
    {
        Assert.Equal(0, new ResearchRules().BaseProgressPerSeason);
    }
}
