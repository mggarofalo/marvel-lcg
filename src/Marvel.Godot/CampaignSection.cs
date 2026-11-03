namespace Marvel.Godot;

/// <summary>A list of printed cards in the campaign log.</summary>
internal enum CampaignSection
{
    /// <summary>Campaign cards one player's deck carries.</summary>
    PlayerCards,

    /// <summary>Experimental weapons added to later encounter decks.</summary>
    Weapons,

    /// <summary>Campaign upgrades consumed for the rest of the campaign.</summary>
    Consumed,

    /// <summary>Allies excluded from every deck.</summary>
    Imprisoned,
}
