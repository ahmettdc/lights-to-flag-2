using System.Linq;
using LTF.Domain;
using Xunit;

namespace LTF.Domain.Tests;

/// <summary>
/// Guards the ROADMAP §4.2 invariant: LTF.Domain is the base layer — it must not depend
/// on the UI or on any other engine layer. Enforced by inspecting the compiled
/// assembly's references, so an accidental <c>using</c> that pulls in Avalonia or a
/// sibling project fails the build via CI.
/// </summary>
public class ArchitectureTests
{
    [Fact]
    public void Domain_does_not_reference_the_ui_or_other_ltf_layers()
    {
        var referenced = typeof(Carset).Assembly
            .GetReferencedAssemblies()
            .Select(a => a.Name ?? "")
            .ToList();

        Assert.DoesNotContain(referenced, name => name.StartsWith("Avalonia", StringComparison.Ordinal));

        // Domain sits at the bottom of the stack: it references no other LTF.* assembly.
        Assert.DoesNotContain(referenced, name => name.StartsWith("LTF.", StringComparison.Ordinal));
    }
}
