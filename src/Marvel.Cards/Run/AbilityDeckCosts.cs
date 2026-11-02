using Marvel.Cards.Dsl;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;

namespace Marvel.Cards.Run;

/// <summary>Prices and commits exact deck movement, and other costs that ask nothing, before the post-arrow effect.</summary>
internal static class AbilityDeckCosts
{
    internal static bool CanPayTogether(World world, int player, IReadOnlyList<AbilityCost> costs)
    {
        var steps = costs.SelectMany(Flatten).ToArray();
        int encounter = steps.OfType<AbilityCost.DealEncounterToYou>().Count();
        foreach (var group in steps.OfType<AbilityCost.DiscardTop>().GroupBy(cost => cost.From))
        {
            int count = group.Sum(cost => cost.Count);
            if (group.Key == AbilitySearchArea.EncounterDeck) encounter += count;
            else if (!CanDiscard(world, player, new(group.Key, count))) return false;
        }
        return encounter == 0 || encounter <= EncounterAvailable(world);
    }

    private static IEnumerable<AbilityCost> Flatten(AbilityCost cost) => cost is AbilityCost.Sequence sequence
        ? sequence.Costs.SelectMany(Flatten) : [cost];

    internal static int EncounterAvailable(World world)
    {
        int count = world.Areas.FirstOrDefault(area => area.Type == DeckType.EncounterDeck)?.Cards.Count ?? 0;
        return count > 0 ? count
            : world.Areas.FirstOrDefault(area => area.Type == DeckType.EncounterDiscardPile)?.Cards.Count ?? 0;
    }

    internal static bool CanDiscard(World world, int player, AbilityCost.DiscardTop cost) =>
        cost.From switch
        {
            AbilitySearchArea.EncounterDeck => EncounterAvailable(world) >= cost.Count,
            AbilitySearchArea.YourDeck => player >= 0 && world.Seats[player].Deck.Cards.Count >= cost.Count,
            _ => throw new InvalidOperationException("Unknown deck discard cost area"),
        };

    internal static IReadOnlyList<Card> Pay(World world, Card source, int player, AbilityCost cost,
        string trigger, List<GameEvent> events)
    {
        if (cost is AbilityCost.PlaceMainSchemeThreat threat)
        {
            AbilityThreatCost.Pay(world, source, threat, trigger, events);
            return [];
        }
        if (cost is AbilityCost.DiscardTop discard)
        {
            // rr:cost-arrow-icon: text before the arrow "must be paid and/or resolved in full".
            if (!CanDiscard(world, player, discard))
                throw new RulesNotImplementedException("deck cannot pay its complete discard cost");
            return discard.From == AbilitySearchArea.EncounterDeck
                ? EncounterDeck.DiscardTop(world, discard.Count, trigger, events)
                : PlayerDeck.DiscardTop(world, player, discard.Count, trigger, events);
        }
        if (cost is not AbilityCost.DealEncounterToYou)
            throw new InvalidOperationException("Unknown deck cost instruction");
        if (player < 0 || EncounterAvailable(world) == 0)
            throw new RulesNotImplementedException("no encounter card is available to pay the cost");
        Deal.EncounterCard(world, player, trigger, events);
        return [];
    }
}
