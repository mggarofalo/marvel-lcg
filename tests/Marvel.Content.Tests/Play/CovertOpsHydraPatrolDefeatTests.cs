using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>Covert Ops (08003) defeats Hydra Patrol (04154) before confusing Taskmaster (04093).</summary>
public sealed class CovertOpsHydraPatrolDefeatTests : CoreCardTestBase
{
    // Covert Ops: "Remove 4 threat from a scheme. Confuse the villain."
    // Hydra Patrol: "When Defeated: Each player searches the encounter deck and discard pile
    // for a Hydra minion and puts it into play engaged with them. Shuffle the encounter deck."
    [Rule("rr:ability.4")]
    [Rule("rr:when-defeated-abilities.2.1")]
    [Rule("rr:confuse-confused.2")]
    [Fact]
    public void CovertOpsFinishesHydraPatrolsSearchThenConfusesTheVillain()
    {
        // rr:ability.4: "resolve the ability one sentence at a time." The removal defeats
        // the patrol; rr:when-defeated-abilities.2.1: "A defeated card leaves play after its
        // 'When Defeated' ability is resolved". rr:confuse-confused.2: "If an ability
        // 'confuses' a character, give that character a confused status card."
        var runner = AuthoredCards.Runner();
        var world = Hero("08001a");
        world.Abilities = runner;
        var villain = world.CreateCard("04093", world.AreaOf(DeckType.VillainArea));
        var scheme = world.CreateCard("04154", world.AreaOf(DeckType.SideSchemesArea));
        scheme.PlaceTokens("k_threat", 2);
        var minion = world.CreateCard("04152", world.AreaOf(DeckType.EncounterDeck));
        world.CreateCard("01186", world.AreaOf(DeckType.EncounterDeck));
        var card = world.CreateCard("08003", world.Seats[0].Hand);
        int[] payment = [.. Enumerable.Range(0, 3).Select(_ => world.CreateCard("01093", world.Seats[0].Hand).ObjectId)];
        var action = Assert.Single(runner.Actions(world, 0), option => option.Card == card.ObjectId);
        var events = runner.ExecuteAction(world, new PlayerAction(action, payment, [])).Events.ToList();
        var choice = Sequence.Work(world, Cards, runner, events)!;
        Sequence.Answer(world, Cards, runner, choice, Decision.Take(scheme.ObjectId), events);

        choice = Sequence.Work(world, Cards, runner, events)!;
        Assert.Equal(minion.ObjectId, Assert.Single(choice.Affordances).AnchorId);
        Assert.False(Statuses.Has(world, villain, Statuses.Confused));
        Sequence.Answer(world, Cards, runner, choice, Decision.Take(minion.ObjectId), events);
        Sequence.Finish(world, Cards, runner, events);

        Assert.Contains(minion, world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)).Cards);
        Assert.Equal(DeckType.EncounterDiscardPile, scheme.Area.Type);
        Assert.Equal(1, Statuses.Count(world, villain, Statuses.Confused));
        Assert.Equal(DeckType.DiscardPile, card.Area.Type);
        Assert.False(world.Agenda.IsBusy);
    }

    [Rule("rr:in-play-and-out-of-play.4")]
    [Theory]
    [InlineData("""{"query":"minions"}""")]
    [InlineData("""{"titled":"Taskmaster"}""")]
    public void AStatusWhoseRecipientsTheDefeatWindowCanChangeStillRaises(string recipients)
    {
        // rr:in-play-and-out-of-play.4: abilities "can only target, cards that are in play".
        // Hydra Patrol's search can put a minion into play before the status resolves,
        // and only the villain query is resolved on the live board.
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
            {"thwart":{"target":{"titled":"Hydra Patrol"},"effect":{"seq":[
              {"removeThreat":{"scheme":{"titled":"Hydra Patrol"},"amount":2}},
              {"giveStatus":{"card":RECIPIENTS,"status":"confused"}}
            ]}}}
            """.Replace("RECIPIENTS", recipients, StringComparison.Ordinal),
            cost: """{"exhaust":"this"}""", includeAuthored: true);
        var world = Hero("08001a");
        world.Abilities = runner;
        world.CreateCard("04093", world.AreaOf(DeckType.VillainArea));
        var source = InPlay(world, AuthoredCards.AuntMay);
        var scheme = world.CreateCard("04154", world.AreaOf(DeckType.SideSchemesArea));
        scheme.PlaceTokens("k_threat", 2);
        world.CreateCard("04152", world.AreaOf(DeckType.EncounterDeck));
        var error = Assert.Throws<RulesNotImplementedException>(() => runner.Actions(world, 0).ToArray());
        Assert.Contains("unprojected defeat window", error.Message);
        Assert.True(source.Ready);
        Assert.Equal(2, scheme.Tokens["k_threat"]);
    }
}
