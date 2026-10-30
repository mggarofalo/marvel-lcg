using System.Globalization;
using Marvel.Cards.Dsl;
using Marvel.Rules.Play;
using Marvel.Rules.State;

namespace Marvel.Cards.Run;

/// <summary>Describes checked arrow costs independently of their post-arrow effects.</summary>
internal static class AbilityCostDescription
{
    internal static string? Summary(World world, Card source, int player, AbilityCost? cost)
    {
        if (cost is null) return null;
        if (cost is AbilityCost.InvocationTop) return AbilityInvocationDescription.Cost(world, player);
        if (cost is AbilityCost.Sequence sequence)
        {
            string[] parts = [.. sequence.Costs.Select(step => Summary(world, source, player, step))
                .Where(part => !string.IsNullOrWhiteSpace(part)).Select(part => part!)];
            return parts.Length == 0 ? null : string.Join("; ", parts);
        }
        // Resource amounts and canonical symbols are already offered in
        // CostOptions. This contract supplies the other mandatory commitments.
        if (cost is AbilityCost.Spend { DistinctTypes: true } spend)
            return $"Spend {Number(spend.Amount)} resources of different types";
        if (cost is AbilityCost.Spend or AbilityCost.SpendEnergy) return null;
        return Bound(world, source, player, cost) ?? Chosen(cost);
    }

    private static string? Bound(World world, Card source, int player, AbilityCost cost) => cost switch
    {
        AbilityCost.ExhaustTitled named => $"Exhaust {named.Title}",
        AbilityCost.ReturnTitledToHand named => $"Return {named.Title} to its owner's hand",
        AbilityCost.ReturnSourceToHand => $"Return {world.Facts.Title(source.FaceId)} to its owner's hand",
        AbilityCost.DiscardTop discard => $"Discard {discard.Count} from the top of "
            + (discard.From == AbilitySearchArea.YourDeck ? "your deck" : "the encounter deck"),
        AbilityCost.DiscardUntilMinionEngaged => "Discard encounter cards until a minion is discarded; put it into play engaged with you",
        AbilityCost.DealEncounterToYou => "Deal yourself a facedown encounter card",
        AbilityCost.Exhaust exhaust => $"Exhaust {Name(world, source, player, exhaust.Card)}",
        AbilityCost.ConsumeCampaignCard consume => $"Discard {Name(world, source, player, consume.Card)} and remove it from the campaign",
        AbilityCost.Discard discard => $"Discard {Name(world, source, player, discard.Card)}",
        AbilityCost.RemoveCounters counters =>
            $"Remove {Number(counters.Count)} {Counter(counters.Counter)} counter{Plural(counters.Count)} "
                + $"from {Name(world, source, player, counters.Card)}",
        AbilityCost.Heal heal => $"Heal {Number(heal.Amount)} damage from {Name(world, source, player, heal.Card)}",
        AbilityCost.Damage damage => Damage(world, source, player, damage),
        _ => null,
    };

    private static string Chosen(AbilityCost cost) => cost switch
    {
        AbilityCost.RandomDiscardFromHand discard => $"Discard {discard.Count} card(s) at random from your hand",
        AbilityCost.DiscardFromHand { Resource: { } resource } =>
            $"Discard a card with a printed {AbilityBasicUseDescription.Resource(resource)} resource from your hand",
        AbilityCost.DiscardFromHand discard => $"Discard {Range(discard.Range)} cards from your hand",
        AbilityCost.ChooseScheme => "Choose one scheme before paying for the thwart",
        AbilityCost.ChooseFromDiscard choice => $"Choose one {choice.Trait} card in your discard pile",
        AbilityCost.ExhaustChosen exhaust => $"Exhaust {Range(exhaust.Range)} {exhaust.Trait + (exhaust.Trait is null ? "" : " ")}{Relation(exhaust.From)}",
        _ => throw new RulesNotImplementedException($"Cost description for {cost.GetType().Name}"),
    };

    private static string Damage(World world, Card source, int player, AbilityCost.Damage damage) =>
        damage.MustTakeAll
            ? $"{Name(world, source, player, damage.Card)} must take all {Number(damage.Amount)} damage to pay"
            : $"Deal {Number(damage.Amount)} damage to {Name(world, source, player, damage.Card)} as a cost";

    private static string Name(World world, Card source, int player, AbilityCostCard card) =>
        world.Facts.Title((card == AbilityCostCard.Source ? source : world.Seats[player].IdentityCard).FaceId);

    private static string Range(AbilityCostRange range) => range switch
    {
        AbilityCostRange.Exact exact => exact.Count.ToString(CultureInfo.InvariantCulture),
        AbilityCostRange.UpTo upTo => $"1–{upTo.Count.ToString(CultureInfo.InvariantCulture)}",
        AbilityCostRange.Any => "one or more",
        _ => throw new RulesNotImplementedException($"Cost range description for {range.GetType().Name}"),
    };

    private static string Relation(AbilityCardQuery relation) => relation switch
    {
        AbilityCardQuery.UpgradesOnYourHero => "upgrades attached to your hero",
        AbilityCardQuery.HeroesAndAllies => "heroes or allies",
        AbilityCardQuery.CharactersYouControl => "characters you control",
        AbilityCardQuery.AlliesYouControl => "allies you control",
        _ => throw new RulesNotImplementedException($"Cost relation description for {relation}"),
    };

    private static string Counter(string counter) => counter == "allPurpose" ? "all-purpose" : counter;
    private static string Plural(long count) => count == 1 ? string.Empty : "s";
    private static string Number(long number) => number.ToString(CultureInfo.InvariantCulture);
}
