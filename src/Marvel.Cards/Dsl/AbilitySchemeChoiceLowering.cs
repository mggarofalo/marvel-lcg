using Marvel.Rules.State;
using Marvel.Rules.Play;

namespace Marvel.Cards.Dsl;

/// <summary>Bounds scheme choice costs to a single labelled Action thwart.</summary>
internal static class AbilitySchemeChoiceLowering
{
    internal static void Validate(CardAbility ability, AbilityCost? cost, AbilityEffect effect,
        AbilityLocation location)
    {
        if (!Contains(cost)) return;
        // Engine choice: compound costs and different power envelopes need their
        // own commitment and continuation proof before they are admitted.
        if (cost is not AbilityCost.ChooseScheme
            || ability.Trigger.Timing != AbilityType.Action
            || ability.Labels is not { Count: 1 } || ability.Labels[0] != BasicPowers.ThwartVerb
            || effect is not AbilityEffect.Power { Kind: AbilityPowerKind.Thwart,
                Target: AbilityCardSelection.Bound { Binding: AbilityCardBinding.Chosen }, Effect: var body }
            || !(SupportedBody(body) || InspectionBody(body)))
            throw location.Error("chooseScheme requires a standalone cost and a labelled Action thwart targeting chosen");
    }

    // Engine choice: these simple bodies have target-lifetime recovery proof.
    // The complete inspection body is checked separately below.
    private static bool SupportedBody(AbilityEffect body) => body switch
    {
        AbilityEffect.RemoveThreat { Schemes: AbilityCardSelection.Bound
            { Binding: AbilityCardBinding.Chosen }, Amount: AbilityNumber.Constant } => true,
        AbilityEffect.Draw { Players: AbilityPlayerSelection.OnePlayer
            { Player: AbilityPlayer.You } } => true,
        AbilityEffect.Sequence sequence => sequence.Effects.All(SupportedBody),
        _ => false,
    };

    // Engine choice: admit the complete 09003 inspection shape separately from
    // the simpler bodies. Other observations, results and tails need proof.
    private static bool InspectionBody(AbilityEffect body) => body is
        AbilityEffect.Sequence { Effects.Length: 2 } sequence
        && sequence.Effects[0] is AbilityEffect.Simultaneous { Effects.Length: 2 } pair
        && (InspectionPair(pair.Effects[0], pair.Effects[1])
            || InspectionPair(pair.Effects[1], pair.Effects[0]))
        && sequence.Effects[1] is AbilityEffect.RemoveThreat
        {
            Schemes: AbilityCardSelection.Bound { Binding: AbilityCardBinding.Chosen },
            Amount: AbilityNumber.Result { Name: "boostIconsLookedAt" },
        };

    private static bool InspectionPair(AbilityEffect removal, AbilityEffect inspection) =>
        removal is AbilityEffect.RemoveThreat
        {
            Schemes: AbilityCardSelection.Bound { Binding: AbilityCardBinding.Chosen },
            Amount: AbilityNumber.Constant { Value: 3 },
        }
        && inspection is AbilityEffect.InspectEncounterTop
        {
            Count: 1, Result: "boostIconsLookedAt",
        };

    private static bool Contains(AbilityCost? cost) => cost switch
    {
        AbilityCost.ChooseScheme => true,
        AbilityCost.Sequence sequence => sequence.Costs.Any(Contains),
        _ => false,
    };
}
