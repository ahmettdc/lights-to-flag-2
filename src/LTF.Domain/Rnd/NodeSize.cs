namespace LTF.Domain.Rnd;

/// <summary>A tech node's size (M14 / ADR-0016): larger nodes cost more, deliver more and sit deeper
/// behind prerequisites.</summary>
public enum NodeSize
{
    Minor,
    Major,
    Ultimate,
}
