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

internal static class AbilityRepeatedDamageAnalysis
{
    private static readonly ImmutableArray<string> CardMutationTransferOperations =
    [
        "giveStatus", "grantUntil", "discard", "putIntoPlay",
    ];

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
            "moveActiveCounter" => [new DamageTransfer(-1, -1, 0)
                { MovesActiveCounter = true, ToSingular = ((AbilityEffect.CardAction)node).Selection }],
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
            FromVillain: healed.VillainSelector)
        { FromSingular = cast.World.EncounterTopology is null ? null : heal.Card }];
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
            ToVillain: to.VillainSelector)
        {
            FromSingular = cast.World.EncounterTopology is null ? null : move.From,
            ToSingular = cast.World.EncounterTopology is null ? null : move.To,
        }];
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
            GrantsHealth: true, ToVillain: healthier.VillainSelector)
        { ToSingular = cast.World.EncounterTopology is null ? null : health.Cards }];
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
