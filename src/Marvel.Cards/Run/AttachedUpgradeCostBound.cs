using Marvel.Cards.Dsl;
using Marvel.Rules.Play;
using Marvel.Rules.State;

namespace Marvel.Cards.Run;

/// <summary>Refuses unsupported resource composition before any attached-upgrade cost is paid.</summary>
internal static class AttachedUpgradeCostBound
{
    internal static void Validate(World world, Card source, int player, AbilityCost? cost,
        IReadOnlyList<int> paying)
    {
        if (cost is not (AbilityCost.ExhaustChosen { From: AbilityCardQuery.UpgradesOnYourHero }
            or AbilityCost.ExhaustChosen { CaptureBasicUsePower: true })) return;
        // rr:cost.5: costs "must be paid simultaneously". Resource abilities can
        // change the selected copy, so this bounded primitive admits no generation.
        if (paying.Count != 0)
            throw new RulesNotImplementedException(
                "attached-upgrade exhaustion combined with resource generation requires simultaneous payment");
        if (EffectiveCards.Kind(source, world.Facts) == CardKind.Event
            && (CardPayment.CostOf(world, world.Facts, world.Seats[player], source).Amount != 0
                || Resources.Required(world, source, world.Facts).Length != 0))
            throw new RulesNotImplementedException(
                "attached-upgrade exhaustion with a positive event price requires simultaneous payment");
    }
}
