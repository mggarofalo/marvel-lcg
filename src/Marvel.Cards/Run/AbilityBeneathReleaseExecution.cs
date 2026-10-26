using System.Collections.Immutable;
using Marvel.Rules.Play;
using Marvel.Cards.Dsl;
using Marvel.Rules.State;

namespace Marvel.Cards.Run;

/// <summary>Resolves a host's printed return instruction before ordinary departure can discard stored cards.</summary>
internal static class AbilityBeneathReleaseExecution
{
    internal static AbilityDeckAndRevealResult Run(AbilityEffect.ReleaseBeneath effect, AbilityDeckAndRevealContext context)
    {
        if (effect.InvocationDeck)
        {
            bool returned = BeneathCards.Of(context.World, context.Source).Count > 0;
            InvocationDeck.ShuffleFromBeneath(context.World, context.Source, context.Trigger, context.Events);
            return new(true, returned, null, ImmutableDictionary<string, long>.Empty);
        }
        bool applied = effect.RemoveHost || BeneathCards.Of(context.World, context.Source).Count > 0;
        var state = new AbilityCardStateContext(context.Expressions, context.Trigger,
            context.Events, context.CardPlayAbilities, context.Readiness, new AbilityCardStateResult());
        if (effect.Rescue) Rescue(context);
        // rr:when-defeated-abilities.2.1 resolves the printed instruction before
        // the defeated card leaves play. Return is not a discard: detach these
        // cards before host removal can perform the ordinary attachment cascade.
        AbilityCardMovementExecution.ReturnToHand(new AbilityCardSelection.Query(AbilityCardQuery.BeneathThis), state);
        if (effect.RemoveHost)
            AbilityCardStateExecution.TryRun(new AbilityEffect.CardAction(AbilityCardInstruction.RemoveFromGame,
                new AbilityCardSelection.Bound(AbilityCardBinding.This)), state);
        return new(true, applied, null, ImmutableDictionary<string, long>.Empty);
    }

    private static void Rescue(AbilityDeckAndRevealContext context)
    {
        var cards = BeneathCards.Of(context.World, context.Source).ToArray();
        int player = AbilityCardQueries.DefeatingPlayer(context.Expressions.Bindings);
        if (cards.Any(card => EffectiveCards.Kind(card, context.World.Facts) != CardKind.Ally
            || !context.World.Facts.Traits(card.FaceId).Contains("CAPTIVE", StringComparer.Ordinal)))
            throw new RulesNotImplementedException("rescue beneath requires printed CAPTIVE allies");
        foreach (var card in cards)
        {
            // rr:ownership-and-control.2.2: taking control of a scenario-specific
            // player card also confers ownership. Record the physical captive's
            // campaign provenance before moving it into the rescuer's hand.
            context.World.Campaign?.Rescue(card, player, context.Trigger, context.Events);
            card.TransferScenarioOwnership(player);
        }
    }
}
