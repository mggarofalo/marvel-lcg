using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Rules.Tests.Play;

/// <summary>
/// A player whose identity was defeated — <c>rr:player-elimination</c>.
/// </summary>
/// <remarks>
/// Five numbered steps, and the order is load-bearing: step 1 hands the first
/// player token on before step 5 removes the play area it was sitting in.
/// </remarks>
public sealed class EliminationTests
{
    [Rule("rr:player-elimination.step.1")]
    [Fact]
    public void TheFirstPlayerTokenGoesToTheNextPlayer()
    {
        // "If the eliminated player has the first player token, they pass it to
        // the next clockwise player."
        var printed = Cards();
        var world = Board(printed, players: 3);
        world.FirstPlayer = 1;

        Elimination.Eliminate(world, printed, 1, "test", []);

        Assert.Equal(2, world.FirstPlayer);
    }

    [Rule("rr:player-elimination.step.2")]
    [Fact]
    public void EngagedMinionsEngageTheNextPlayerAndKeepWhatIsOnThem()
    {
        // "Each of those minions engages the next clockwise player, **retaining
        // any tokens, attached cards, boost cards, tucked cards, and status
        // cards on them**." All of those hang off the minion's object id rather
        // than off the area, so moving the card keeps them.
        var printed = Cards();
        var world = Board(printed, players: 2);
        var minion = world.CreateCard(
            "minion", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        minion.TakeDamage(2);
        Statuses.Give(world, minion, Statuses.Tough);

        Elimination.Eliminate(world, printed, 0, "test", []);

        Assert.Equal(
            PlayArea.Of(1),
            world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1)).PlayArea);
        Assert.Contains(
            minion, world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1)).Cards);
        Assert.Equal(2, minion.Damage);
        Assert.True(Statuses.Has(world, minion, Statuses.Tough));
    }

    [Rule("rr:defeat.2")]
    [Fact]
    public void TheIdentityIsRemovedFromTheGameRatherThanDiscarded()
    {
        // "If an identity or stage of the villain is defeated, it is **removed
        // from the game**" -- not discarded, which is what happens to an ally.
        var printed = Cards();
        var world = Board(printed, players: 2);
        var identity = world.Seats[0].IdentityCard;
        var events = new List<GameEvent>();

        Elimination.Eliminate(world, printed, 0, "test", events);

        Assert.Equal(DeckType.RemovedArea, identity.Area.Type);

        // **And it never passes through a discard pile on the way.**
        // `rr:identity.3`: "identity cards cannot be discarded from play." Step
        // 5 removes the whole play area including its discard pile, so the end
        // state is the same either way -- only the event stream tells the two
        // apart, and a client drawing from it would show the card discarded.
        Assert.DoesNotContain(
            events.OfType<CardsMoved>(),
            moved => moved.To.Zone == nameof(DeckType.DiscardPile)
                && moved.Cards.Any(landing => landing.Card == identity.ObjectId));
    }

    [Rule("rr:player-elimination.step.4")]
    [Fact]
    public void EveryCardInThePlayAreaGoesToItsOwnersDiscardPile()
    {
        // Steps 3.3 and 4 are the same instruction from two sides: each card
        // goes to **its owner's** discard pile. An ally somebody else owns does
        // not end up in the eliminated player's.
        var printed = Cards();
        var world = Board(printed, players: 2);
        var mine = world.CreateCard(
            "ally", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));

        Elimination.Eliminate(world, printed, 0, "test", []);

        // Their own ally, to their own discard pile (step 4) -- and then step 5
        // removes that pile with the rest of the play area, so it ends up
        // removed from the game either way.
        Assert.Equal(DeckType.RemovedArea, mine.Area.Type);

        // **A card another player owns is not tested here and cannot be.**
        // `World.AreaOf` matches on (type, play area, host) and not on owner,
        // so one play area cannot hold two allies areas with different owners.
        // Step 3.3's "each other card in its owner's discard pile" is written
        // and unreachable on this model; see `Discard.Card`, which reads the
        // owner off the card.
    }

    [Rule("rr:player-elimination.6")]
    [Fact]
    public void AnEliminatedPlayerTakesNoMoreTurnsAndStillCountsForPerPlayer()
    {
        // "Effects that refer to the players in the game ignore eliminated
        // players, **except for the per player icon**." So the turn order
        // shrinks and a villain's `14*` hit points do not.
        var printed = Cards();
        var world = Board(printed, players: 3);

        Assert.Equal([0, 1, 2], world.PlayerOrder);

        Elimination.Eliminate(world, printed, 1, "test", []);

        Assert.Equal([0, 2], world.PlayerOrder);
        Assert.Equal(3, world.Players);
    }

    [Rule("rr:player-elimination.4")]
    [Fact]
    public void TheGameEndsWhenTheLastPlayerIsEliminated()
    {
        // "If all players are eliminated, the game ends and the players lose."
        var printed = Cards();
        var world = Board(printed, players: 2);

        Elimination.Eliminate(world, printed, 0, "test", []);
        Assert.Equal(Outcome.Unfinished, world.Result);

        Elimination.Eliminate(world, printed, 1, "test", []);
        Assert.Equal(Outcome.PlayersLose, world.Result);
    }

    [Rule("rr:player-elimination")]
    [Fact]
    public void EliminatingAPlayerTwiceDoesNothingTheSecondTime()
    {
        // A defeat resolved twice would pass the token on twice and move the
        // same minions again -- and at the last player it would end the game
        // twice, which `World.Finish` refuses outright.
        var printed = Cards();
        var world = Board(printed, players: 1);

        Elimination.Eliminate(world, printed, 0, "test", []);
        Assert.Equal(Outcome.PlayersLose, world.Result);

        Elimination.Eliminate(world, printed, 0, "test", []);
        Assert.Equal(Outcome.PlayersLose, world.Result);
    }

    [Rule("rr:defeat")]
    [Rule("rr:side-scheme.2")]
    [Fact]
    public void ASideSchemeThwartedToZeroIsDefeated()
    {
        // "If a character has zero or fewer remaining hit points, **or if a
        // side scheme has no threat on it**, it is defeated." Before this, a
        // side scheme thwarted to zero sat on the board forever.
        var printed = Cards();
        var world = Board(printed, players: 1);
        world.Seats[0].IdentityCard.TurnTo("hero");
        var side = world.CreateCard("sideScheme", world.AreaOf(DeckType.SideSchemesArea));
        side.PlaceTokens("k_threat", 2);

        BasicPowers.BasicThwart(world, printed, 0, side, []);
        Agendas.Finish(world, printed);

        Assert.Equal(DeckType.EncounterDiscardPile, side.Area.Type);
    }

    [Rule("rr:main-scheme-main-scheme-deck.6")]
    [Fact]
    public void TheMainSchemeAtZeroThreatStays()
    {
        // "Main scheme cards cannot be discarded from play." A main scheme at
        // zero threat is one the players are winning, not one that is defeated.
        var printed = Cards();
        var world = Board(printed, players: 1);
        world.Seats[0].IdentityCard.TurnTo("hero");
        var main = world.TheCardIn(DeckType.MainSchemesArea)!;
        main.PlaceTokens("k_threat", 2);

        BasicPowers.BasicThwart(world, printed, 0, main, []);
        Agendas.Finish(world, printed);

        Assert.Equal(DeckType.MainSchemesArea, main.Area.Type);
        Assert.Equal(0, main.Tokens["k_threat"]);
    }

    private static World Board(Printed printed, int players)
    {
        var world = new World(printed, players);
        for (int seat = 0; seat < players; seat++)
        {
            world.CreateSeat($"p{seat}");
            world.Seats[seat].IdentityCard =
                world.CreateCard("alterego,hero", world.Seats[seat].Hero);

            // A card in the deck, so a discard does not trigger
            // `rr:player-deck.4`'s reset and pull it straight back out.
            world.CreateCard("ally", world.Seats[seat].Deck);
        }

        world.CreateCard("villain", world.AreaOf(DeckType.VillainArea));
        world.CreateCard("scheme", world.AreaOf(DeckType.MainSchemesArea));
        return world;
    }

    private static Printed Cards() => new Printed()
        .With("hero", ("HP", "10"), ("THW", "2"))
        .With("alterego", ("HP", "10"))
        .With("villain", ("HP", "20"))
        .With("minion", ("HP", "3"))
        .With("ally", ("HP", "3"));

    private sealed class Printed : ICardFacts
    {
        private readonly Dictionary<string, Dictionary<string, string>> attributes =
            new(StringComparer.Ordinal);

        public Printed With(string faceId, params (string Key, string Value)[] values)
        {
            var table = attributes.TryGetValue(faceId, out var found)
                ? found
                : attributes[faceId] = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var (key, value) in values)
            {
                table[key] = value;
            }

            return this;
        }

        public CardKind Kind(string faceId) => faceId switch
        {
            "alterego" => CardKind.AlterEgo,
            "hero" => CardKind.Hero,
            "villain" => CardKind.EncounterVillain,
            "scheme" => CardKind.MainScheme,
            "sideScheme" => CardKind.EncounterSideScheme,
            "minion" => CardKind.Minion,
            "ally" => CardKind.Ally,
            "tough" => CardKind.Status,
            _ => CardKind.Treachery,
        };

        public IReadOnlyList<string> Traits(string faceId) => [];

        public IReadOnlyDictionary<string, string> Attributes(string faceId) =>
            attributes.TryGetValue(faceId, out var found)
                ? found
                : new Dictionary<string, string>(StringComparer.Ordinal);

        public long PrintedValue(string faceId, string attribute, int players, long fallback = 0) =>
            Attributes(faceId).TryGetValue(attribute, out string? value)
            && long.TryParse(value, out long number)
                ? number
                : fallback;
    }
}
