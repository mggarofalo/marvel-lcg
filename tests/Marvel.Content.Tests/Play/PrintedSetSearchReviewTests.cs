using System.Collections.Immutable;
using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Xunit;
using static Marvel.Content.Tests.Play.PrintedSetSearchFixture;

namespace Marvel.Content.Tests.Play;

// Independent component review of the selector; no production card admission.
public sealed class PrintedSetSearchReviewTests
{
    [Fact]
    public void LiveSingularAndProjectedQueriesKeepSetAndOtherPrintedCriteria()
    {
        var world = Board();
        var source = world.CreateCard("09006", world.Seats[1].Hand);
        var match = world.CreateCard("09007", Discard(world));
        world.CreateCard("09006", world.Seats[1].Deck);
        world.CreateCard("09012", world.Seats[1].Deck);
        world.CreateCard("01005", Discard(world));
        world.CreateCard("09007", Discard(world, 0));
        var selection = new AbilityCardSelection.InAreas(
            [AbilitySearchArea.YourDeck, AbilitySearchArea.YourDiscardPile],
            CardKind.Event, "SPELL", "Protective Ward")
        { Set = "doctor_strange", CardClass = "Hero" };
        var context = new AbilityQueryContext(world, source, new Occurrence(1, []),
            1, source.Incarnation, null, null, null, []);
        var evaluator = new AbilitySelectorEvaluation(context);
        var expressions = new AbilityExpressionContext(context,
            ImmutableDictionary<string, long>.Empty, [], "", 0, false, null);
        var admission = new AbilityAdmissionContext(
            AbilityLowering.Book(AbilityCatalog.Parse("{\"cards\":[]}")),
            world.ResourceAbilities, expressions, new(), null);
        var projected = new AreaProjectionState(admission);
        string before = world.Digest().Canonical();
        long random = world.Random.Generator.WordsConsumed;

        Assert.Equal([match], evaluator.Every(selection));
        Assert.Same(match, evaluator.Find(selection));
        Assert.Equal([match], AbilityProjectedStateQueries.ProjectedEvery(selection, projected, admission));
        Assert.Same(match, AbilityProjectedStateQueries.ProjectedFind(selection, projected, admission));
        var wrongCase = selection with { Set = "Doctor_Strange" };
        Assert.Empty(evaluator.Every(wrongCase));
        Assert.Null(evaluator.Find(wrongCase));
        Assert.Empty(AbilityProjectedStateQueries.ProjectedEvery(wrongCase, projected, admission));
        Assert.Null(AbilityProjectedStateQueries.ProjectedFind(wrongCase, projected, admission));
        projected.Departed.Add(match.ObjectId);
        Assert.Empty(AbilityProjectedStateQueries.ProjectedEvery(selection, projected, admission));
        Assert.Same(match, evaluator.Find(selection));
        Assert.Equal(before, world.Digest().Canonical());
        Assert.Equal(random, world.Random.Generator.WordsConsumed);
        Assert.Empty(world.Agenda.Outstanding);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void SearchDescriptionDoesNotRevealHiddenMatchCountOrChangeState(int matches)
    {
        var world = Board();
        var source = world.CreateCard("09006", world.Seats[1].Hand);
        world.CreateCard("01093", world.Seats[1].Hand);
        for (int i = 0; i < matches; i++) world.CreateCard("09007", world.Seats[1].Deck);
        var runner = world.Abilities!;
        string before = world.Digest().Canonical();
        var random = world.Random.Generator.GetState();
        long words = world.Random.Generator.WordsConsumed;

        var action = Assert.Single(runner.Actions(world, 1), action => action.Card == source.ObjectId);
        var offer = runner.Describe(world, action);

        // Wording and privacy of this pre-search offer are engine choices.
        Assert.Equal("Search your deck and discard pile for a card from the Doctor Strange set and add it to your hand",
            offer.DisplayLabel);
        Assert.Equal(offer.DisplayLabel, offer.CommitLabel);
        Assert.Contains("Choose a matching card if available, then shuffle your deck", offer.Description);
        Assert.Null(offer.Targets);
        Assert.Equal(before, world.Digest().Canonical());
        Assert.Equal(random.Index, world.Random.Generator.GetState().Index);
        Assert.Equal(random.Words, world.Random.Generator.GetState().Words);
        Assert.Equal(words, world.Random.Generator.WordsConsumed);
        Assert.Empty(world.Agenda.Outstanding);
    }

    [Theory]
    [InlineData("", false)]
    [InlineData(",\"set\":\"Doctor_Strange\"", false)]
    [InlineData(",\"set\":\"doctor_strange \"", false)]
    [InlineData(",\"set\":\"doctor_strange\"", true)]
    public void ConditionalSearchDescriptionRequiresTheExactSameSetRestriction(string guardSet, bool described)
    {
        var world = Board(Runner(Areas + ",\"set\":\"doctor_strange\"", Areas + guardSet));
        var source = world.CreateCard("09006", world.Seats[1].Hand);
        world.CreateCard("01093", world.Seats[1].Hand);
        world.CreateCard("09007", world.Seats[1].Deck);
        var runner = world.Abilities!;
        var action = Assert.Single(runner.Actions(world, 1), action => action.Card == source.ObjectId);

        string? label = runner.Describe(world, action).DisplayLabel;

        if (described) Assert.Contains("Doctor Strange set", label);
        else Assert.Null(label);
    }

    [Fact]
    public void WhitespaceSetIsRejectedAtItsExactPath()
    {
        var failure = Assert.Throws<AbilityException>(() => Runner(Areas + ",\"set\":\"\\t\\r\\n\""));
        Assert.Contains("cardsIn/set", failure.Message);
    }

    [Theory]
    [InlineData("true")]
    [InlineData("false")]
    [InlineData("null")]
    public void NonValueSetSyntaxIsRejectedByTheParser(string value)
    {
        var failure = Assert.Throws<AbilityException>(() => Runner(Areas + ",\"set\":" + value));
        Assert.Contains("where a value was expected", failure.Message);
    }

    [Fact]
    public void UnsupportedCardSetAliasIsRejectedInsteadOfSilentlyBroadeningSearch()
    {
        var failure = Assert.Throws<AbilityException>(() => Runner(Areas + ",\"cardSet\":\"doctor_strange\""));
        Assert.Contains("cardSet", failure.Message);
    }
}
