namespace Marvel.Rules.State;
/// <summary>Creates a seeded world and resolves scenario setup up to its first decision.</summary>
/// <remarks>
/// Creation order fixes object ids. Player decks are shuffled in seat order,
/// followed by the encounter deck, using the world's one seeded RNG stream.
/// Scenario abilities resolve before opening hands; mandatory choices suspend
/// through the normal game prompt boundary. Tests that intentionally omit card
/// text use <see cref="DealWithoutCardAbilities"/>.
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
    /// What cards do — <c>rr:appendix-ii-setup.step.12</c>.
    /// </param>
    /// <param name="events">
    /// Where to record what setup's abilities did, or null to discard it. Null
    /// is safe when the caller does not need a setup transcript.
    /// </param>
    /// <param name="expert">
    /// Whether this is expert mode — <c>rr:modes-of-play.2</c>. What it changes
    /// about the <i>deal</i> is already in the blueprints (different villain
    /// stages, the Expert encounter set); this is the flag the cards read.
    /// </param>
    public static World Deal(
        ICardFacts facts, IReadOnlyList<CardBlueprint> blueprints,
        IReadOnlyList<string> seats, uint seed,
        Play.ICardAbilities abilities, List<Events.GameEvent>? events = null,
        bool expert = false) => DealConfigured(facts, blueprints, seats, seed, abilities,
            new WorldSetupOptions(expert), events);

    /// <summary>Deals a world with deterministic campaign bindings before any setup abilities run.</summary>
    public static World DealConfigured(
        ICardFacts facts, IReadOnlyList<CardBlueprint> blueprints,
        IReadOnlyList<string> seats, uint seed,
        Play.ICardAbilities abilities, WorldSetupOptions options,
        List<Events.GameEvent>? events = null)
    {
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(blueprints);
        ArgumentNullException.ThrowIfNull(seats);
        ArgumentNullException.ThrowIfNull(abilities);
        ArgumentNullException.ThrowIfNull(options);

        Setup.PlayerCountRules.Validate(seats.Count);

        Setup.InvocationSetup.Validate(blueprints, seats.Count);
        RefuseUnsupportedKinds(facts, blueprints);
        if (options.EncounterGroups is { } encounterGroups)
            EncounterSetupValidation.Validate(encounterGroups, blueprints, facts);

        int players = seats.Count;
        var happened = events ?? [];

        // The seed goes to the world, not to a local generator: the reshuffle
        // in `rr:player-deck.1` draws from this same stream years of turns
        // later, and a second generator would restart it.
        var world = new World(facts, players, seed);
        world.Abilities = abilities;
        world.Expert = options.Expert;

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
        var created = CreateOpeningCards(
            world, blueprints, players, insert, encounterDeck, villainDeck, mainSchemeDeck);
        var obligations = created.Obligations;
        var identities = created.Identities;
        if (options.Campaign is { } campaign)
            new Campaign.CampaignAttemptState(campaign, options.CampaignCards ?? [], options.ExperimentalWeapons).Bind(world);

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

            // The setup dataset lists stages in printed resolution order,
            // while Area stores a deck bottom-first. Object ids retain deal
            // order; only the remaining deck positions are reversed so stage
            // two, not the final stage, is on top of a three-stage deck.
            mainSchemeDeck.Replace(mainSchemeDeck.Cards.Reverse().ToList());
        }

        // 5. The first villain stage enters play; the later stages wait.
        if (options.EncounterGroups is { } separateDecks)
            EncounterSetupPlacement.Place(world, separateDecks);
        else if (villainDeck.Cards.Count > 0)
        {
            World.MoveToTop(villainDeck.Cards[0], villainArea);
        }

        // 6. Obligations go on top of the encounter deck, then it is shuffled.
        //    Order matters: shuffling first would leave them on top.
        foreach (var obligation in obligations)
        {
            World.MoveToTop(obligation, encounterDeck);
        }

        ShuffleEncounterDecks(world);

        // Engine choice: separate Invocation shuffles follow all ordinary decks,
        // in seat order, before scenario abilities or opening hands.
        Setup.InvocationSetup.Bind(world, blueprints, happened);

        world.ScenarioSetup = new ScenarioSetup();
        world.ScenarioSetup.Advance(world, happened, happened.Count);

        return world;
    }

    private static void ShuffleEncounterDecks(World world)
    {
        if (world.EncounterTopology is { } topology)
            foreach (var group in topology.Groups) world.Shuffle(group.Deck);
        else world.Shuffle(world.AreaOf(DeckType.EncounterDeck));
    }

    private sealed record OpeningCards(List<Card> Obligations, Card[] Identities);

    private static OpeningCards CreateOpeningCards(
        World world, IReadOnlyList<CardBlueprint> blueprints, int players,
        Area insert, Area encounterDeck, Area villainDeck, Area mainSchemeDeck)
    {
        var obligations = new List<Card>();
        var identities = new Card[players];
        foreach (var blueprint in blueprints)
        {
            var destination = SetupDestination(
                world, blueprint, insert, encounterDeck, villainDeck, mainSchemeDeck);
            var card = world.CreateCard(blueprint.Spec, destination);
            if (blueprint.Slot == SetupSlot.Obligation)
            {
                obligations.Add(card);
            }
            else if (blueprint.Slot == SetupSlot.Identity)
            {
                identities[blueprint.Seat] = card;
            }
        }
        return new OpeningCards(obligations, identities);
    }

    private static Area SetupDestination(
        World world, CardBlueprint blueprint, Area insert, Area encounterDeck,
        Area villainDeck, Area mainSchemeDeck) => blueprint.Slot switch
    {
        SetupSlot.Rules or SetupSlot.Challenge => insert,
        SetupSlot.Identity => world.Seats[blueprint.Seat].Identity,
        SetupSlot.Obligation or SetupSlot.Nemesis => world.Seats[blueprint.Seat].Nemesis,
        SetupSlot.PlayerDeck => world.Seats[blueprint.Seat].Deck,
        SetupSlot.MainScheme => mainSchemeDeck,
        SetupSlot.Villain => villainDeck,
        SetupSlot.Encounter => encounterDeck,
        SetupSlot.InvocationDeck => world.AreaOf(DeckType.AsideDeck, PlayArea.Of(blueprint.Seat), cardOwner: blueprint.Seat),
        SetupSlot.SetAside => blueprint.Seat >= 0
            ? world.Seats[blueprint.Seat].SetAside
            : world.AreaOf(DeckType.AsideDeck),
        _ => throw new ArgumentOutOfRangeException(nameof(blueprint)),
    };

    /// <summary>Deals a board whose cards intentionally have no executable text.</summary>
    public static World DealWithoutCardAbilities(
        ICardFacts facts, IReadOnlyList<CardBlueprint> blueprints,
        IReadOnlyList<string> seats, uint seed,
        List<Events.GameEvent>? events = null, bool expert = false) =>
        Deal(facts, blueprints, seats, seed, new Play.NoCardAbilities(), events, expert);

    private static void RefuseUnsupportedKinds(
        ICardFacts facts, IReadOnlyList<CardBlueprint> blueprints)
    {
        foreach (var blueprint in blueprints)
        {
            foreach (string faceId in blueprint.Spec.Split(','))
            {
                var kind = facts.Kind(faceId);
                string missing = kind switch
                {
                    CardKind.Evidence =>
                        "campaign evidence envelopes and their setup abilities",
                    CardKind.PlayerSideScheme =>
                        "playing and resolving player side schemes",
                    CardKind.Challenge =>
                        "challenge setup and rules modifiers",
                    _ => string.Empty,
                };
                if (missing.Length == 0)
                {
                    continue;
                }

                throw new Play.RulesNotImplementedException(
                    $"card '{faceId}' is a {kind}, and this engine does not "
                    + $"yet support {missing}");
            }
        }
    }

}
