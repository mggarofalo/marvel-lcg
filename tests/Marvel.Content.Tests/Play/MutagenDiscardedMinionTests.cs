using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>Test-local Mutagen shapes; these do not admit the scenario.</summary>
public sealed class MutagenDiscardedMinionTests
{
    [Theory]
    [InlineData(false, "02024", "02025")]
    [InlineData(true, "02024", "02025")]
    [InlineData(false, "02024", "02024")]
    [InlineData(true, "02024", "02024")]
    [Rule("rr:encounter-deck.1")]
    [Rule("rr:when-revealed-abilities.2")]
    public void FirstPrintedGoblinMinionUsesDiscardOrderAndExactCopy(bool reset, string firstFace, string laterFace)
    {
        // 02017b: "put the first Goblin minion they discarded this way into play".
        // rr:when-revealed-abilities.2: When Revealed "does not resolve" on put into play.
        var runner = Runner("""
            {"seq":[{"discardTop":{"from":"encounterDeck","count":4}},
              {"recruitDiscarded":{"trait":"GOBLIN"}}]}
            """);
        var world = Board(runner);
        var deck = world.AreaOf(DeckType.EncounterDeck);
        var outsider = world.CreateCard(firstFace, world.AreaOf(DeckType.EncounterDiscardPile));
        if (!reset) world.CreateCard("01101", deck);
        var later = world.CreateCard(laterFace, deck);
        var first = world.CreateCard(firstFace, deck);
        var nongoblin = world.CreateCard("01101", deck);
        var nonminion = world.CreateCard("02014", deck);
        Execute(runner, world);
        Assert.Equal(DeckType.EngagedEnemiesArea, first.Area.Type);
        Assert.Equal(PlayArea.Of(1), first.Area.PlayArea);
        Assert.All(new[] { outsider, later, nongoblin, nonminion }, card =>
            Assert.NotEqual(DeckType.EngagedEnemiesArea, card.Area.Type));
        Assert.Empty(world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)).Cards);
        Assert.False(world.Seats[1].IdentityCard.Tokens.ContainsKey("c_revealed"));
        Assert.Equal(reset ? 1 : 0, world.TheCardIn(DeckType.MainSchemesArea)!.Tokens
            .GetValueOrDefault(EncounterDeck.AccelerationToken));
    }

    [Theory]
    [InlineData("02014")]
    [InlineData("01101")]
    public void OutsideReceiptAndWrongKindOrTraitDoNotBecomeMatches(string discardedFace)
    {
        var runner = Runner("""
            {"seq":[{"discardTop":{"from":"encounterDeck","count":1}},
              {"recruitDiscarded":{"trait":"GOBLIN"}}]}
            """);
        var world = Board(runner);
        var deck = world.AreaOf(DeckType.EncounterDeck);
        var outsideDeck = world.CreateCard("02024", deck);
        var outsideDiscard = world.CreateCard("02024", world.AreaOf(DeckType.EncounterDiscardPile));
        world.CreateCard(discardedFace, deck);
        Execute(runner, world);
        Assert.Empty(world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1)).Cards);
        Assert.Same(deck, outsideDeck.Area);
        Assert.Equal(DeckType.EncounterDiscardPile, outsideDiscard.Area.Type);
    }

    [Fact]
    public void FirstReceiptMatchSurvivesNestedSuspensionAndDoesNotRevealMonster()
    {
        var runner = Runner("""
            {"seq":[{"discardTop":{"from":"encounterDeck","count":2}},
              {"choose":{"options":[
                {"choose":{"options":[{"exhaust":"you"},{"exhaust":"this"}]}},
                {"exhaust":"this"}]}},
              {"recruitDiscarded":{"trait":"GOBLIN"}}]}
            """);
        var world = Board(runner);
        var deck = world.AreaOf(DeckType.EncounterDeck);
        var later = world.CreateCard("02024", deck);
        var first = world.CreateCard("02025", deck);
        runner.ExecuteReveal(world, world.CreateCard("02032", world.AreaOf(DeckType.RevealingArea)), 1);
        for (int choice = 0; choice < 2; choice++)
        {
            var prompt = Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, runner, []));
            Assert.Equal(1, prompt.Player);
            Sequence.Answer(world, world.Facts, runner, prompt, Decision.Take(prompt.Affordances[0].Id), []);
        }
        Sequence.Finish(world, world.Facts, runner, []);
        Assert.Equal(DeckType.EngagedEnemiesArea, first.Area.Type);
        Assert.Equal(PlayArea.Of(1), first.Area.PlayArea);
        Assert.NotEqual(DeckType.EngagedEnemiesArea, later.Area.Type);
        Assert.False(world.Seats[1].IdentityCard.Tokens.ContainsKey("c_revealed"));
    }

    [Fact]
    public void PlayerOrderedMillsKeepEachPlayersPhysicalFirstMatch()
    {
        // 02017b orders each player's discard and entry in player order.
        var runner = Runner("""
            {"eachPlayer":{"order":"playerOrder","effect":{"seq":[
              {"discardTop":{"from":"encounterDeck","count":2}},
              {"recruitDiscarded":{"trait":"GOBLIN"}}]}}}
            """);
        var world = Board(runner);
        world.FirstPlayer = 1;
        var deck = world.AreaOf(DeckType.EncounterDeck);
        var laterForZero = world.CreateCard("02024", deck);
        var firstForZero = world.CreateCard("02025", deck);
        var laterForOne = world.CreateCard("02024", deck);
        var firstForOne = world.CreateCard("02023", deck);
        Execute(runner, world);
        Assert.Equal(PlayArea.Of(1), firstForOne.Area.PlayArea);
        Assert.Equal(PlayArea.Of(0), firstForZero.Area.PlayArea);
        Assert.Equal(DeckType.EngagedEnemiesArea, firstForOne.Area.Type);
        Assert.Equal(DeckType.EngagedEnemiesArea, firstForZero.Area.Type);
        Assert.NotEqual(DeckType.EngagedEnemiesArea, laterForZero.Area.Type);
        Assert.NotEqual(DeckType.EngagedEnemiesArea, laterForOne.Area.Type);
        Assert.False(world.Seats[0].IdentityCard.Tokens.ContainsKey("c_revealed"));
    }

    [Theory]
    [InlineData("{\"trait\":\"GOBLIN\",\"all\":1}")]
    [InlineData("{\"kind\":\"Minion\"}")]
    [InlineData("{\"trait\":1}")]
    public void RecruitmentRejectsUnimplementedOrMalformedSelection(string selection)
    {
        Assert.Throws<AbilityException>(() => Runner("{\"recruitDiscarded\":" + selection + "}"));
    }

    private static void Execute(AbilityRunner runner, World world)
    {
        runner.ExecuteReveal(world, world.CreateCard("02032", world.AreaOf(DeckType.RevealingArea)), 1);
        Sequence.Finish(world, world.Facts, runner, []);
    }

    private static AbilityRunner Runner(string effect) => new(AbilityCatalog.Parse($$$$"""
        {"cards":[{"card":"02032","abilities":[{
          "trigger":{"event":"WhenCardRevealed","timing":"WhenRevealed","subject":"this"},
          "effect":{{{{effect}}}}}]},
          {"card":"02025","abilities":[{
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
