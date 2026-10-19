using Marvel.Cards.Run;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class HydraPatrolPowerContinuationTests : CoreCardTestBase
{
    [Theory]
    [InlineData("04049")]
    [InlineData("04038")]
    [Rule("rr:when-defeated-abilities.2.1")]
    public void ThwartWaitsForBothHydraSearchesThenCompletesExactlyOnce(string face)
    {
        // Hydra Patrol: each player searches and puts a Hydra minion into play, then shuffles.
        // The When Defeated ability resolves before the scheme leaves play.
        var probed = FinishPatrol(face, extraQueries: 3);
        var control = FinishPatrol(face, extraQueries: 0);
        Assert.Equal(control.Digest().Canonical(), probed.Digest().Canonical());
        Assert.Equal(control.Random.Generator.GetState().Words, probed.Random.Generator.GetState().Words);
        Assert.Equal(control.Random.Generator.GetState().Index, probed.Random.Generator.GetState().Index);
        Assert.Equal(control.Random.Generator.WordsConsumed, probed.Random.Generator.WordsConsumed);
    }

    [Theory]
    [InlineData("minions")]
    [InlineData("encounterDeck")]
    public void OpaqueDefeatCannotAdmitBoardOrPileDependentFollowup(string read)
    {
        string selection = read == "minions" ? """{"query":"minions"}""" : """{"cardsIn":{"area":"encounterDeck"}}""";
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
            {"thwart":{"target":{"titled":"Hydra Patrol"},"effect":{"seq":[
              {"removeThreat":{"scheme":{"titled":"Hydra Patrol"},"amount":2}},
              {"if":{"test":{"exists":SELECTION},"then":{"draw":{"player":"you","count":1}}}}
            ]}}}
            """.Replace("SELECTION", selection, StringComparison.Ordinal),
            cost: """{"exhaust":"this"}""", includeAuthored: true);
        var world = Hero("01001a", 2);
        world.Abilities = runner;
        var source = InPlay(world, AuthoredCards.AuntMay);
        var scheme = world.CreateCard("04154", world.AreaOf(DeckType.SideSchemesArea));
        scheme.PlaceTokens("k_threat", 2);
        world.CreateCard("01182", world.AreaOf(DeckType.EncounterDeck));
        var error = Assert.Throws<RulesNotImplementedException>(() => runner.Actions(world, 0).ToArray());
        Assert.Contains("unprojected defeat window", error.Message);
        Assert.True(source.Ready);
        Assert.Equal(2, scheme.Tokens["k_threat"]);
    }

    private static World FinishPatrol(string face, int extraQueries)
    {
        var runner = AuthoredCards.Runner();
        var world = Hero("01001a", 2);
        world.Abilities = runner;
        world.CreateCard("01094", world.AreaOf(DeckType.VillainArea));
        var scheme = world.CreateCard("04154", world.AreaOf(DeckType.SideSchemesArea));
        scheme.PlaceTokens("k_threat", 2);
        var minions = new[] {
            world.CreateCard("01182", world.AreaOf(DeckType.EncounterDeck)),
            world.CreateCard("01182", world.AreaOf(DeckType.EncounterDiscardPile)),
        };
        world.CreateCard("01186", world.AreaOf(DeckType.EncounterDeck));
        world.CreateCard("01187", world.AreaOf(DeckType.EncounterDeck));
        var card = world.CreateCard(face, world.Seats[0].Hand);
        var payment = world.CreateCard("01088", world.Seats[0].Hand);
        world.CreateCard("01089", world.Seats[0].Deck);
        var top = world.CreateCard("01090", world.Seats[0].Deck);
        AssertPureQueries(world, runner, extraQueries);
        var action = Assert.Single(runner.Actions(world, 0), option => option.Card == card.ObjectId);
        var events = runner.ExecuteAction(world, new PlayerAction(action, [payment.ObjectId], [])).Events.ToList();
        var choice = Sequence.Work(world, Cards, runner, events)!;
        Sequence.Answer(world, Cards, runner, choice, face == "04038"
            ? Decision.Take(Assert.Single(choice.Affordances).Id, [scheme.ObjectId, scheme.ObjectId], [])
            : Decision.Take(scheme.ObjectId), events);
        choice = Sequence.Work(world, Cards, runner, events)!;
        Sequence.Answer(world, Cards, runner, choice,
            Decision.Take(scheme.ObjectId, world.Seats.Select(seat => seat.IdentityCard.ObjectId).ToArray(), []), events);
        for (int player = 0; player < 2; player++)
        {
            Assert.DoesNotContain(top, world.Seats[0].Hand.Cards);
            choice = Sequence.Work(world, Cards, runner, events)!;
            Assert.NotNull(choice);
            Sequence.Answer(world, Cards, runner, choice, Decision.Take(minions[player].ObjectId), events);
        }
        Sequence.Finish(world, Cards, runner, events);
        for (int player = 0; player < 2; player++)
            Assert.Contains(minions[player], world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(player)).Cards);
        if (face == "04049") Assert.Same(top, Assert.Single(world.Seats[0].Hand.Cards));
        else Assert.Empty(world.Seats[0].Hand.Cards);
        Assert.Equal(DeckType.EncounterDiscardPile, scheme.Area.Type);
        Assert.Equal(DeckType.DiscardPile, card.Area.Type);
        Assert.False(world.Agenda.IsBusy);
        return world;
    }

    private static void AssertPureQueries(World world, AbilityRunner runner, int count)
    {
        var digest = world.Digest().Canonical();
        var rng = world.Random.Generator.GetState();
        long consumed = world.Random.Generator.WordsConsumed;
        var pending = (world.Agenda.Count, world.Agenda.Current, world.Agenda.Stage, world.Agenda.Occurrence);
        for (int query = 0; query < count; query++) Assert.NotEmpty(runner.Actions(world, 0));
        Assert.Equal(digest, world.Digest().Canonical());
        Assert.Equal(rng.Words, world.Random.Generator.GetState().Words);
        Assert.Equal(rng.Index, world.Random.Generator.GetState().Index);
        Assert.Equal(consumed, world.Random.Generator.WordsConsumed);
        Assert.Equal(pending, (world.Agenda.Count, world.Agenda.Current, world.Agenda.Stage, world.Agenda.Occurrence));
    }
}
