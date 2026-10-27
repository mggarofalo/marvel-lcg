using Marvel.Cards.Dsl;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Cards.Run;

// Read-only projections for forced replacement windows. They ask the same
// admission owner as live window discovery and interpret its authored candidate
// directly, without an execution frame.
internal sealed class AbilityDamageProjection
{
    private readonly AbilityProgram program;
    private readonly IResourceCardAbilities resourceAbilities;

    internal AbilityDamageProjection(
        AbilityProgram program, IResourceCardAbilities resourceAbilities)
    {
        ArgumentNullException.ThrowIfNull(program);
        this.program = program;
        this.resourceAbilities = resourceAbilities;
    }

    internal AbilityQueryProjection? Projection { get; init; }

    internal DamageProjection PreviewDamageReplacement(
        World world, Card target, Card source, long amount)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(source);

        var occurrence = new Occurrence(
            0, [Steps.DamageWouldBeDealt], Subject: target.ObjectId, Player: target.Owner);
        var candidates = AbilityWindowAdmission.Waiting(
                program, world, occurrence, WindowKind.Interrupt, resourceAbilities, Projection)
            .Where(candidate => candidate.Ability.Trigger.Timing == AbilityType.ForcedInterrupt)
            .ToList();
        DamageProjection? native = GreenGoblinDamageReplacement.Preview(world, target, amount);
        if (native is not null && candidates.Count > 0)
            return new(new RuleProjection<long>.Unsupported(
                "Multiple forced damage replacements may resolve in a chosen order"));
        foreach (var candidate in candidates)
        {
            Card card = candidate.Card;
            CompiledCardAbility ability = candidate.Ability;
            string name = world.Facts.Title(card.FaceId);
            if (AbilityDamageProtectionTrace.ContainsEffect(ability.Effect, "soakDamage"))
            {
                long threshold = AbilityDamageProtectionTrace.SoakDiscardThreshold(ability.Effect);
                bool discarded = threshold > 0
                    && AbilityRepeatedStatusTrace.SaturatingSum((Projection?.Quantities(card) ?? card.InstanceState).Damage, [amount]) >= threshold;
                return new DamageProjection(
                    0,
                    $"{name} takes the damage instead"
                    + (discarded ? " and will be discarded" : string.Empty));
            }

            return new DamageProjection(
                new RuleProjection<long>.Unsupported(
                    $"{name} has a forced interrupt whose damage projection is not implemented"));
        }

        return native ?? new DamageProjection(amount);
    }

    internal DefeatProjection? PreviewDefeatReplacement(
        World world, Card target, long maximumHealth)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(target);

        var occurrence = new Occurrence(
            0, [Steps.CardWouldBeDefeated], Subject: target.ObjectId, Player: target.Owner);
        foreach (var candidate in AbilityWindowAdmission.Waiting(
                     program, world, occurrence, WindowKind.Interrupt, resourceAbilities)
                     .Where(candidate => candidate.Ability.Trigger.Timing == AbilityType.ForcedInterrupt))
        {
            Card card = candidate.Card;
            CompiledCardAbility ability = candidate.Ability;
            string name = world.Facts.Title(card.FaceId);
            if (HealsAllDamage(ability.Effect))
            {
                return new DefeatProjection(
                    maximumHealth,
                    $"{name} heals all damage instead"
                    + (AbilityDamageProtectionTrace.ContainsEffect(ability.Effect, "discard")
                        ? " and will be discarded"
                        : string.Empty));
            }
            return new DefeatProjection(
                null, $"{name} has a forced interrupt before defeat");
        }
        return null;
    }

    private static bool HealsAllDamage(AbilityEffect node) =>
        node is AbilityEffect.Heal
        {
            Amount: AbilityNumber.CardValue { Property: AbilityCardNumberProperty.Damage },
        }
        || AbilityRepeatedStatusTrace.MutationChildren(node).Any(HealsAllDamage);
}
