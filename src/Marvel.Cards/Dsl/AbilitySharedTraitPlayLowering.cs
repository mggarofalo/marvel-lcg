using Marvel.Rules.Timing;

namespace Marvel.Cards.Dsl;

// These synthetic capabilities model a hand-card play that begins its Action.
internal static class AbilitySharedTraitPlayLowering
{
    internal static void Validate(CardAbility ability, AbilityCost? cost, AbilityEffect effect,
        AbilityLocation location, IEnumerable<AbilityEffect> nodes)
    {
        var list = nodes.ToList();
        bool action = ability.Trigger.Timing == AbilityType.Action;
        if (!Admitted(effect, list, AbilityFixedInstruction.PlaySharedHeroTraitCard,
                action && cost is AbilityCost.Exhaust { Card: AbilityCostCard.Source }))
            throw location.Error("playSharedHeroTraitCard requires an Action exhausting its source and beginning with this play");
        if (!Admitted(effect, list, AbilityFixedInstruction.PlayIgnoringResourceCost, action && cost is null))
            throw location.Error("playIgnoringResourceCost requires an Action without an arrow cost beginning with this play");
    }

    // Absent, or the single play that is the whole effect or begins its sequence.
    private static bool Admitted(AbilityEffect effect, IReadOnlyList<AbilityEffect> nodes,
        AbilityFixedInstruction instruction, bool abilityAccepted)
    {
        bool IsPlay(AbilityEffect node) => node is AbilityEffect.Fixed fixedNode && fixedNode.Instruction == instruction;
        int plays = nodes.Count(IsPlay);
        return plays == 0 || plays == 1 && abilityAccepted
            && (IsPlay(effect) || effect is AbilityEffect.Sequence { Effects: [var first, ..] } && IsPlay(first));
    }
}
