using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;
namespace Marvel.Content.Tests.Play;

public sealed class ThreatMovePairChoiceTests : CoreCardTestBase
{
    [Theory]
    [InlineData(1, 1, 2)]
    [InlineData(1, 3, 0)]
    [InlineData(0, 0, 0)]
    [Rule("rr:move.1")]
    [Rule("rr:move.2")]
    public void RankedPoolsOfferEveryDistinctPairOrCompleteWithoutAChoice(int first, int second, int choices)
    {
        // An element "cannot move to its same (current) placement"; without
        // a valid source or destination "the move cannot be made".
        var runner = Runner();
        var world = Hero("01001a", 2);
        world.FirstPlayer = 1;
        world.Abilities = runner;
        var main = world.CreateCard("01097b", world.AreaOf(DeckType.MainSchemesArea));
        var from = world.CreateCard("01107", world.AreaOf(DeckType.SideSchemesArea));
        var to = world.CreateCard("01108", world.AreaOf(DeckType.SideSchemesArea));
        from.PlaceTokens("k_threat", first);
        to.PlaceTokens("k_threat", second);
        var source = world.CreateCard("01168", world.AreaOf(DeckType.RevealingArea));
        runner.ExecuteReveal(world, source, 0);
        var prompt = Sequence.Work(world, Cards, runner, []);
        if (choices > 0)
        {
            Assert.NotNull(prompt);
            Assert.Equal(choices, prompt.Affordances.Count);
            Assert.Equal(1, prompt.Player);
            Assert.False(prompt.Cancellable);
            Assert.Contains("Breakin' & Takin'", prompt.Affordances[0].Description!);
            Assert.Contains("Crowd Control", prompt.Affordances[0].Description!);
            Sequence.Answer(world, Cards, runner, prompt, Decision.Take(0), []);
            Assert.Null(Sequence.Work(world, Cards, runner, []));
        }
        else Assert.Null(prompt);
        Assert.Equal(1, main.Tokens["k_threat"]);
        Assert.Equal(0, from.Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(first + second, to.Tokens.GetValueOrDefault("k_threat"));
    }

    [Fact]
    [Rule("rr:leaves-play.1")]
    public void ReenteredEndpointCannotAnswerThePersistedPair()
    {
        // A card that leaves play and reenters is "a new instance of that card".
        var runner = Runner();
        var world = Hero("01001a", 2);
        world.FirstPlayer = 1;
        world.Abilities = runner;
        world.CreateCard("01097b", world.AreaOf(DeckType.MainSchemesArea));
        var from = world.CreateCard("01107", world.AreaOf(DeckType.SideSchemesArea));
        var to = world.CreateCard("01108", world.AreaOf(DeckType.SideSchemesArea));
        from.PlaceTokens("k_threat", 2);
        to.PlaceTokens("k_threat", 2);
        var source = world.CreateCard("01168", world.AreaOf(DeckType.RevealingArea));
        runner.ExecuteReveal(world, source, 0);
        var prompt = Assert.IsType<Prompt>(Sequence.Work(world, Cards, runner, []));
        World.MoveToTop(from, world.AreaOf(DeckType.EncounterDiscardPile));
        World.MoveToTop(from, world.AreaOf(DeckType.SideSchemesArea));
        Assert.Throws<ArgumentException>(() => Sequence.Answer(world, Cards, runner, prompt, Decision.Take(0), []));
        Assert.Equal(2, to.Tokens["k_threat"]);
    }

    private static AbilityRunner Runner() => new(AbilityCatalog.Parse("""
        {"cards":[{"card":"01168","abilities":[{
          "trigger":{"event":"WhenCardRevealed","subject":"this","timing":"WhenRevealed"},
          "effect":{"seq":[{"moveThreat":{
            "from":{"minBy":{"of":{"query":"sideSchemes"},"by":"threat"}},
            "to":{"maxBy":{"of":{"query":"sideSchemes"},"by":"threat"}},"amount":"all"}},
            {"placeThreat":{"scheme":{"query":"mainScheme"},"amount":1}}]}
        }]}]}
        """));
}
