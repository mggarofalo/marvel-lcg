using Marvel.Rules.Events;
using Marvel.Rules.State;

namespace Marvel.Rules.Play;

/// <summary>Establishes state printed or granted as a card enters play.</summary>
internal static class CardEntryState
{
    internal static void EstablishOwnership(World world, ICardFacts facts, Card card)
    {
        if (!Characteristics.IsLost(world, card, "linked")
            && facts.Attributes(card.FaceId).ContainsKey("Linked")
            && card.Area.PlayArea.IsPlayers)
        {
            card.TransferLinkedOwnership(card.Area.PlayArea.Player);
        }
        if (card.Area.PlayArea.IsPlayers)
        {
            CardControlTransfer.TakeScenarioCardOwnership(
                facts, card, card.Area.PlayArea.Player);
        }
    }

    internal static void GiveToughness(
        World world, ICardFacts facts, Card card, List<GameEvent> events)
    {
        if (StateFields.Modified(world, card, "toughness", facts, world.Players) <= 0
            || Statuses.Has(world, card, Statuses.Tough))
        {
            return;
        }
        var status = Statuses.Give(world, card, Statuses.Tough);
        events.Add(new CardsCreated(
            Places.Reference(status.Area),
            [new CreatedCard(status.ObjectId, status.FaceId)])
        {
            Trigger = "toughness",
            Verb = "Enter_Play",
        });
    }

    internal static void PlaceCounters(
        World world, Card card, List<GameEvent> events, ICardPlayAbilities? abilities)
    {
        CardCounterPool? pool = (abilities ?? world.CardPlayAbilities).CounterPool(world, card);
        if (pool is not { Starting: > 0 }
            || pool.Uses && Characteristics.IsLost(world, card, "uses"))
        {
            return;
        }
        string key = "c_" + pool.Type;
        card.PlaceTokens(key, pool.Starting);
        events.Add(new FieldSet(card.ObjectId, key, 0, pool.Starting)
        {
            Trigger = pool.Uses ? "uses" : "card text",
            Verb = "Enter_Play",
        });
    }

    internal static void PlaceThreat(
        World world, ICardFacts facts, Card card, List<GameEvent> events)
    {
        long hinder = StateFields.Modified(world, card, "hinder", facts, world.Players);
        if (hinder <= 0)
        {
            return;
        }
        card.PlaceTokens("k_threat", hinder);
        events.Add(new FieldSet(card.ObjectId, "k_threat", 0, hinder)
        {
            Trigger = "hinder",
            Verb = "Enter_Play",
        });
    }
}
