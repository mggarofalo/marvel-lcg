using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class WreckingActiveCounterExecutionTests
{
    [Rule("rr:then.1")]
    [Rule("rr:then.2")]
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DependentTextRequiresAnActualCounterMove(bool move)
    {
        // Synthetic dependency: rr:then.1 says the follow-up "must also attempt
        // to resolve" after full resolution; rr:then.2 says it "does not attempt
        // to resolve" otherwise.
        var (world, groups, _) = WreckingProjectionFixture.Create();
        var source = world.CreateCard("07014", world.AreaOf(DeckType.RevealingArea));
        var runner = new AbilityRunner(AbilityCatalog.Parse("""
        {"cards":[{"card":"07014","abilities":[{
          "trigger":{"event":"WhenCardRevealed","timing":"WhenRevealed","subject":"this"},
          "effect":{"then":{"effect":{"moveActiveCounter":TARGET},
            "then":{"giveStatus":{"card":"you","status":"tough"}}}}
        }]}]}
        """.Replace("TARGET", move ? """{"titled":"Bulldozer"}""" : """{"query":"villain"}""",
            StringComparison.Ordinal)));

        runner.ExecuteReveal(world, source, 0);
        Sequence.Finish(world, world.Facts, runner, []);

        Assert.Equal(move, Statuses.Has(world, world.Seats[0].IdentityCard, Statuses.Tough));
        Assert.Same(groups[move ? 3 : 1], world.EncounterTopology!.Active);
    }

    [Rule("rr:move.1")]
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RankedMovementExcludesTheCurrentHolderAfterRanking(bool tied)
    {
        // Synthetic encounter instruction isolates movement. rr:move.1:
        // "it cannot move to its same (current) placement."
        Assert.Equal(Resolve(tied, false), Resolve(tied, true));
    }

    private static (string Digest, long Words) Resolve(bool tied, bool probe)
    {
        var (world, groups, _) = WreckingProjectionFixture.Create();
        groups[0].Scheme.PlaceTokens("k_threat", tied ? 0 : 2);
        groups[2].Scheme.PlaceTokens("k_threat", tied ? 0 : 3);
        groups[3].Scheme.PlaceTokens("k_threat", 4);
        var source = world.CreateCard("07014", world.AreaOf(DeckType.RevealingArea));
        var runner = new AbilityRunner(AbilityCatalog.Parse("""
        {"cards":[{"card":"07014","abilities":[{
          "trigger":{"event":"WhenCardRevealed","timing":"WhenRevealed","subject":"this"},
          "effect":{"seq":[
            {"chooseCard":{"chooser":"firstPlayer","from":{"query":"villainsWithLeastSchemeThreat"},
              "effect":{"moveActiveCounter":"chosen"}}},
            {"giveStatus":{"card":{"query":"villain"},"status":"tough"}}
          ]}
        }]}]}
        """));
        List<GameEvent> events = [];
        runner.ExecuteReveal(world, source, 0);
        var before = (world.Digest().Canonical(), world.Random.Generator.WordsConsumed,
            world.Agenda.Outstanding.ToArray());
        if (probe && tied)
        {
            var waiting = AbilityContinuationFixtures.Pending(world, source);
            _ = runner.Choosing(world, waiting);
            Assert.Equal(before.Item1, world.Digest().Canonical());
            Assert.Equal(before.Item2, world.Random.Generator.WordsConsumed);
            Assert.Equal(before.Item3, world.Agenda.Outstanding);
        }
        var prompt = Sequence.Work(world, world.Facts, runner, events);
        if (tied)
        {
            var choice = Assert.IsType<Prompt>(prompt);
            Assert.Contains("villain whose side scheme has the least threat", choice.Description);
            Assert.Contains("move the active counter", choice.Description);
            Assert.Equal(new[] { groups[0].Villain.ObjectId, groups[2].Villain.ObjectId },
                choice.Affordances.Select(option => option.Id));
            Assert.Equal(world.FirstPlayer, choice.Player);
            Sequence.Answer(world, world.Facts, runner, choice,
                Decision.Take(groups[2].Villain.ObjectId), events);
            Sequence.Finish(world, world.Facts, runner, events);
        }
        else Assert.Null(prompt);
        var expected = groups[tied ? 2 : 1];
        Assert.Same(expected, world.EncounterTopology!.Active);
        Assert.True(Statuses.Has(world, expected.Villain, Statuses.Tough));
        Assert.False(Statuses.Has(world, groups[0].Villain, Statuses.Tough));
        return (world.Digest().Canonical(), world.Random.Generator.WordsConsumed);
    }
}
