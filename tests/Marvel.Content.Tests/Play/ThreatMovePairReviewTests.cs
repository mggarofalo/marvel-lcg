using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class ThreatMovePairReviewTests : CoreCardTestBase
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [Rule("rr:first-player.1")]
    [Rule("rr:you-your.2")]
    public void FirstPlayerPairAnswerPreservesRevealingPlayersYouAndQueryPurity(int answer)
    {
        // The first player "selects among the eligible options"; damage to "you"
        // applies to "the hit point dial of their identity".
        Assert.Equal(Resolve(answer, false), Resolve(answer, true));
    }

    private static (string Digest, long Words) Resolve(int answer, bool probe)
    {
        var (world, runner, from, to, source) = Board();
        runner.ExecuteReveal(world, source, 0);
        var prompt = Assert.IsType<Prompt>(Sequence.Work(world, Cards, runner, []));
        Assert.Equal(1, prompt.Player);
        Assert.Equal(2, prompt.Affordances.Count);
        Assert.Equal(2, from.Tokens["k_threat"]);
        Assert.Equal(2, to.Tokens["k_threat"]);
        Assert.Equal(0, world.Seats[0].IdentityCard.Damage);
        var before = (world.Digest().Canonical(), world.Random.Generator.WordsConsumed,
            world.Agenda.Outstanding.ToArray());
        if (probe)
        {
            var waiting = AbilityContinuationFixtures.Pending(world, source);
            var repeated = Assert.IsType<Prompt>(runner.Choosing(world, waiting));
            Assert.Equal(prompt.Player, repeated.Player);
            Assert.Equal(prompt.Affordances.Select(item => (item.Id, item.Description)),
                repeated.Affordances.Select(item => (item.Id, item.Description)));
        }
        Assert.Equal(before.Item1, world.Digest().Canonical());
        Assert.Equal(before.Item2, world.Random.Generator.WordsConsumed);
        Assert.Equal(before.Item3, world.Agenda.Outstanding);
        Sequence.Answer(world, Cards, runner, prompt, Decision.Take(answer), []);
        Assert.Null(Sequence.Work(world, Cards, runner, []));
        Assert.Equal(1, world.Seats[0].IdentityCard.Damage);
        Assert.Equal(0, world.Seats[1].IdentityCard.Damage);
        Assert.Equal(answer == 0 ? 0 : 4, from.Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(answer == 0 ? 4 : 0, to.Tokens.GetValueOrDefault("k_threat"));
        return (world.Digest().Canonical(), world.Random.Generator.WordsConsumed);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:leaves-play.1")]
    public void EitherReenteredEndpointRejectsAnswerWithoutMovingThreat(bool replaceDestination)
    {
        // A card leaving play and returning is "a new instance of that card".
        var (world, runner, from, to, source) = Board();
        runner.ExecuteReveal(world, source, 0);
        var prompt = Assert.IsType<Prompt>(Sequence.Work(world, Cards, runner, []));
        var replaced = replaceDestination ? to : from;
        World.MoveToTop(replaced, world.AreaOf(DeckType.EncounterDiscardPile));
        World.MoveToTop(replaced, world.AreaOf(DeckType.SideSchemesArea));
        replaced.PlaceTokens("k_threat", 2);
        string before = world.Digest().Canonical();
        Assert.Throws<ArgumentException>(() => Sequence.Answer(world, Cards, runner,
            prompt, Decision.Take(0), []));
        Assert.Equal(before, world.Digest().Canonical());
        Assert.Equal(0, world.Seats[0].IdentityCard.Damage);
    }

    private static (World, AbilityRunner, Card, Card, Card) Board()
    {
        // Synthetic encounter text tests continuation ownership without admitting content.
        var runner = new AbilityRunner(AbilityCatalog.Parse("""
            {"cards":[{"card":"01168","abilities":[{
              "trigger":{"event":"WhenCardRevealed","subject":"this","timing":"WhenRevealed"},
              "effect":{"seq":[{"moveThreat":{
                "from":{"minBy":{"of":{"query":"sideSchemes"},"by":"threat"}},
                "to":{"maxBy":{"of":{"query":"sideSchemes"},"by":"threat"}},"amount":"all"}},
                {"dealDamage":{"cards":"you","amount":1}}]}
            }]}]}
            """));
        var world = Hero("01001a", 2);
        world.FirstPlayer = 1;
        world.Abilities = runner;
        var from = world.CreateCard("01107", world.AreaOf(DeckType.SideSchemesArea));
        var to = world.CreateCard("01108", world.AreaOf(DeckType.SideSchemesArea));
        from.PlaceTokens("k_threat", 2);
        to.PlaceTokens("k_threat", 2);
        var source = world.CreateCard("01168", world.AreaOf(DeckType.RevealingArea));
        return (world, runner, from, to, source);
    }
}
