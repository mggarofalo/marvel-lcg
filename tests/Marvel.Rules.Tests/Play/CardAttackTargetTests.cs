using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Rules.Tests.Play;

public sealed class CardAttackTargetTests : BasicPowerTestBase
{
    private sealed record AggregateContinuation : CardPowerContinuation;

    [Rule("rr:guard.1")]
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void GuardAndVillainCannotBeSelectedTogether(bool villainFirst)
    {
        // "The engaged player cannot attack any villain." Selecting the Guard
        // first does not let an aggregate attack resolve its damage early.
        var facts = Facts().With("minion", ("Guard", "1"));
        var world = Board(facts);
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        var guard = world.CreateCard("minion", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        Card[] targets = villainFirst ? [villain, guard] : [guard, villain];
        RejectWithoutMutation(world, facts, Invocation(world, targets));
    }

    [Rule("rr:choose-game-element.3.1")]
    [Fact]
    public void OneEnemyCannotFillTwoTargetPositions()
    {
        // "The same target cannot be chosen multiple times this way."
        var facts = Facts();
        var world = Board(facts);
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        RejectWithoutMutation(world, facts, Invocation(world, [villain, villain]));
    }

    [Rule("rr:attack-player-ability-type.4")]
    [Fact]
    public void DistinctEnemiesScheduleOneInterpreterAttack()
    {
        // "Hero and ally attacks can target any enemy, unless a card ability
        // (such as guard) is preventing that enemy from being attacked."
        var facts = Facts();
        var world = Board(facts, players: 2);
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        var minion = world.CreateCard("minion", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1)));
        var invocation = Invocation(world, [villain, minion]);
        var observer = new RecordingCardPowers();
        world.Abilities = observer;
        Assert.True(BasicPowerInitiation.CardAttack(world, facts, invocation, []));
        Agendas.Finish(world, facts, observer);
        Assert.Equal([villain.ObjectId], observer.Targets);
        Assert.Same(invocation.Continuation, Assert.Single(observer.Continuations));
        Assert.Equal(0, villain.Damage);
        Assert.Equal(0, minion.Damage);
    }

    [Fact]
    public void ExplicitAggregateRequiresInterpreterContinuation()
    {
        // Engine contract: a fixed-amount single-target procedure cannot silently
        // accept a recipient list it has no interpreter to resolve.
        var facts = Facts();
        var world = Board(facts);
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        RejectWithoutMutation(world, facts,
            Invocation(world, [villain]) with { Continuation = null });
    }

    [Fact]
    public void ExplicitAggregateRequiresItsPrimaryTargetFirst()
    {
        var facts = Facts();
        var world = Board(facts);
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        var minion = world.CreateCard("minion", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        RejectWithoutMutation(world, facts,
            Invocation(world, [villain, minion]) with { Targets = [minion, villain] });
    }

    [Fact]
    public void ExplicitAggregateCannotBeEmpty()
    {
        var facts = Facts();
        var world = Board(facts);
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        RejectWithoutMutation(world, facts,
            Invocation(world, [villain]) with { Targets = [] });
    }

    [Rule("rr:target.3.8")]
    [Fact]
    public void NonEnemySecondaryTargetIsRejected()
    {
        // "A target that cannot be attacked is not a valid target for an
        // attack-labelled ability."
        var facts = Facts();
        var world = Board(facts);
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        RejectWithoutMutation(world, facts,
            Invocation(world, [villain, world.Seats[0].IdentityCard]));
    }

    private static Printed Facts() => new Printed()
        .With("hero", ("ATK", "2")).With("villain", ("HP", "20"))
        .With("minion", ("HP", "5"));

    private static CardPowerInvocation Invocation(World world, Card[] targets) =>
        new(0, world.Seats[0].IdentityCard, targets[0], 4, "synthetic aggregate")
        {
            Targets = [.. targets],
            Continuation = new AggregateContinuation(),
            Performer = world.Seats[0].IdentityCard,
        };

    private static void RejectWithoutMutation(World world, Printed facts, CardPowerInvocation invocation)
    {
        string before = world.Digest().Canonical();
        long words = world.Random.Generator.WordsConsumed;
        var events = new List<GameEvent>();
        Assert.Throws<RulesNotImplementedException>(() =>
            BasicPowerInitiation.CardAttack(world, facts, invocation, events));
        Assert.Equal(before, world.Digest().Canonical());
        Assert.Equal(words, world.Random.Generator.WordsConsumed);
        Assert.Empty(events);
        Assert.Null(world.CharacterAttack);
        Assert.Null(world.Agenda.Current);
    }
}
