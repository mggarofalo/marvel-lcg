using Marvel.Rules.Play;
using Marvel.Rules.State;

namespace Marvel.View;

/// <summary>One game-area grouping.</summary>
public sealed record GameAreaDescriptor(int Id, IReadOnlyList<int> PlayAreas);
