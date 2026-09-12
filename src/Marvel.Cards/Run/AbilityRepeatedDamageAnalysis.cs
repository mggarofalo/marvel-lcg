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

internal static class AbilityRepeatedDamageAnalysis
{
    private static readonly HashSet<string> CardMutationTransferOperations =
    [
        "giveStatus", "grantUntil", "discard", "putIntoPlay",
    ];

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
            AbilityEffect.PreventDamageFrom prohibition => cast.World.Facts.Kind(source.FaceId)
                    == prohibition.SourceKind
                && Rules.State.Traits.Has(
                    cast.World, source, prohibition.SourceTrait,
                    cast.World.Facts),
            AbilityEffect.PreventDamageWhile prohibition => TraceTest(
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

    internal static IReadOnlyList<IReadOnlyList<DamageTransfer>> DamageTraces(
        AbilityEffect node, AbilityAdmissionScope cast, RepeatedChange assumed, bool binding)
    {
        string operation = node.OperationName();
        if (operation == "forEach")
            return RepeatedDamageTraces(node, cast, assumed, binding);
        if (operation == "if")
            return ConditionalDamageTraces(node, cast, assumed, binding);
        if (operation == "choose")
            return ChoiceDamageTraces(node, cast, assumed, binding);
        return ChildDamageTraces(node, cast, assumed, binding);
    }

    private static IReadOnlyList<IReadOnlyList<DamageTransfer>> RepeatedDamageTraces(
        AbilityEffect node, AbilityAdmissionScope cast, RepeatedChange assumed,
        bool binding)
    {
        long count = ForEachCount(node, cast);
        EnsureStableRepeatedDamage(node, cast);
        if (count == 0) return [[]];
        var effect = EffectBody(node);
        var iteration = DamageTraces(effect, cast, assumed, binding);
        if (!Choices(effect).Any()
            && effect.OperationName() is "dealDamage" or "removeThreat")
        {
            return ScaleDamageTraces(iteration, count);
        }
        return RepeatDamageTraces(iteration, count);
    }

    private static void EnsureStableRepeatedDamage(
        AbilityEffect node, AbilityAdmissionScope cast)
    {
        if (AmountMayChange(ForEachOf(node, cast).Count))
        {
            throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' has a for-each count that can change "
                + "between traced iterations");
        }
        if (ContainsMutableAmount(EffectBody(node), cast))
        {
            throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' has a for-each amount that can change "
                + "between traced iterations");
        }
    }

    private static IReadOnlyList<IReadOnlyList<DamageTransfer>> ScaleDamageTraces(
        IReadOnlyList<IReadOnlyList<DamageTransfer>> iteration, long count) =>
        [.. iteration.Select(trace => (IReadOnlyList<DamageTransfer>)
            [.. trace.Select(transfer => transfer with
            {
                Amount = SaturatingMultiply(transfer.Amount, count),
            })])];

    private static IReadOnlyList<IReadOnlyList<DamageTransfer>> RepeatDamageTraces(
        IReadOnlyList<IReadOnlyList<DamageTransfer>> iteration, long count)
    {
        IReadOnlyList<IReadOnlyList<DamageTransfer>> repeated = [[]];
        for (long frame = 0; frame < count; frame++)
        {
            repeated = [.. repeated.SelectMany(prefix => iteration.Select(suffix =>
                (IReadOnlyList<DamageTransfer>)[.. prefix, .. suffix]))];
        }
        return repeated;
    }

    private static List<IReadOnlyList<DamageTransfer>> ConditionalDamageTraces(
        AbilityEffect node, AbilityAdmissionScope cast, RepeatedChange assumed,
        bool binding)
    {
        var test = ConditionalOf(node, cast).Test;
        var branches = RepeatedTestCanChange(test, assumed)
                || binding && BindingCanChange(test)
            ? ConditionalBranches((AbilityEffect.Conditional)node)
                .Where(value => value is not null)
            : ActiveRepeatedBranch(node, test, cast);
        var branchTraces = branches.SelectMany(branch => DamageTraces(
            branch, cast, assumed, binding)).ToList();
        // A skipped branch is one empty trace so composition retains prior effects.
        return branchTraces.Count == 0 ? [[]] : branchTraces;
    }

    private static IEnumerable<AbilityEffect> ActiveRepeatedBranch(
        AbilityEffect node, AbilityCondition test, AbilityAdmissionScope cast) =>
        ConditionalBranch(node, Test(test, cast) ? "then" : "else") is { } active
            ? [active]
            : [];

    private static IReadOnlyList<IReadOnlyList<DamageTransfer>> ChoiceDamageTraces(
        AbilityEffect node, AbilityAdmissionScope cast, RepeatedChange assumed,
        bool binding) =>
        [.. MutationChildren(node).SelectMany(child =>
            DamageTraces(child, cast, assumed, binding))];

    private static IReadOnlyList<IReadOnlyList<DamageTransfer>> ChildDamageTraces(
        AbilityEffect node, AbilityAdmissionScope cast, RepeatedChange assumed,
        bool binding)
    {
        var own = DamageTransfers(node, cast);
        var children = MutationChildren(node).ToList();
        IReadOnlyList<IReadOnlyList<DamageTransfer>> traces = [own];
        foreach (var child in children)
        {
            var next = DamageTraces(child, cast, assumed, binding);
            traces =
            [
                .. traces.SelectMany(prefix => next.Select(suffix =>
                    (IReadOnlyList<DamageTransfer>)[.. prefix, .. suffix])),
            ];
        }
        return traces;
    }

    internal static IReadOnlyList<DamageTransfer> DamageTransfers(
        AbilityEffect node, AbilityAdmissionScope cast) =>
        node.OperationName() switch
        {
            "changeForm" => FormTransfer(node, cast),
            "dealDamage" or "indirectDamage" => DealtDamageTransfers(node, cast),
            "replaceThreatWithDamage" => ReplacedThreatTransfer(node, cast),
            "heal" => HealingTransfer(node, cast),
            "moveDamage" => MovedDamageTransfer(node, cast),
            var operation when CardMutationTransferOperations.Contains(operation) =>
                CardMutationTransfers(node, cast),
            "removeThreat" or "placeThreat" => ThreatTransfers(node, cast),
            _ => [],
        };

    private static IReadOnlyList<DamageTransfer> FormTransfer(
        AbilityEffect node, AbilityAdmissionScope cast)
    {
        var change = FormChangeOf(node, cast);
        return [new DamageTransfer(
            0, 0, 0, ChangesForm: Seat(change.Player, cast), Form: change.Form)];
    }

    private static IReadOnlyList<DamageTransfer> DealtDamageTransfers(
        AbilityEffect node, AbilityAdmissionScope cast) =>
        [.. TraceCards(DamageSelectionOf(node, cast), cast).Select(target =>
            new DamageTransfer(
                -1, target.Card.ObjectId, Amount(DamageAmountOf(node, cast), cast),
                DealsDamage: true, ToVillain: target.VillainSelector))];

    private static IReadOnlyList<DamageTransfer> ReplacedThreatTransfer(
        AbilityEffect node, AbilityAdmissionScope cast)
    {
        var candidate = TraceCardNamed(
            EffectOf<AbilityEffect.CardAction>(node, cast).Selection, cast);
        if (candidate is not { } replaced) return [];
        return [new DamageTransfer(
            -1, replaced.Card.ObjectId,
            cast.Occurrence.Threat?.Remaining ?? long.MaxValue,
            DealsDamage: true, ToVillain: replaced.VillainSelector)];
    }

    private static IReadOnlyList<DamageTransfer> HealingTransfer(
        AbilityEffect node, AbilityAdmissionScope cast)
    {
        var heal = EffectOf<AbilityEffect.Heal>(node, cast);
        if (TraceCardNamed(heal.Card, cast) is not { } healed) return [];
        return [new DamageTransfer(
            healed.Card.ObjectId, -1, Amount(heal.Amount, cast),
            FromVillain: healed.VillainSelector)];
    }

    private static IReadOnlyList<DamageTransfer> MovedDamageTransfer(
        AbilityEffect node, AbilityAdmissionScope cast)
    {
        var move = EffectOf<AbilityEffect.MoveDamage>(node, cast);
        if (TraceCardNamed(move.From, cast) is not { } from
            || TraceCardNamed(move.To, cast) is not { } to) return [];
        return [new DamageTransfer(
            from.Card.ObjectId, to.Card.ObjectId, Amount(move.Amount, cast),
            DealsDamage: true, FromVillain: from.VillainSelector,
            ToVillain: to.VillainSelector)];
    }

    private static IReadOnlyList<DamageTransfer> CardMutationTransfers(
        AbilityEffect node, AbilityAdmissionScope cast) =>
        node.OperationName() switch
        {
            "giveStatus" => StatusTransfers(node, cast),
            "grantUntil" => GrantTransfers(node, cast),
            "discard" => DiscardTransfers(node, cast),
            "putIntoPlay" => EnteringTransfer(node, cast),
            _ => [],
        };

    private static IReadOnlyList<DamageTransfer> StatusTransfers(
        AbilityEffect node, AbilityAdmissionScope cast)
    {
        var status = EffectOf<AbilityEffect.GiveStatus>(node, cast);
        return [.. TraceCards(status.Cards, cast).Select(target =>
            new DamageTransfer(
                0, target.Card.ObjectId, 0, GrantsStatus: status.Status,
                ToVillain: target.VillainSelector))];
    }

    private static IReadOnlyList<DamageTransfer> GrantTransfers(
        AbilityEffect node, AbilityAdmissionScope cast)
    {
        return EffectOf<AbilityEffect>(node, cast) switch
        {
            AbilityEffect.GrantTrait trait =>
                [.. TraceCards(trait.Cards, cast).Select(target =>
                    new DamageTransfer(
                        0, target.Card.ObjectId, 0, GrantsTrait: trait.Trait,
                        ToVillain: target.VillainSelector))],
            AbilityEffect.GrantField { Field: "health" } health =>
                HealthGrantTransfer(health, cast),
            AbilityEffect.GrantField field => FieldGrantTransfers(field, cast),
            _ => [],
        };
    }

    private static IReadOnlyList<DamageTransfer> FieldGrantTransfers(
        AbilityEffect.GrantField field, AbilityAdmissionScope cast) =>
        [.. TraceCards(field.Cards, cast).Select(target => new DamageTransfer(
            0, target.Card.ObjectId, Amount(field.Amount, cast),
            GrantsField: field.Field, ToVillain: target.VillainSelector))];

    private static IReadOnlyList<DamageTransfer> HealthGrantTransfer(
        AbilityEffect.GrantField health, AbilityAdmissionScope cast)
    {
        if (TraceCardNamed(health.Cards, cast) is not { } healthier) return [];
        return [new DamageTransfer(
            0, healthier.Card.ObjectId, Amount(health.Amount, cast),
            GrantsHealth: true, ToVillain: healthier.VillainSelector)];
    }

    private static IReadOnlyList<DamageTransfer> DiscardTransfers(
        AbilityEffect node, AbilityAdmissionScope cast) =>
        [.. TraceCards(
            EffectOf<AbilityEffect.CardAction>(node, cast).Selection, cast)
            .Select(target => new DamageTransfer(
                0, target.Card.ObjectId, 0, Discards: true,
                ToVillain: target.VillainSelector))];

    private static IReadOnlyList<DamageTransfer> EnteringTransfer(
        AbilityEffect node, AbilityAdmissionScope cast)
    {
        if (TraceCardNamed(
            EffectOf<AbilityEffect.PutIntoPlay>(node, cast).Card, cast)
            is not { } entering) return [];
        if (AbilityProgramQueries.On(cast.Context.Program, entering.Card).Any(ability =>
            ability.Trigger.Timing == AbilityType.Constant))
        {
            throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' puts '{entering.Card.FaceId}' into play "
                + "before a repeated continuation reads its constant abilities, "
                + "which is not implemented");
        }
        return [new DamageTransfer(0, entering.Card.ObjectId, 0, EntersPlay: true)];
    }

    private static IReadOnlyList<DamageTransfer> ThreatTransfers(
        AbilityEffect node, AbilityAdmissionScope cast)
    {
        bool removes = node.OperationName() == "removeThreat";
        var amount = removes
            ? EffectOf<AbilityEffect.RemoveThreat>(node, cast).Amount
            : EffectOf<AbilityEffect.PlaceThreat>(node, cast).Amount;
        return [.. Every(ThreatSelectionOf(node, cast), cast).Select(scheme =>
            new DamageTransfer(
                0, scheme.ObjectId, Amount(amount, cast),
                RemovesThreat: removes, PlacesThreat: !removes))];
    }

}
