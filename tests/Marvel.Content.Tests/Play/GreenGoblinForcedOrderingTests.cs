using Marvel.Content.Tests.Cards;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class GreenGoblinForcedOrderingTests : SpiderManCardTestBase
{
    [Rule("rr:status-cards.2")]
    [Fact]
    public void StunCancelsBeforeEitherWouldInterrupt()
    {
        // “Status card abilities have timing priority over all conflicting
        // triggered abilities.” The attached Webbed Up and Norman's replacement
        // must both remain unused for the next activation.
        var world = Deal(hero: true);
        var runner = AuthoredCards.Runner();
        var rhino = world.TheCardIn(DeckType.VillainArea)!;
        World.MoveToTop(rhino, world.AreaOf(DeckType.EncounterDiscardPile));
        var norman = world.CreateCard("02001a,02001b", world.AreaOf(DeckType.VillainArea));
        var environment = world.CreateCard("02006a,02006b",
            world.AreaOf(DeckType.EnvironmentArea));
        environment.PlaceTokens("c_infamy", 2);
        var webbed = world.CreateCard(AuthoredCards.WebbedUp,
            world.AreaOf(DeckType.UpgradesArea, norman.Area.PlayArea, norman.ObjectId, 0));
        Statuses.Give(world, norman, Statuses.Stunned);
        world.Agenda.Add(new PhaseStep(new EnemyAttackOperation(), 1, 2,
            Subject: norman.ObjectId, Seat: 0));

        Assert.Null(Sequence.Work(world, Cards, runner, []));

        Assert.Equal(2, environment.Tokens["c_infamy"]);
        Assert.Equal(DeckType.UpgradesArea, webbed.Area.Type);
        Assert.False(Statuses.Has(world, norman, Statuses.Stunned));
        Assert.Null(world.Attack);
    }

    [Rule("rr:forced.5")]
    [Rule("rr:forced.6")]
    [Rule("rr:replacement-effect.1")]
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void FirstPlayerOrdersWebbedUpAndNormanReplacement(bool webbedFirst)
    {
        // “If two or more forced abilities would initiate at the same moment,
        // the first player determines the order.” Each ability resolves before
        // the next is considered; both replace this one imminent attack.
        var world = Deal(hero: true);
        var runner = AuthoredCards.Runner();
        var rhino = world.TheCardIn(DeckType.VillainArea)!;
        World.MoveToTop(rhino, world.AreaOf(DeckType.EncounterDiscardPile));
        var norman = world.CreateCard("02001a,02001b", world.AreaOf(DeckType.VillainArea));
        var environment = world.CreateCard("02006a,02006b",
            world.AreaOf(DeckType.EnvironmentArea));
        environment.PlaceTokens("c_infamy", 2);
        var webbed = world.CreateCard(AuthoredCards.WebbedUp,
            world.AreaOf(DeckType.UpgradesArea, norman.Area.PlayArea, norman.ObjectId, 0));
        world.Agenda.Add(new PhaseStep(new EnemyAttackOperation(), 1, 2,
            Subject: norman.ObjectId, Seat: 0));
        var events = new List<GameEvent>();

        var order = Assert.IsType<Prompt>(Sequence.Work(world, Cards, runner, events));
        Assert.Equal(Question.Order, order.Asking);
        Assert.False(order.Cancellable);
        Assert.Equal(2, order.Affordances.Count);
        var choice = Assert.Single(order.Affordances,
            option => option.AnchorId == (webbedFirst ? webbed.ObjectId : norman.ObjectId));
        Sequence.Answer(world, Cards, runner, order, Decision.Take(choice.Id), events);
        Assert.Null(Sequence.Work(world, Cards, runner, events));

        Assert.Equal(webbedFirst ? 2 : 3, environment.Tokens["c_infamy"]);
        Assert.Equal(webbedFirst ? DeckType.DiscardPile : DeckType.UpgradesArea,
            webbed.Area.Type);
        Assert.Equal(webbedFirst, Statuses.Has(world, norman, Statuses.Stunned));
        Assert.Null(world.Attack);
        Assert.Null(world.Activation);
    }
}
