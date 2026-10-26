using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class ThorUtilityEventTests : CoreCardTestBase
{
    [Fact]
    [Rule("rr:status-cards.1")]
    public void InvulnerabilityGivesOnlyOwnHeroToughAndCannotRepeatOrPlayInAlterEgo()
    {
        // "A character cannot have more than one status card of each type at a time."
        var world = ThorActualFixture.Board("06001a", 2);
        var runner = AuthoredCards.Runner();
        var seat = world.Seats[0];
        var card = world.CreateCard("06021", seat.Hand);
        var energy = world.CreateCard("06022", seat.Hand);
        var physical = world.CreateCard("01003", seat.Hand);
        var action = Assert.Single(runner.Actions(world, 0), a => a.Card == card.ObjectId);
        runner.ExecuteAction(world, new PlayerAction(action, [energy.ObjectId, physical.ObjectId], []));
        Sequence.Finish(world, world.Facts, runner, []);
        Assert.True(Statuses.Has(world, seat.IdentityCard, Statuses.Tough));
        Assert.False(Statuses.Has(world, world.Seats[1].IdentityCard, Statuses.Tough));
        Assert.Equal(DeckType.DiscardPile, card.Area.Type);
        var duplicate = world.CreateCard("06021", seat.Hand);
        world.CreateCard("06022", seat.Hand);
        world.CreateCard("01003", seat.Hand);
        Assert.DoesNotContain(runner.Actions(world, 0), a => a.Card == duplicate.ObjectId);
        World.MoveToTop(Assert.Single(Statuses.On(world, seat.IdentityCard, Statuses.Tough)), world.AreaOf(DeckType.RemovedArea));
        seat.IdentityCard.TurnTo("06001b");
        Assert.DoesNotContain(runner.Actions(world, 0), a => a.Card == duplicate.ObjectId);
        seat.IdentityCard.TurnTo("06001a");
        Assert.Contains(runner.Actions(world, 0), a => a.Card == duplicate.ObjectId);
    }

    [Theory]
    [InlineData("06001a", "01003", "R", 7, 3)]
    [InlineData("06001b", "01005", "B", 7, 2)]
    [InlineData("06001a", "01011", "B", 7, 2)]
    [InlineData("06001b", "01011", "R", 7, 3)]
    [InlineData("06001b", "01005", "B", 2, 0)]
    [Rule("rr:heal.1")]
    public void SecondWindSelectsAnotherIdentityAndReadsDeclaredPayment(string form, string resource, string declared, int damage, int remaining)
    {
        // "A heal effect can only bring a character to its maximum hit points."
        var world = ThorActualFixture.Board(form, 2);
        var runner = AuthoredCards.Runner();
        var seat = world.Seats[0];
        world.Seats[1].IdentityCard.TurnTo("01001b");
        var other = world.Seats[1].IdentityCard;
        seat.IdentityCard.TakeDamage(1);
        other.TakeDamage(damage);
        var ally = world.CreateCard("06002", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        ally.TakeDamage(1);
        var card = world.CreateCard("06033", seat.Hand);
        var doubleResource = world.CreateCard("06022", seat.Hand);
        var single = world.CreateCard(resource, seat.Hand);
        var action = Assert.Single(runner.Actions(world, 0), a => a.Card == card.ObjectId);
        runner.ExecuteAction(world, new PlayerAction(action, [doubleResource.ObjectId, single.ObjectId], [], Allocations:
            [new(doubleResource.ObjectId, 0, "YY"), new(single.ObjectId, 0, declared)]));
        var prompt = Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, runner, []));
        Assert.Equal(new[] { seat.IdentityCard.ObjectId, other.ObjectId }.Order(), prompt.Affordances.Select(a => a.AnchorId).Order());
        Assert.DoesNotContain(prompt.Affordances, a => a.AnchorId == ally.ObjectId);
        var before = world.Digest().Canonical();
        var rng = world.Random.Generator.GetState();
        var pending = world.Agenda.Outstanding.ToArray();
        Assert.Throws<ArgumentException>(() => Sequence.Answer(world, world.Facts, runner, prompt, Decision.Take(ally.ObjectId), []));
        Assert.Equal(before, world.Digest().Canonical());
        Assert.Equal(rng.Words, world.Random.Generator.GetState().Words);
        Assert.Equal(rng.Index, world.Random.Generator.GetState().Index);
        Assert.Equal(pending, world.Agenda.Outstanding);
        Sequence.Answer(world, world.Facts, runner, prompt, Decision.Take(other.ObjectId), []);
        Sequence.Finish(world, world.Facts, runner, []);
        Assert.Equal(remaining, other.Damage);
        Assert.Equal(1, seat.IdentityCard.Damage);
        Assert.Equal(DeckType.DiscardPile, card.Area.Type);
        Assert.Equal(DeckType.DiscardPile, single.Area.Type);
        Assert.Equal(DeckType.DiscardPile, doubleResource.Area.Type);
        Assert.Empty(world.Agenda.Outstanding);
    }

    [Fact]
    [Rule("rr:initiating-abilities.step.2")]
    public void HealthyIdentitiesCannotJustifySecondWindPayment()
    {
        // "If the card or ability does not have at least one valid target, it cannot be played or initiated."
        var world = ThorActualFixture.Board("06001b", 2);
        var runner = AuthoredCards.Runner();
        var card = world.CreateCard("06033", world.Seats[0].Hand);
        var energy = world.CreateCard("06022", world.Seats[0].Hand);
        var mental = world.CreateCard("01005", world.Seats[0].Hand);
        world.Seats[1].IdentityCard.TakeDamage(1);
        var action = Assert.Single(runner.Actions(world, 0), a => a.Card == card.ObjectId);
        world.Seats[1].IdentityCard.TakeDamage(-1);
        var before = world.Digest().Canonical();
        Assert.DoesNotContain(runner.Actions(world, 0), a => a.Card == card.ObjectId);
        Assert.Throws<RulesNotImplementedException>(() => runner.ExecuteAction(world, new PlayerAction(action, [energy.ObjectId, mental.ObjectId], [])));
        Assert.Equal(before, world.Digest().Canonical());
        world.Seats[1].IdentityCard.TakeDamage(1);
        Assert.Contains(runner.Actions(world, 0), a => a.Card == card.ObjectId);
    }
}

