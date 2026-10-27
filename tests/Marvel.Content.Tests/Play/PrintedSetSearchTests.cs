using System.Text.Json;
using Marvel.Cards.Dsl;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;
using static Marvel.Content.Tests.Play.PrintedSetSearchFixture;

namespace Marvel.Content.Tests.Play;

public sealed class PrintedSetSearchTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:search.1")]
    [Rule("rr:search.3")]
    public void StudiesChoosesExactOwnDeckOrDiscardSetAndShuffles(bool discard)
    {
        // "If a player finds multiple cards ... the player chooses among those options."
        // "If any portion of a deck is searched ... shuffle that entire deck."
        var world = Board();
        var seat = world.Seats[1];
        var deck = world.CreateCard("09007", seat.Deck);
        var pile = world.CreateCard("09007", Discard(world));
        var otherHero = world.CreateCard("01016", seat.Deck);
        var samePack = world.CreateCard("09012", seat.Deck);
        var foreign = world.CreateCard("09007", world.Seats[0].Deck);
        var foreignDiscard = world.CreateCard("09007", Discard(world, 0));
        var inPlay = world.CreateCard("09008", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(1)));
        Assert.Equal("Hero", world.Facts.Attributes(otherHero.FaceId)["Class"]);
        Assert.Equal("doctor_strange", world.Facts.EncounterSet(deck.FaceId));
        Assert.NotEqual("doctor_strange", world.Facts.EncounterSet(samePack.FaceId));
        var invocation = seat.InvocationDeck!;
        int[] invocationOrder = invocation.Deck.Cards.Select(c => c.ObjectId).ToArray();
        int[] invocationDiscard = invocation.Discard.Cards.Select(c => c.ObjectId).ToArray();
        PlayStudies(world);
        var prompt = Prompt(world);
        Assert.Equal(1, prompt.Player);
        Assert.Contains("card from the Doctor Strange set", prompt.DisplayQuestion);
        Assert.Contains("shuffle your deck", prompt.Description);
        Assert.Equal(new[] { deck.ObjectId, pile.ObjectId }.Order(),
            prompt.Affordances.Select(a => a.AnchorId).Order());
        string before = Snapshot(world);
        Assert.Throws<ArgumentException>(() => Answer(world, prompt, foreign.ObjectId));
        Assert.Equal(before, Snapshot(world));
        var chosen = discard ? pile : deck;
        long words = world.Random.Generator.WordsConsumed;
        Answer(world, prompt, Assert.Single(prompt.Affordances, a => a.AnchorId == chosen.ObjectId).Id);
        Finish(world);
        Assert.Same(seat.Hand, chosen.Area);
        Assert.Same(seat.Deck, otherHero.Area);
        Assert.Same(seat.Deck, samePack.Area);
        Assert.Same(world.Seats[0].Deck, foreign.Area);
        Assert.Same(Discard(world, 0), foreignDiscard.Area);
        Assert.Equal(DeckType.SupportsArea, inPlay.Area.Type);
        Assert.True(world.Random.Generator.WordsConsumed > words);
        Assert.Equal(invocationOrder, invocation.Deck.Cards.Select(c => c.ObjectId));
        Assert.Equal(invocationDiscard, invocation.Discard.Cards.Select(c => c.ObjectId));
        Assert.Empty(world.Agenda.Outstanding);
    }

    [Theory]
    [InlineData("doctor_strange")]
    [InlineData("Doctor_Strange")]
    [InlineData("doctor_strange_invocation_deck")]
    [Rule("rr:search.3")]
    public void NoExactMatchStillPaysPrintedCostAndShuffles(string set)
    {
        // "If any portion of a deck is searched ... shuffle that entire deck."
        var world = Board(set == "doctor_strange" ? null : Runner(Areas + ",\"set\":\"" + set + "\""));
        if (set != "doctor_strange") world.CreateCard("09007", world.Seats[1].Deck);
        long words = world.Random.Generator.WordsConsumed;
        PlayStudies(world);
        Finish(world);
        Assert.Empty(world.Seats[1].Hand.Cards);
        Assert.Equal(2, Discard(world).Cards.Count);
        Assert.True(world.Random.Generator.WordsConsumed > words);
        Assert.Empty(world.Agenda.Outstanding);
    }

    [Fact]
    public void TopBoundPrecedesExactSetAndOtherCriteria()
    {
        var world = Board(Runner("\"area\":\"yourDeck\",\"top\":2,\"set\":\"doctor_strange\",\"kind\":\"Event\",\"cardClass\":\"Hero\""));
        var deck = world.Seats[1].Deck;
        var below = world.CreateCard("09007", deck);
        var middle = world.CreateCard("01005", deck);
        var top = world.CreateCard("09006", deck);
        PlayStudies(world);
        var prompt = Prompt(world);
        Assert.Equal(top.ObjectId, Assert.Single(prompt.Affordances).AnchorId);
        Answer(world, prompt, Assert.Single(prompt.Affordances).Id);
        Finish(world);
        Assert.Same(world.Seats[1].Hand, top.Area);
        Assert.Same(deck, below.Area);
        Assert.Same(deck, middle.Area);
    }

    [Theory]
    [InlineData("\"\"")]
    [InlineData("\"   \"")]
    [InlineData("3")]
    
    [InlineData("[]")]
    [InlineData("{}")]
    public void MalformedSetFailsAtLoweringWithExactPath(string value)
    {
        var failure = Assert.Throws<AbilityException>(() => Runner(Areas + ",\"set\":" + value));
        Assert.Contains("cardsIn/set", failure.Message);
    }

    [Fact]
    public void OmittedSetRetainsLegacyBroadSearch()
    {
        var world = Board(Runner(Areas + ",\"cardClass\":\"Hero\""));
        var otherHero = world.CreateCard("01005", world.Seats[1].Deck);
        var strange = world.CreateCard("09007", Discard(world));
        PlayStudies(world);
        var prompt = Prompt(world);
        Assert.Equal(new[] { otherHero.ObjectId, strange.ObjectId }.Order(),
            prompt.Affordances.Select(a => a.AnchorId).Order());
        Answer(world, prompt, Assert.Single(prompt.Affordances, a => a.AnchorId == otherHero.ObjectId).Id);
        Finish(world);
        Assert.Same(world.Seats[1].Hand, otherHero.Area);
    }

    [Theory]
    [InlineData("doctor_strange", true)]
    [InlineData("spider_man", false)]
    public void SearchDescriptionRequiresIdenticalPrintedSetGuard(string guard, bool described)
    {
        var world = Board(Runner(Areas + ",\"set\":\"doctor_strange\"", Areas + ",\"set\":\"" + guard + "\""));
        var source = world.CreateCard("09006", world.Seats[1].Hand);
        world.CreateCard("01093", world.Seats[1].Hand);
        var runner = world.Abilities!;
        var action = Assert.Single(runner.Actions(world, 1), a => a.Card == source.ObjectId);
        string? label = runner.Describe(world, action).DisplayLabel;
        if (described) Assert.Equal("Search your deck and discard pile for a card from the Doctor Strange set and add it to your hand", label);
        else Assert.Null(label);
    }

    private static string Snapshot(World world) => JsonSerializer.Serialize(new
    {
        Digest = world.Digest().Canonical(),
        Random = world.Random.Generator.GetState(), world.Random.Generator.WordsConsumed,
        world.Agenda.Stage,
        Frames = world.Agenda.Outstanding.Select(step => new { step.What, step.Index, step.Subject }),
    });
}
