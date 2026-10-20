using System.Collections.Immutable;
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
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

using static Marvel.Cards.Run.AbilityRepeatedDamageTrace;
using static Marvel.Cards.Run.AbilityRepeatedDamageAnalysis;
using static Marvel.Cards.Run.AbilityRepeatedSelectorTrace;
using static Marvel.Cards.Run.AbilityRepeatedStatusTrace;
namespace Marvel.Cards.Run;

/// <summary>Traces damage prevention and replacement against projected card state.</summary>
internal static class AbilityDamageProtectionTrace
{
    internal static long AfterForcedDamageReplacements(
        AbilityAdmissionScope cast, int target, long amount, Dictionary<int, long> damage,
        HashSet<int> discarded, int currentVillain)
    {
        if (amount <= 0)
        {
            return amount;
        }

        foreach (var attachment in DamageReplacementAttachments(
            cast, target, discarded, currentVillain))
        {
            var replacement = AbilityProgramQueries.On(cast.Context.Program, attachment)
                .FirstOrDefault(ability =>
                    ability.Trigger.Timing == AbilityType.ForcedInterrupt
                    && ContainsEffect(ability.Effect, "soakDamage"));
            if (replacement is null) continue;
            long placed = SaturatingSum(
                damage.TryGetValue(attachment.ObjectId, out long traced)
                    ? traced
                    : attachment.Damage,
                [amount]);
            damage[attachment.ObjectId] = placed;
            long threshold = SoakDiscardThreshold(replacement.Effect);
            if (threshold > 0 && placed >= threshold)
            {
                discarded.Add(attachment.ObjectId);
            }
            return 0;
        }
        return amount;
    }

    private static IEnumerable<Card> DamageReplacementAttachments(
        AbilityAdmissionScope cast, int target, HashSet<int> discarded,
        int currentVillain)
    {
        int boardVillain = cast.World.TheCardIn(DeckType.VillainArea)?.ObjectId ?? -1;
        bool followsVillain = SameVillainTitle(cast, currentVillain, boardVillain);
        return cast.World.Areas
            .Where(area => area.Host == target
                || followsVillain && area.Host == boardVillain)
            .SelectMany(area => area.Cards)
            .Where(card => !discarded.Contains(card.ObjectId));
    }

    private static bool SameVillainTitle(
        AbilityAdmissionScope cast, int currentVillain, int boardVillain) =>
        currentVillain >= 0
        && boardVillain >= 0
        && string.Equals(
            cast.World.Facts.Title(cast.World.Cards[currentVillain].FaceId),
            cast.World.Facts.Title(cast.World.Cards[boardVillain].FaceId),
            StringComparison.Ordinal);

    internal static bool ContainsEffect(AbilityEffect node, string kind) =>
        node.OperationName() == kind || MutationChildren(node).Any(child =>
            ContainsEffect(child, kind));

    internal static long SoakDiscardThreshold(AbilityEffect node)
    {
        if (node.OperationName() == "if"
            && node is AbilityEffect.Conditional
            {
                Test: AbilityCondition.AtLeast
                { Value: AbilityNumber.CardValue { Property: AbilityCardNumberProperty.Damage } } comparison,
            }
            && ConditionalBranch(node, "then") is { } then
            && ContainsEffect(then, "discard"))
        {
            return comparison.Count is AbilityNumber.Constant constant
                ? constant.Value
                : throw new AbilityException("Soak discard threshold must be a constant number");
        }
        return MutationChildren(node)
            .Select(SoakDiscardThreshold)
            .FirstOrDefault(threshold => threshold > 0);
    }

    internal static bool CanTakeDamageInTrace(
        AbilityAdmissionScope cast, Card target, HashSet<int> discarded)
    {
        if (discarded.Contains(target.ObjectId))
        {
            return false;
        }
        if (AbilityTargetDamageProtection.Prevents(cast.World, cast.Context.Program, target,
            (owner, condition) => TraceTest(condition, cast.ForConstant(owner), discarded), discarded)) return false;
        foreach (var ability in AbilityProgramQueries.On(cast.Context.Program, target).Where(ability =>
            ability.Trigger.Timing == AbilityType.Constant))
        {
            var constant = cast.ForConstant(target);
            if (ProhibitsDamageInTrace(
                ability.Effect, constant, cast.Source, discarded))
            {
                return false;
            }
        }
        return true;
    }

    internal static bool ProhibitsDamageInTrace(
        AbilityEffect effect, AbilityAdmissionScope cast, Card source,
        HashSet<int> discarded) => effect switch
        {
            AbilityEffect.Sequence sequence => sequence.Effects.Any(step =>
                ProhibitsDamageInTrace(step, cast, source, discarded)),
            AbilityEffect.Simultaneous simultaneous => simultaneous.Effects.Any(step =>
                ProhibitsDamageInTrace(step, cast, source, discarded)),
            AbilityEffect.Conditional conditional =>
                (TraceTest(conditional.Test, cast, discarded) ? conditional.Then : conditional.Else)
                is { } branch && ProhibitsDamageInTrace(
                    branch, cast, source, discarded),
            AbilityEffect.PreventDamageFrom prohibition => EffectiveCards.Kind(source, cast.World.Facts)
                    == prohibition.SourceKind
                && Rules.State.Traits.Has(
                    cast.World, source, prohibition.SourceTrait,
                    cast.World.Facts),
            AbilityEffect.PreventDamageWhile { Target: null } prohibition => TraceTest(
                prohibition.Condition, cast, discarded),
            _ => false,
        };

    internal static bool TraceTest(
        AbilityCondition condition, AbilityAdmissionScope cast, HashSet<int> discarded) => condition switch
        {
            AbilityCondition.All all => all.Operands.All(test =>
                TraceTest(test, cast, discarded)),
            AbilityCondition.Any any => any.Operands.Any(test =>
                TraceTest(test, cast, discarded)),
            AbilityCondition.Negated negated => !TraceTest(negated.Operand, cast, discarded),
            AbilityCondition.Exists exists => Every(exists.Cards, cast).Any(card =>
                !discarded.Contains(card.ObjectId)),
            AbilityCondition.TitleInPlay title => cast.World.Areas
                .Where(area => DeckTypes.IsInPlay(area.Type))
                .SelectMany(area => area.Cards)
                .Any(card => !discarded.Contains(card.ObjectId)
                    && string.Equals(
                        cast.World.Facts.Title(card.FaceId),
                        title.Title, StringComparison.Ordinal)),
            _ => Test(condition, cast),
        };

}
