using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>Synthetic programs review set-discard continuation and admission.</summary>
public sealed class EncounterSetContinuationReviewTests : CoreCardTestBase
{
    [Rule("rr:reveal.3")]
    [Fact]
    public void SetMatchEntersEngagedWithTheRevealingPlayer()
    {
        // rr:reveal.3: a minion enters the play area of the player revealing it.
        var world = Hero("01001a", players: 2);
        var source = world.CreateCard("12029", world.AreaOf(DeckType.RevealingArea));
        var deck = world.AreaOf(DeckType.EncounterDeck);
        var below = world.CreateCard("01121", deck);
        var match = world.CreateCard("12027", deck);
        var runner = Runner("{\"discardUntil\":{\"from\":\"encounterDeck\",\"set\":\"ant_nemesis\",\"then\":\"reveal\"}}");

        runner.ExecuteReveal(world, source, 1);
        Sequence.Finish(world, world.Facts, runner, []);

        Assert.Same(world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1)), match.Area);
        Assert.Equal([below], deck.Cards);
        Assert.Empty(world.Agenda.Outstanding);
    }

    [Fact]
    public void DependentSetDiscardRefusesBeforeChangingState()
    {
        var world = Hero("01001a");
        var source = world.CreateCard("12029", world.AreaOf(DeckType.RevealingArea));
        world.CreateCard("12027", world.AreaOf(DeckType.EncounterDeck));
        var runner = Runner("{\"then\":{\"effect\":{\"discardUntil\":{\"from\":\"encounterDeck\",\"set\":\"ant_nemesis\",\"then\":\"reveal\"}},\"then\":{\"giveStatus\":{\"card\":\"you\",\"status\":\"tough\"}}}}");
        var before = (world.Digest().Canonical(), world.Random.Generator.WordsConsumed);

        Assert.Throws<RulesNotImplementedException>(() => runner.ExecuteReveal(world, source, 0));

        Assert.Equal(before, (world.Digest().Canonical(), world.Random.Generator.WordsConsumed));
        Assert.Empty(world.Agenda.Outstanding);
    }

    private static AbilityRunner Runner(string effect) => new(AbilityCatalog.Parse("""
        {"cards":[{"card":"12029","abilities":[{
          "trigger":{"event":"WhenCardRevealed","timing":"WhenRevealed","subject":"this"},
          "effect":EFFECT
        }]},{"card":"12027","abilities":[]}]}
        """.Replace("EFFECT", effect, StringComparison.Ordinal)));
}
