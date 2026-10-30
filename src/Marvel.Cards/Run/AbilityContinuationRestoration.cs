using System.Collections.Immutable;
using Marvel.Rules.State;
using Marvel.Rules.Play;

namespace Marvel.Cards.Run;

/// <summary>Restores exact bindings from one game-owned execution frame.</summary>
internal static class AbilityContinuationRestoration
{
    internal static RestoredContinuationState RestoreState(
        IReadOnlyList<Card> cards, AbilityContinuationState state)
    {
        Card At(int id) => id >= 0 && id < cards.Count ? cards[id]
            : throw new InvalidOperationException("the execution frame refers to an unknown card");
        var memory = state.Memory;
        var chosen = state.AllyEntryBinding is { } binding
            ? binding.Entered is { } entered
                ? new AbilityContinuationCardBinding(entered.Card, entered.Area, entered.Incarnation)
                : throw new InvalidOperationException("ally entry binding has not completed")
            : memory.Chosen;
        return new(memory.Discarded.Select(At).ToImmutableArray(), memory.Results,
            state.Invocation.Source.Incarnation, chosen,
            state.Invocation.AbilityActor >= 0 ? At(state.Invocation.AbilityActor) : null,
            memory.CrisisIgnoringThwarts, memory.SourceState)
        { ExhaustedForCost = memory.ExhaustedForCost, SourceWasPublicInPlay = memory.SourceWasPublicInPlay };
    }
}
