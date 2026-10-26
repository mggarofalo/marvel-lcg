using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>Test-local Power Drain shapes prove prerequisites without admitting the module.</summary>
public sealed class PowerDrainReceiptTests
{
    [Theory]
    [InlineData("01121", "01121", 0)]
    [InlineData("02042", "02041", 5)]
    [Rule("rr:encounter-deck.1")]
    [Rule("rr:encounter-deck.2")]
    public void NumericReceiptSurvivesResetAndCountsOnlyThePhysicalMill(string first, string second, int expected)
    {
        // "Do not continue the discard effect with the newly shuffled encounter deck."
        // 02041 requires one hand resource per numeric boost icon; this fixture
        // proves that quantity separately from the hand-resource prerequisite.
        var runner = RevealRunner("02041", """
            {"seq":[{"discardTop":{"from":"encounterDeck","count":7}},
              {"placeCounters":{"card":"you","counter":"observed","count":{"add":[1,{"printedBoostIconsDiscarded":"game"}]}}},
              {"placeCounters":{"card":"you","counter":"milled","count":{"cardsDiscarded":"game"}}}]}
            """);
        var world = Board(runner);
        world.CreateCard("02041", world.AreaOf(DeckType.EncounterDiscardPile));
        var deck = world.AreaOf(DeckType.EncounterDeck);
        var cards = new[] { world.CreateCard(first, deck), world.CreateCard(second, deck) };
        var source = world.CreateCard("02041", world.AreaOf(DeckType.SideSchemesArea));
        runner.ExecuteReveal(world, source, 1);
        Assert.Equal(expected + 1, world.Seats[1].IdentityCard.Tokens["c_observed"]);
        Assert.Equal(2, world.Seats[1].IdentityCard.Tokens["c_milled"]);
        Assert.Equal(3, deck.Cards.Count);
        Assert.All(cards, card => Assert.Same(deck, card.Area));
        Assert.Equal(1, world.TheCardIn(DeckType.MainSchemesArea)!.Tokens[EncounterDeck.AccelerationToken]);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:when-revealed-abilities.2")]
    public void PulseRecruitsExactMilledElectroForResolvingPlayerEvenAfterReset(bool exhaust)
    {
        // A When Revealed ability "does not resolve" when a card is put into play.
        var runner = PulseRunner();
        var world = Board(runner);
        var deck = world.AreaOf(DeckType.EncounterDeck);
        var outsider = world.CreateCard("02042", world.AreaOf(DeckType.EncounterDiscardPile));
        if (!exhaust) for (int i = 0; i < 7; i++) world.CreateCard("01101", deck);
        var electro = world.CreateCard("02042", deck);
        var source = world.CreateCard("02043", world.AreaOf(DeckType.RevealingArea));
        runner.ExecuteReveal(world, source, 1);
        Sequence.Finish(world, world.Facts, runner, []);
        Assert.Equal(DeckType.EngagedEnemiesArea, electro.Area.Type);
        Assert.Equal(PlayArea.Of(1), electro.Area.PlayArea);
        Assert.NotEqual(DeckType.EngagedEnemiesArea, outsider.Area.Type);
        Assert.Empty(world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(1)).Cards);
        Assert.Empty(world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)).Cards);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ElectroOutsidePhysicalMillDoesNotSuppressSurge(bool belowMill)
    {
        var runner = PulseRunner();
        var world = Board(runner);
        var deck = world.AreaOf(DeckType.EncounterDeck);
        var outsider = world.CreateCard("02042", belowMill ? deck : world.AreaOf(DeckType.EncounterDiscardPile));
        for (int i = 0; i < 8; i++) world.CreateCard("01101", deck);
        var source = world.CreateCard("02043", world.AreaOf(DeckType.RevealingArea));
        runner.ExecuteReveal(world, source, 1);
        Assert.Single(world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(1)).Cards);
        Assert.NotEqual(DeckType.EngagedEnemiesArea, outsider.Area.Type);
    }

    private static AbilityRunner PulseRunner() => RevealRunner("02043", """
        {"seq":[{"discardTop":{"from":"encounterDeck","count":7}},
          {"recruitDiscarded":"02042"},
          {"if":{"test":{"atLeast":{"value":{"result":"found"},"count":1}},
            "else":{"gainSurge":1}}}]}
        """);

    [Fact]
    public void ReceiptSurvivesNestedChoicesBeforeRecruitment()
    {
        var runner = RevealRunner("02043", """
            {"seq":[{"discardTop":{"from":"encounterDeck","count":1}},
              {"choose":{"options":[
                {"choose":{"options":[{"exhaust":"you"},{"exhaust":"this"}]}},
                {"exhaust":"this"}]}},
              {"recruitDiscarded":"02042"},
              {"placeCounters":{"card":"you","counter":"observed","count":{"printedBoostIconsDiscarded":"game"}}}]}
            """);
        var world = Board(runner);
        var electro = world.CreateCard("02042", world.AreaOf(DeckType.EncounterDeck));
        var source = world.CreateCard("02043", world.AreaOf(DeckType.RevealingArea));
        runner.ExecuteReveal(world, source, 1);
        for (int choice = 0; choice < 2; choice++)
        {
            var prompt = Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, runner, []));
            Assert.Equal(1, prompt.Player);
            Sequence.Answer(world, world.Facts, runner, prompt, Decision.Take(prompt.Affordances[0].Id), []);
        }
        Sequence.Finish(world, world.Facts, runner, []);
        Assert.Equal(PlayArea.Of(1), electro.Area.PlayArea);
        Assert.Equal(DeckType.EngagedEnemiesArea, electro.Area.Type);
        Assert.Equal(2, world.Seats[1].IdentityCard.Tokens["c_observed"]);
    }

    [Theory]
    [InlineData("01186", 1, "non-minion")]
    [InlineData("02042", 2, "multiple discarded copies")]
    public void UnsupportedRecruitmentRaisesInsteadOfChoosingOrMovingOtherKinds(string face, int count, string message)
    {
        var runner = RevealRunner("02043", $$$$"""
            {"seq":[{"discardTop":{"from":"encounterDeck","count":{{{{count}}}}}},
              {"recruitDiscarded":"{{{{face}}}}"}]}
            """);
        var world = Board(runner);
        for (int i = 0; i < count; i++) world.CreateCard(face, world.AreaOf(DeckType.EncounterDeck));
        var source = world.CreateCard("02043", world.AreaOf(DeckType.RevealingArea));
        var error = Assert.Throws<RulesNotImplementedException>(() => runner.ExecuteReveal(world, source, 1));
        Assert.Contains(message, error.Message, StringComparison.Ordinal);
        Assert.Empty(world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1)).Cards);
    }

    private static AbilityRunner RevealRunner(string face, string effect) => new(AbilityCatalog.Parse($$"""
        {"cards":[{"card":"{{face}}","abilities":[{
          "trigger":{"event":"WhenCardRevealed","timing":"WhenRevealed","subject":"this"},
          "effect":{{effect}}}]}]}
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




