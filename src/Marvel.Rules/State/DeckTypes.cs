namespace Marvel.Rules.State;

/// <summary>What each <see cref="DeckType"/> means for the cards in it.</summary>
public static class DeckTypes
{
    // Only the three flags that decide `face_up` are carried. A zone is a
    // deck, a hand, or neither, and that is all the digest needs to know to
    // say whether a card in it is visible.
    private static readonly HashSet<DeckType> Decks =
    [
        DeckType.PlayerDeck, DeckType.DiscardPile, DeckType.AdditionalDeck,
        DeckType.AdditionalDiscardPile, DeckType.EncounterDeck, DeckType.EncounterDiscardPile,

        // `rr:deal-deal-an-encounter-card`: a dealt encounter card is placed
        // facedown in front of the player and is not revealed at that time.
        DeckType.DealtEncounterCardsDeck,

        // The one entry here the recording cannot vouch for, because nothing in
        // it ever sits in this place. `rr:attack-enemy-activation.step.1` is
        // where it comes from instead: "give it one *facedown* boost card from
        // the encounter deck", and `rr:boost-boost-icon.6` keeps it facedown
        // until the enemy activates.
        DeckType.BoostCardsDeck,
    ];

    private static readonly HashSet<DeckType> FaceUpFlagged =
    [
        DeckType.UpgradesArea, DeckType.DiscardPile, DeckType.AlliesArea, DeckType.SupportsArea,
        DeckType.EngagedEnemiesArea, DeckType.HeroArea, DeckType.ResourcesArea,
        DeckType.AdditionalDiscardPile, DeckType.AsideDeck, DeckType.ObligationsArea,
        DeckType.EncounterDiscardPile, DeckType.MainSchemesArea, DeckType.SideSchemesArea,
        DeckType.VillainArea, DeckType.BoostingArea, DeckType.EnvironmentArea,
        DeckType.EvidenceArea, DeckType.ProcessingArea, DeckType.RevealingArea,
        DeckType.VictoryDisplay, DeckType.StatusArea, DeckType.RuleArea,
    ];

    /// <summary>Whether a card entering this kind of place is turned face down.</summary>
    /// <remarks>
    /// <para>
    /// True for the places a card can sit hidden: a draw pile that is not a
    /// discard pile, a hand, a dealt encounter queue, and an enemy's facedown
    /// boost cards.
    /// </para>
    /// <para>
    /// <b>Measured, not read off a flag.</b> The obvious candidate,
    /// <c>DeckTypeFlags.is_face_up</c>, is wrong: it is <c>False</c> for
    /// <c>RemovedArea</c> and <c>VillainDeck</c>, and cards in both are recorded
    /// face up. This predicate agrees with all 571 card records across the seven
    /// recorded steps of <c>rhino / spider_man / 12345</c>, over twelve distinct
    /// zones, in which <c>face_up</c> is a function of the zone with no
    /// exceptions — including <c>EncounterDiscardPile</c>, which is a deck and is
    /// nonetheless face up, and which is what rules out the simpler
    /// "decks are hidden".
    /// </para>
    /// </remarks>
    /// <param name="type">The kind of place.</param>
    public static bool FaceDownOnEntry(DeckType type) =>
        type == DeckType.HandsArea || (Decks.Contains(type) && !FaceUpFlagged.Contains(type));

    /// <summary>Whether the area is a concealed pile rather than a player hand.</summary>
    public static bool IsConcealedPile(DeckType type) => type is
        DeckType.PlayerDeck
        or DeckType.AdditionalDeck
        or DeckType.EncounterDeck
        or DeckType.DealtEncounterCardsDeck
        or DeckType.BoostCardsDeck;
    /// <summary>Whether a card in this kind of place is <i>in play</i>.</summary>
    /// <remarks>
    /// <c>rr:in-play-and-out-of-play</c>. Read off the recorded milestone board
    /// rather than from <c>DeckTypeFlags</c>, which answers a different
    /// question.
    /// </remarks>
    /// <param name="type">The kind of place.</param>
    public static bool IsInPlay(DeckType type) => type is
        DeckType.UpgradesArea or DeckType.AlliesArea or DeckType.SupportsArea or
        DeckType.EngagedEnemiesArea or DeckType.HeroArea or DeckType.ObligationsArea or
        DeckType.MainSchemesArea or DeckType.SideSchemesArea or DeckType.VillainArea or
        DeckType.EnvironmentArea or DeckType.EvidenceArea or DeckType.RuleArea;

    /// <summary>Whether a card in this kind of place registers its token pools.</summary>
    /// <remarks>
    /// <para>
    /// <b>Not the same question as <see cref="IsInPlay"/>, and the recording
    /// forces them apart.</b> Both treacheries in the first villain phase come
    /// out of the encounter deck with no <c>k_threat</c> key and end in the
    /// discard pile with one — and neither ever reaches an in-play zone. The
    /// boost card goes through <c>BoostingArea</c> and the encounter card
    /// through <c>RevealingArea</c>.
    /// </para>
    /// <para>
    /// The two also rule out the tempting alternative, that <i>being revealed</i>
    /// is what registers the pool: the engine's log never says the boost card
    /// was revealed, and it gets a pool anyway. What the two have in common is
    /// the place they passed through.
    /// </para>
    /// </remarks>
    /// <param name="type">The kind of place.</param>
    public static bool GrantsTokenPool(DeckType type) =>
        IsInPlay(type) || type is DeckType.BoostingArea or DeckType.RevealingArea;
}
