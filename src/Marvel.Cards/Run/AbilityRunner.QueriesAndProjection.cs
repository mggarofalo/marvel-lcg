using Marvel.Cards.Dsl;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Cards.Run;

public sealed partial class AbilityRunner
{
    /// <summary>The top cards of a deck, in top-to-bottom order.</summary>
    private static IReadOnlyList<Card> TopCards(Area deck, int count) =>
        [.. deck.Cards.TakeLast(count).Reverse()];

    private static Card? OneSearchedCard(IReadOnlyList<Card> found, Cast cast) => found.Count switch
        {
            0 => null,
            1 => found[0],
            _ => throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' searched and found {found.Count} matching cards; "
                + "rr:search.1 gives the player that choice and asking is not implemented"),
        };

    private static bool SingularAreaQueryIsStable(IReadOnlySet<DeckType> areas, Cast cast)
    {
        bool priorCanChange = EffectsMayChangeAnyArea(
            cast.PriorSteps, areas, cast);
        bool paymentCanChange = cast.PaymentCost is { } cost
            && CostMayChangeAnyArea(cost, areas, cast);
        if (priorCanChange || paymentCanChange)
        {
            if (cast.FilteringContinuationOption)
            {
                return false;
            }
            throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' reaches a singular area query after its "
                + "matching cards may change"
                + (cast.PriorSteps.Count > 0
                    ? " during prior effects"
                    : " during payment"));
        }
        return true;
    }

    private static bool MayChangeAnyArea(
        AbilityNode effect, IReadOnlySet<DeckType> queried, Cast cast,
        long multiplier = 1)
    {
        bool Includes(params DeckType[] areas) => areas.Any(queried.Contains);

        bool SelectedCardMovesToDiscard(AbilityCardSelection selector) =>
            Every(selector, cast).Any(card =>
                queried.Contains(card.Area.Type)
                || card.Owner < 0
                    && queried.Contains(DeckType.EncounterDiscardPile)
                || card.Owner >= 0 && queried.Contains(DeckType.DiscardPile));

        bool SelectedCardMoves(AbilityCardSelection selector, params DeckType[] destinations) =>
            Every(selector, cast).Any(card => queried.Contains(card.Area.Type))
            || Includes(destinations);

        bool DamageCouldDiscard(AbilityNode damage)
        {
            long amount = SaturatingSum(
                SaturatingMultiply(
                    Amount(DamageAmountOf(damage, cast), cast), multiplier),
                [EventModifier(cast, "eventDamage")]);
            if (cast.Power == BasicPowers.AttackVerb)
            {
                amount = SaturatingSum(
                    amount, [EventModifier(cast, "attackDamage")]);
            }
            return DamageTargets(DamageSelectionOf(damage, cast), cast).Any(card =>
                cast.Abilities.CanTakeDamage(cast.World, card, cast.Source)
                && Statuses.Count(cast.World, card, Statuses.Tough) == 0
                && Damage.Health(cast.World, cast.World.Facts, card) - card.Damage <= amount
                && (cast.World.Facts.Kind(card.FaceId) == CardKind.Minion
                        && queried.Contains(DeckType.EncounterDiscardPile)
                    || cast.World.Facts.Kind(card.FaceId) == CardKind.Ally
                        && queried.Contains(DeckType.DiscardPile)));
        }

        bool ThreatRemovalCouldDiscard(AbilityNode removal)
        {
            if (!queried.Contains(DeckType.EncounterDiscardPile))
            {
                return false;
            }
            long amount = SaturatingSum(
                SaturatingMultiply(
                    Amount(EffectOf<AbilityEffect.RemoveThreat>(removal, cast).Amount, cast), multiplier),
                [EventModifier(cast, "eventThreatRemoval")]);
            return Every(EffectOf<AbilityEffect.RemoveThreat>(removal, cast).Schemes, cast).Any(scheme =>
                cast.World.Facts.Kind(scheme.FaceId) == CardKind.EncounterSideScheme
                && scheme.Tokens.GetValueOrDefault("k_threat") <= amount
                && DefeatTreeChangesArea(scheme, queried, cast));
        }

        bool MovedDamageCouldDiscard(AbilityNode movement)
        {
            var from = Find(EffectOf<AbilityEffect.MoveDamage>(movement, cast).From, cast);
            var to = Find(EffectOf<AbilityEffect.MoveDamage>(movement, cast).To, cast);
            if (from is null || to is null)
            {
                return false;
            }
            long amount = Math.Min(
                from.Damage,
                SaturatingMultiply(
                    Amount(EffectOf<AbilityEffect.MoveDamage>(movement, cast).Amount, cast), multiplier));
            return amount > 0
                && cast.Abilities.CanTakeDamage(cast.World, to, cast.Source)
                && Statuses.Count(cast.World, to, Statuses.Tough) == 0
                && Damage.Health(cast.World, cast.World.Facts, to) - to.Damage <= amount
                && (cast.World.Facts.Kind(to.FaceId) == CardKind.Minion
                        && queried.Contains(DeckType.EncounterDiscardPile)
                    || cast.World.Facts.Kind(to.FaceId) == CardKind.Ally
                        && queried.Contains(DeckType.DiscardPile));
        }

        bool direct = effect.Kind switch
        {
            "draw" or "drawToHandSize" or "drawToPrintedHandSize" =>
                Includes(DeckType.PlayerDeck, DeckType.HandsArea),
            "discard" => SelectedCardMovesToDiscard(
                EffectOf<AbilityEffect.CardAction>(effect, cast).Selection),
            "removeFromGame" => SelectedCardMoves(
                EffectOf<AbilityEffect.CardAction>(effect, cast).Selection, DeckType.RemovedArea),
            "returnToHand" => SelectedCardMoves(
                EffectOf<AbilityEffect.CardAction>(effect, cast).Selection, DeckType.HandsArea),
            "reveal" => SelectedCardMoves(
                EffectOf<AbilityEffect.CardAction>(effect, cast).Selection, DeckType.RevealingArea),
            "putIntoPlay" => SelectedCardMoves(
                EffectOf<AbilityEffect.PutIntoPlay>(effect, cast).Card,
                DeckType.AlliesArea, DeckType.SupportsArea, DeckType.UpgradesArea,
                DeckType.EngagedEnemiesArea, DeckType.SideSchemesArea,
                DeckType.EnvironmentArea, DeckType.ObligationsArea),
            "search" => SearchAreaTypes(effect, cast).Any(queried.Contains)
                || queried.Contains(DeckType.RevealingArea),
            "shuffleInto" => Includes(
                DeckType.EncounterDeck, DeckType.EncounterDiscardPile,
                DeckType.AsideDeck),
            "dealEncounterCard" or "dealEncounterCards" or "revealTop"
                or "discardTop" or "discardUntil" or "createDrones" =>
                Includes(
                    DeckType.EncounterDeck, DeckType.EncounterDiscardPile,
                    DeckType.RevealingArea, DeckType.DealtEncounterCardsDeck),
            "dealDamage" or "dealAttackDamage" => DamageCouldDiscard(effect),
            "moveDamage" or "moveAttackDamage" =>
                MovedDamageCouldDiscard(effect),
            "removeThreat" => ThreatRemovalCouldDiscard(effect),
            "indirectDamage" => queried.Contains(DeckType.DiscardPile),
            "discardFromHand" or "discardUpToFromHand" or "discardAnyFromHand"
                or "spend" or "spendPrinted" or "spendEnergyX" =>
                Includes(DeckType.HandsArea, DeckType.DiscardPile),
            _ => false,
        };
        if (direct)
        {
            return true;
        }

        if (effect.Kind == "forEach")
        {
            if (CurrentlyZeroForEach(effect, cast))
            {
                return false;
            }
            long count = ForEachCount(effect, cast);
            return MayChangeAnyArea(
                Tree(effect.Require("effect")), queried, cast,
                SaturatingMultiply(multiplier, count));
        }
        if (effect.Kind == "eachPlayer")
        {
            int priorPlayer = cast.Player;
            try
            {
                return cast.World.PlayerOrder.Any(player =>
                {
                    cast.RestorePlayer(player);
                    return MayChangeAnyArea(
                        Tree(effect.Require("effect")), queried, cast, multiplier);
                });
            }
            finally
            {
                cast.RestorePlayer(priorPlayer);
            }
        }
        if (effect.Kind is "seq" or "and")
        {
            return EffectsMayChangeAnyArea(
                Nodes(effect.Argument).ToList(), queried, cast, multiplier);
        }

        IEnumerable<AbilityNode> reachable = effect.Kind switch
        {
            // A choice has not run during the enclosing sequence preflight.
            // Its selected option/effect is added to PriorSteps when that
            // answer is validated against the saved continuation.
            "choose" or "chooseCard" => [],
            "if" => ReachableMutationBranches(effect, cast),
            "defense" or "delayUntil"
                or "attack" or "thwart" => [Tree(effect.Require("effect"))],
            _ => StructuralChildren(effect),
        };
        return reachable.Any(child =>
            MayChangeAnyArea(child, queried, cast, multiplier));
    }

    private static bool EffectsMayChangeAnyArea(
        IReadOnlyList<AbilityNode> effects, IReadOnlySet<DeckType> queried,
        Cast cast, long baseMultiplier = 1)
        => ProjectedAreaMayChange(effects, null, queried, cast, baseMultiplier);

    private static bool CostMayChangeAnyArea(
        AbilityCost cost, IReadOnlySet<DeckType> queried, Cast cast)
        => ProjectedAreaMayChange([], cost, queried, cast);

    private static bool ProjectedAreaMayChange(
        IReadOnlyList<AbilityNode> effects, AbilityCost? cost,
        IReadOnlySet<DeckType> queried, Cast cast, long baseMultiplier = 1)
    {
        bool couldDiscard = false;

        bool DiscardedByDamage(AreaProjectionState state, Card target) =>
            state.DamageOf(target) >= state.HealthOf(cast, target)
            && DefeatTreeChangesArea(target, queried, cast);

        bool RootLeavesOnDefeat(Card root)
        {
            var kind = cast.World.Facts.Kind(root.FaceId);
            if (kind is CardKind.Minion or CardKind.Ally
                or CardKind.EncounterSideScheme)
            {
                return true;
            }
            if (!CardKinds.IsVillain(kind))
            {
                return false;
            }
            var villainDeck = cast.World.AreaOf(DeckType.VillainDeck).Cards;
            var next = villainDeck.Count > 0 ? villainDeck[^1] : null;
            return next is null || !string.Equals(
                cast.World.Facts.Title(root.FaceId),
                cast.World.Facts.Title(next.FaceId),
                StringComparison.Ordinal);
        }

        void MarkDiscardedTree(AreaProjectionState state, Card root)
        {
            var pending = new Stack<Card>();
            pending.Push(root);
            while (pending.TryPop(out var card))
            {
                if (!state.Departed.Add(card.ObjectId))
                {
                    continue;
                }
                foreach (var child in ProjectedHostedCards(
                             state, card.ObjectId).ToList())
                {
                    pending.Push(child);
                }
            }
        }

        IEnumerable<Card> ProjectedHostedCards(
            AreaProjectionState state, int host) => cast.World.Cards
            .Where(card => !state.Departed.Contains(card.ObjectId))
            .Where(card => state.Hosts.TryGetValue(card.ObjectId, out int projected)
                ? projected == host
                : card.Area.Host == host);

        void MarkHostedCardsDiscarded(AreaProjectionState state, int host)
        {
            foreach (var child in ProjectedHostedCards(state, host).ToList())
            {
                MarkDiscardedTree(state, child);
            }
        }

        bool HostedCardsChangeArea(AreaProjectionState state, int host) =>
            ProjectedHostedCards(state, host)
            .Any(card => DiscardTreeChangesArea(card));

        bool DefeatedHostsCardsChangeArea(
            AreaProjectionState state, int host) =>
            ProjectedHostedCards(state, host).Any(card =>
            {
                bool movesToVictory = cast.World.Facts.Kind(card.FaceId) is
                        CardKind.Attachment or CardKind.Upgrade
                    && Keywords.Has(
                        cast.World, card, "victory", cast.World.Facts);
                return movesToVictory
                    ? ProjectedHostedCards(state, card.ObjectId)
                        .Any(child => DiscardTreeChangesArea(child))
                    : DiscardTreeChangesArea(card);
            });

        Card? NextVillainStage(AreaProjectionState state) =>
            cast.World.AreaOf(DeckType.VillainDeck).Cards
                .LastOrDefault(card => !state.Entered.Contains(card.ObjectId));

        bool DiscardTreeChangesArea(Card root)
        {
            var pending = new Stack<Card>();
            var seen = new HashSet<int>();
            pending.Push(root);
            while (pending.Count > 0)
            {
                var card = pending.Pop();
                if (!seen.Add(card.ObjectId))
                {
                    throw new RulesNotImplementedException(
                        $"'{cast.Source.FaceId}' reaches a hosted-card cycle while "
                        + "projecting a discard");
                }
                var destination = card.Owner < 0
                    ? DeckType.EncounterDiscardPile : DeckType.DiscardPile;
                if (queried.Contains(destination))
                {
                    return true;
                }
                foreach (var child in cast.World.Areas
                             .Where(area => area.Host == card.ObjectId)
                             .SelectMany(area => area.Cards))
                {
                    pending.Push(child);
                }
            }
            return false;
        }

        void DealProjected(
            AreaProjectionState state, Card target, long amount,
            long repetitions)
        {
            if (CardKinds.IsVillain(cast.World.Facts.Kind(target.FaceId))
                && state.ActiveVillain >= 0)
            {
                target = cast.World.Cards[state.ActiveVillain];
            }
            if (amount <= 0 || repetitions <= 0
                || state.Departed.Contains(target.ObjectId)
                || !cast.Abilities.CanTakeDamage(
                    cast.World, target, cast.Source))
            {
                return;
            }
            long prevented = Math.Min(state.ToughOf(cast, target), repetitions);
            state.Tough[target.ObjectId] =
                state.ToughOf(cast, target) - prevented;
            long dealt = SaturatingMultiply(amount, repetitions - prevented);
            state.Damage[target.ObjectId] = SaturatingSum(
                state.DamageOf(target), [dealt]);
            couldDiscard |= DiscardedByDamage(state, target);
            if (state.DamageOf(target) < state.HealthOf(cast, target))
            {
                return;
            }

            if (CardKinds.IsVillain(cast.World.Facts.Kind(target.FaceId)))
            {
                var next = NextVillainStage(state);
                bool carries = next is not null && string.Equals(
                    cast.World.Facts.Title(target.FaceId),
                    cast.World.Facts.Title(next.FaceId),
                    StringComparison.Ordinal);
                int attachmentHost = state.VillainAttachmentHost >= 0
                    ? state.VillainAttachmentHost : target.ObjectId;
                if (!carries)
                {
                    couldDiscard |= HostedCardsChangeArea(state, attachmentHost);
                    MarkHostedCardsDiscarded(state, attachmentHost);
                }
                state.Departed.Add(target.ObjectId);
                state.ActiveVillain = next?.ObjectId ?? -1;
                if (next is not null)
                {
                    state.Entered.Add(next.ObjectId);
                    state.Damage[next.ObjectId] = 0;
                    if (StateFields.Modified(
                            cast.World, next, "toughness",
                            cast.World.Facts, cast.World.Players) > 0)
                    {
                        state.Tough[next.ObjectId] = Math.Max(
                            1, state.ToughOf(cast, next));
                    }
                    if (carries)
                    {
                        foreach (string status in new[]
                                 {
                                     Statuses.Tough,
                                     Statuses.Stunned,
                                     Statuses.Confused,
                                 })
                        {
                            long carried = state.StatusOf(cast, target, status);
                            if (status == Statuses.Tough)
                            {
                                state.Tough[next.ObjectId] = Math.Max(
                                    state.ToughOf(cast, next), carried);
                            }
                            else
                            {
                                state.Status[(next.ObjectId, status)] = carried;
                            }
                        }
                        foreach (var attachment in ProjectedHostedCards(
                                     state, attachmentHost).ToList())
                        {
                            state.Hosts[attachment.ObjectId] = next.ObjectId;
                        }
                        state.VillainAttachmentHost = next.ObjectId;
                    }
                }
                return;
            }

            if (RootLeavesOnDefeat(target))
            {
                couldDiscard |= DefeatedHostsCardsChangeArea(
                    state, target.ObjectId);
                MarkDiscardedTree(state, target);
            }
        }

        List<AreaProjectionState> TraceSequence(
            IEnumerable<AbilityNode> sequence,
            List<AreaProjectionState> states, long multiplier = 1)
        {
            foreach (var step in sequence)
            {
                states = Trace(step, states, baseMultiplier: multiplier);
            }
            return states;
        }

        List<AreaProjectionState> Trace(
            AbilityNode effect, List<AreaProjectionState> states,
            long repetitions = 1, long baseMultiplier = 1)
        {
            if (repetitions <= 0 || states.Count == 0)
            {
                return states;
            }

            if (effect.Kind is "dealDamage" or "dealAttackDamage")
            {
                var (cards, printedAmount) = EffectOf<AbilityEffect>(effect, cast) switch
                {
                    AbilityEffect.Damage damage => (damage.Cards, damage.Amount),
                    AbilityEffect.AttackDamage damage => (damage.Cards, damage.Amount),
                    _ => throw new InvalidOperationException("Expected a compiled damage instruction"),
                };
                long amount = SaturatingSum(
                    SaturatingMultiply(
                        Amount(printedAmount, cast), baseMultiplier),
                    [EventModifier(cast, "eventDamage"),
                     effect.Kind == "dealAttackDamage"
                         || cast.Power == BasicPowers.AttackVerb
                            ? EventModifier(cast, "attackDamage")
                            : 0]);
                foreach (var state in states)
                {
                    foreach (var target in ProjectedEvery(
                                 cards, state, cast))
                    {
                        DealProjected(state, target, amount, repetitions);
                    }
                }
                return states;
            }

            if (effect.Kind is "moveDamage" or "moveAttackDamage")
            {
                var instruction = EffectOf<AbilityEffect.MoveDamage>(effect, cast);
                long requested = SaturatingMultiply(
                    Amount(instruction.Amount, cast), baseMultiplier);
                foreach (var state in states)
                {
                    var from = ProjectedFind(instruction.From, state, cast);
                    var to = ProjectedFind(instruction.To, state, cast);
                    if (from is null || to is null
                        || !cast.Abilities.CanTakeDamage(
                            cast.World, to, cast.Source))
                    {
                        continue;
                    }
                    for (long repeat = 0;
                         repeat < repetitions && state.DamageOf(from) > 0;
                         repeat++)
                    {
                        long moved = Math.Min(state.DamageOf(from), requested);
                        state.Damage[from.ObjectId] = state.DamageOf(from) - moved;
                        DealProjected(state, to, moved, 1);
                    }
                }
                return states;
            }

            if (effect.Kind == "heal")
            {
                var instruction = EffectOf<AbilityEffect.Heal>(effect, cast);
                foreach (var state in states)
                {
                    var healed = ProjectedFind(instruction.Card, state, cast);
                    if (healed is null)
                    {
                        continue;
                    }
                    long amount = SaturatingMultiply(
                        SaturatingMultiply(
                            Amount(instruction.Amount, cast), baseMultiplier),
                        repetitions);
                    state.Damage[healed.ObjectId] = Math.Max(
                        0, state.DamageOf(healed) - amount);
                }
                return states;
            }

            if (effect.Kind == "removeThreat")
            {
                var instruction = EffectOf<AbilityEffect.RemoveThreat>(effect, cast);
                long amount = SaturatingSum(
                    SaturatingMultiply(
                        Amount(instruction.Amount, cast), baseMultiplier),
                    [EventModifier(cast, "eventThreatRemoval")]);
                foreach (var state in states)
                {
                    foreach (var scheme in ProjectedEvery(
                                 instruction.Schemes, state, cast))
                    {
                        long removed = SaturatingMultiply(amount, repetitions);
                        state.Threat[scheme.ObjectId] = Math.Max(
                            0, state.ThreatOf(scheme) - removed);
                        couldDiscard |= state.ThreatOf(scheme) == 0
                            && cast.World.Facts.Kind(scheme.FaceId)
                                == CardKind.EncounterSideScheme
                            && DefeatTreeChangesArea(scheme, queried, cast);
                        if (state.ThreatOf(scheme) == 0
                            && cast.World.Facts.Kind(scheme.FaceId)
                                == CardKind.EncounterSideScheme)
                        {
                            MarkDiscardedTree(state, scheme);
                        }
                    }
                }
                return states;
            }

            if (effect.Kind == "forEach")
            {
                if (CurrentlyZeroForEach(effect, cast))
                {
                    return states;
                }
                var repeated = Tree(effect.Require("effect"));
                long count = ForEachCount(effect, cast);
                return repeated.Kind is "dealDamage" or "dealAttackDamage"
                    or "removeThreat"
                    ? Trace(
                        repeated, states, repetitions,
                        SaturatingMultiply(baseMultiplier, count))
                    : Trace(
                        repeated, states,
                        SaturatingMultiply(repetitions, count), baseMultiplier);
            }

            if (effect.Kind == "giveStatus")
            {
                var instruction = EffectOf<AbilityEffect.GiveStatus>(effect, cast);
                string status = instruction.Status;
                foreach (var state in states)
                {
                    foreach (var target in ProjectedEvery(
                                 instruction.Cards, state, cast))
                    {
                        long limit = Statuses.Limit(
                            cast.World, cast.World.Facts, target, status);
                        long held = state.StatusOf(cast, target, status);
                        if (held >= limit)
                        {
                            continue;
                        }
                        state.Status[(target.ObjectId, status)] = held + 1;
                        if (status == Statuses.Tough)
                        {
                            state.Tough[target.ObjectId] = held + 1;
                        }
                        bool vulnerable = status is Statuses.Stunned
                                or Statuses.Confused
                            && StateFields.Modified(
                                cast.World, target, "vulnerable",
                                cast.World.Facts, cast.World.Players) > 0
                            && held + 1 >= limit && limit > 0;
                        if (vulnerable && DiscardTreeChangesArea(target))
                        {
                            couldDiscard = true;
                        }
                    }
                }
                return states;
            }

            if (effect.Kind == "grantUntil"
                && EffectOf<AbilityEffect>(effect, cast) is AbilityEffect.GrantTrait traitGrant)
            {
                string trait = traitGrant.Trait;
                foreach (var state in states)
                {
                    var target = ProjectedFind(
                        traitGrant.Cards, state, cast);
                    if (target is null)
                    {
                        continue;
                    }
                    if (!state.Traits.TryGetValue(
                            target.ObjectId, out var granted))
                    {
                        state.Traits[target.ObjectId] = granted = [];
                    }
                    granted.Add(trait);
                }
                return states;
            }

            if (effect.Kind == "grantUntil"
                && EffectOf<AbilityEffect>(effect, cast) is AbilityEffect.GrantField fieldGrant)
            {
                string field = fieldGrant.Field;
                long amount = Amount(fieldGrant.Amount, cast);
                foreach (var state in states)
                {
                    var target = ProjectedFind(
                        fieldGrant.Cards, state, cast);
                    if (target is not null)
                    {
                        var key = (target.ObjectId, field);
                        state.Modifiers[key] = SaturatingSum(
                            state.Modifiers.GetValueOrDefault(key), [amount]);
                    }
                }
                return states;
            }

            if (effect.Kind == "eachPlayer")
            {
                int priorPlayer = cast.Player;
                try
                {
                    foreach (int player in cast.World.PlayerOrder)
                    {
                        cast.RestorePlayer(player);
                        states = Trace(
                            Tree(effect.Require("effect")), states,
                            repetitions, baseMultiplier);
                    }
                    return states;
                }
                finally
                {
                    cast.RestorePlayer(priorPlayer);
                }
            }

            if (effect.Kind == "and")
            {
                if (repetitions > 1)
                {
                    for (long repeat = 0; repeat < repetitions; repeat++)
                    {
                        states = Trace(
                            effect, states, 1, baseMultiplier);
                    }
                    return states;
                }
                var simultaneous = Nodes(effect.Argument).ToList();
                const int MaximumProjectedAndEffects = 12;
                if (simultaneous.Count > MaximumProjectedAndEffects)
                {
                    throw new RulesNotImplementedException(
                        $"'{cast.Source.FaceId}' has an and-group with "
                        + $"{simultaneous.Count} effects; projecting more than "
                        + $"{MaximumProjectedAndEffects} orders is not implemented");
                }

                var frontier = states
                    .Select(state => (Mask: 0UL, State: state.Clone()))
                    .ToList();
                for (int depth = 0; depth < simultaneous.Count; depth++)
                {
                    var next = new List<(ulong Mask, AreaProjectionState State)>();
                    foreach (var (mask, state) in frontier)
                    {
                        for (int index = 0; index < simultaneous.Count; index++)
                        {
                            ulong bit = 1UL << index;
                            if ((mask & bit) != 0)
                            {
                                continue;
                            }
                            foreach (var projected in Trace(
                                         simultaneous[index], [state.Clone()],
                                         baseMultiplier: baseMultiplier))
                            {
                                next.Add((mask | bit, projected));
                            }
                        }
                    }
                    frontier = next
                        .GroupBy(candidate =>
                            (candidate.Mask, candidate.State.Key()))
                        .Select(group => group.First()).ToList();
                }
                return AreaProjectionState.Distinct(
                    frontier.Select(candidate => candidate.State));
            }

            if (effect.Kind == "if")
            {
                var test = ConditionalOf(effect, cast).Test;
                var branched = new List<AreaProjectionState>();
                foreach (var state in states)
                {
                    bool? projected = ProjectedTest(test, state, cast);
                    var branches = projected is { } result
                        ? effect.Field(result ? "then" : "else") is { } taken
                            ? [Tree(taken)]
                            : []
                        : ReachableMutationBranches(effect, cast).ToList();
                    if (branches.Count == 0)
                    {
                        branched.Add(state);
                        continue;
                    }
                    foreach (var branch in branches)
                    {
                        branched.AddRange(Trace(
                            branch, [state.Clone()], repetitions, baseMultiplier));
                    }
                }
                return AreaProjectionState.Distinct(branched);
            }

            if (effect.Kind is "then" or "otherwise")
            {
                var predecessor = Tree(effect.Require("effect"));
                var dependent = Tree(effect.Require(effect.Kind));
                var required = effect.Kind == "then"
                    ? ResolutionOutcome.Full : ResolutionOutcome.None;
                var branched = new List<AreaProjectionState>();
                foreach (var state in states)
                {
                    var outcome = ProjectedResolution(
                        predecessor, state, cast);
                    var projected = outcome == ResolutionOutcome.None
                        ? [state]
                        : Trace(
                            predecessor, [state], repetitions,
                            baseMultiplier);
                    if (outcome == required)
                    {
                        projected = Trace(
                            dependent, projected, repetitions,
                            baseMultiplier);
                    }
                    branched.AddRange(projected);
                }
                return AreaProjectionState.Distinct(branched);
            }

            if (effect.Kind is "choose" or "chooseCard")
            {
                return states;
            }

            if (effect.Kind is "attack" or "thwart")
            {
                var instruction = EffectOf<AbilityEffect.Power>(effect, cast);
                var projected = new List<AreaProjectionState>();
                foreach (var state in states)
                {
                    var target = ProjectedFind(
                        instruction.Target!, state, cast);
                    if (target is null)
                    {
                        projected.Add(state);
                        continue;
                    }
                    var prior = cast.CaptureChosen();
                    try
                    {
                        cast.Choose(target);
                        projected.AddRange(Trace(
                            Tree(effect.Require("effect")), [state],
                            repetitions, baseMultiplier));
                    }
                    finally
                    {
                        cast.RestoreChosen(prior);
                    }
                }
                return projected;
            }

            if (effect.Kind == "thwartSchemes")
            {
                var instruction = EffectOf<AbilityEffect.ThwartGroup>(effect, cast);
                var projected = new List<AreaProjectionState>();
                foreach (var state in states)
                {
                    var schemes = ProjectedEvery(
                        instruction.Schemes, state, cast);
                    if (schemes.Count == 0)
                    {
                        projected.Add(state);
                        continue;
                    }
                    var prior = cast.CaptureChosen();
                    var priorTargets = cast.PowerTargets;
                    try
                    {
                        cast.Choose(schemes[0]);
                        cast.SetPowerTargets(schemes);
                        projected.AddRange(Trace(
                            Tree(effect.Require("power")), [state],
                            repetitions, baseMultiplier));
                    }
                    finally
                    {
                        cast.RestoreChosen(prior);
                        cast.SetPowerTargets(priorTargets);
                    }
                }
                return projected;
            }

            if (effect.Kind is "defense" or "delayUntil")
            {
                return Trace(
                    Tree(effect.Require("effect")), states,
                    repetitions, baseMultiplier);
            }

            if (effect.Kind == "discard")
            {
                var instruction = EffectOf<AbilityEffect.CardAction>(effect, cast);
                foreach (var state in states)
                {
                    foreach (var card in ProjectedEvery(
                                 instruction.Selection,
                                 state,
                                 cast))
                    {
                        couldDiscard |= queried.Contains(card.Area.Type)
                            || DiscardTreeChangesArea(card);
                        MarkDiscardedTree(state, card);
                    }
                }
                return states;
            }

            if (effect.Kind == "attachTo")
            {
                var instruction = EffectOf<AbilityEffect.CardAction>(effect, cast);
                foreach (var state in states)
                {
                    var host = ProjectedFind(instruction.Selection, state, cast);
                    if (host is null)
                    {
                        continue;
                    }
                    couldDiscard |= queried.Contains(cast.Source.Area.Type)
                        || queried.Contains(DeckType.UpgradesArea);
                    state.Departed.Remove(cast.Source.ObjectId);
                    state.Entered.Add(cast.Source.ObjectId);
                    state.Hosts[cast.Source.ObjectId] = host.ObjectId;
                }
                return states;
            }

            if (effect.Kind is "removeFromGame" or "returnToHand" or "reveal")
            {
                var instruction = EffectOf<AbilityEffect.CardAction>(effect, cast);
                var destination = effect.Kind switch
                {
                    "removeFromGame" => DeckType.RemovedArea,
                    "returnToHand" => DeckType.HandsArea,
                    _ => DeckType.RevealingArea,
                };
                foreach (var state in states)
                {
                    foreach (var card in ProjectedEvery(
                                 instruction.Selection, state, cast))
                    {
                        couldDiscard |= queried.Contains(card.Area.Type)
                            || queried.Contains(destination)
                            || HostedCardsChangeArea(state, card.ObjectId);
                        MarkDiscardedTree(state, card);
                    }
                }
                return states;
            }

            if (effect.Kind == "putIntoPlay")
            {
                var instruction = EffectOf<AbilityEffect.PutIntoPlay>(effect, cast);
                foreach (var state in states)
                {
                    var selector = instruction.Card;
                    var cards = ProjectedEvery(selector, state, cast);
                    if (cards.Count == 0
                        && selector is AbilityCardSelection.Bound { Binding: AbilityCardBinding.This }
                        && state.SourceReferenceCurrent)
                    {
                        cards.Add(cast.Source);
                    }
                    foreach (var card in cards)
                    {
                        state.Departed.Remove(card.ObjectId);
                        state.Entered.Add(card.ObjectId);
                        if (!instruction.PrintedDestination)
                        {
                            state.EngagedWith[card.ObjectId] = Resolver(cast);
                        }
                        if (StateFields.Modified(
                                cast.World, card, "toughness",
                                cast.World.Facts, cast.World.Players) > 0)
                        {
                            state.Tough[card.ObjectId] = Math.Max(
                                1, state.ToughOf(cast, card));
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
                couldDiscard |= MayChangeAnyArea(
                    effect, queried, cast, baseMultiplier);
                return states;
            }

            if (effect.Kind is "draw" or "drawToHandSize"
                or "drawToPrintedHandSize" or "search"
                or "shuffleInto" or "dealEncounterCard" or "dealEncounterCards"
                or "revealTop" or "discardTop" or "discardUntil"
                or "createDrones" or "indirectDamage" or "discardFromHand"
                or "discardUpToFromHand" or "discardAnyFromHand" or "spend"
                or "spendPrinted" or "spendEnergyX")
            {
                couldDiscard |= MayChangeAnyArea(
                    effect, queried, cast, baseMultiplier);
                return states;
            }

            if (repetitions > 1)
            {
                for (long repeat = 0; repeat < repetitions; repeat++)
                {
                    states = Trace(effect, states, 1, baseMultiplier);
                }
                return states;
            }
            return TraceSequence(
                StructuralChildren(effect), states, baseMultiplier);
        }

        var removedCounters = new Dictionary<int, long>();
        void TraceCost(AbilityCost payment, AreaProjectionState state)
        {
            Card? Target(AbilityCostCard binding)
            {
                var card = CostReference(binding, cast);
                return card is not null && !state.Departed.Contains(card.ObjectId)
                    ? card : null;
            }

            switch (payment)
            {
                case AbilityCost.Sequence sequence:
                    foreach (var step in sequence.Costs
                        .Where(step => step is AbilityCost.Damage { MustTakeAll: true })
                        .Concat(sequence.Costs.OfType<AbilityCost.Spend>())
                        .Concat(sequence.Costs.Where(step => step is not
                            (AbilityCost.Spend or AbilityCost.Damage { MustTakeAll: true }))))
                    {
                        TraceCost(step, state);
                    }
                    break;
                case AbilityCost.Damage damage:
                    if (Target(damage.Card) is { } damaged)
                    {
                        DealProjected(state, damaged,
                            damage.MustTakeAll ? damage.Amount : ModifiedAbilityDamage(damage.Amount, cast), 1);
                    }
                    break;
                case AbilityCost.Heal heal:
                    if (Target(heal.Card) is { } healed)
                    {
                        state.Damage[healed.ObjectId] = Math.Max(0, state.DamageOf(healed) - heal.Amount);
                    }
                    break;
                case AbilityCost.Discard discard:
                    if (Target(discard.Card) is { } discarded)
                    {
                        couldDiscard |= queried.Contains(discarded.Area.Type)
                            || DiscardTreeChangesArea(discarded);
                        MarkDiscardedTree(state, discarded);
                    }
                    break;
                case AbilityCost.DiscardFromHand or AbilityCost.Spend or AbilityCost.SpendEnergy:
                    couldDiscard |= queried.Contains(DeckType.HandsArea)
                        || queried.Contains(DeckType.DiscardPile);
                    break;
                case AbilityCost.RemoveCounters counters:
                    if (Target(counters.Card) is { } holder)
                    {
                        long removed = checked(removedCounters.GetValueOrDefault(holder.ObjectId) + counters.Count);
                        removedCounters[holder.ObjectId] = removed;
                        if (CounterCount(holder, "allPurpose") == removed
                            && !Characteristics.IsLost(cast.World, holder, "uses")
                            && cast.Abilities.CounterPool(cast.World, holder)?.Uses == true)
                        {
                            couldDiscard |= queried.Contains(holder.Area.Type)
                                || (Keywords.Has(cast.World, holder, "victory", cast.World.Facts)
                                    ? HostedCardsChangeArea(state, holder.ObjectId)
                                    : DiscardTreeChangesArea(holder));
                            MarkDiscardedTree(state, holder);
                        }
                    }
                    break;
                case AbilityCost.Exhaust or AbilityCost.ExhaustChosen:
                    break;
                default:
                    throw new InvalidOperationException("Unknown compiled cost in area projection");
            }
        }

        var initial = new AreaProjectionState(cast);
        if (cost is not null) TraceCost(cost, initial);
        _ = TraceSequence(effects, [initial], baseMultiplier);
        return couldDiscard;
    }

    private static bool DefeatTreeChangesArea(
        Card root, IReadOnlySet<DeckType> queried, Cast cast)
    {
        var kind = cast.World.Facts.Kind(root.FaceId);
        bool rootDiscards = kind is CardKind.Minion or CardKind.Ally
                or CardKind.EncounterSideScheme
            && !Keywords.Has(cast.World, root, "victory", cast.World.Facts);
        if (rootDiscards
            && queried.Contains(root.Owner < 0
                ? DeckType.EncounterDiscardPile : DeckType.DiscardPile))
        {
            return true;
        }

        if (CardKinds.IsVillain(kind))
        {
            var villainDeck = cast.World.AreaOf(DeckType.VillainDeck).Cards;
            var next = villainDeck.Count > 0 ? villainDeck[^1] : null;
            if (next is not null && string.Equals(
                cast.World.Facts.Title(root.FaceId),
                cast.World.Facts.Title(next.FaceId),
                StringComparison.Ordinal))
            {
                return false;
            }
        }
        else if (kind is not (
            CardKind.Minion or CardKind.Ally or CardKind.EncounterSideScheme))
        {
            return false;
        }

        var direct = cast.World.Areas
            .Where(area => area.Host == root.ObjectId)
            .SelectMany(area => area.Cards)
            .ToList();
        var pending = new Stack<Card>();
        var seen = new HashSet<int> { root.ObjectId };
        foreach (var card in direct)
        {
            bool movesToVictory = cast.World.Facts.Kind(card.FaceId) is
                    CardKind.Attachment or CardKind.Upgrade
                && DeckTypes.IsInPlay(card.Area.Type)
                && Keywords.Has(cast.World, card, "victory", cast.World.Facts);
            if (movesToVictory)
            {
                foreach (var child in cast.World.Areas
                             .Where(area => area.Host == card.ObjectId)
                             .SelectMany(area => area.Cards))
                {
                    pending.Push(child);
                }
                seen.Add(card.ObjectId);
            }
            else
            {
                pending.Push(card);
            }
        }
        while (pending.TryPop(out var card))
        {
            if (!seen.Add(card.ObjectId))
            {
                throw new RulesNotImplementedException(
                    $"'{cast.Source.FaceId}' reaches a hosted-card cycle while "
                    + "projecting defeat");
            }
            if (queried.Contains(card.Owner < 0
                    ? DeckType.EncounterDiscardPile : DeckType.DiscardPile))
            {
                return true;
            }
            foreach (var child in cast.World.Areas
                         .Where(area => area.Host == card.ObjectId)
                         .SelectMany(area => area.Cards))
            {
                pending.Push(child);
            }
        }
        return false;
    }

    private static List<Card> ProjectedEvery(
        AbilityCardSelection selector, AreaProjectionState state, Cast cast)
    {
        var found = Every(selector, cast)
            .Where(card => !state.Departed.Contains(card.ObjectId))
            .ToList();
        if (selector is AbilityCardSelection.Bound bound)
        {
            return bound.Binding == AbilityCardBinding.This && !state.SourceReferenceCurrent
                ? [] : found;
        }
        if (selector is AbilityCardSelection.EnemiesWithTrait enemies)
        {
            return [.. ProjectedEvery(new AbilityCardSelection.Query(AbilityCardQuery.Enemies), state, cast)
                .Where(card => state.HasTrait(cast, card, enemies.Trait))];
        }
        if (selector is AbilityCardSelection.Ranked ranked)
        {
            var among = ProjectedEvery(ranked.Cards, state, cast)
                .Where(card => state.Entered.Contains(card.ObjectId)
                    ? Rules.Play.Discard.EffectCanRemove(cast.World, cast.World.Facts, cast.Source, card)
                    : CanRemoveByEffect(ranked.Cards, cast, card))
                .ToList();
            if (among.Count == 0) return [];
            long Rank(Card card) => ranked.By switch
            {
                AbilityCardRank.Cost => cast.World.Facts.PrintedValue(card.FaceId, "Cost", cast.World.Players),
                AbilityCardRank.Attack => state.ModifiedOf(cast, card, "attack"),
                AbilityCardRank.PrintedHealth => FacedownDrones.BaseValue(card, cast.World.Facts, "HP", cast.World.Players),
                _ => throw new InvalidOperationException("Unknown compiled card rank in area projection"),
            };
            long extreme = ranked.Maximum ? among.Max(Rank) : among.Min(Rank);
            return [.. among.Where(card => Rank(card) == extreme)];
        }
        if (selector is AbilityCardSelection.WithTrait trait)
        {
            return [.. ProjectedEvery(trait.Cards, state, cast)
                .Where(card => state.HasTrait(cast, card, trait.Trait))];
        }
        if (selector is AbilityCardSelection.Query { Kind: AbilityCardQuery.Villain }
            && state.ActiveVillain >= 0)
        {
            found.RemoveAll(card => CardKinds.IsVillain(cast.World.Facts.Kind(card.FaceId)));
            found.Add(cast.World.Cards[state.ActiveVillain]);
        }
        else if (selector is AbilityCardSelection.Query query)
        {
            if (query.Kind is AbilityCardQuery.AttackableEnemies or AbilityCardQuery.AttackableMinions)
            {
                var candidates = query.Kind == AbilityCardQuery.AttackableEnemies
                    ? AbilityCardQuery.Enemies : AbilityCardQuery.Minions;
                found = ProjectedEvery(new AbilityCardSelection.Query(candidates), state, cast)
                    .Where(card => cast.World.Abilities.CanTakeDamage(cast.World, card, cast.Source))
                    .Where(card => query.Kind != AbilityCardQuery.AttackableMinions
                        || cast.World.Facts.Kind(card.FaceId) == CardKind.Minion)
                    .ToList();
                bool guard = found.Any(card =>
                    cast.World.Facts.Kind(card.FaceId) == CardKind.Minion
                    && state.ModifiedOf(cast, card, "guard") > 0
                    && (state.EngagedWith.GetValueOrDefault(card.ObjectId, -1) == Resolver(cast)
                        || !state.EngagedWith.ContainsKey(card.ObjectId)
                            && card.Area.PlayArea == PlayArea.Of(Resolver(cast))));
                if (guard)
                {
                    found.RemoveAll(card => CardKinds.IsVillain(cast.World.Facts.Kind(card.FaceId)));
                }
            }
            else
            {
                found.AddRange(state.Entered
                    .Select(id => cast.World.Cards[id])
                    .Where(card => !state.Departed.Contains(card.ObjectId))
                    .Where(card => query.Kind switch
                    {
                        AbilityCardQuery.Minions => cast.World.Facts.Kind(card.FaceId) == CardKind.Minion,
                        AbilityCardQuery.MinionsEngagedWithYou =>
                            cast.World.Facts.Kind(card.FaceId) == CardKind.Minion
                            && state.EngagedWith.GetValueOrDefault(card.ObjectId, -1) == Resolver(cast),
                        AbilityCardQuery.EnemiesEngagedWithChosenPlayer =>
                            cast.World.Facts.Kind(card.FaceId) == CardKind.Minion
                            && state.EngagedWith.GetValueOrDefault(card.ObjectId, -1) == ChosenPlayer(cast).Owner,
                        AbilityCardQuery.Enemies => CardKinds.IsEnemy(cast.World.Facts.Kind(card.FaceId)),
                        AbilityCardQuery.Characters => CardKinds.IsCharacter(cast.World.Facts.Kind(card.FaceId)),
                        AbilityCardQuery.SideSchemes or AbilityCardQuery.Schemes or AbilityCardQuery.ThwartableSchemes =>
                            cast.World.Facts.Kind(card.FaceId) == CardKind.EncounterSideScheme,
                        _ => false,
                    }));
            }
        }
        else if (selector is AbilityCardSelection.Titled titled)
        {
            found.AddRange(state.Entered
                .Select(id => cast.World.Cards[id])
                .Where(card => !state.Departed.Contains(card.ObjectId))
                .Where(card => string.Equals(cast.World.Facts.Title(card.FaceId), titled.Title, StringComparison.Ordinal)));
        }
        return [.. found.DistinctBy(card => card.ObjectId)];
    }

    private static Card? ProjectedFind(
        AbilityCardSelection selector, AreaProjectionState state, Cast cast)
    {
        var found = ProjectedEvery(selector, state, cast);
        return found.Count switch
        {
            0 => null,
            1 => found[0],
            _ => throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' projects {found.Count} cards where one is required"),
        };
    }

    private static bool? ProjectedTest(
        AbilityCondition test, AreaProjectionState state, Cast cast)
    {
        if (test is AbilityCondition.CardText { Property: AbilityCardTextProperty.Status } status
            && ProjectedFind(status.Card, state, cast) is { } target)
        {
            return state.StatusOf(cast, target, status.Text) > 0;
        }
        if (test is AbilityCondition.Negated negated)
        {
            return ProjectedTest(negated.Operand, state, cast) is { } inner
                ? !inner : null;
        }
        if (test is AbilityCondition.All or AbilityCondition.Any)
        {
            var operands = test is AbilityCondition.All all ? all.Operands : ((AbilityCondition.Any)test).Operands;
            var values = operands
                .Select(child => ProjectedTest(child, state, cast)).ToList();
            if (test is AbilityCondition.All && values.Any(value => value == false))
            {
                return false;
            }
            if (test is AbilityCondition.Any && values.Any(value => value == true))
            {
                return true;
            }
            return values.Any(value => value is null)
                ? null
                : test is AbilityCondition.All;
        }
        return null;
    }

    private static ResolutionOutcome ProjectedResolution(
        AbilityNode effect, AreaProjectionState state, Cast cast) =>
        effect.Kind switch
        {
            "seq" or "and" => CombinedOutcomes(
                Nodes(effect.Argument).Select(child =>
                    ProjectedResolution(child, state, cast))),
            "if" => ProjectedTest(
                    ConditionalOf(effect, cast).Test, state, cast)
                is { } result
                    ? effect.Field(result ? "then" : "else") is { } branch
                        ? ProjectedResolution(Tree(branch), state, cast)
                        : ResolutionOutcome.None
                    : throw new RulesNotImplementedException(
                        $"'{cast.Source.FaceId}' has a projected dependent condition "
                        + "whose outcome is not implemented"),
            "forEach" when ForEachCount(effect, cast) == 0 =>
                ResolutionOutcome.None,
            "forEach" => ProjectedResolution(
                Tree(effect.Require("effect")), state, cast),
            "heal" => ResolutionOfAmount(
                ProjectedFind(EffectOf<AbilityEffect.Heal>(effect, cast).Card, state, cast) is { } healed
                    ? state.DamageOf(healed) : 0,
                Amount(EffectOf<AbilityEffect.Heal>(effect, cast).Amount, cast)),
            "removeThreat" => CombinedOutcomes(
                ProjectedEvery(EffectOf<AbilityEffect.RemoveThreat>(effect, cast).Schemes, state, cast).Select(scheme =>
                    ResolutionOfAmount(
                        state.ThreatOf(scheme),
                        Amount(EffectOf<AbilityEffect.RemoveThreat>(effect, cast).Amount, cast)))),
            _ => ResolutionOf(effect, cast),
        };

    private sealed class AreaProjectionState(Cast cast)
    {
        public Dictionary<int, long> Damage { get; } = [];
        public Dictionary<int, long> Tough { get; } = [];
        public Dictionary<int, long> Threat { get; } = [];
        public Dictionary<(int Card, string Status), long> Status { get; } = [];
        public HashSet<int> Departed { get; } = [];
        public HashSet<int> Entered { get; } = [];
        public Dictionary<int, int> Hosts { get; } = [];
        public Dictionary<int, int> EngagedWith { get; } = [];
        public Dictionary<int, HashSet<string>> Traits { get; } = [];
        public Dictionary<(int Card, string Field), long> Modifiers { get; } = [];
        public bool SourceReferenceCurrent { get; set; } = true;
        public int ActiveVillain { get; set; } =
            cast.World.TheCardIn(DeckType.VillainArea)?.ObjectId ?? -1;
        public int VillainAttachmentHost { get; set; } = -1;

        public long DamageOf(Card card) =>
            Damage.GetValueOrDefault(card.ObjectId, card.Damage);

        public long ToughOf(Cast current, Card card) => Tough.GetValueOrDefault(
            card.ObjectId,
            Statuses.Count(current.World, card, Statuses.Tough));

        public long StatusOf(Cast current, Card card, string status) =>
            status == Statuses.Tough
                ? ToughOf(current, card)
                : Status.GetValueOrDefault(
                    (card.ObjectId, status),
                    Statuses.Count(current.World, card, status));

        public long ThreatOf(Card card) => Threat.GetValueOrDefault(
            card.ObjectId, card.Tokens.GetValueOrDefault("k_threat"));

        public long HealthOf(Cast current, Card card) => SaturatingSum(
            FacedownDrones.BaseValue(
                card, current.World.Facts, "HP", current.World.Players),
            [ModifiedOf(current, card, "health")]);

        public long ModifiedOf(Cast current, Card card, string field)
        {
            long value = StateFields.Modified(
                current.World, card, field,
                current.World.Facts, current.World.Players);
            var effects = current.World.Effects.Active();
            foreach (var effect in effects)
            {
                if (effect.Source == EffectSource.ConstantAbility
                    && effect.Card is int hostedSource
                    && !Departed.Contains(hostedSource)
                    && Hosts.TryGetValue(hostedSource, out int projectedHost)
                    && string.Equals(effect.Kind, field, StringComparison.Ordinal))
                {
                    bool liveApplies = effect.AppliesTo(current.World, card);
                    if (liveApplies && projectedHost != card.ObjectId)
                    {
                        value -= effect.Amount;
                    }
                    else if (!liveApplies && projectedHost == card.ObjectId)
                    {
                        value += effect.Amount;
                    }
                }
                if (effect.Source == EffectSource.ConstantAbility
                    && effect.Card is int source
                    && Departed.Contains(source)
                    && string.Equals(effect.Kind, field, StringComparison.Ordinal)
                    && effect.AppliesTo(current.World, card))
                {
                    value -= effect.Amount;
                }
            }
            if (ProjectedPredicateInputsChanged(current)
                && ConditionalConstantMayModify(current, card, field))
            {
                throw new RulesNotImplementedException(
                    $"'{current.Source.FaceId}' changes game state before reading "
                    + $"a conditional constant '{field}' modifier on "
                    + $"'{card.FaceId}'; projecting that predicate is not implemented");
            }
            value = SaturatingSum(
                value,
                [Modifiers.GetValueOrDefault((card.ObjectId, field))]);

            string? printedModifier = field switch
            {
                "attack" => "ATK+",
                "scheme" => "SCH+",
                "thwart" => "THW+",
                _ => null,
            };
            if (printedModifier is null)
            {
                return value;
            }
            foreach (var attached in current.World.Cards.Where(candidate =>
                         candidate.Area.Host == card.ObjectId
                         && DeckTypes.IsInPlay(candidate.Area.Type)))
            {
                if (Departed.Contains(attached.ObjectId)
                    || Hosts.TryGetValue(attached.ObjectId, out int projected)
                        && projected != card.ObjectId)
                {
                    value -= current.World.Facts.PrintedValue(
                        attached.FaceId, printedModifier, current.World.Players);
                }
            }
            foreach (var (attachedId, host) in Hosts)
            {
                var attached = current.World.Cards[attachedId];
                if (host == card.ObjectId
                    && attached.Area.Host != card.ObjectId
                    && !Departed.Contains(attachedId))
                {
                    value += current.World.Facts.PrintedValue(
                        attached.FaceId, printedModifier, current.World.Players);
                }
            }
            return value;
        }

        private bool ProjectedPredicateInputsChanged(Cast current) =>
            Damage.Any(pair => pair.Value
                != current.World.Cards[pair.Key].Damage)
            || Tough.Any(pair => pair.Value != Statuses.Count(
                current.World, current.World.Cards[pair.Key], Statuses.Tough))
            || Threat.Any(pair => pair.Value != current.World.Cards[pair.Key]
                .Tokens.GetValueOrDefault("k_threat"))
            || Status.Any(pair => pair.Value != Statuses.Count(
                current.World,
                current.World.Cards[pair.Key.Card], pair.Key.Status))
            || Departed.Count > 0
            || Entered.Count > 0
            || Hosts.Count > 0
            || EngagedWith.Count > 0
            || Traits.Count > 0
            || Modifiers.Count > 0
            || ActiveVillain != (current.World.TheCardIn(
                DeckType.VillainArea)?.ObjectId ?? -1);

        private bool ConditionalConstantMayModify(
            Cast current, Card target, string field)
        {
            if (current.Abilities is not AbilityRunner runner)
            {
                return false;
            }
            var sources = current.World.Areas
                .Where(area => DeckTypes.IsInPlay(area.Type))
                .SelectMany(area => area.Cards)
                .Concat(Entered.Select(id => current.World.Cards[id]))
                .Where(source => !Departed.Contains(source.ObjectId))
                .DistinctBy(source => source.ObjectId);
            return sources
                .Any(source => runner.CompiledOn(source)
                    .Where(ability =>
                        ability.Trigger.Timing == AbilityType.Constant)
                    .Any(ability => ConditionalGrant(
                        ability.Effect, field, ability.When is not null,
                        source, target, current)));
        }

        private bool ConditionalGrant(
            AbilityEffect effect, string field, bool conditioned,
            Card source, Card target, Cast current)
        {
            if (effect is AbilityEffect.Conditional conditional)
            {
                return conditional.Then is { } then && ConditionalGrant(
                        then, field, conditioned: true, source, target, current)
                    || conditional.Else is { } otherwise && ConditionalGrant(
                        otherwise, field, conditioned: true, source, target, current);
            }
            if (effect is AbilityEffect.GrantField { Until: null } grant
                && string.Equals(grant.Field, field, StringComparison.Ordinal))
            {
                bool dynamicAmount = grant.Amount is not AbilityNumber.Constant;
                return (conditioned || dynamicAmount)
                    && GrantCouldAffect(grant.Cards, source, target, current);
            }
            if (effect is AbilityEffect.Sequence sequence)
            {
                return sequence.Effects.Any(child =>
                    ConditionalGrant(
                        child, field, conditioned, source, target, current));
            }
            if (effect is AbilityEffect.Simultaneous simultaneous)
            {
                return simultaneous.Effects.Any(child =>
                    ConditionalGrant(
                        child, field, conditioned, source, target, current));
            }
            return false;
        }

        private bool GrantCouldAffect(
            AbilityCardSelection selector, Card source, Card target, Cast current)
        {
            if (selector is AbilityCardSelection.Bound { Binding: AbilityCardBinding.This })
            {
                return source.ObjectId == target.ObjectId;
            }
            if (selector is AbilityCardSelection.Bound { Binding: AbilityCardBinding.AttachedTo })
            {
                return Hosts.GetValueOrDefault(
                    source.ObjectId, source.Area.Host) == target.ObjectId;
            }
            if (selector is AbilityCardSelection.Titled titled)
            {
                return string.Equals(
                    current.World.Facts.Title(target.FaceId),
                    titled.Title, StringComparison.Ordinal);
            }
            if (selector is AbilityCardSelection.Query { Kind: AbilityCardQuery.Villain })
            {
                return CardKinds.IsVillain(
                    current.World.Facts.Kind(target.FaceId));
            }
            // Other selectors may change membership with the projected facts.
            // Failing closed is required until that membership is projected.
            return true;
        }

        public bool HasTrait(Cast current, Card card, string trait)
        {
            var active = current.World.Effects.Active()
                .Where(effect => effect.Source != EffectSource.ConstantAbility
                    || effect.Card is not int source
                    || !Departed.Contains(source))
                .ToList();
            bool lost = active.Any(effect =>
                ProjectedTraitEffectApplies(current, effect, card)
                && string.Equals(
                    effect.Kind,
                    Characteristics.Lost + Rules.State.Traits.Granted + trait,
                    StringComparison.Ordinal));
            if (lost)
            {
                return false;
            }
            return FacedownDrones.InherentTraits(card, current.World.Facts)
                    .Contains(trait, StringComparer.Ordinal)
                || active.Any(effect =>
                    ProjectedTraitEffectApplies(current, effect, card)
                    && string.Equals(
                        effect.Kind,
                        Rules.State.Traits.Granted + trait,
                        StringComparison.Ordinal))
                || Traits.TryGetValue(card.ObjectId, out var granted)
                    && granted.Contains(trait);
        }

        private bool ProjectedTraitEffectApplies(
            Cast current, ContinuousEffect effect, Card card)
        {
            if (effect.Source == EffectSource.ConstantAbility
                && effect.Card is int source
                && Hosts.TryGetValue(source, out int projectedHost)
                && current.World.Cards[source].Area.Host == effect.Affects)
            {
                return projectedHost == card.ObjectId;
            }
            return effect.AppliesTo(current.World, card);
        }

        public AreaProjectionState Clone()
        {
            var clone = new AreaProjectionState(cast);
            foreach (var (card, amount) in Damage)
            {
                clone.Damage[card] = amount;
            }
            foreach (var (card, count) in Tough)
            {
                clone.Tough[card] = count;
            }
            foreach (var (card, amount) in Threat)
            {
                clone.Threat[card] = amount;
            }
            foreach (var (key, count) in Status)
            {
                clone.Status[key] = count;
            }
            clone.Departed.UnionWith(Departed);
            clone.Entered.UnionWith(Entered);
            foreach (var (card, host) in Hosts)
            {
                clone.Hosts[card] = host;
            }
            foreach (var (card, player) in EngagedWith)
            {
                clone.EngagedWith[card] = player;
            }
            foreach (var (card, traits) in Traits)
            {
                clone.Traits[card] = [.. traits];
            }
            foreach (var (key, amount) in Modifiers)
            {
                clone.Modifiers[key] = amount;
            }
            clone.ActiveVillain = ActiveVillain;
            clone.VillainAttachmentHost = VillainAttachmentHost;
            clone.SourceReferenceCurrent = SourceReferenceCurrent;
            return clone;
        }

        public static List<AreaProjectionState> Distinct(
            IEnumerable<AreaProjectionState> states) =>
            states.GroupBy(state => state.Key(), StringComparer.Ordinal)
                .Select(group => group.First()).ToList();

        public string Key() => string.Join(
            ";",
            Damage.OrderBy(pair => pair.Key)
                .Select(pair => $"d{pair.Key}:{pair.Value}")
                .Concat(Tough.OrderBy(pair => pair.Key)
                    .Select(pair => $"s{pair.Key}:{pair.Value}"))
                .Concat(Threat.OrderBy(pair => pair.Key)
                    .Select(pair => $"t{pair.Key}:{pair.Value}"))
                .Concat(Status.OrderBy(pair => pair.Key.Card)
                    .ThenBy(pair => pair.Key.Status, StringComparer.Ordinal)
                    .Select(pair =>
                        $"x{pair.Key.Card}:{pair.Key.Status}:{pair.Value}"))
                .Concat(Departed.Order().Select(card => $"o{card}"))
                .Concat(Entered.Order().Select(card => $"i{card}"))
                .Concat(Hosts.OrderBy(pair => pair.Key)
                    .Select(pair => $"h{pair.Key}:{pair.Value}"))
                .Concat(EngagedWith.OrderBy(pair => pair.Key)
                    .Select(pair => $"e{pair.Key}:{pair.Value}"))
                .Concat(Traits.OrderBy(pair => pair.Key)
                    .SelectMany(pair => pair.Value.Order(StringComparer.Ordinal)
                        .Select(trait => $"g{pair.Key}:{trait}")))
                .Concat(Modifiers.OrderBy(pair => pair.Key.Card)
                    .ThenBy(pair => pair.Key.Field, StringComparer.Ordinal)
                    .Select(pair =>
                        $"m{pair.Key.Card}:{pair.Key.Field}:{pair.Value}"))
                .Append($"v{ActiveVillain}:{VillainAttachmentHost}")
                .Append($"r{SourceReferenceCurrent}"));
    }

}
