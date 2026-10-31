using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Cards;

// The classed hand-discard cost and deck-top storage, from lowering to admission.
public sealed class KangObligationLoweringTests
{
    [Fact]
    public void AClassedHandDiscardRefusesAnUnknownClass()
    {
        var failure = Assert.Throws<AbilityException>(() =>
            AbilityLowering.Book(Action("""{"discardClassFromHand":"Villain"}""")));
        Assert.Contains("not a card class", failure.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AClassedHandDiscardLowersExactlyOneCardOfThatClass()
    {
        var program = AbilityLowering.Book(Action("""{"discardClassFromHand":"Hero"}"""));
        var discard = Assert.IsType<AbilityCost.DiscardFromHand>(Assert.Single(program.On("11019")).Cost);
        Assert.Equal(new AbilityCostRange.Exact(1), discard.Range);
        Assert.Equal(new AbilityHandFilter.PrintedClass("Hero"), discard.Filter);
    }

    [Theory]
    [InlineData("""{"count":0,"host":"this"}""", "positive integer")]
    [InlineData("""{"count":8}""", "host")]
    [InlineData("""{"count":8,"host":"this","faceUp":"true"}""", "faceUp")]
    public void DeckTopStorageRefusesMalformedFields(string store, string says)
    {
        var source = AbilityCatalog.Parse($$$"""
            {"cards":[{"card":"11019","abilities":[{
              "trigger":{"event":"WhenCardRevealed","timing":"WhenRevealed","subject":"this"},
              "effect":{"storeDeckTopBeneath":{{{store}}}}}]}]}
            """);
        var failure = Assert.Throws<AbilityException>(() => AbilityLowering.Book(source));
        Assert.Contains(says, failure.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("11019", "Discard a card with a printed mental resource from your hand")]
    [InlineData("11020", "Discard a hero-specific card from your hand")]
    [InlineData("11021", "Discard a card with a printed energy resource from your hand")]
    public void TheAuthoredCostsDescribeTheirPrintedFilter(string face, string described)
    {
        // The player must understand which cards pay before committing.
        var world = new World(CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json"))), 1);
        world.CreateSeat("p0").IdentityCard = world.CreateCard("01001a,01001b", world.Seats[0].Hero);
        var card = world.CreateCard(face, world.AreaOf(DeckType.ObligationsArea, PlayArea.Of(0)));
        var cost = Assert.Single(AuthoredCards.Program.On(face), ability => ability.Cost is not null).Cost;
        Assert.Equal(described, AbilityCostDescription.Summary(world, card, 0, cost));
    }

    [Fact]
    [Rule("rr:choose-option.1")]
    public void DeckTopStorageUnderAHostOutOfPlayIsNotAnOption()
    {
        // Synthetic row: a treachery still being revealed is not in play, so it
        // cannot hold stored cards and that option is not offered.
        var runner = new AbilityRunner(AbilityCatalog.Parse("""
            {"cards":[{"card":"01104","abilities":[{
              "trigger":{"event":"WhenCardRevealed","timing":"WhenRevealed","subject":"this"},
              "effect":{"choose":{"options":[
                {"storeDeckTopBeneath":{"count":1,"host":"this"}},
                {"placeThreat":{"scheme":{"query":"mainScheme"},"amount":1}}]}}}]}]}
            """));
        var (world, f) = Marvel.Content.Tests.Play.KangTemporalFixture.Board(players: 1, encounter: 0);
        world.Abilities = runner;
        f = new Marvel.Content.Tests.Play.RedSkullFixture(world, runner);
        int deck = world.Seats[0].Deck.Cards.Count;
        var offered = new List<int>();

        f.Reveal("01104");
        f.Finish(prompt =>
        {
            offered.Add(prompt.Affordances.Count);
            return Decision.Take(prompt.Affordances[0].Id);
        });

        Assert.All(offered, count => Assert.Equal(1, count));
        Assert.Equal(1, f.Main.Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(deck, world.Seats[0].Deck.Cards.Count);
    }

    private static AbilityBook Action(string cost) => AbilityCatalog.Parse($$$"""
        {"cards":[{"card":"11019","abilities":[{
          "trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"this","form":"alter-ego"},
          "cost":{{{cost}}},
          "effect":{"discard":"this"}}]}]}
        """);
}
