
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
/// The order of operations matters twice over: card creation order fixes every
/// <c>object_id</c>, and the <b>two shuffles</b> draw from one seeded stream, so
/// swapping them changes every card's position. Nothing currently checks the
/// result — MARVEL-251 — so treat any change here as changing every game.
/// </para>
/// <para>
/// Exactly two RNG calls happen during setup, measured on
/// <c>rhino / spider_man / 12345</c>: the player deck, then the encounter deck.
/// Nothing else consumes randomness — not the opening hand, which is dealt off
/// the top of an already-shuffled deck.
/// </para>
/// <para>
/// <b>Card text runs here now.</b> <c>rr:appendix-ii-setup.step.12</c> resolves
/// the scenario's "Setup" and "When Revealed" abilities as part of the deal, so
/// a dealer that could not run a card could not deal a board that was correct —
/// it could only deal one that looked correct on the scenarios whose first card
/// happens to say nothing. The interpreter is a parameter and defaults to
/// <c>NoCardAbilities</c>, which is what a test building a board by hand wants.
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
    /// <param name="abilities">
    /// What cards do — <c>rr:appendix-ii-setup.step.12</c>. Defaults to
    /// <see cref="Play.NoCardAbilities"/>, which deals the board and runs no
    /// text.
    /// </param>
    /// <param name="events">
    /// Where to record what setup's abilities did, or null to discard it. Null
    /// is safe rather than lossy for the same reason the parameter above
    /// defaults: with no interpreter there is nothing to lose.
    /// </param>
    /// <param name="expert">
    /// Whether this is expert mode — <c>rr:modes-of-play.2</c>. What it changes
    /// about the <i>deal</i> is already in the blueprints (different villain
    /// stages, the Expert encounter set); this is the flag the cards read.
    /// </param>
    public static World Deal(
        ICardFacts facts, IReadOnlyList<CardBlueprint> blueprints,
        IReadOnlyList<string> seats, uint seed,
        Play.ICardAbilities? abilities = null, List<Events.GameEvent>? events = null,
        bool expert = false)
    {
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(blueprints);
        ArgumentNullException.ThrowIfNull(seats);

        int players = seats.Count;
        var happened = events ?? [];

        // The seed goes to the world, not to a local generator: the reshuffle
        // in `rr:player-deck.1` draws from this same stream years of turns
        // later, and a second generator would restart it.
        var world = new World(facts, players, seed);
        world.Abilities = abilities ?? new Play.NoCardAbilities();
        world.Expert = expert;

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
                // distinct from their nemesis set; the scenario's goes in the
                // villain's, which is the split `rr:play-area` already makes.
                SetupSlot.SetAside => seat >= 0
                    ? world.Seats[seat].SetAside
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

        // 4. `rr:appendix-ii-setup.step.8` -- the main scheme deck goes into
        //    play, **showing its A side**, which is simply the first face of
        //    its spec. Step 12b is what turns it over, and 12a reads the A side
        //    before that happens.
        var scheme = mainSchemeDeck.Cards.Count > 0 ? mainSchemeDeck.Cards[0] : null;
        if (scheme is not null)
        {
            World.MoveToTop(scheme, mainSchemeArea);
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

        // 11. `rr:appendix-ii-setup.step.11`, "Put Setup Cards Into Play":
        //     "search each deck and the set aside area for any cards with the
        //     setup keyword and put them into play."
        //
        //     **Every deck, and that is not the same as the set-aside pile.**
        //     Nothing in the pool puts a setup card in a player deck today, so
        //     searching one finds nothing — but the rule says to search, and a
        //     loop that only looked where cards happen to be would have to be
        //     found and changed by whoever adds the first such card.
        //
        //     `rr:setup-keyword.1` names this step and nothing else about it,
        //     so where each card goes is the ordinary question of a card
        //     entering play, which `Play.Reveal` already answers.
        SetupCards(world, facts, happened);
        FinishSetupAgenda(world, facts, happened);

        // 12. `rr:appendix-ii-setup.step.12`, "Resolve Scenario Setup and When
        //     Revealed Abilities", which is three sub-steps in a stated order.
        //     The order is the whole of the rule: 12a reads a face that 12b
        //     then turns over.
        if (scheme is not null)
        {
            // 12a. "Resolve any 'Setup' abilities on main scheme card 1A."
            happened.AddRange(world.Abilities.Setup(world, scheme));
            FinishSetupAgenda(world, facts, happened);

            // 12b. "Flip the main scheme card to side 1B and resolve any 'When
            //      Revealed' abilities on that side."
            scheme.TurnTo(scheme.Faces[^1]);

            // Starting threat is placed once, on entry, and it is the B side's
            // number -- no A side in the pool prints one. Every scheme on a
            // recorded opening board starts at zero, so this is invisible until
            // a scenario that does not appears, which is the reason to place it
            // rather than to leave the field derived from print.
            scheme.PlaceTokens("k_threat",
                facts.PrintedValue(scheme.FaceId, "StartingThreat", players));

            happened.AddRange(world.Abilities.WhenRevealed(world, scheme, world.FirstPlayer));
            FinishSetupAgenda(world, facts, happened);
        }

        // 12c. "Resolve any 'Setup' and 'When Revealed' abilities on the
        //      villain." After the scheme, and the appendix says so by
        //      numbering it after -- a villain whose text reads the main scheme
        //      reads the side the players will be playing against.
        if (world.TheCardIn(DeckType.VillainArea) is { } villain)
        {
            happened.AddRange(world.Abilities.Setup(world, villain));
            FinishSetupAgenda(world, facts, happened);
            happened.AddRange(world.Abilities.WhenRevealed(world, villain, world.FirstPlayer));
            FinishSetupAgenda(world, facts, happened);
        }

        // 14. "Draw Cards." Opening hands, off the top of an already-shuffled
        //     deck. No draw, and after step 12 -- which is what lets a setup
        //     ability that searches or shuffles the player decks matter.
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

    /// <summary>Resolve everything one scenario setup ability scheduled.</summary>
    private static void FinishSetupAgenda(
        World world, ICardFacts facts, List<Events.GameEvent> happened)
    {
        // `rr:ability.6` excludes player-card abilities during game setup.
        // The Setup ability itself has already resolved above; nested windows
        // may therefore read encounter cards only. A question from one of
        // those cards is still refused rather than answered on a player's
        // behalf, because setup has no prompt channel.
        if (Play.Sequence.Work(
                world,
                facts,
                world.Abilities,
                happened,
                Timing.WindowAbilityScope.EncounterCardsOnly) is { } asked)
        {
            throw new Play.RulesNotImplementedException(
                $"setup asked '{asked.Label}', and rr:appendix-ii-setup has nobody to ask");
        }
    }

    /// <summary>
    /// Step 11 — every card with the setup keyword, put into play.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Put into play, not revealed.</b>
    /// <c>rr:when-revealed-abilities.2</c>: "if an encounter card with a 'When
    /// Revealed' ability is put into play <b>without being revealed</b>, the
    /// 'When Revealed' ability does not trigger." So this places the card and
    /// runs its keywords — <c>rr:enters-play</c> — and nothing else, which is
    /// exactly what <see cref="Play.Reveal.Resolve"/> does.
    /// </para>
    /// <para>
    /// <b>A card it cannot place stops the deal.</b> Where a card goes comes
    /// from its type for a side scheme or an environment, and from its own text
    /// for an attachment (<c>rr:attach-to</c>) or for a scenario's ally or
    /// support (<c>rr:ownership-and-control.2.2</c>, "when a player takes
    /// control of a […] player card with a player card back"). A card whose
    /// text nobody has read is placed nowhere, and leaving it in the pile it
    /// was searched out of would deal a board that is quietly missing a card
    /// the rules put on the table.
    /// </para>
    /// </remarks>
    private static void SetupCards(
        World world, ICardFacts facts, List<Events.GameEvent> happened)
    {
        // A copy, because putting a card into play moves it between areas and
        // `World.Cards` is walked by object id rather than by place.
        foreach (var card in world.Cards.ToList())
        {
            if (facts.PrintedValue(card.FaceId, "Setup", world.Players) <= 0
                || DeckTypes.IsInPlay(card.Area.Type))
            {
                continue;
            }

            var from = card.Area;
            Play.Reveal.Resolve(world, facts, card, world.FirstPlayer, happened);

            if (card.Area == from)
            {
                throw new Play.RulesNotImplementedException(
                    $"card '{card.FaceId}' has the setup keyword and "
                    + "rr:appendix-ii-setup.step.11 puts it into play, and this engine has "
                    + "nowhere to put it. MARVEL-247");
            }
        }
    }
}
