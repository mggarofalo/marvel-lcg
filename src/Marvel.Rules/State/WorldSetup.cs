
namespace Marvel.Rules.State;

/// <summary>Where a card starts. What the rules need from a deal order.</summary>
public enum SetupSlot
{
#pragma warning disable CS1591, SA1602
    Rules,
    Challenge,
    Identity,
    Obligation,
    Nemesis,
    PlayerDeck,
    MainScheme,
    Villain,
    Encounter,

    /// <summary>
    /// Set aside before setup, rather than shuffled into a deck —
    /// <c>rr:permanent.2</c> and <c>rr:setup-keyword.1</c>.
    /// </summary>
    SetAside,
#pragma warning restore CS1591, SA1602
}

/// <summary>One card to make, and where it goes.</summary>
/// <param name="Spec">Comma-separated face ids. One card, however many faces.</param>
/// <param name="Slot">Where it starts.</param>
/// <param name="Seat">The seat it belongs to, or -1 for the scenario.</param>
/// <remarks>
/// Coarser than the content layer's <c>CreationSource</c>, and deliberately: a
/// hero's signature cards and their aspect cards are two different questions
/// about deck-building and the same answer about where they go.
/// </remarks>
public sealed record CardBlueprint(string Spec, SetupSlot Slot, int Seat);

/// <summary>
/// Deals the opening board: the whole state model and none of the rules.
/// </summary>
/// <remarks>
/// <para>
/// Read out of <c>World.Initialize</c>, <c>PlayerSetup.SelectIdentity</c> and
/// <c>Scenario.SelectVillain</c> in the Python engine, and held against a
/// recorded digest. The order of operations matters twice over: card creation
/// order fixes every <c>object_id</c>, and the <b>two shuffles</b> draw from one
/// seeded stream, so swapping them changes every card's position.
/// </para>
/// <para>
/// Exactly two RNG calls happen during setup, measured on
/// <c>rhino / spider_man / 12345</c>: the player deck, then the encounter deck.
/// Nothing else consumes randomness — not the opening hand, which is dealt off
/// the top of an already-shuffled deck.
/// </para>
/// <para>
/// No card abilities. A scenario whose setup fires an ability — Doctor Strange's
/// Invocations, a challenge that places a status card — needs the engine, and the
/// deal order documents which those are.
/// </para>
/// </remarks>
public static class WorldSetup
{
    /// <summary>Deals a board.</summary>
    /// <param name="facts">The printed card data.</param>
    /// <param name="blueprints">The deal order. Position is the card's id.</param>
    /// <param name="seats">
    /// The players' names, in seat order. The count is the player count, which
    /// decides every <c>*</c> in the printed data.
    /// </param>
    /// <param name="seed">The game's seed. One stream, seeded once.</param>
    public static World Deal(
        ICardFacts facts, IReadOnlyList<CardBlueprint> blueprints,
        IReadOnlyList<string> seats, uint seed)
    {
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(blueprints);
        ArgumentNullException.ThrowIfNull(seats);

        int players = seats.Count;

        // The seed goes to the world, not to a local generator: the reshuffle
        // in `rr:player-deck.1` draws from this same stream years of turns
        // later, and a second generator would restart it.
        var world = new World(facts, players, seed);

        var insert = world.CreateArea(DeckType.RemovedArea);
        var encounterDeck = world.CreateArea(DeckType.EncounterDeck);
        var villainDeck = world.CreateArea(DeckType.VillainDeck);
        var villainArea = world.CreateArea(DeckType.VillainArea);
        var mainSchemeDeck = world.CreateArea(DeckType.MainSchemesDeck);
        var mainSchemeArea = world.CreateArea(DeckType.MainSchemesArea);

        foreach (string name in seats)
        {
            world.CreateSeat(name);
        }

        // 1. Make every card, in order. This is the id contract and nothing
        //    below may reorder it.
        var obligations = new List<Card>();
        var identities = new Card[players];
        foreach (var blueprint in blueprints)
        {
            int seat = blueprint.Seat;
            var destination = blueprint.Slot switch
            {
                SetupSlot.Rules or SetupSlot.Challenge => insert,
                SetupSlot.Identity => world.Seats[seat].Identity,
                SetupSlot.Obligation or SetupSlot.Nemesis => world.Seats[seat].Nemesis,
                SetupSlot.PlayerDeck => world.Seats[seat].Deck,
                SetupSlot.MainScheme => mainSchemeDeck,
                SetupSlot.Villain => villainDeck,
                SetupSlot.Encounter => encounterDeck,

                // `rr:permanent.2` -- "permanent cards are set aside **before
                // step 1 of setup**". A player's goes in their own aside pile,
                // beside their nemesis set; the scenario's goes in the
                // villain's, which is the split `rr:play-area` already makes.
                SetupSlot.SetAside => seat >= 0
                    ? world.Seats[seat].Nemesis
                    : world.AreaOf(DeckType.AsideDeck),
                _ => throw new ArgumentOutOfRangeException(nameof(blueprints)),
            };

            var card = world.CreateCard(blueprint.Spec, destination);
            if (blueprint.Slot == SetupSlot.Obligation)
            {
                obligations.Add(card);
            }
            else if (blueprint.Slot == SetupSlot.Identity)
            {
                identities[seat] = card;
            }
        }

        // 2. Each identity enters play. The alter-ego side is already first --
        //    the deal order put it there -- so nothing is flipped here.
        for (int seat = 0; seat < players; seat++)
        {
            World.MoveToTop(identities[seat], world.Seats[seat].Hero);
            world.Seats[seat].IdentityCard = identities[seat];
        }

        // 3. The player decks are shuffled, in seat order. First draw of the game.
        foreach (var seat in world.Seats)
        {
            world.Shuffle(seat.Deck);
        }

        // 4. The first main scheme enters play, turned to its `B` side. This is
        //    the one card on an opening board whose showing face is not the
        //    first face of its spec.
        if (mainSchemeDeck.Cards.Count > 0)
        {
            var scheme = mainSchemeDeck.Cards[0];
            World.MoveToTop(scheme, mainSchemeArea);
            scheme.TurnTo(scheme.Faces[^1]);

            // Starting threat is placed once, on entry. Every scheme on a
            // recorded opening board starts at zero, so this is invisible until
            // a scenario that does not appears -- which is the reason to place
            // it rather than to leave the field derived from print.
            scheme.PlaceTokens("k_threat",
                facts.PrintedValue(scheme.FaceId, "StartingThreat", players));
        }

        // 5. The first villain stage enters play; the later stages wait.
        if (villainDeck.Cards.Count > 0)
        {
            World.MoveToTop(villainDeck.Cards[0], villainArea);
        }

        // 6. Obligations go on top of the encounter deck, then it is shuffled.
        //    Order matters: shuffling first would leave them on top.
        foreach (var obligation in obligations)
        {
            World.MoveToTop(obligation, encounterDeck);
        }

        world.Shuffle(encounterDeck);

        // 7. Opening hands, off the top of an already-shuffled deck. No draw.
        for (int seat = 0; seat < players; seat++)
        {
            long handSize = facts.PrintedValue(identities[seat].FaceId, "HS", players);
            for (long drawn = 0; drawn < handSize; drawn++)
            {
                var card = world.Seats[seat].Deck.TakeTop();
                if (card is null)
                {
                    break;
                }

                world.Seats[seat].Hand.Append(card);
            }
        }

        return world;
    }

}
