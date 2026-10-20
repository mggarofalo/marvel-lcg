using System.Collections.Immutable;

namespace Marvel.Cards.Run;

internal sealed record DiscardResourceIconsCommand(ImmutableArray<int> Targets) : AbilityStructuralTransition;
