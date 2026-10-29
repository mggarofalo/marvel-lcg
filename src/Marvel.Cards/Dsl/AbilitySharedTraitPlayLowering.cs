using Marvel.Rules.Timing;

namespace Marvel.Cards.Dsl;

// This synthetic capability models the source's exhaust cost, not arbitrary costs.
internal static class AbilitySharedTraitPlayLowering
{
    internal static void Validate(CardAbility ability, AbilityCost? cost, AbilityEffect effect,
        AbilityLocation location, IEnumerable<AbilityEffect> nodes)
    {
        static bool IsPlay(AbilityEffect node) => node is AbilityEffect.Fixed
            { Instruction: AbilityFixedInstruction.PlaySharedHeroTraitCard };
        int plays = nodes.Count(IsPlay);
        if (plays == 0) return;
        if (plays != 1 || ability.Trigger.Timing != AbilityType.Action
            || cost is not AbilityCost.Exhaust { Card: AbilityCostCard.Source }
            || !(IsPlay(effect) || effect is AbilityEffect.Sequence sequence
                && sequence.Effects.Length > 0 && IsPlay(sequence.Effects[0])
                && sequence.Effects.Skip(1).All(node => !IsPlay(node))))
            throw location.Error("playSharedHeroTraitCard requires an Action exhausting its source and beginning with this play");
    }
}
