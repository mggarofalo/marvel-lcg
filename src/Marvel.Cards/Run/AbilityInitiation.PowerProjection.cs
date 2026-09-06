using static Marvel.Cards.Run.AbilityEffectStructure;
using System.Collections.Immutable;
using Marvel.Cards.Dsl;
using PowerReachability = Marvel.Rules.Play.RuleProjection<Marvel.Cards.Run.AbilityPowerState>;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Cards.Run;

internal static partial class AbilityInitiation
{
    private static EliminationLayout PlanTracePlayerElimination(
        int player, AbilityAdmissionScope cast, HashSet<int> discarded,
        Dictionary<int, int> engagement)
        => EliminationLayout.Calculate(
            new AbilityEliminationLayout(cast.World, discarded, engagement), player);

    private static AbilityPowerState AdvancePowerVillain(
        AbilityPowerState state, Card damaged, Card first, AbilityAdmissionScope cast)
    {
        if (damaged.ObjectId != state.CurrentVillain
            || PowerDamage(state, damaged) < PowerHealth(state, damaged, cast))
        {
            return state;
        }

        var deck = cast.World.AreaOf(DeckType.VillainDeck);
        int nextIndex = deck.Cards.Count - 1 - state.VillainStagesDrawn;
        Card? next = nextIndex >= 0 ? deck.Cards[nextIndex] : null;
        bool carriesAttachments = next is not null && string.Equals(
            cast.World.Facts.Title(damaged.FaceId),
            cast.World.Facts.Title(next.FaceId),
            StringComparison.Ordinal);
        var discarded = new HashSet<int>(state.Discarded) { damaged.ObjectId };
        if (!carriesAttachments)
        {
            foreach (int leaving in PowerLeavingTree(damaged, cast).Skip(1))
            {
                discarded.Add(leaving);
            }
        }
        if (next is not null
            && cast.Context.Program.On(next.FaceId).Any(ability =>
                ability.Trigger.Timing == AbilityType.Constant))
        {
            // The stage is intentionally not moved while eligibility is
            // traced, so its constant abilities are absent from the unchanged
            // World. Refuse the labelled continuation before its cost mutates.
            throw new RulesNotImplementedException(
                $"villain stage '{next.FaceId}' enters play before a "
                + "labelled-power continuation reads its constant abilities, "
                + "which is not implemented");
        }
        if (next is not null
            && HasLiveVillainRetargetingConstant(
                discarded, damaged, next, cast,
                threatChanges: state.SchemeThreat,
                damageChanges: state.CardDamage,
                modifierChanges: state.Modifiers,
                traitChanges: state.Traits,
                statusChanges:
                [
                    .. state.StatusChanges,
                    .. state.CardTough.Keys.Select(card => (card, Statuses.Tough)),
                ],
                engagementChanges: state.Engagement,
                formsMayChange: state.FormsMayChange,
                traceFirstPlayer: state.FirstPlayer))
        {
            // A constant selector such as { query: villain } follows the new
            // stage as soon as it enters play. The unchanged World still binds
            // that effect to the defeated stage, so refuse before the labelled
            // cost rather than project the continuation from stale modifiers.
            throw new RulesNotImplementedException(
                $"villain stage '{next.FaceId}' enters play before a "
                + "labelled-power continuation reads retargeting constant "
                + "abilities, which is not implemented");
        }
        var engagement = new Dictionary<int, int>(state.Engagement);
        if (!carriesAttachments)
        {
            foreach (int leaving in discarded.Where(cardId =>
                cardId != damaged.ObjectId
                && !state.Discarded.Contains(cardId)))
            {
                engagement.Remove(leaving);
            }
        }
        if (nextIndex < 0)
        {
            return state with
            {
                Discarded = discarded,
                Engagement = engagement,
                CurrentVillain = -1,
                Finished = true,
            };
        }

        next = deck.Cards[nextIndex];
        bool carriesTough = string.Equals(
                cast.World.Facts.Title(damaged.FaceId),
                cast.World.Facts.Title(next.FaceId),
                StringComparison.Ordinal)
            && PowerTough(state, damaged, cast);
        var advanced = state with
        {
            Discarded = discarded,
            Engagement = engagement,
            CurrentVillain = next.ObjectId,
            VillainStagesDrawn = state.VillainStagesDrawn + 1,
        };
        advanced = SetPowerDamage(advanced, next, 0, first, cast);
        bool printedTough = StateFields.Modified(
            cast.World, next, "toughness",
            cast.World.Facts, cast.World.Players) > 0;
        return SetPowerTough(
            advanced, next, carriesTough || printedTough, first, cast);
    }

    private static bool ConstantCanRetargetVillain(
        AbilityEffect node, Card current, Card next, AbilityAdmissionScope cast,
        HashSet<int> discarded,
        IReadOnlyDictionary<int, long> threatChanges,
        IReadOnlyDictionary<int, long> damageChanges,
        IReadOnlyDictionary<(int Card, string Field), long> modifierChanges,
        IReadOnlyDictionary<int, HashSet<string>> traitChanges,
        IReadOnlySet<(int Card, string Status)> statusChanges,
        IReadOnlyDictionary<int, int> engagementChanges,
        ulong formsMayChange,
        int traceFirstPlayer)
    {
        if (node is AbilityEffect.Conditional conditional)
        {
            var test = conditional.Test;
            if (TryTraceConstantTest(
                test, current, next, cast, discarded,
                threatChanges, damageChanges, modifierChanges,
                traitChanges, statusChanges, engagementChanges,
                formsMayChange, traceFirstPlayer,
                out bool tracedTest))
            {
                return (tracedTest ? conditional.Then : conditional.Else) is { } traced
                    && ConstantCanRetargetVillain(
                        traced, current, next, cast, discarded,
                        threatChanges, damageChanges, modifierChanges,
                        traitChanges, statusChanges, engagementChanges,
                        formsMayChange,
                        traceFirstPlayer);
            }
            if (TestCanChangeOnVillainAdvance(
                test, current, next, cast, discarded,
                threatChanges, damageChanges, modifierChanges,
                traitChanges, statusChanges, engagementChanges,
                formsMayChange, traceFirstPlayer))
            {
                return StructuralChildren(node).Any(child =>
                    ConstantCanRetargetVillain(
                        child, current, next, cast, discarded,
                        threatChanges, damageChanges, modifierChanges,
                        traitChanges, statusChanges, engagementChanges,
                        formsMayChange, traceFirstPlayer));
            }
            return (Test(test, cast) ? conditional.Then : conditional.Else) is { } active
                && ConstantCanRetargetVillain(
                    active, current, next, cast, discarded,
                    threatChanges, damageChanges, modifierChanges,
                    traitChanges, statusChanges, engagementChanges,
                    formsMayChange, traceFirstPlayer);
        }
        AbilityCardSelection? target = node switch
        {
            AbilityEffect.GrantField { Until: null } grant => grant.Cards,
            AbilityEffect.GrantTrait { Until: null } grant => grant.Cards,
            _ => null,
        };
        if (target is not null && PotentialVillainSelector(target, cast)) return true;
        return StructuralChildren(node).Any(child =>
            ConstantCanRetargetVillain(
                child, current, next, cast, discarded,
                threatChanges, damageChanges, modifierChanges,
                traitChanges, statusChanges, engagementChanges,
                formsMayChange, traceFirstPlayer));
    }

    private static bool HasLiveVillainRetargetingConstant(
        HashSet<int> discarded, Card current, Card next, AbilityAdmissionScope cast,
        IReadOnlyDictionary<int, long>? threatChanges = null,
        IReadOnlyDictionary<int, long>? damageChanges = null,
        IReadOnlyDictionary<(int Card, string Field), long>? modifierChanges = null,
        IReadOnlyDictionary<int, HashSet<string>>? traitChanges = null,
        HashSet<(int Card, string Status)>? statusChanges = null,
        IReadOnlyDictionary<int, int>? engagementChanges = null,
        ulong formsMayChange = 0, int traceFirstPlayer = -1) =>
        cast.World.Areas
            .Where(area => DeckTypes.IsInPlay(area.Type))
            .SelectMany(area => area.Cards)
            .ToList()
            .Where(card => !discarded.Contains(card.ObjectId))
            .Any(card =>
            {
                var placement = engagementChanges ?? new Dictionary<int, int>();
                var constantCast = TraceConstantCast(cast, card, placement);
                return cast.Context.Program.On(card.FaceId).Any(ability =>
                    ability.Trigger.Timing == AbilityType.Constant
                    && ConstantCanRetargetVillain(
                        ability.Effect, current, next, constantCast, discarded,
                        threatChanges ?? new Dictionary<int, long>(),
                        damageChanges ?? new Dictionary<int, long>(),
                        modifierChanges
                            ?? new Dictionary<(int Card, string Field), long>(),
                        traitChanges ?? new Dictionary<int, HashSet<string>>(),
                        statusChanges ?? [],
                        placement,
                        formsMayChange,
                        traceFirstPlayer < 0
                            ? cast.World.FirstPlayer
                            : traceFirstPlayer));
            });

    private static AbilityAdmissionScope TraceConstantCast(
        AbilityAdmissionScope cast, Card source,
        IReadOnlyDictionary<int, int> placement)
    {
        int controller = AbilityCardQueries.ControllerOf(cast.World, source);
        if (AbilityCardQueries.IsPlayerCard(cast.World.Facts, source)
            && DeckTypes.IsInPlay(source.Area.Type)
            && placement.TryGetValue(source.ObjectId, out int projectedPlayer))
        {
            controller = projectedPlayer;
        }
        int? projected = placement.TryGetValue(source.ObjectId, out int player)
            ? player : null;
        var bindings = new AbilityQueryContext(
            cast.World, source, new Occurrence(0, []), controller,
            source.Incarnation, null, null, null, []);
        var expressions = new AbilityExpressionContext(
            bindings, System.Collections.Immutable.ImmutableDictionary<string, long>.Empty,
            [], string.Empty, -1, false, projected);
        return new AbilityAdmissionScope(
            new AbilityAdmissionContext(
                cast.Context.Program, expressions, cast.Reachability, cast.Power),
            []);
    }


    private static bool ConditionalModifierCanDiffer(
        Card target, string field, Card current, Card next, AbilityAdmissionScope cast,
        HashSet<int> discarded,
        IReadOnlyDictionary<int, long> threatChanges,
        IReadOnlyDictionary<int, long> damageChanges,
        IReadOnlyDictionary<(int Card, string Field), long> modifierChanges,
        IReadOnlyDictionary<int, HashSet<string>> traitChanges,
        IReadOnlySet<(int Card, string Status)> statusChanges,
        IReadOnlyDictionary<int, int> engagementChanges,
        ulong formsMayChange,
        int traceFirstPlayer,
        int dependencyDepth)
    {
        if (dependencyDepth <= 0)
        {
            throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' reaches a cyclic conditional modifier "
                + "before a labelled-power continuation, which is not implemented");
        }
        return cast.World.Areas
                .Where(area => DeckTypes.IsInPlay(area.Type))
                .SelectMany(area => area.Cards)
                .Where(source => !discarded.Contains(source.ObjectId))
                .Any(source =>
                {
                    var constantCast = TraceConstantCast(
                        cast, source, engagementChanges);
                    return cast.Context.Program.On(source.FaceId).Any(ability =>
                        ability.Trigger.Timing == AbilityType.Constant
                        && ConstantFieldCanDiffer(
                            ability.Effect, source, target, field,
                            current, next, constantCast, discarded,
                            threatChanges, damageChanges, modifierChanges,
                            traitChanges, statusChanges, engagementChanges,
                            formsMayChange, traceFirstPlayer, dependencyDepth));
                });
    }

    private static bool ConstantFieldCanDiffer(
        AbilityEffect node, Card source, Card target, string field,
        Card current, Card next, AbilityAdmissionScope cast, HashSet<int> discarded,
        IReadOnlyDictionary<int, long> threatChanges,
        IReadOnlyDictionary<int, long> damageChanges,
        IReadOnlyDictionary<(int Card, string Field), long> modifierChanges,
        IReadOnlyDictionary<int, HashSet<string>> traitChanges,
        IReadOnlySet<(int Card, string Status)> statusChanges,
        IReadOnlyDictionary<int, int> engagementChanges,
        ulong formsMayChange,
        int traceFirstPlayer,
        int dependencyDepth)
    {
        if (!ContainsFieldGrant(node, source, target, field, cast))
        {
            return false;
        }
        if (node is AbilityEffect.Conditional conditional)
        {
            var test = conditional.Test;
            if (TryTraceConstantTest(
                test, current, next, cast, discarded,
                threatChanges, damageChanges, modifierChanges,
                traitChanges, statusChanges, engagementChanges,
                formsMayChange, traceFirstPlayer,
                out bool tracedTest))
            {
                bool liveTest = Test(test, cast);
                if (tracedTest != liveTest)
                {
                    return (liveTest ? conditional.Then : conditional.Else) is { } live
                            && ContainsFieldGrant(
                                live, source, target, field, cast)
                        || (tracedTest ? conditional.Then : conditional.Else) is { } traced
                            && ContainsFieldGrant(
                                traced, source, target, field, cast);
                }
                return (tracedTest ? conditional.Then : conditional.Else) is { } stable
                    && ConstantFieldCanDiffer(
                        stable, source, target, field,
                        current, next, cast, discarded,
                        threatChanges, damageChanges, modifierChanges,
                        traitChanges, statusChanges, engagementChanges,
                        formsMayChange, traceFirstPlayer, dependencyDepth);
            }
            if (TestCanChangeOnVillainAdvance(
                test, current, next, cast, discarded,
                threatChanges, damageChanges, modifierChanges,
                traitChanges, statusChanges, engagementChanges,
                formsMayChange, traceFirstPlayer, dependencyDepth - 1))
            {
                return StructuralChildren(node).Any(child =>
                    ContainsFieldGrant(child, source, target, field, cast));
            }
            return (Test(test, cast) ? conditional.Then : conditional.Else) is { } active
                && ConstantFieldCanDiffer(
                    active, source, target, field,
                    current, next, cast, discarded,
                    threatChanges, damageChanges, modifierChanges,
                    traitChanges, statusChanges, engagementChanges,
                    formsMayChange, traceFirstPlayer, dependencyDepth);
        }
        return StructuralChildren(node).Any(child => ConstantFieldCanDiffer(
            child, source, target, field, current, next, cast, discarded,
            threatChanges, damageChanges, modifierChanges,
            traitChanges, statusChanges, engagementChanges,
            formsMayChange, traceFirstPlayer, dependencyDepth));
    }

    private static bool ContainsFieldGrant(
        AbilityEffect node, Card source, Card target, string field, AbilityAdmissionScope cast)
    {
        if (node is AbilityEffect.GrantField { Until: null } grant
            && string.Equals(grant.Field, field, StringComparison.Ordinal)
            && ConstantSelectorAffects(
                grant.Cards, source, target, cast))
        {
            return true;
        }
        return StructuralChildren(node).Any(child =>
            ContainsFieldGrant(child, source, target, field, cast));
    }

    private static bool ConstantSelectorAffects(
        AbilityCardSelection selector, Card source, Card target, AbilityAdmissionScope cast) =>
        selector is AbilityCardSelection.Bound { Binding: AbilityCardBinding.This }
            ? source.ObjectId == target.ObjectId
            : Every(selector, cast).Any(card => card.ObjectId == target.ObjectId);

    private static bool TraceCardsInPlayMayDiffer(
        HashSet<int> discarded, AbilityAdmissionScope cast) => cast.World.Cards.Any(card =>
            DeckTypes.IsInPlay(card.Area.Type)
                ? discarded.Contains(card.ObjectId)
                : FacedownDrones.Kind(card, cast.World.Facts) == CardKind.Minion
                    && !discarded.Contains(card.ObjectId));


    private static bool TraceTitlePresenceMayDiffer(
        string title, HashSet<int> discarded, AbilityAdmissionScope cast) =>
        cast.World.Cards.Any(card => string.Equals(
                cast.World.Facts.Title(card.FaceId), title,
                StringComparison.Ordinal)
            && (DeckTypes.IsInPlay(card.Area.Type)
                ? discarded.Contains(card.ObjectId)
                : FacedownDrones.Kind(card, cast.World.Facts) == CardKind.Minion
                    && !discarded.Contains(card.ObjectId)));


    private static long PowerDamage(AbilityPowerState state, Card card) =>
        state.CardDamage.TryGetValue(card.ObjectId, out long damage)
            ? damage
            : card.Damage;

    private static long PowerThreat(AbilityPowerState state, Card scheme) =>
        TraceThreat(state.SchemeThreat, scheme);

    private static long TraceThreat(
        Dictionary<int, long> schemeThreat, Card scheme) =>
        schemeThreat.TryGetValue(scheme.ObjectId, out long threat)
            ? threat
            : scheme.Tokens.GetValueOrDefault("k_threat");

    private static bool PowerCrisis(AbilityPowerState state, AbilityAdmissionScope cast) =>
        cast.World.Areas
            .Where(area => DeckTypes.IsInPlay(area.Type))
            .SelectMany(area => area.Cards)
            .Where(card => !state.Discarded.Contains(card.ObjectId))
            .Any(card => TraceModified(
                card, "crisis", cast, state.Discarded, state.Modifiers) > 0);

    private static AbilityPowerState SetPowerThreat(
        AbilityPowerState state, Card scheme, long threat)
    {
        var values = new Dictionary<int, long>(state.SchemeThreat);
        long live = scheme.Tokens.GetValueOrDefault("k_threat");
        if (threat == live)
        {
            values.Remove(scheme.ObjectId);
        }
        else
        {
            values[scheme.ObjectId] = threat;
        }
        return state with { SchemeThreat = values };
    }

    private static bool PowerTough(
        AbilityPowerState state, Card card, AbilityAdmissionScope cast) =>
        state.CardTough.TryGetValue(card.ObjectId, out bool tough)
            ? tough
            : Statuses.Has(cast.World, card, Statuses.Tough);

    private static PowerReadiness PowerReady(
        Card card, AbilityPowerState state) =>
        state.CardReadiness.TryGetValue(card.ObjectId, out var readiness)
            ? readiness
            : card.Ready
                ? PowerReadiness.Ready
                : PowerReadiness.Exhausted;

    private static AbilityPowerState SetPowerReady(
        AbilityPowerState state, Card card, PowerReadiness readiness)
    {
        var cards = new Dictionary<int, PowerReadiness>(state.CardReadiness);
        var live = card.Ready
            ? PowerReadiness.Ready
            : PowerReadiness.Exhausted;
        if (readiness == live)
        {
            cards.Remove(card.ObjectId);
        }
        else
        {
            cards[card.ObjectId] = readiness;
        }
        return state with { CardReadiness = cards };
    }

    private static long PowerCardsAvailable(
        AbilityPowerState state, int player, AbilityAdmissionScope cast) =>
        state.PlayerCardsAvailable.TryGetValue(player, out long available)
            ? available
            : cast.World.Seats[player].Deck.Cards.Count
                + cast.World.AreaOf(
                    DeckType.DiscardPile, PlayArea.Of(player)).Cards.Count;

    private static AbilityPowerState SetPowerCardsAvailable(
        AbilityPowerState state, int player, long available, AbilityAdmissionScope cast)
    {
        var cards = new Dictionary<int, long>(state.PlayerCardsAvailable);
        long live = cast.World.Seats[player].Deck.Cards.Count
            + cast.World.AreaOf(
                DeckType.DiscardPile, PlayArea.Of(player)).Cards.Count;
        if (available == live)
        {
            cards.Remove(player);
        }
        else
        {
            cards[player] = available;
        }
        return state with { PlayerCardsAvailable = cards };
    }

    private static AbilityPowerState SetPowerDamage(
        AbilityPowerState state, Card card, long damage, Card first, AbilityAdmissionScope cast)
    {
        if (damage == PowerDamage(state, card))
        {
            return state;
        }
        var inventory = new Dictionary<int, long>(state.CardDamage);
        if (damage == card.Damage)
        {
            inventory.Remove(card.ObjectId);
        }
        else
        {
            inventory[card.ObjectId] = damage;
        }
        ulong forms = state.FormsMayChange;
        int firstPlayer = state.FirstPlayer;
        var tracedFirst = cast.World.Seats[firstPlayer].IdentityCard;
        if (card == tracedFirst
            && damage >= PowerHealth(state, tracedFirst, cast))
        {
            forms |= FirstPlayerRebinding;
            var changed = state with { CardDamage = inventory };
            for (int offset = 1; offset < cast.World.Seats.Count; offset++)
            {
                int candidate = (firstPlayer + offset) % cast.World.Seats.Count;
                var identity = cast.World.Seats[candidate].IdentityCard;
                if (PowerDamage(changed, identity) < PowerHealth(changed, identity, cast))
                {
                    firstPlayer = candidate;
                    break;
                }
            }
        }
        return state with
        {
            FormsMayChange = forms,
            FirstPlayer = firstPlayer,
            FirstPlayerDamage = card == first ? damage : state.FirstPlayerDamage,
            CardDamage = inventory,
        };
    }

    private static AbilityPowerState SetPowerTough(
        AbilityPowerState state, Card card, bool tough, Card first, AbilityAdmissionScope cast)
    {
        if (tough == PowerTough(state, card, cast))
        {
            return state;
        }
        var statuses = new Dictionary<int, bool>(state.CardTough);
        bool live = Statuses.Has(cast.World, card, Statuses.Tough);
        if (tough == live)
        {
            statuses.Remove(card.ObjectId);
        }
        else
        {
            statuses[card.ObjectId] = tough;
        }
        return state with
        {
            FirstPlayerTough = card == first ? tough : state.FirstPlayerTough,
            CardTough = statuses,
        };
    }

    private static PowerReachability MergePowerStates(
        PowerReachability left, PowerReachability right, AbilityAdmissionScope cast) =>
        MergePowerAlternatives([left, right]);

    private static PowerReachability MergePowerAlternatives(
        IEnumerable<PowerReachability> states)
    {
        var alternatives = new List<AbilityPowerState>();
        foreach (var projection in states)
        {
            if (projection is PowerReachability.Unsupported)
            {
                return projection;
            }
            foreach (var state in PowerPaths(projection))
            {
                if (!alternatives.Any(existing => SameConcretePowerState(existing, state)))
                {
                    alternatives.Add(state);
                }
            }
        }
        if (alternatives.Count == 0)
        {
            throw new InvalidOperationException("A power state must have a reachable path.");
        }
        return alternatives.Count == 1
            ? new PowerReachability.Known(alternatives[0])
            : new PowerReachability.Possible([.. alternatives]);
    }

    private static ImmutableArray<AbilityPowerState> PowerPaths(
        PowerReachability state) => state switch
        {
            PowerReachability.Known known => [known.Value],
            PowerReachability.Possible possible => possible.Alternatives,
            PowerReachability.Unsupported unsupported =>
                throw new RulesNotImplementedException(unsupported.Reason),
            _ => throw new InvalidOperationException("Unknown power projection outcome."),
        };

    private static ulong PowerForms(PowerReachability state) =>
        PowerPaths(state).Aggregate(0UL, (forms, path) => forms | path.FormsMayChange);

    private static bool SamePowerState(
        PowerReachability left, PowerReachability right)
    {
        var leftPaths = PowerPaths(left).ToList();
        var rightPaths = PowerPaths(right).ToList();
        return leftPaths.Count == rightPaths.Count
            && leftPaths.All(path => rightPaths.Any(other =>
                SameConcretePowerState(path, other)));
    }

    private static bool SameConcretePowerState(
        AbilityPowerState left, AbilityPowerState right) =>
        left.FormsMayChange == right.FormsMayChange
        && left.FirstPlayer == right.FirstPlayer
        && left.FirstPlayerDamage == right.FirstPlayerDamage
        && left.FirstPlayerTough == right.FirstPlayerTough
        && left.CardDamage.Count == right.CardDamage.Count
        && left.CardDamage.All(pair =>
            right.CardDamage.GetValueOrDefault(pair.Key) == pair.Value)
        && left.CardTough.Count == right.CardTough.Count
        && left.CardTough.All(pair =>
            right.CardTough.TryGetValue(pair.Key, out bool value)
                && value == pair.Value)
        && left.StatusChanges.SetEquals(right.StatusChanges)
        && left.StatusCounts.Count == right.StatusCounts.Count
        && left.StatusCounts.All(pair =>
            right.StatusCounts.GetValueOrDefault(pair.Key, -1) == pair.Value)
        && left.CardReadiness.Count == right.CardReadiness.Count
        && left.CardReadiness.All(pair =>
            right.CardReadiness.TryGetValue(pair.Key, out var value)
                && value == pair.Value)
        && left.Discarded.SetEquals(right.Discarded)
        && left.SchemeThreat.Count == right.SchemeThreat.Count
        && left.SchemeThreat.All(pair =>
            right.SchemeThreat.GetValueOrDefault(pair.Key) == pair.Value)
        && left.PlayerCardsAvailable.Count == right.PlayerCardsAvailable.Count
        && left.PlayerCardsAvailable.All(pair =>
            right.PlayerCardsAvailable.GetValueOrDefault(pair.Key) == pair.Value)
        && left.Modifiers.Count == right.Modifiers.Count
        && left.Modifiers.All(pair =>
            right.Modifiers.GetValueOrDefault(pair.Key) == pair.Value)
        && left.Traits.Count == right.Traits.Count
        && left.Traits.All(pair =>
            right.Traits.TryGetValue(pair.Key, out var traits)
                && pair.Value.SetEquals(traits))
        && left.Engagement.Count == right.Engagement.Count
        && left.Engagement.All(pair =>
            right.Engagement.GetValueOrDefault(pair.Key, -1) == pair.Value)
        && left.CurrentVillain == right.CurrentVillain
        && left.VillainStagesDrawn == right.VillainStagesDrawn
        && left.Finished == right.Finished;

    private static long SaturatingAdd(long left, long right) =>
        right > 0 && left > long.MaxValue - right ? long.MaxValue : left + right;

    private static long SaturatingSubtract(long left, long right)
    {
        if (right > 0 && left < long.MinValue + right)
        {
            return long.MinValue;
        }
        if (right < 0 && left > long.MaxValue + right)
        {
            return long.MaxValue;
        }
        return left - right;
    }

    private static ulong AllPlayerSeats(AbilityAdmissionScope cast) =>
        cast.World.Seats.Count >= 63
            ? FirstPlayerRebinding - 1
            : (1UL << cast.World.Seats.Count) - 1;

    internal static IEnumerable<AbilityEffect> EachPlayers(AbilityEffect node)
    {
        if (node.OperationName() == "eachPlayer")
        {
            yield return node;
            yield break;
        }
        IEnumerable<AbilityEffect> children = node.OperationName() switch
        {
            "seq" or "and" => OrderedEffects(node),
            "if" => ConditionalBranches((AbilityEffect.Conditional)node).Where(value => value is not null)
                .Select(value => value),
            "then" =>
            [
                EffectBody(node),
                EffectFollowing(node),
            ],
            "otherwise" =>
            [
                EffectBody(node),
                EffectFollowing(node),
            ],
            "forEach" => [EffectBody(node)],
            _ => [],
        };
        foreach (var found in children.SelectMany(EachPlayers))
        {
            yield return found;
        }
    }

}
