using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>Test-local printed Mutagen shapes; no scenario admission.</summary>
public sealed class MutagenOrderedDiscardTests
{
    private const string Goblin = """
        {"and":[{"isKind":{"card":"that","kind":"minion"}},
          {"hasTrait":{"card":"that","trait":"GOBLIN"}}]}
        """;
    private const string Enter = """{"putIntoPlay":{"card":"that","where":"engagedWithYou"}}""";

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:encounter-deck.1")]
    [Rule("rr:when-revealed-abilities.2")]
    public void AllReceiptMatchesEnterAfterDiscardAndImmediateReset(bool reset)
    {
        // 02028: "discard 2 cards ... and put each Goblin minion they discarded this way into play".
        // rr:encounter-deck.1: "If the encounter deck runs out of cards, the encounter discard pile
        // is immediately shuffled to create a new encounter deck."
        // rr:when-revealed-abilities.2: When Revealed "does not resolve" on put into play.
        var runner = Runner(Traversal("forEachDiscarded", 2, Enter));
        var world = Board(runner);
        var deck = world.AreaOf(DeckType.EncounterDeck);
        var outsider = world.CreateCard("02024", world.AreaOf(DeckType.EncounterDiscardPile));
        if (!reset) world.CreateCard("01101", deck);
        var second = world.CreateCard("02024", deck);
        var first = world.CreateCard("02024", deck);
        var events = Reveal(runner, world);
        Sequence.Finish(world, world.Facts, runner, events);
        var entries = events.OfType<CardsMoved>().Where(e => e.Verb == "Put_Into_Play").ToList();
        Assert.Equal(new[] { first.ObjectId, second.ObjectId }, entries.SelectMany(e => e.Cards).Select(c => c.Card));
        Assert.All(new[] { first, second }, card => Assert.Equal(PlayArea.Of(1), card.Area.PlayArea));
        Assert.NotEqual(DeckType.EngagedEnemiesArea, outsider.Area.Type);
        Assert.False(world.Seats[1].IdentityCard.Tokens.ContainsKey("c_revealed"));
        Assert.Equal(reset ? 1 : 0, world.TheCardIn(DeckType.MainSchemesArea)!.Tokens
            .GetValueOrDefault(EncounterDeck.AccelerationToken));
        int lastDiscard = events.FindLastIndex(e => e is CardsMoved && e.Verb == "Discard");
        Assert.True(lastDiscard < events.IndexOf(entries[0]));
    }

    [Fact]
    public void PlayerOrderKeepsEachPlayersEntireReceipt()
    {
        // 02028: "In player order, each player" resolves their own discard and entries.
        var runner = Runner("{\"eachPlayer\":{\"order\":\"playerOrder\",\"effect\":"
            + Traversal("forEachDiscarded", 2, Enter) + "}}");
        var world = Board(runner);
        world.FirstPlayer = 1;
        var deck = world.AreaOf(DeckType.EncounterDeck);
        world.CreateCard("01101", deck);
        var zeroSecond = world.CreateCard("02024", deck);
        var zeroFirst = world.CreateCard("02024", deck);
        var oneSecond = world.CreateCard("02024", deck);
        var oneFirst = world.CreateCard("02024", deck);
        Sequence.Finish(world, world.Facts, runner, Reveal(runner, world));
        Assert.All(new[] { oneFirst, oneSecond }, card => Assert.Equal(PlayArea.Of(1), card.Area.PlayArea));
        Assert.All(new[] { zeroFirst, zeroSecond }, card => Assert.Equal(PlayArea.Of(0), card.Area.PlayArea));
        Assert.All(new[] { oneFirst, oneSecond, zeroFirst, zeroSecond }, card =>
            Assert.Equal(DeckType.EngagedEnemiesArea, card.Area.Type));
    }

    [Fact]
    public void ReceiptSnapshotSurvivesNestedChoiceAndSeparateDiscard()
    {
        // Synthetic composition stresses the receipt owner; the nested discard is not 02028 text.
        var body = "{\"seq\":[{\"choose\":{\"options\":[{\"placeCounters\":{\"card\":\"you\",\"counter\":\"one\",\"count\":1}},{\"placeCounters\":{\"card\":\"you\",\"counter\":\"two\",\"count\":1}}]}},"
            + Enter + ",{\"discardTop\":{\"from\":\"encounterDeck\",\"count\":1}}]}";
        var runner = Runner(Traversal("forEachDiscarded", 2, body));
        var world = Board(runner);
        var deck = world.AreaOf(DeckType.EncounterDeck);
        world.CreateCard("01101", deck);
        var outsideSecond = world.CreateCard("02024", deck);
        var outsideFirst = world.CreateCard("02024", deck);
        var second = world.CreateCard("02024", deck);
        var first = world.CreateCard("02024", deck);
        var events = Reveal(runner, world);
        for (int index = 0; index < 2; index++)
        {
            var prompt = Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, runner, events));
            Assert.Equal(1, prompt.Player);
            Sequence.Answer(world, world.Facts, runner, prompt, Decision.Take(prompt.Affordances[1].Id), events);
        }
        Sequence.Finish(world, world.Facts, runner, events);
        Assert.All(new[] { first, second }, card => Assert.Equal(DeckType.EngagedEnemiesArea, card.Area.Type));
        Assert.All(new[] { outsideFirst, outsideSecond }, card => Assert.Equal(DeckType.EncounterDiscardPile, card.Area.Type));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void WickedAmbitionsChoosesSeparatelyForExactCopiesAndSkipsOtherCards(bool reset)
    {
        // 02032: each discarded Goblin minion gives "take 3 damage or put that minion into play".
        // This test-local stage-II shape uses X=4 (double the printed stage number).
        var body = "{\"choose\":{\"options\":[{\"dealDamage\":{\"cards\":\"you\",\"amount\":3}}," + Enter + "]}}";
        var runner = Runner(Traversal("eachTime", 4, body));
        var world = Board(runner);
        var deck = world.AreaOf(DeckType.EncounterDeck);
        if (!reset) world.CreateCard("01101", deck);
        var second = world.CreateCard("02024", deck);
        var nongoblin = world.CreateCard("01101", deck);
        var nonminion = world.CreateCard("02014", deck);
        var first = world.CreateCard("02024", deck);
        var events = Reveal(runner, world);
        var firstPrompt = Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, runner, events));
        Assert.Equal(1, firstPrompt.Player);
        Assert.Equal(0, world.Seats[1].IdentityCard.Damage);
        Assert.Same(deck, second.Area);
        Sequence.Answer(world, world.Facts, runner, firstPrompt, Decision.Take(firstPrompt.Affordances[0].Id), events);
        var secondPrompt = Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, runner, events));
        Assert.Equal(1, secondPrompt.Player);
        Assert.Equal(3, world.Seats[1].IdentityCard.Damage);
        Assert.NotEqual(DeckType.EngagedEnemiesArea, first.Area.Type);
        Sequence.Answer(world, world.Facts, runner, secondPrompt, Decision.Take(secondPrompt.Affordances[1].Id), events);
        Sequence.Finish(world, world.Facts, runner, events);
        Assert.Equal(DeckType.EngagedEnemiesArea, second.Area.Type);
        Assert.Equal(PlayArea.Of(1), second.Area.PlayArea);
        Assert.All(new[] { first, nongoblin, nonminion }, card => Assert.NotEqual(DeckType.EngagedEnemiesArea, card.Area.Type));
        Assert.Equal(0, world.Seats[0].IdentityCard.Damage);
        Assert.False(world.Seats[1].IdentityCard.Tokens.ContainsKey("c_revealed"));
    }

    private static string Traversal(string operation, int count, string then) =>
        "{\"" + operation + "\":{\"effect\":{\"discardTop\":{\"from\":\"encounterDeck\",\"count\":"
        + count + "}},\"when\":" + Goblin + ",\"then\":" + then + "}}";

    private static List<GameEvent> Reveal(AbilityRunner runner, World world) =>
        runner.ExecuteReveal(world, world.CreateCard("02032", world.AreaOf(DeckType.RevealingArea)), 1).Events.ToList();

    private static AbilityRunner Runner(string effect) => new(AbilityCatalog.Parse($$$$"""
        {"cards":[{"card":"02032","abilities":[{
          "trigger":{"event":"WhenCardRevealed","timing":"WhenRevealed","subject":"this"},
          "effect":{{{{effect}}}}}]},
          {"card":"02024","abilities":[{
            "trigger":{"event":"WhenCardRevealed","timing":"WhenRevealed","subject":"this"},
            "effect":{"placeCounters":{"card":"you","counter":"revealed","count":1}}}]}]}
        """));

    private static World Board(AbilityRunner runner)
    {
        var world = new World(CardCatalogData, players: 2) { Abilities = runner };
        for (int player = 0; player < 2; player++)
        {
            var seat = world.CreateSeat($"p{player}");
            seat.IdentityCard = world.CreateCard("01001a,01001b", seat.Hero);
        }
        world.CreateCard("01107", world.AreaOf(DeckType.MainSchemesArea));
        return world;
    }
}

