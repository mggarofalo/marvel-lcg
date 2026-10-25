using System.Collections.Immutable;

namespace Marvel.Cards.Run;

internal sealed record DiscardSelectedHandCardsCommand(ImmutableArray<int> Targets) : AbilityStructuralTransition;
