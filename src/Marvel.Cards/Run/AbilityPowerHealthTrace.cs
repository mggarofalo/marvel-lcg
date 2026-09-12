using static Marvel.Cards.Run.AbilityAdmission;
using static Marvel.Cards.Run.AbilityChoiceAnalysis;
using static Marvel.Cards.Run.AbilityDelayedReachability;
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
            var damage = new Dictionary<int, long>(state.CardDamage);
            var discarded = new HashSet<int>(state.Discarded);
            long landed = AfterForcedDamageReplacements(
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
        if (PowerWouldBeDefeatedHasTriggeredWork(state, damaged, cast))
        {
            throw new RulesNotImplementedException(
                $"character '{damaged.FaceId}' would be defeated before a "
                + "labelled-power continuation reads a step-6 interrupt, "
                + "which is not implemented");
        }
        if (PowerDefeatHasTriggeredWork(state, damaged, cast))
        {
            throw new RulesNotImplementedException(
                $"character '{damaged.FaceId}' is defeated before a "
                + "labelled-power continuation reads a defeat-triggered ability, "
                + "which is not implemented");
        }
        if (damaged.ObjectId == state.CurrentVillain)
        {
            return AdvancePowerVillain(state, damaged, first, cast);
        }
        if (FacedownDrones.Kind(damaged, cast.World.Facts)
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

    internal static bool TryPowerTest(
        AbilityCondition test, Dictionary<int, long> schemeThreat,
        AbilityAdmissionScope cast, out bool result)
    {
        var operands = test switch
        {
            AbilityCondition.All all => all.Operands,
            AbilityCondition.Any any => any.Operands,
            _ => default,
        };
        if (!operands.IsDefault)
        {
            var values = new List<bool>();
            foreach (var child in operands)
            {
                if (!TryPowerTest(child, schemeThreat, cast, out bool value))
                {
                    result = false;
                    return false;
                }
                values.Add(value);
            }
            result = test is AbilityCondition.All ? values.All(value => value) : values.Any(value => value);
            return true;
        }
        if (test is AbilityCondition.Negated negated)
        {
            if (!TryPowerTest(negated.Operand, schemeThreat, cast, out bool value))
            {
                result = false;
                return false;
            }
            result = !value;
            return true;
        }
        if (test is AbilityCondition.AtLeast comparison
            && TryPowerAmount(comparison.Value, schemeThreat, cast, out long valueAt)
            && TryPowerAmount(comparison.Count, schemeThreat, cast, out long count))
        {
            result = valueAt >= count;
            return true;
        }
        if (!ReadsChangedThreat(test, schemeThreat, cast))
        {
            result = Test(test, cast);
            return true;
        }
        result = false;
        return false;
    }

    internal static bool TryPowerAmount(
        AbilityNumber number, Dictionary<int, long> schemeThreat,
        AbilityAdmissionScope cast, out long amount)
    {
        if (!ReadsChangedThreat(number, schemeThreat, cast))
        {
            amount = Amount(number, cast);
            return true;
        }
        if (number is AbilityNumber.CardValue { Property: AbilityCardNumberProperty.Threat } value
            && Find(value.Card, cast) is { } scheme)
        {
            amount = TraceThreat(schemeThreat, scheme);
            return true;
        }
        amount = 0;
        return false;
    }

    internal static bool ReadsChangedThreat(
        AbilityNumber number, Dictionary<int, long> schemeThreat, AbilityAdmissionScope cast) => number switch
        {
            AbilityNumber.CardValue { Property: AbilityCardNumberProperty.Threat } value =>
                Find(value.Card, cast) is { } scheme && schemeThreat.ContainsKey(scheme.ObjectId),
            AbilityNumber.Sum sum => sum.Operands.Any(value => ReadsChangedThreat(value, schemeThreat, cast)),
            AbilityNumber.Minimum minimum => minimum.Operands.Any(value => ReadsChangedThreat(value, schemeThreat, cast)),
            AbilityNumber.Product product => product.Operands.Any(value => ReadsChangedThreat(value, schemeThreat, cast)),
            AbilityNumber.Conditional conditional => ReadsChangedThreat(conditional.Test, schemeThreat, cast)
                || ReadsChangedThreat(conditional.Then, schemeThreat, cast) || ReadsChangedThreat(conditional.Else, schemeThreat, cast),
            _ => false,
        };

    internal static bool ReadsChangedThreat(
        AbilityCondition condition, Dictionary<int, long> schemeThreat, AbilityAdmissionScope cast) => condition switch
        {
            AbilityCondition.All all => all.Operands.Any(test => ReadsChangedThreat(test, schemeThreat, cast)),
            AbilityCondition.Any any => any.Operands.Any(test => ReadsChangedThreat(test, schemeThreat, cast)),
            AbilityCondition.Negated negated => ReadsChangedThreat(negated.Operand, schemeThreat, cast),
            AbilityCondition.AtLeast comparison => ReadsChangedThreat(comparison.Value, schemeThreat, cast)
                || ReadsChangedThreat(comparison.Count, schemeThreat, cast),
            _ => false,
        };

    internal static bool PowerWouldBeDefeatedHasTriggeredWork(
        AbilityPowerState state, Card defeated, AbilityAdmissionScope cast) =>
        PowerHasMatchingInterrupt(
            state, defeated, cast, Steps.CardWouldBeDefeated);

    internal static bool PowerDefeatHasTriggeredWork(
        AbilityPowerState state, Card defeated, AbilityAdmissionScope cast) =>
        PowerHasMatchingInterrupt(state, defeated, cast, Steps.CardDefeated);

    internal static bool PowerHasMatchingInterrupt(
        AbilityPowerState state, Card subject, AbilityAdmissionScope cast, string condition)
    {
        if (string.Equals(condition, Steps.CardDefeated, StringComparison.Ordinal)
            && cast.World.Facts.HasWhenDefeated(subject.FaceId)
            && !AbilityProgramQueries.On(cast.Context.Program, subject).Any(ability =>
                ability.Trigger.Timing == AbilityType.WhenDefeated))
        {
            // Runtime refuses printed defeat text with no authored behavior.
            // Eligibility must make the same refusal before a labelled cost.
            return true;
        }

        return AbilityWindowAdmission.WaitingCards(
                cast.Context.Program, cast.World,
                new Occurrence(
                    0, [condition], Subject: subject.ObjectId, Player: subject.Owner),
                WindowKind.Interrupt, cast.Context.ResourceAbilities)
            .Any(card => !state.Discarded.Contains(card));
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
