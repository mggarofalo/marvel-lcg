using Marvel.Cards.Dsl;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using static Marvel.Cards.Run.AbilityAdmission;
using static Marvel.Cards.Run.AbilityChoiceAnalysis;
using static Marvel.Cards.Run.AbilityEffectStructure;
using static Marvel.Cards.Run.AbilityInitiation;
using static Marvel.Cards.Run.AbilityInitiationPrimitives;
using static Marvel.Cards.Run.AbilityPowerHealthTrace;
using static Marvel.Cards.Run.AbilityPowerOutcomeTrace;
using static Marvel.Cards.Run.AbilityPowerProjection;
using static Marvel.Cards.Run.AbilityPowerTrace;
using static Marvel.Cards.Run.AbilityProjection;
using static Marvel.Cards.Run.AbilityResolutionAdmission;

namespace Marvel.Cards.Run;

internal static class AbilityPowerStateMutation
{
    internal static AbilityPowerState ApplyPowerLeafState(
        AbilityEffect node, AbilityAdmissionScope cast, bool bindingMayChange,
        AbilityPowerState state, long multiplier = 1)
    {
        RefuseMutableAmountAfterDamage(node, cast, state);
        return node.OperationName() switch
        {
            "exhaust" or "ready" => Readiness(node, cast, state),
            "grantUntil" => Grant(node, cast, state),
            "putIntoPlay" => Enter(node, cast, state),
            "draw" => Draw(node, cast, state),
            "discard" => Discard(node, cast, state),
            "removeThreat" => RemoveThreat(node, cast, state, multiplier),
            _ => Targeted(node, cast, bindingMayChange, state, multiplier),
        };
    }

    private static void RefuseMutableAmountAfterDamage(
        AbilityEffect node, AbilityAdmissionScope cast, AbilityPowerState state)
    {
        if (EffectAmount(node) is not { } amount
            || state.CardDamage.Count == 0 && state.SchemeThreat.Count == 0
            || !AmountMayChange(amount)) return;
        throw new RulesNotImplementedException(
            $"'{cast.Source.FaceId}' reads a mutable power amount after damage changed");
    }

    private static AbilityPowerState Readiness(
        AbilityEffect node, AbilityAdmissionScope cast, AbilityPowerState state)
    {
        var value = node.OperationName() == "exhaust"
            ? PowerReadiness.Exhausted : PowerReadiness.Ready;
        foreach (var card in PowerEvery(
            EffectOf<AbilityEffect.CardAction>(node, cast).Selection, cast, state))
        {
            state = SetPowerReady(state, card, value);
        }
        return state;
    }

    private static AbilityPowerState Grant(
        AbilityEffect node, AbilityAdmissionScope cast, AbilityPowerState state)
    {
        var target = PowerFind(GrantSelectionOf(node, cast), cast, state);
        if (target is null) return state;
        if (EffectOf<AbilityEffect>(node, cast) is AbilityEffect.GrantTrait trait)
        {
            var traits = state.Traits.ToDictionary(
                pair => pair.Key,
                pair => new HashSet<string>(pair.Value, StringComparer.Ordinal));
            if (!traits.TryGetValue(target.ObjectId, out var values))
            {
                values = new HashSet<string>(StringComparer.Ordinal);
                traits[target.ObjectId] = values;
            }
            values.Add(trait.Trait);
            return state with { Traits = traits };
        }
        var grant = EffectOf<AbilityEffect.GrantField>(node, cast);
        var modifiers = new Dictionary<(int Card, string Field), long>(state.Modifiers);
        var key = (target.ObjectId, grant.Field);
        long changed = SaturatingAdd(
            modifiers.GetValueOrDefault(key), Amount(grant.Amount, cast));
        if (changed == 0) modifiers.Remove(key);
        else modifiers[key] = changed;
        return state with { Modifiers = modifiers };
    }

    private static AbilityPowerState Enter(
        AbilityEffect node, AbilityAdmissionScope cast, AbilityPowerState state)
    {
        var entry = EffectOf<AbilityEffect.PutIntoPlay>(node, cast);
        var card = Find(entry.Card, cast);
        if (card is null) return state;
        if (AbilityProgramQueries.On(cast.Context.Program, card).Any(ability =>
                ability.Trigger.Timing == AbilityType.Constant))
        {
            throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' puts '{card.FaceId}' into play before "
                + "a labelled-power continuation reads its constant abilities, "
                + "which is not implemented");
        }
        var discarded = new HashSet<int>(state.Discarded);
        if (!discarded.Remove(card.ObjectId)) return state;
        var engagement = new Dictionary<int, int>(state.Engagement);
        if (!entry.PrintedDestination) engagement[card.ObjectId] = Resolver(cast);
        state = state with { Discarded = discarded, Engagement = engagement };
        if (StateFields.Modified(
                cast.World, card, "toughness", cast.World.Facts,
                cast.World.Players) <= 0) return state;
        var first = cast.World.Seats[cast.World.FirstPlayer].IdentityCard;
        return SetPowerTough(state, card, true, first, cast);
    }

    private static AbilityPowerState Draw(
        AbilityEffect node, AbilityAdmissionScope cast, AbilityPowerState state)
    {
        var draw = EffectOf<AbilityEffect.Draw>(node, cast);
        foreach (int player in Seats(draw.Players, cast))
        {
            long available = PowerCardsAvailable(state, player, cast);
            state = SetPowerCardsAvailable(
                state, player, Math.Max(0, available - draw.Count), cast);
        }
        return state;
    }

    private static AbilityPowerState Discard(
        AbilityEffect node, AbilityAdmissionScope cast, AbilityPowerState state)
    {
        var card = PowerFind(
            EffectOf<AbilityEffect.CardAction>(node, cast).Selection, cast, state);
        if (card is null) return state;
        var discarded = new HashSet<int>(state.Discarded);
        var engagement = new Dictionary<int, int>(state.Engagement);
        var counts = new Dictionary<(int Card, string Status), int>(state.StatusCounts);
        var changes = new HashSet<(int Card, string Status)>(state.StatusChanges);
        foreach (int leaving in PowerLeavingTree(card, cast))
        {
            discarded.Add(leaving);
            TraceStatusesLeave(leaving, cast, counts, changes);
            engagement.Remove(leaving);
        }
        return state with
        {
            Discarded = discarded, Engagement = engagement,
            StatusCounts = counts, StatusChanges = changes,
        };
    }

    private static AbilityPowerState RemoveThreat(
        AbilityEffect node, AbilityAdmissionScope cast,
        AbilityPowerState state, long multiplier)
    {
        long removed = SaturatingMultiply(
            Amount(EffectOf<AbilityEffect.RemoveThreat>(node, cast).Amount, cast),
            multiplier);
        foreach (var scheme in PowerEvery(
            EffectOf<AbilityEffect.RemoveThreat>(node, cast).Schemes, cast, state))
        {
            if (!CanRemoveThreat(node, cast, state, scheme)) continue;
            long current = PowerThreat(state, scheme);
            long changed = Math.Max(0, current - removed);
            state = SetPowerThreat(state, scheme, changed);
            if (current > 0 && changed == 0
                && scheme.Area.Type == DeckType.SideSchemesArea)
            {
                state = DefeatSideScheme(state, scheme, cast);
            }
        }
        return state;
    }

    private static bool CanRemoveThreat(
        AbilityEffect node, AbilityAdmissionScope cast,
        AbilityPowerState state, Card scheme) =>
        AbilityProgramQueries.CanRemoveThreat(
            cast.World, cast.Context.Program, scheme,
            OverriddenThreatRemovalSource(node, cast))
        && (IgnoresCrisis(node, cast)
            || scheme.Area.Type != DeckType.MainSchemesArea
            || !IsPlayerCard(cast)
            || !PowerCrisis(state, cast));

    private static AbilityPowerState DefeatSideScheme(
        AbilityPowerState state, Card scheme, AbilityAdmissionScope cast)
    {
        if (PowerDefeatHasTriggeredWork(state, scheme, cast))
        {
            throw new RulesNotImplementedException(
                $"side scheme '{scheme.FaceId}' is defeated before a "
                + "labelled-power continuation reads a defeat-triggered ability, "
                + "which is not implemented");
        }
        var discarded = new HashSet<int>(state.Discarded);
        var engagement = new Dictionary<int, int>(state.Engagement);
        foreach (int leaving in PowerLeavingTree(scheme, cast))
        {
            discarded.Add(leaving);
            engagement.Remove(leaving);
        }
        return state with { Discarded = discarded, Engagement = engagement };
    }

    private static AbilityPowerState Targeted(
        AbilityEffect node, AbilityAdmissionScope cast, bool bindingMayChange,
        AbilityPowerState state, long multiplier)
    {
        var targets = TargetSelection(node, cast);
        if (targets is null) return state;
        var cards = TargetCards(node, targets, cast, state);
        if (cards.Count == 0)
        {
            return bindingMayChange && BindingCanChange(targets)
                ? state with
                {
                    FormsMayChange = state.FormsMayChange | FirstPlayerRebinding,
                }
                : state;
        }
        return node.OperationName() switch
        {
            "heal" => Heal(node, cast, state, cards),
            "giveStatus" => GiveStatus(node, cast, state, cards),
            "moveDamage" or "moveAttackDamage" =>
                MoveDamage(node, cast, state, cards),
            _ => DealDamage(node, cast, state, cards, multiplier),
        };
    }

    private static AbilityCardSelection? TargetSelection(
        AbilityEffect node, AbilityAdmissionScope cast) => node.OperationName() switch
        {
            "dealDamage" or "dealAttackDamage" or "indirectDamage" =>
                DamageSelectionOf(node, cast),
            "moveDamage" or "moveAttackDamage" =>
                EffectOf<AbilityEffect.MoveDamage>(node, cast).To,
            "replaceThreatWithDamage" =>
                EffectOf<AbilityEffect.CardAction>(node, cast).Selection,
            "heal" => EffectOf<AbilityEffect.Heal>(node, cast).Card,
            "giveStatus" => EffectOf<AbilityEffect.GiveStatus>(node, cast).Cards,
            _ => null,
        };

    private static List<Card> TargetCards(
        AbilityEffect node, AbilityCardSelection targets,
        AbilityAdmissionScope cast, AbilityPowerState state) =>
        node.OperationName() switch
        {
            "dealDamage" or "dealAttackDamage" =>
                [.. PowerEvery(targets, cast, state).Where(card =>
                    CanTakeDamageInTrace(cast, card, state.Discarded))],
            "moveDamage" or "moveAttackDamage" =>
                PowerFind(targets, cast, state) is { } destination
                    && CanTakeDamageInTrace(cast, destination, state.Discarded)
                        ? [destination] : [],
            _ => PowerEvery(targets, cast, state),
        };

    private static AbilityPowerState Heal(
        AbilityEffect node, AbilityAdmissionScope cast,
        AbilityPowerState state, IEnumerable<Card> cards)
    {
        long amount = Amount(EffectOf<AbilityEffect.Heal>(node, cast).Amount, cast);
        var first = cast.World.Seats[cast.World.FirstPlayer].IdentityCard;
        foreach (var card in cards)
        {
            state = SetPowerDamage(
                state, card, Math.Max(0, PowerDamage(state, card) - amount),
                first, cast);
        }
        return state;
    }

    private static AbilityPowerState GiveStatus(
        AbilityEffect node, AbilityAdmissionScope cast,
        AbilityPowerState state, IReadOnlyList<Card> cards)
    {
        string status = EffectOf<AbilityEffect.GiveStatus>(node, cast).Status;
        if (status == Statuses.Tough) return GiveTough(state, cards, cast);
        var changes = new HashSet<(int Card, string Status)>(state.StatusChanges);
        var counts = new Dictionary<(int Card, string Status), int>(state.StatusCounts);
        var discarded = new HashSet<int>(state.Discarded);
        var engagement = new Dictionary<int, int>(state.Engagement);
        foreach (var card in cards)
        {
            GiveVulnerableStatus(
                card, status, cast, state.Modifiers,
                discarded, engagement, counts, changes);
        }
        return state with
        {
            StatusChanges = changes, StatusCounts = counts,
            Discarded = discarded, Engagement = engagement,
        };
    }

    private static AbilityPowerState GiveTough(
        AbilityPowerState state, IEnumerable<Card> cards, AbilityAdmissionScope cast)
    {
        var first = cast.World.Seats[cast.World.FirstPlayer].IdentityCard;
        foreach (var card in cards)
        {
            state = SetPowerTough(state, card, true, first, cast);
        }
        return state;
    }

    private static void GiveVulnerableStatus(
        Card card, string status, AbilityAdmissionScope cast,
        Dictionary<(int Card, string Field), long> modifiers,
        HashSet<int> discarded, Dictionary<int, int> engagement,
        Dictionary<(int Card, string Status), int> counts,
        HashSet<(int Card, string Status)> changes)
    {
        var key = (card.ObjectId, status);
        int current = counts.GetValueOrDefault(
            key, Statuses.Count(cast.World, card, status));
        int limit = TraceStatusLimit(card, status, cast, discarded, modifiers);
        if (current >= limit) return;
        int changed = current + 1;
        TraceSetStatusCount(card, status, changed, cast, counts, changes);
        if (!TraceStatusMakesVulnerable(
            card, status, changed, limit, cast, discarded, modifiers)) return;
        foreach (int leaving in PowerLeavingTree(card, cast))
        {
            discarded.Add(leaving);
            TraceStatusesLeave(leaving, cast, counts, changes);
            engagement.Remove(leaving);
        }
    }

    private static AbilityPowerState MoveDamage(
        AbilityEffect node, AbilityAdmissionScope cast,
        AbilityPowerState state, IReadOnlyList<Card> cards)
    {
        var movement = EffectOf<AbilityEffect.MoveDamage>(node, cast);
        var from = PowerFind(movement.From, cast, state);
        if (from is null) return state;
        long moved = Math.Min(
            PowerDamage(state, from), Amount(movement.Amount, cast));
        var first = cast.World.Seats[cast.World.FirstPlayer].IdentityCard;
        state = SetPowerDamage(
            state, from, PowerDamage(state, from) - moved, first, cast);
        return ApplyPowerDamage(state, cards, moved, first, cast);
    }

    private static AbilityPowerState DealDamage(
        AbilityEffect node, AbilityAdmissionScope cast, AbilityPowerState state,
        IReadOnlyList<Card> cards, long multiplier)
    {
        long authored = node.OperationName() switch
        {
            "indirectDamage" => Amount(
                EffectOf<AbilityEffect.IndirectDamage>(node, cast).Amount, cast),
            "replaceThreatWithDamage" => cast.Occurrence.Threat?.Remaining ?? 0,
            _ => Amount(DamageAmountOf(node, cast), cast),
        };
        long amount = SaturatingMultiply(authored, multiplier);
        var first = cast.World.Seats[cast.World.FirstPlayer].IdentityCard;
        return ApplyPowerDamage(state, cards, amount, first, cast);
    }
}
