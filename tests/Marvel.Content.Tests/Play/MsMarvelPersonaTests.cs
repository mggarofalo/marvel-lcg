using Marvel.Cards.Run;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class MsMarvelPersonaTests
{
    private static AbilityRunner Runner() => AuthoredCards.Runner();

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:lasting-effects.5")]
    public void NakiasUnusedReductionSurvivesFormChangeAndExpiresAtPhaseEnd(bool changeForm)
    {
        // Printed 05008: "the next card you play this phase by 1."
        // rr:lasting-effects.5: "remains in effect for the specified duration".
        var runner = Runner();
        Card? nakia = null;
        var (_, world) = Playing(board => nakia = board.CreateCard("05008",
            board.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0)),
            hero: false, heroes: ["spider_man", "captain_marvel"], abilities: runner);
        runner.ExecuteAction(world, new PlayerAction(Assert.Single(runner.Actions(world, 0),
            action => action.Card == nakia!.ObjectId), [], []));
        Assert.False(nakia!.Ready);
        if (changeForm) world.Seats[0].IdentityCard.TurnTo("01001a");
        var mine = world.CreateCard("01083", world.Seats[0].Hand);
        var theirs = world.CreateCard("01083", world.Seats[1].Hand);
        string before = world.Digest().Canonical();
        long random = world.Random.Generator.WordsConsumed;
        var pending = world.Agenda.Outstanding.ToArray();
        Assert.Equal(2, CardPayment.CostOf(world, world.Facts, world.Seats[0], mine).Amount);
        Assert.Equal(2, CardPayment.CostOf(world, world.Facts, world.Seats[0], mine).Amount);
        Assert.Equal(3, CardPayment.CostOf(world, world.Facts, world.Seats[1], theirs).Amount);
        Assert.Equal(before, world.Digest().Canonical());
        Assert.Equal(random, world.Random.Generator.WordsConsumed);
        Assert.Equal(pending, world.Agenda.Outstanding);
        PhaseEnd.EndPlayerPhase(world, []);
        Assert.Equal(3, CardPayment.CostOf(world, world.Facts, world.Seats[0], mine).Amount);
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void NakiaRequiresAlterEgoAndReadiness(bool hero, bool exhausted)
    {
        Card? nakia = null;
        var runner = Runner();
        var (_, world) = Playing(board =>
        {
            nakia = board.CreateCard("05008", board.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
            if (exhausted) nakia.Exhaust();
        }, hero: hero, abilities: runner);
        Assert.DoesNotContain(runner.Actions(world, 0), action => action.Card == nakia!.ObjectId);
        Assert.Empty(world.Effects.Registered);
    }

    [Fact]
    [Rule("rr:initiating-abilities.step.5")]
    public void RejectedPaymentPreservesNakiaUntilTheActualNextPlayAcrossForms()
    {
        // rr:initiating-abilities.step.5: "abort this process without paying any costs".
        var runner = Runner();
        Card? nakia = null;
        var (_, world) = Playing(board => nakia = board.CreateCard("05008",
            board.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0)),
            hero: false, abilities: runner);
        runner.ExecuteAction(world, new PlayerAction(Assert.Single(runner.Actions(world, 0),
            item => item.Card == nakia!.ObjectId), [], []));
        world.Seats[0].IdentityCard.TurnTo("01001a");
        var attack = world.CreateCard("05003", world.Seats[0].Hand);
        var next = world.CreateCard("05003", world.Seats[0].Hand);
        var resource = world.CreateCard("01003", world.Seats[0].Hand);
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        var action = Assert.Single(runner.Actions(world, 0), item => item.Card == attack.ObjectId);
        Assert.Equal("1", Assert.Single(runner.Describe(world, action).CostOptions).Cost);
        string before = world.Digest().Canonical();
        long random = world.Random.Generator.WordsConsumed;
        var pending = world.Agenda.Outstanding.ToArray();
        Assert.Throws<RulesNotImplementedException>(() => runner.ExecuteAction(world,
            new PlayerAction(action, [], [villain.ObjectId])));
        Assert.Equal(before, world.Digest().Canonical());
        Assert.Equal(random, world.Random.Generator.WordsConsumed);
        Assert.Equal(pending, world.Agenda.Outstanding);
        Assert.Equal(1, CardPayment.CostOf(world, world.Facts, world.Seats[0], next).Amount);
        runner.ExecuteAction(world, new PlayerAction(action, [resource.ObjectId], [villain.ObjectId]));
        Sequence.Finish(world, world.Facts, runner, []);
        Assert.Equal(4, villain.Damage);
        Assert.Equal(DeckType.DiscardPile, attack.Area.Type);
        Assert.Equal(DeckType.DiscardPile, resource.Area.Type);
        Assert.Equal(2, CardPayment.CostOf(world, world.Facts, world.Seats[0], next).Amount);
        Assert.False(nakia!.Ready);
    }
}


