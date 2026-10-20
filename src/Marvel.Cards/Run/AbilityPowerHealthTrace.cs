using static Marvel.Cards.Run.AbilityPowerNumberTrace;
using static Marvel.Cards.Run.AbilityAdmission;
using static Marvel.Cards.Run.AbilityChoiceAnalysis;
using static Marvel.Cards.Run.AbilityPowerProjection;
using static Marvel.Cards.Run.AbilityPowerTrace;
using static Marvel.Cards.Run.AbilityInitiationPrimitives;
using static Marvel.Cards.Run.AbilityProjection;
using static Marvel.Cards.Run.AbilityRepeatedEffectAnalysis;
using static Marvel.Cards.Run.AbilityResolutionAdmission;
using static Marvel.Cards.Run.AbilityInitiation;
using static Marvel.Cards.Run.AbilityEffectStructure;
using Marvel.Cards.Dsl;
using PowerReachability = Marvel.Rules.Play.RuleProjection<Marvel.Cards.Run.AbilityPowerState>;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

using static Marvel.Cards.Run.AbilityPowerOutcomeTrace;
using static Marvel.Cards.Run.AbilityPowerStateMutation;
using static Marvel.Cards.Run.AbilityPowerHealthTrace;
namespace Marvel.Cards.Run;

internal static class AbilityPowerHealthTrace
{
    internal static AbilityPowerState ApplyPowerDamage(
        AbilityPowerState reachability, IReadOnlyList<Card> cards,
        long amount, Card first, AbilityAdmissionScope cast)
    {
        if (amount <= 0)
        {
            return reachability;
        }
        var state = reachability;
        foreach (var card in cards)
        {
            if (state.UnprojectedStageEntry is { } reason)
                throw new RulesNotImplementedException(reason);
            var damage = new Dictionary<int, long>(state.CardDamage);
            var discarded = new HashSet<int>(state.Discarded);
            long landed = AbilityDamageProtectionTrace.AfterForcedDamageReplacements(
                cast, card.ObjectId, amount, damage, discarded,
                state.CurrentVillain);
            state = state with
            {
                CardDamage = damage,
                Discarded = discarded,
            };
            var assignment = DamageAssignment.AfterReplacement(
                landed, landed > 0 && PowerTough(state, card, cast));
            if (assignment.Dealt <= 0)
            {
                continue;
            }
            if (assignment.SpendsTough)
            {
                state = SetPowerTough(state, card, false, first, cast);
                continue;
            }
            state = SetPowerDamage(
                state, card, SaturatingAdd(PowerDamage(state, card), assignment.Taken),
                first, cast);
            state = ResolvePowerCharacterDefeat(state, card, first, cast);
        }
        return state;
    }

    internal static AbilityPowerState ResolvePowerCharacterDefeat(
        AbilityPowerState state, Card damaged, Card first, AbilityAdmissionScope cast)
    {
        long health = PowerHealth(state, damaged, cast);
        if (PowerDamage(state, damaged) < health)
        {
            return state;
        }
        if (AbilityProjectedDefeatWindows.HasWork(state, damaged, cast, would: true))
        {
            throw new RulesNotImplementedException(
                $"character '{damaged.FaceId}' would be defeated before a "
                + "labelled-power continuation reads a step-6 interrupt, "
                + "which is not implemented");
        }
        if (AbilityProjectedDefeatWindows.CanProjectEncounterDeal(state, damaged, cast))
            state = state with { EncounterCardsChanged = true };
        else if (AbilityProjectedDefeatWindows.HasWork(state, damaged, cast))
        {
            throw new RulesNotImplementedException(
                $"character '{damaged.FaceId}' is defeated before a "
                + "labelled-power continuation reads a defeat-triggered ability, "
                + "which is not implemented");
        }
        if (damaged.ObjectId == state.CurrentVillain
            || AbilityPowerEncounterProjection.IsCurrent(damaged, cast, state))
        {
            return AbilityPowerVillainProjection.AdvancePowerVillain(state, damaged, first, cast);
        }
        if (EffectiveCards.Kind(damaged, cast.World.Facts)
            is not (CardKind.Minion or CardKind.Ally))
        {
            if (!cast.World.Seats.Any(seat => seat.IdentityCard == damaged))
            {
                return state;
            }
            int eliminatedPlayer = cast.World.Seats
                .Select((seat, player) => (seat, player))
                .Single(pair => pair.seat.IdentityCard == damaged)
                .player;
            var plan = PlanTracePlayerElimination(
                eliminatedPlayer, cast, state.Discarded, state.Engagement);
            var eliminated = new HashSet<int>(state.Discarded);
            var eliminatedEngagement = new Dictionary<int, int>(state.Engagement);
            var eliminatedStatusCounts = new Dictionary<(int Card, string Status), int>(
                state.StatusCounts);
            var eliminatedStatusChanges = new HashSet<(int Card, string Status)>(
                state.StatusChanges);
            var eliminatedTough = new Dictionary<int, bool>(state.CardTough);
            foreach (int relocated in plan.RelocatedCards)
            {
                eliminatedEngagement[relocated] = plan.NextPlayer!.Value;
            }
            foreach (int eliminatedCard in plan.Leaving)
            {
                eliminated.Add(eliminatedCard);
                eliminatedEngagement.Remove(eliminatedCard);
                eliminatedTough.Remove(eliminatedCard);
                TraceStatusesLeave(
                    eliminatedCard, cast,
                    eliminatedStatusCounts, eliminatedStatusChanges);
            }
            return state with
            {
                Discarded = eliminated,
                Engagement = eliminatedEngagement,
                CardTough = eliminatedTough,
                StatusCounts = eliminatedStatusCounts,
                StatusChanges = eliminatedStatusChanges,
            };
        }

        var leaving = PowerLeavingTree(damaged, cast);
        var discarded = new HashSet<int>(state.Discarded);
        var engagement = new Dictionary<int, int>(state.Engagement);
        var statusCounts = new Dictionary<(int Card, string Status), int>(
            state.StatusCounts);
        var statusChanges = new HashSet<(int Card, string Status)>(
            state.StatusChanges);
        foreach (int cardId in leaving)
        {
            discarded.Add(cardId);
            TraceStatusesLeave(
                cardId, cast, statusCounts, statusChanges);
            engagement.Remove(cardId);
        }
        return state with
        {
            Discarded = discarded,
            Engagement = engagement,
            StatusCounts = statusCounts,
            StatusChanges = statusChanges,
        };
    }

    internal static long PowerHealth(
        AbilityPowerState state, Card character, AbilityAdmissionScope cast)
        => SaturatingAdd(
            TraceHealth(
                character, state.Discarded, state.SchemeThreat, cast),
            state.Modifiers.GetValueOrDefault((character.ObjectId, "health")));

    internal static long TraceHealth(
        Card character, HashSet<int> discarded,
        Dictionary<int, long> schemeThreat, AbilityAdmissionScope cast)
    {
        long health = DamagePlacement.Health(cast.World, cast.World.Facts, character);
        var active = cast.World.Effects.Active()
            .Where(effect => effect.Source == EffectSource.ConstantAbility
                && string.Equals(effect.Kind, "health", StringComparison.Ordinal)
                && effect.Card is not null
                && effect.AppliesTo(cast.World, character))
            .ToList();
        var sources = active.Select(effect => effect.Card!.Value).ToHashSet();
        if (schemeThreat.Count > 0)
        {
            foreach (var source in cast.World.Areas
                .Where(area => DeckTypes.IsInPlay(area.Type))
                .SelectMany(area => area.Cards)
                .Where(card => AbilityProgramQueries.On(cast.Context.Program, card).Any(ability =>
                    ability.Trigger.Timing == AbilityType.Constant)))
            {
                sources.Add(source.ObjectId);
            }
        }

        foreach (int sourceId in sources)
        {
            long live = 0;
            foreach (var effect in active.Where(effect => effect.Card == sourceId))
            {
                live = SaturatingAdd(live, effect.Amount);
            }
            long traced = live;
            if (discarded.Contains(sourceId))
            {
                traced = 0;
            }
            else if (schemeThreat.Count > 0)
            {
                var source = cast.World.Cards[sourceId];
                var constantCast = cast.ForConstant(source);
                traced = 0;
                foreach (var ability in AbilityProgramQueries.On(cast.Context.Program, source).Where(ability =>
                    ability.Trigger.Timing == AbilityType.Constant))
                {
                    if (!TryTraceConstantHealth(
                        ability.Effect, character, schemeThreat,
                        constantCast, out long amount))
                    {
                        throw new RulesNotImplementedException(
                            $"character '{character.FaceId}' has a conditional health "
                            + "constant whose traced predicate is not implemented");
                    }
                    traced = SaturatingAdd(traced, amount);
                }
            }
            health = SaturatingAdd(
                SaturatingSubtract(health, live), traced);
        }
        return health;
    }

    internal static bool TryTraceConstantHealth(
        AbilityEffect effect, Card character, Dictionary<int, long> schemeThreat,
        AbilityAdmissionScope cast, out long amount)
    {
        if (effect is AbilityEffect.Sequence or AbilityEffect.Simultaneous)
            return TryTraceHealthChildren(effect, character, schemeThreat, cast, out amount);
        if (effect is AbilityEffect.Conditional conditional)
            return TryTraceConditionalHealth(
                conditional, character, schemeThreat, cast, out amount);
        if (effect is AbilityEffect.GrantField { Until: null, Field: "health" } grant
            && (grant.EachCard
                ? Every(grant.Cards, cast).Any(card => card.ObjectId == character.ObjectId)
                : Find(grant.Cards, cast)?.ObjectId == character.ObjectId))
        {
            return TryPowerAmount(grant.Amount, schemeThreat, cast, out amount);
        }
        amount = 0;
        return true;
    }

    private static bool TryTraceHealthChildren(
        AbilityEffect effect, Card character, Dictionary<int, long> schemeThreat,
        AbilityAdmissionScope cast, out long amount)
    {
        amount = 0;
        foreach (var child in StructuralChildren(effect))
        {
            if (!TryTraceConstantHealth(child, character, schemeThreat, cast, out long childAmount))
                return false;
            amount = SaturatingAdd(amount, childAmount);
        }
        return true;
    }

    private static bool TryTraceConditionalHealth(
        AbilityEffect.Conditional conditional, Card character,
        Dictionary<int, long> schemeThreat, AbilityAdmissionScope cast, out long amount)
    {
        if (!TryPowerTest(conditional.Test, schemeThreat, cast, out bool branch))
        {
            amount = 0;
            return false;
        }
        if ((branch ? conditional.Then : conditional.Else) is not { } chosen)
        {
            amount = 0;
            return true;
        }
        return TryTraceConstantHealth(chosen, character, schemeThreat, cast, out amount);
    }

    internal static List<int> PowerLeavingTree(Card host, AbilityAdmissionScope cast)
    {
        var leaving = new List<int> { host.ObjectId };
        var pending = new Stack<Card>(cast.World.Areas
            .Where(area => area.Host == host.ObjectId)
            .SelectMany(area => area.Cards)
            .Reverse());
        var seen = new HashSet<int> { host.ObjectId };
        while (pending.TryPop(out var hosted))
        {
            if (!seen.Add(hosted.ObjectId))
            {
                throw new RulesNotImplementedException(
                    $"attachment {hosted.ObjectId} forms a hosting cycle");
            }
            if (StateFields.Modified(
                    cast.World, hosted, "permanent",
                    cast.World.Facts, cast.World.Players) > 0)
            {
                throw new RulesNotImplementedException(
                    $"permanent attachment {hosted.ObjectId} lost host "
                    + $"{host.ObjectId}, and rr:permanent.5 is not implemented");
            }
            leaving.Add(hosted.ObjectId);
            foreach (var child in cast.World.Areas
                .Where(area => area.Host == hosted.ObjectId)
                .SelectMany(area => area.Cards)
                .Reverse())
            {
                pending.Push(child);
            }
        }
        return leaving;
    }

}
