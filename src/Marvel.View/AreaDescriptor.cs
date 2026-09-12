using Marvel.Rules.Play;
using Marvel.Rules.State;

namespace Marvel.View;

/// <summary>One area and both card containers it owns.</summary>
/// <remarks>
/// The projector walks <see cref="World.Areas"/> rather than naming zones.
/// Therefore a newly allocated area is filtered on its first response without
/// adding it to a visibility list.
/// </remarks>
public sealed record AreaDescriptor(
    int Id,
    string Zone,
    int Owner,
    int Host,
    IReadOnlyList<CardDescriptor> Cards,
    IReadOnlyList<CardDescriptor> Removed);
