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

    internal DamageProjection PreviewDamageReplacement(
        World world, Card target, Card source, long amount)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(source);

        var occurrence = new Occurrence(
            0, [Steps.DamageWouldBeDealt], Subject: target.ObjectId, Player: target.Owner);
        foreach (var candidate in AbilityWindowAdmission.Waiting(
                     program, world, occurrence, WindowKind.Interrupt, resourceAbilities)
                     .Where(candidate => candidate.Ability.Trigger.Timing == AbilityType.ForcedInterrupt))
        {
            Card card = candidate.Card;
            CompiledCardAbility ability = candidate.Ability;
            string name = world.Facts.Title(card.FaceId);
            if (AbilityInitiation.ContainsEffect(ability.Effect, "soakDamage"))
            {
                long threshold = AbilityInitiation.SoakDiscardThreshold(ability.Effect);
                bool discarded = threshold > 0
                    && AbilityInitiation.SaturatingSum(card.Damage, [amount]) >= threshold;
                return new DamageProjection(
                    0,
                    $"{name} takes the damage instead"
                    + (discarded ? " and will be discarded" : string.Empty));
            }

            return new DamageProjection(
                new RuleProjection<long>.Unsupported(
                    $"{name} has a forced interrupt whose damage projection is not implemented"));
        }

        return new DamageProjection(amount);
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
                    + (AbilityInitiation.ContainsEffect(ability.Effect, "discard")
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
        || AbilityInitiation.MutationChildren(node).Any(HealsAllDamage);
}
