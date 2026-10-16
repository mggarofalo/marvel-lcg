using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class DelayedAbilityChoiceTests
{
    [Rule("rr:delayed-effect.1")]
    [Rule("rr:delayed-effect.2")]
    [Fact]
    public void DelayedChoiceResumesOnlyItsBodyAndIsConsumedOnce()
    {
        // Synthetic DSL fixture: a delayed effect resolves at its specified
        // timing point and is not a new triggered ability. Its suspended body
        // must not repeat the suffix of the ability that registered it.
        var cards = CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));
        var world = new World(cards, players: 1);
        var seat = world.CreateSeat("p0");
        seat.IdentityCard = world.CreateCard("01001a", seat.Hero);
        var villain = world.CreateCard("01113", world.AreaOf(DeckType.VillainArea));
        var source = world.CreateCard("01123", world.AreaOf(DeckType.BoostingArea));
        for (int index = 0; index < 6; index++) world.CreateCard("01002", seat.Deck);
        var runner = new AbilityRunner(AbilityCatalog.Parse("""
            {"cards":[{"card":"01123","abilities":[{
              "trigger":{"event":"WhenCardRevealed","timing":"Boost","subject":"this"},
              "effect":{"seq":[
                {"afterActivation":{"effect":{"seq":[
                  {"choose":{"options":[{"draw":{"player":"you","count":2}},
                                         {"draw":{"player":"you","count":3}}]}},
                  {"exhaust":"you"}]}}},
                {"draw":{"player":"you","count":1}}]}}]}]}
            """));
        world.Activation = new EnemyActivation(villain.ObjectId, 0, Attacking: true, Id: 12);
        var completed = world.Activation with { Made = true };
        runner.Boost(world, source, 0);
        Assert.Single(seat.Hand.Cards);
        Assert.True(seat.IdentityCard.Ready);
        Assert.Empty(world.Agenda.Outstanding);

        runner.ActivationCompleted(world, completed);
        Assert.Single(world.Agenda.Outstanding);
        runner.ActivationCompleted(world, completed);
        Assert.Single(world.Agenda.Outstanding);
        Assert.Single(seat.Hand.Cards);
        var events = new List<GameEvent>();
        var prompt = Sequence.Work(world, cards, runner, events)!;
        Assert.Equal(2, prompt.Affordances.Count);
        Sequence.Answer(world, cards, runner, prompt, Decision.Take(0), events);
        Assert.Null(Sequence.Work(world, cards, runner, events));

        Assert.Equal(3, seat.Hand.Cards.Count);
        Assert.False(seat.IdentityCard.Ready);
        string final = world.Digest().Canonical();
        Assert.Empty(runner.ActivationCompleted(world, completed));
        Assert.Empty(world.Agenda.Outstanding);
        Assert.Equal(final, world.Digest().Canonical());
    }
}
