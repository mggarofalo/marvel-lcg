namespace Marvel.Rules.State;

/// <summary>What kind of thing a printed face is.</summary>
/// <remarks>
/// One member per kind of printed face. The mapping from the card data's
/// <c>engine.type</c> preserves that printed identity even when two kinds obey
/// the same rules, as villains and leaders do.
/// </remarks>
public enum CardKind
{
#pragma warning disable CS1591, SA1602
    Unknown = 0,
    Insert,
    AlterEgo,
    Hero,
    Ally,
    Event,
    Resource,
    Support,
    Upgrade,
    Attachment,
    Obligation,
    Treachery,
    Minion,
    MainScheme,
    Status,
    EncounterVillain,
    EncounterSideScheme,
    Environment,
    Leader,
    Evidence,
    PlayerSideScheme,
    Challenge,
#pragma warning restore CS1591, SA1602
}
