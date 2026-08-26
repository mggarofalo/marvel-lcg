namespace Marvel.Rules.State;

/// <summary>
/// One player: their name, their identity, and the places that are theirs.
/// </summary>
/// <remarks>
/// <para>
/// Promoted out of <c>WorldSetup</c>, where it was a private record used to
/// carry areas between the steps of a deal. The engine needs the same grouping for
/// a different reason — a prompt is put to a seat, and answering it reads that
/// seat's hand — so it belongs to the state rather than to the procedure that
/// builds it.
/// </para>
/// <para>
/// <b>This is not yet a play area.</b> A seat holds the areas a deal makes for
/// one player; the Rules Reference's <i>play area</i> is a grouping a card can
/// be in, which decides what "the main scheme" resolves to and what a game area
/// contains. MARVEL-175 is that model, and this type is deliberately smaller
/// than it: adding areas here that the engine does not use would be guessing at
/// its shape.
/// </para>
/// </remarks>
public sealed class Seat
{
    internal Seat(int index, string name, Area identity, Area nemesis, Area deck, Area hand, Area hero)
    {
        Index = index;
        Name = name;
        Identity = identity;
        Nemesis = nemesis;
        Deck = deck;
        Hand = hand;
        Hero = hero;
    }

    /// <summary>Which seat this is, from 0.</summary>
    public int Index { get; }

    /// <summary>
    /// The player's name, e.g. <c>Spider-Man</c>.
    /// </summary>
    /// <remarks>
    /// The <b>hero</b> side's name, and it does not change when the identity
    /// flips: the engine says "Spider-Man's Turn" while Peter Parker is the face
    /// showing. It names the player, not the card. Supplied by the content layer
    /// from <c>datasets/setup/setup.json</c>, which records it per hero.
    /// </remarks>
    public string Name { get; }

    /// <summary>Where the identity was made. Empty once it enters play.</summary>
    public Area Identity { get; }

    /// <summary>This player's nemesis pile. Theirs, and the scenario's property.</summary>
    public Area Nemesis { get; }

    /// <summary>This player's deck. The <b>last</b> element is the top.</summary>
    public Area Deck { get; }

    /// <summary>This player's hand.</summary>
    public Area Hand { get; }

    /// <summary>Where this player's identity sits once it is in play.</summary>
    public Area Hero { get; }

    /// <summary>The identity card, once the deal has made it.</summary>
    /// <remarks>
    /// Settable because a seat's identity is established by whoever builds the
    /// board, and <c>WorldSetup</c> is not the only thing that does: a test
    /// board assembled by hand has to say which card is whose. It is not a
    /// thing the rules change — a hero flips form, and flipping is a face
    /// change on this same card.
    /// </remarks>
    public Card IdentityCard { get; set; } = null!;

    /// <summary>
    /// The round this player last used their voluntary form change, or 0.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>rr:form-change-form.1</c> permits one form change per player per
    /// round, and this is the whole of that permission. <b>It is not the
    /// form</b> — that is read off the identity card by
    /// <see cref="Forms.Of"/> and never stored — it is the once-a-round budget
    /// for changing it voluntarily.
    /// </para>
    /// <para>
    /// Separate for a reason the rulebook states outright.
    /// <c>rr:form-change-form.3</c>: "If a card ability causes a player to
    /// change forms, it does not count against the one voluntary form change
    /// the player is permitted during their turn that round." A single flag on
    /// the flip itself would spend the budget on an ability's change, which is
    /// exactly what that clause forbids.
    /// </para>
    /// <para>
    /// <b>Not on the wire.</b> No recorded digest field carries it, so a game
    /// restored from a digest alone would forget whether the budget had been
    /// spent. That is a gap in the digest rather than a reason to invent a key
    /// for it — see <c>docs/forms.md</c>.
    /// </para>
    /// </remarks>
    public int FormChangedInRound { get; set; }

    /// <summary>
    /// Whether this player has been eliminated — <c>rr:player-elimination</c>.
    /// </summary>
    /// <remarks>
    /// "A player is eliminated from the game if their identity is defeated."
    /// The seat stays in <c>World.Seats</c> rather than being removed, because
    /// <c>rr:player-elimination.6</c> keeps one thing about them alive:
    /// "effects that refer to the players in the game ignore eliminated
    /// players, <b>except for the per player icon</b>." A villain's <c>14*</c>
    /// hit points do not shrink when somebody dies, so the starting count has
    /// to survive — which it does as <c>World.Players</c>, while
    /// <c>World.PlayerOrder</c> skips the eliminated.
    /// </remarks>
    public bool Eliminated { get; set; }
}
