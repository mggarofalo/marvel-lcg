using System.Collections.Immutable;

namespace Marvel.Cards.Run;

internal sealed record RetrieveCardsCommand(ImmutableArray<int> Targets) : AbilityStructuralTransition;
