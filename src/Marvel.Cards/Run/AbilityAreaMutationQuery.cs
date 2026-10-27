using System.Collections.Immutable;
using Marvel.Cards.Dsl;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using static Marvel.Cards.Run.AbilityEffectStructure;
using static Marvel.Cards.Run.AbilityInitiationPrimitives;
using static Marvel.Cards.Run.AbilityProjection;
using static Marvel.Cards.Run.AbilityRuntimeQueries;

namespace Marvel.Cards.Run;

internal sealed class AbilityAreaMutationQuery(
    IReadOnlySet<DeckType> queried, AbilityAdmissionContext context,
    long multiplier)
{
    private static readonly ImmutableArray<string> DrawOperations =
    [
        "draw", "drawToHandSize", "drawToPrintedHandSize",
    ];

    private static readonly ImmutableArray<string> EncounterDeckOperations =
    [
        "dealEncounterCard", "dealEncounterCards", "revealTop", "discardTop",
        "discardUntil",
    ];

    private static readonly ImmutableArray<string> HandPaymentOperations =
    [
        "discardFromHand", "discardUpToFromHand", "discardAnyFromHand",
        "discardHandCards", "discardResourceIcons",
        "spend", "spendPrinted", "spendEnergyX",
    ];

    internal bool MayChange(AbilityEffect effect)
    {
        if (effect is AbilityEffect.DelayedStun) return false;
        if (DirectChange(effect)) return true;
        string operation = effect.OperationName();
        if (operation == "forEach") return RepeatedChange(effect);
        if (operation == "eachPlayer") return EachPlayerChange(effect);
        if (operation is "seq" or "and")
        {
            return AbilityRuntimeQueries.EffectsMayChangeAnyArea(
                OrderedEffects(effect).ToList(), queried, context, multiplier);
        }
        return ReachableChildren(effect).Any(MayChange);
    }

    private bool DirectChange(AbilityEffect effect) =>
        SimpleDirectChange(effect) ?? CombatDirectChange(effect);

    private bool? SimpleDirectChange(AbilityEffect effect)
    {
        string operation = effect.OperationName();
        if (DeckOperationChange(effect) is { } deckChange) return deckChange;
        if (operation is "discard" or "removeFromGame" or "returnToHand"
            or "placeOnBottomOfDeck" or "returnOwnedToHand" or "addToHand" or "returnOwnedToDiscard"
            or "reveal" or "putIntoPlay")
        {
            return CardMovementChange(effect);
        }
        if (operation == "chooseCardsToHand") return queried.Count > 0;
        if (operation == "search")
            return SearchAreaTypes(effect, context).Any(queried.Contains)
                || queried.Contains(DeckType.RevealingArea);
        if (operation == "shuffleInto")
            return effect is AbilityEffect.ShuffleInto { Deck: AbilityShuffleDestination.OwnerDeck } ownerShuffle
                ? SelectedCardMoves(ownerShuffle.Cards, DeckType.PlayerDeck, DeckType.RemovedArea)
                : Includes(DeckType.EncounterDeck, DeckType.EncounterDiscardPile, DeckType.AsideDeck);
        if (HandPaymentOperations.Contains(operation))
            return Includes(DeckType.HandsArea, DeckType.DiscardPile);
        return null;
    }

    private bool? DeckOperationChange(AbilityEffect effect) => effect.OperationName() switch
    {
        // A bounded player discard can recover a card to hand or empty and reset
        // its deck, moving the discard pile and dealing an encounter card.
        "discardUntil" when effect is AbilityEffect.DiscardUntil { From: AbilitySearchArea.YourDeck } => Includes(
            DeckType.PlayerDeck, DeckType.DiscardPile, DeckType.HandsArea,
            DeckType.EncounterDeck, DeckType.EncounterDiscardPile,
            DeckType.RevealingArea, DeckType.DealtEncounterCardsDeck),
        "revealFromVillainDeck" => queried.Count > 0,
        "moveActiveCounter" => queried.Contains(DeckType.VillainArea),
        _ when DrawOperations.Contains(effect.OperationName()) =>
            Includes(DeckType.PlayerDeck, DeckType.HandsArea),
        // Engaging a player card can empty its deck and reset its discard pile;
        // that reset can also deal a card from the encounter deck.
        "engageTopAsMinion" => Includes(
            DeckType.PlayerDeck, DeckType.DiscardPile, DeckType.EngagedEnemiesArea,
            DeckType.EncounterDeck, DeckType.EncounterDiscardPile,
            DeckType.RevealingArea, DeckType.DealtEncounterCardsDeck),
        _ when EncounterDeckOperations.Contains(effect.OperationName()) => Includes(
            DeckType.EncounterDeck, DeckType.EncounterDiscardPile,
            DeckType.RevealingArea, DeckType.DealtEncounterCardsDeck),
        _ => null,
    };

    private bool CombatDirectChange(AbilityEffect effect) =>
        effect.OperationName() switch
        {
            "dealDamage" or "dealAttackDamage" => DamageCouldDiscard(effect),
            "moveDamage" or "moveAttackDamage" => MovedDamageCouldDiscard(effect),
            "removeThreat" => ThreatRemovalCouldDiscard(effect),
            "indirectDamage" => queried.Contains(DeckType.DiscardPile),
            _ => false,
        };

    private bool CardMovementChange(AbilityEffect effect) =>
        effect.OperationName() switch
        {
            "discard" or "returnOwnedToDiscard" => SelectedCardMovesToDiscard(
                EffectOf<AbilityEffect.CardAction>(effect, context).Selection),
            "removeFromGame" => SelectedCardMoves(
                EffectOf<AbilityEffect.CardAction>(effect, context).Selection,
                DeckType.RemovedArea),
            "returnToHand" or "returnOwnedToHand" or "addToHand" => SelectedCardMoves(
                EffectOf<AbilityEffect.CardAction>(effect, context).Selection,
                DeckType.HandsArea),
            "placeOnBottomOfDeck" => SelectedCardMoves(
                EffectOf<AbilityEffect.CardAction>(effect, context).Selection, DeckType.PlayerDeck),
            "reveal" => SelectedCardMoves(
                EffectOf<AbilityEffect.CardAction>(effect, context).Selection,
                DeckType.RevealingArea),
            "putIntoPlay" => SelectedCardMoves(
                EffectOf<AbilityEffect.PutIntoPlay>(effect, context).Card,
                DeckType.AlliesArea, DeckType.SupportsArea, DeckType.UpgradesArea,
                DeckType.EngagedEnemiesArea, DeckType.SideSchemesArea,
                DeckType.EnvironmentArea, DeckType.ObligationsArea),
            _ => false,
        };

    private bool SelectedCardMovesToDiscard(AbilityCardSelection selector) =>
        Every(selector, context).Any(card =>
            queried.Contains(card.Area.Type)
            || card.Owner < 0 && queried.Contains(DeckType.EncounterDiscardPile)
            || card.Owner >= 0 && queried.Contains(DeckType.DiscardPile));

    private bool SelectedCardMoves(
        AbilityCardSelection selector, params DeckType[] destinations) =>
        Every(selector, context).Any(card => queried.Contains(card.Area.Type))
        || Includes(destinations);

    private bool DamageCouldDiscard(AbilityEffect damage)
    {
        long amount = AbilityAmounts.SaturatingSum(
            AbilityAmounts.SaturatingMultiply(
                Amount(DamageAmountOf(damage, context), context), multiplier),
            [EventModifier(context, "eventDamage")]);
        if (context.Power == BasicPowers.AttackVerb)
            amount = AbilityAmounts.SaturatingSum(
                amount, [EventModifier(context, "attackDamage")]);
        return DamageTargets(DamageSelectionOf(damage, context), context).Any(card =>
            CanDefeatIntoQueriedDiscard(card, amount));
    }

    private bool MovedDamageCouldDiscard(AbilityEffect movement)
    {
        var move = EffectOf<AbilityEffect.MoveDamage>(movement, context);
        var from = Find(move.From, context);
        var to = Find(move.To, context);
        if (from is null || to is null) return false;
        long amount = Math.Min(from.Damage,
            AbilityAmounts.SaturatingMultiply(Amount(move.Amount, context), multiplier));
        return CanDefeatIntoQueriedDiscard(to, amount);
    }

    private bool CanDefeatIntoQueriedDiscard(Card card, long amount) =>
        amount > 0
        && AbilityProgramQueries.CanTakeDamage(
            context.World, context.Program, card, context.Source)
        && Statuses.Count(context.World, card, Statuses.Tough) == 0
        && DamagePlacement.Health(context.World, context.World.Facts, card)
            - card.Damage <= amount
        && DiscardAreaIsQueried(card);

    private bool DiscardAreaIsQueried(Card card) =>
        EffectiveCards.Kind(card, context.World.Facts) switch
        {
            CardKind.Minion => queried.Contains(DeckType.EncounterDiscardPile),
            CardKind.Ally => queried.Contains(DeckType.DiscardPile),
            _ => false,
        };

    private bool ThreatRemovalCouldDiscard(AbilityEffect removal)
    {
        if (!queried.Contains(DeckType.EncounterDiscardPile)) return false;
        var effect = EffectOf<AbilityEffect.RemoveThreat>(removal, context);
        long amount = AbilityEventModifiers.ThreatRemovalAmount(effect,
            context.World, context.Source, Amount(effect.Amount, context), multiplier);
        return Every(effect.Schemes, context).Any(scheme =>
            EffectiveCards.Kind(scheme, context.World.Facts) == CardKind.EncounterSideScheme
            && AbilityThreatPermissions.CanRemoveThreat(context.World, context.Program, scheme,
                byThwarting: context.Power == BasicPowers.ThwartVerb)
            && scheme.Tokens.GetValueOrDefault("k_threat") <= amount
            && AbilityProjectedDefeat.DefeatTreeChangesArea(
                scheme, queried, context));
    }

    private bool RepeatedChange(AbilityEffect effect)
    {
        if (CurrentlyZeroForEach(effect, context)) return false;
        long count = ForEachCount(effect, context);
        return new AbilityAreaMutationQuery(
            queried, context, AbilityAmounts.SaturatingMultiply(multiplier, count))
            .MayChange(EffectBody(effect));
    }

    private bool EachPlayerChange(AbilityEffect effect) =>
        AbilityEachPlayerSelection.Players(effect, context.Expressions).Any(player =>
            new AbilityAreaMutationQuery(queried, context.WithPlayer(player), multiplier)
                .MayChange(EffectBody(effect)));

    private IEnumerable<AbilityEffect> ReachableChildren(AbilityEffect effect) =>
        effect.OperationName() switch
        {
            // Choice effects join PriorSteps only after their answer is validated.
            "choose" or "chooseCard" => [],
            "if" => ReachableMutationBranches(effect, context),
            "defense" or "delayUntil" or "attack" or "thwart" =>
                [EffectBody(effect)],
            _ => ResolutionChildren(effect),
        };

    private bool Includes(params DeckType[] areas) => areas.Any(queried.Contains);
}
