namespace Marvel.Rules.State;

/// <summary>
/// The kinds of place a card can be. The <b>name</b> is a wire format.
/// </summary>
/// <remarks>
/// <para>
/// The state digest records <c>zone</c> as this enum's member name, so the
/// spellings are a wire format: renaming a member changes every digest. The
/// numeric values are not on the wire and carry no meaning.
/// </para>
/// <para>
/// <b>A zone name is not an area.</b> One name can belong to several distinct
/// areas at once — <c>HandsArea</c> names one per player, and a player's
/// set-aside pile and their nemesis pile are both <c>AsideDeck</c>. That is why
/// an area carries an identity of its own (the original investigation) and why <c>index</c> in
/// the digest is per area rather than per name.
/// </para>
/// </remarks>
public enum DeckType
{
#pragma warning disable CS1591, SA1602
    PlaceCardArea = 1,
    UpgradesArea = 2,
    BoostCardsDeck = 3,
    PlayerDeck = 10,
    DiscardPile = 11,
    AlliesArea = 12,
    SupportsArea = 13,
    HandsArea = 14,
    EngagedEnemiesArea = 15,
    DealtEncounterCardsDeck = 16,
    HeroArea = 17,
    ResourcesArea = 18,
    AdditionalDeck = 20,
    AdditionalDiscardPile = 21,
    AsideDeck = 22,
    ObligationsArea = 23,
    EncounterDeck = 30,
    EncounterDiscardPile = 31,
    MainSchemesArea = 32,
    MainSchemesDeck = 33,
    SideSchemesArea = 34,
    VillainArea = 35,
    VillainDeck = 36,
    BoostingArea = 37,
    EnvironmentArea = 38,
    EvidenceArea = 39,
    ProcessingArea = 40,
    RevealingArea = 41,
    RemovedArea = 51,
    VictoryDisplay = 52,
    StatusArea = 53,
    RuleArea = 54,
#pragma warning restore CS1591, SA1602
}
