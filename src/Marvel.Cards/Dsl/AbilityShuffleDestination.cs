namespace Marvel.Cards.Dsl;

/// <summary>Deck destinations supported by shuffle-into instructions.</summary>
public enum AbilityShuffleDestination
{
    /// <summary>The resolving player's deck.</summary>
    YourDeck,
    /// <summary>The scenario's encounter deck.</summary>
    EncounterDeck,
    /// <summary>Each selected physical card's owner's player deck.</summary>
    OwnerDeck,
}
