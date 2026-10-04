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
        if (cost is AbilityCost.Sequence sequence)
        {
            string[] parts = [.. sequence.Costs.Select(step => Summary(world, source, player, step))
                .Where(part => !string.IsNullOrWhiteSpace(part)).Select(part => part!)];
            return parts.Length == 0 ? null : string.Join("; ", parts);
        }
        // Resource amounts and canonical symbols are already offered in
        // CostOptions. This contract supplies the other mandatory commitments.
        if (cost is AbilityCost.Spend or AbilityCost.SpendEnergy) return null;
        return Bound(world, source, player, cost) ?? Chosen(cost);
    }

    private static string? Bound(World world, Card source, int player, AbilityCost cost) => cost switch
    {
        AbilityCost.Exhaust exhaust => $"Exhaust {Name(world, source, player, exhaust.Card)}",
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
        AbilityCost.DiscardFromHand discard => $"Discard {Range(discard.Range)} cards from your hand",
        AbilityCost.ExhaustChosen exhaust => $"Exhaust {Range(exhaust.Range)} {Relation(exhaust.From)}",
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
        AbilityCardQuery.HeroesAndAllies => "heroes or allies",
        AbilityCardQuery.CharactersYouControl => "characters you control",
        AbilityCardQuery.AlliesYouControl => "allies you control",
        _ => throw new RulesNotImplementedException($"Cost relation description for {relation}"),
    };

    private static string Counter(string counter) => counter == "allPurpose" ? "all-purpose" : counter;
    private static string Plural(long count) => count == 1 ? string.Empty : "s";
    private static string Number(long number) => number.ToString(CultureInfo.InvariantCulture);
}
