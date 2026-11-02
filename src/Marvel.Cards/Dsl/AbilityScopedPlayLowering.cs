using Marvel.Rules.Timing;
using static Marvel.Cards.Dsl.AbilityCostLowering;
using static Marvel.Cards.Dsl.AbilityEffectLowering;
using static Marvel.Cards.Dsl.AbilityLowering;
using static Marvel.Cards.Dsl.AbilitySelectorLowering;

namespace Marvel.Cards.Dsl;

// These synthetic capabilities model a hand-card play opened by a resolving ability.
internal static class AbilityScopedPlayLowering
{
    internal static bool Owns(string kind) => kind is "playSharedHeroTraitCard" or "playIgnoringResourceCost" or "playFromHand";

    internal static AbilityEffect Lower(string kind, AbilityValue value, AbilityLocation location) => kind switch
    {
        "playSharedHeroTraitCard" => FixedEffect(value, location, "game", AbilityFixedInstruction.PlaySharedHeroTraitCard),
        "playIgnoringResourceCost" => FixedEffect(value, location, "game", AbilityFixedInstruction.PlayIgnoringResourceCost),
        _ => PlayFromHand(value, location),
    };

    // Engine spelling: {"playFromHand": {"reduction": N, "optional": "true"}}.
    private static AbilityCardPlayEffect.PlayFromHand PlayFromHand(AbilityValue value, AbilityLocation location)
    {
        var fields = Fields(value, location, "reduction", "optional");
        return new(PositiveInteger(Required(fields, "reduction", location), location.Child("reduction")),
            Boolean(fields, "optional", location));
    }

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
        if (list.Any(node => node is AbilityCardPlayEffect.PlayFromHand) && !IsWholeFrame(effect))
            throw location.Error("playFromHand must be the whole effect or the whole body of a defeater's eachPlayer frame");
    }

    // The play is the last thing its ability does, so its continuation only closes the frame.
    private static bool IsWholeFrame(AbilityEffect effect) =>
        effect is AbilityCardPlayEffect.PlayFromHand
        || effect is AbilityEffect.EachPlayer { DefeaterOnly: true, Effect: AbilityCardPlayEffect.PlayFromHand };

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
