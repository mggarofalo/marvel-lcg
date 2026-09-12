using Marvel.Rules.State;

namespace Marvel.Cards.Run;

internal sealed record BindingCandidateState(
    IReadOnlyList<Card> Cards,
    bool MayBeEmpty);
