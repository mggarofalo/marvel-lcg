using Marvel.View;

namespace Marvel.Protocol;

/// <summary>Validates published auxiliary-pile references without interpreting card rules.</summary>
public static class InvocationDeckContract
{
    /// <summary>Requires unique owners and distinct, correctly owned runtime areas.</summary>
    public static bool IsValid(WorldDescriptor world)
    {
        if (world.InvocationDecks is null) return false;
        if (world.InvocationDecks.Count == 0) return true;
        if (world.Players is null || world.Areas is null) return false;
        var owners = new HashSet<int>();
        var piles = new HashSet<int>();
        return world.InvocationDecks.All(deck => ValidDeck(world, deck, owners, piles));
    }

    private static bool ValidDeck(WorldDescriptor world, InvocationDeckDescriptor deck,
        HashSet<int> owners, HashSet<int> piles) =>
        deck is not null && deck.Owner >= 0 && owners.Add(deck.Owner)
        && world.Players.Count(player => player is not null && player.Seat == deck.Owner) == 1
        && piles.Add(deck.Deck) && piles.Add(deck.Discard)
        && Matches(world, deck.Deck, deck.Owner, "AdditionalDeck")
        && Matches(world, deck.Discard, deck.Owner, "AdditionalDiscardPile");

    private static bool Matches(WorldDescriptor world, int id, int owner, string zone)
    {
        var matches = world.Areas.Where(area => area is not null && area.Id == id).ToArray();
        return matches.Length == 1 && matches[0].Owner == owner
            && matches[0].Zone == zone && matches[0].Host == -1;
    }
}
