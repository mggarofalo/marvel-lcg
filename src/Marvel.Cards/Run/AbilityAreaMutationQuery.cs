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
    private static readonly HashSet<string> DrawOperations =
    [
        "draw", "drawToHandSize", "drawToPrintedHandSize",
    ];

    private static readonly HashSet<string> EncounterDeckOperations =
    [
        "dealEncounterCard", "dealEncounterCards", "revealTop", "discardTop",
        "discardUntil", "createDrones",
    ];

    private static readonly HashSet<string> HandPaymentOperations =
    [
        "discardFromHand", "discardUpToFromHand", "discardAnyFromHand",
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
        if (DrawOperations.Contains(operation))
            return Includes(DeckType.PlayerDeck, DeckType.HandsArea);
        if (operation is "discard" or "removeFromGame" or "returnToHand"
            or "reveal" or "putIntoPlay")
        {
            return CardMovementChange(effect);
        }
        if (operation == "search")
            return SearchAreaTypes(effect, context).Any(queried.Contains)
                || queried.Contains(DeckType.RevealingArea);
        if (operation == "shuffleInto")
            return Includes(
                DeckType.EncounterDeck, DeckType.EncounterDiscardPile,
                DeckType.AsideDeck);
        if (EncounterDeckOperations.Contains(operation))
            return Includes(
                DeckType.EncounterDeck, DeckType.EncounterDiscardPile,
                DeckType.RevealingArea, DeckType.DealtEncounterCardsDeck);
        if (HandPaymentOperations.Contains(operation))
            return Includes(DeckType.HandsArea, DeckType.DiscardPile);
        return null;
    }

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
            "discard" => SelectedCardMovesToDiscard(
                EffectOf<AbilityEffect.CardAction>(effect, context).Selection),
            "removeFromGame" => SelectedCardMoves(
                EffectOf<AbilityEffect.CardAction>(effect, context).Selection,
                DeckType.RemovedArea),
            "returnToHand" => SelectedCardMoves(
                EffectOf<AbilityEffect.CardAction>(effect, context).Selection,
                DeckType.HandsArea),
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
        context.World.Facts.Kind(card.FaceId) switch
        {
            CardKind.Minion => queried.Contains(DeckType.EncounterDiscardPile),
            CardKind.Ally => queried.Contains(DeckType.DiscardPile),
            _ => false,
        };

    private bool ThreatRemovalCouldDiscard(AbilityEffect removal)
    {
        if (!queried.Contains(DeckType.EncounterDiscardPile)) return false;
        var effect = EffectOf<AbilityEffect.RemoveThreat>(removal, context);
        long amount = AbilityAmounts.SaturatingSum(
            AbilityAmounts.SaturatingMultiply(
                Amount(effect.Amount, context), multiplier),
            [EventModifier(context, "eventThreatRemoval")]);
        return Every(effect.Schemes, context).Any(scheme =>
            context.World.Facts.Kind(scheme.FaceId) == CardKind.EncounterSideScheme
            && scheme.Tokens.GetValueOrDefault("k_threat") <= amount
            && AbilityProjectedStateQueries.DefeatTreeChangesArea(
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
        context.World.PlayerOrder.Any(player =>
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
