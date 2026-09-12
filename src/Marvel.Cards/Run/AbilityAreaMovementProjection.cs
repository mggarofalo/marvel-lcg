using static Marvel.Cards.Run.AbilityEffectStructure;
using static Marvel.Cards.Run.AbilityRuntimeQueries;
using static Marvel.Cards.Run.AbilityProjectedStateQueries;
using Marvel.Cards.Dsl;
using Marvel.Rules.Play;
using Marvel.Rules.State;

namespace Marvel.Cards.Run;

/// <summary>Traces movement operations through an area projection.</summary>
internal static class AbilityAreaMovementProjection
{
    internal static List<AreaProjectionState>? TryTraceDelayedStun(
        this AbilityAreaProjection projection, AbilityEffect effect, List<AreaProjectionState> states,
        long repetitions, long baseMultiplier)
    {
        if (effect is AbilityEffect.DelayedStun) return states;
        return null;
    }

    internal static List<AreaProjectionState>? TryTraceDelayedBody(
        this AbilityAreaProjection projection, AbilityEffect effect, List<AreaProjectionState> states,
        long repetitions, long baseMultiplier)
    {
        if (effect.OperationName() is "defense" or "delayUntil")
        {
            return projection.Trace(
                EffectBody(effect), states,
                repetitions, baseMultiplier);
        }
        return null;
    }

    internal static List<AreaProjectionState>? TryTraceDiscard(
        this AbilityAreaProjection projection, AbilityEffect effect, List<AreaProjectionState> states,
        long repetitions, long baseMultiplier)
    {
        if (effect.OperationName() == "discard")
        {
            var instruction = EffectOf<AbilityEffect.CardAction>(effect, projection.context);
            foreach (var state in states)
            {
                foreach (var card in ProjectedEvery(
                             instruction.Selection,
                             state,
                             projection.context))
                {
                    projection.couldDiscard |= projection.queried.Contains(card.Area.Type)
                        || projection.DiscardTreeChangesArea(card);
                    projection.MarkDiscardedTree(state, card);
                }
            }
            return states;
        }
        return null;
    }

    internal static List<AreaProjectionState>? TryTraceAttach(
        this AbilityAreaProjection projection, AbilityEffect effect, List<AreaProjectionState> states,
        long repetitions, long baseMultiplier)
    {
        if (effect.OperationName() == "attachTo")
        {
            var instruction = EffectOf<AbilityEffect.CardAction>(effect, projection.context);
            foreach (var state in states)
            {
                var host = ProjectedFind(instruction.Selection, state, projection.context);
                if (host is null)
                {
                    continue;
                }
                projection.couldDiscard |= projection.queried.Contains(projection.context.Source.Area.Type)
                    || projection.queried.Contains(DeckType.UpgradesArea);
                state.Departed.Remove(projection.context.Source.ObjectId);
                state.Entered.Add(projection.context.Source.ObjectId);
                state.Hosts[projection.context.Source.ObjectId] = host.ObjectId;
            }
            return states;
        }
        return null;
    }

    internal static List<AreaProjectionState>? TryTraceMoveCard(
        this AbilityAreaProjection projection, AbilityEffect effect, List<AreaProjectionState> states,
        long repetitions, long baseMultiplier)
    {
        if (effect.OperationName() is "removeFromGame" or "returnToHand" or "reveal")
        {
            var instruction = EffectOf<AbilityEffect.CardAction>(effect, projection.context);
            var destination = effect.OperationName() switch
            {
                "removeFromGame" => DeckType.RemovedArea,
                "returnToHand" => DeckType.HandsArea,
                _ => DeckType.RevealingArea,
            };
            foreach (var state in states)
            {
                foreach (var card in ProjectedEvery(
                             instruction.Selection, state, projection.context))
                {
                    projection.couldDiscard |= projection.queried.Contains(card.Area.Type)
                        || projection.queried.Contains(destination)
                        || projection.HostedCardsChangeArea(state, card.ObjectId);
                    projection.MarkDiscardedTree(state, card);
                }
            }
            return states;
        }
        return null;
    }

    internal static List<AreaProjectionState>? TryTracePutIntoPlay(
        this AbilityAreaProjection projection, AbilityEffect effect, List<AreaProjectionState> states,
        long repetitions, long baseMultiplier)
    {
        if (effect.OperationName() == "putIntoPlay")
        {
            var instruction = EffectOf<AbilityEffect.PutIntoPlay>(effect, projection.context);
            foreach (var state in states)
            {
                var selector = instruction.Card;
                var cards = ProjectedEvery(selector, state, projection.context);
                if (cards.Count == 0
                    && selector is AbilityCardSelection.Bound { Binding: AbilityCardBinding.This }
                    && state.SourceReferenceCurrent)
                {
                    cards.Add(projection.context.Source);
                }
                foreach (var card in cards)
                {
                    state.Departed.Remove(card.ObjectId);
                    state.Entered.Add(card.ObjectId);
                    if (!instruction.PrintedDestination)
                    {
                        state.EngagedWith[card.ObjectId] = Resolver(projection.context);
                    }
                    if (StateFields.Modified(
                            projection.context.World, card, "toughness",
                            projection.context.World.Facts, projection.context.World.Players) > 0)
                    {
                        state.Tough[card.ObjectId] = Math.Max(
                            1, state.ToughOf(projection.context, card));
                    }
                    if (selector is AbilityCardSelection.Bound { Binding: AbilityCardBinding.This })
                    {
                        // Moving out of play and entering play creates a
                        // new incarnation. Later `this` references retain
                        // the old binding and no longer denote the card.
                        state.SourceReferenceCurrent = false;
                    }
                }
            }
            projection.couldDiscard |= MayChangeAnyArea(
                effect, projection.queried, projection.context, baseMultiplier);
            return states;
        }
        return null;
    }

    internal static List<AreaProjectionState>? TryTraceOpaqueAreaMutation(
        this AbilityAreaProjection projection, AbilityEffect effect, List<AreaProjectionState> states,
        long repetitions, long baseMultiplier)
    {
        if (effect.OperationName() is "draw" or "drawToHandSize"
            or "drawToPrintedHandSize" or "search"
            or "shuffleInto" or "dealEncounterCard" or "dealEncounterCards"
            or "revealTop" or "discardTop" or "discardUntil"
            or "createDrones" or "indirectDamage" or "discardFromHand"
            or "discardUpToFromHand" or "discardAnyFromHand" or "spend"
            or "spendPrinted" or "spendEnergyX")
        {
            projection.couldDiscard |= MayChangeAnyArea(
                effect, projection.queried, projection.context, baseMultiplier);
            return states;
        }
        return null;
    }
}
