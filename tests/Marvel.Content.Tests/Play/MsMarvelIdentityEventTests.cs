using Marvel.Content.Tests.Cards;
using Marvel.Cards.Run;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class MsMarvelIdentityEventTests
{
    private static void Identity(World world, int player)
    {
        var seat = world.Seats[player];
        World.MoveToTop(seat.IdentityCard, world.AreaOf(DeckType.RemovedArea, PlayArea.Of(player)));
        seat.IdentityCard = world.CreateCard("05001a,05001b", seat.Hero);
    }
    private static AbilityRunner Runner() => AuthoredCards.Runner();

    [Theory]
    [InlineData("removed")]
    [InlineData("wrongOwner")]
    [InlineData("wrongPlayer")]
    [InlineData("exhausted")]
    [InlineData("nonTrait")]
    [InlineData("notPlayed")]
    [Rule("rr:exhausted.2")]
    public void UnavailableRecoveryPreservesStateAndRandomness(string control)
    {
        // "If an exhausted card must exhaust to pay the cost ... that ability
        // cannot be used". Rulings 7f69f006cddce07e and 556ecad3abe55ce9
        // exclude removed cards and require the printed trait, respectively.
        var runner = Runner();
        var (_, world) = Playing(board => Identity(board, 0),
            heroes: ["spider_man", "captain_marvel"], abilities: runner);
        var card = world.CreateCard(control == "nonTrait" ? "01086" : "05003",
            world.AreaOf(control == "removed" ? DeckType.RemovedArea : DeckType.DiscardPile,
                PlayArea.Of(control == "wrongOwner" ? 1 : 0), cardOwner: control == "wrongOwner" ? 1 : 0));
        if (control == "exhausted") world.Seats[0].IdentityCard.Exhaust();
        var occurrence = new Occurrence(81, [control == "notPlayed" ? Steps.TurnAction : Steps.CardPlayed],
            Subject: card.ObjectId, Player: control == "wrongPlayer" ? 1 : 0);
        string before = world.Digest().Canonical();
        var random = world.Random.Generator.GetState();
        Assert.Empty(runner.Waiting(world, occurrence, WindowKind.Response));
        Assert.Empty(runner.Waiting(world, occurrence, WindowKind.Response));
        var forged = new PendingAbility(world.Seats[0].IdentityCard.ObjectId, AbilityType.Response, 0);
        Assert.Throws<RulesNotImplementedException>(() => runner.ResolveWindow(world, occurrence,
            forged, Decision.Take(forged.Card)));
        Assert.Equal(before, world.Digest().Canonical());
        Assert.Equal(random.Index, world.Random.Generator.GetState().Index);
        Assert.Equal(random.Words, world.Random.Generator.GetState().Words);
        Assert.False(world.Agenda.IsBusy);
    }

    [Fact]
    public void SamePhysicalWiggleRoomCanBeReplayedInTheRealDamageWindow()
    {
        // Ruling b144656def387a1e: "You fully resolve one before playing the other."
        var runner = Runner();
        var (_, world) = Playing(board => Identity(board, 1),
            heroes: ["spider_man", "captain_marvel"], abilities: runner);
        var hero = world.Seats[1].IdentityCard;
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        var card = world.CreateCard("05005", world.Seats[1].Hand);
        int held = world.Seats[1].Hand.Cards.Count;
        world.Activation = new(villain.ObjectId, 1, Attacking: true);
        world.Attack = new(villain.ObjectId, 1, hero.ObjectId, CalculatedDamage: 5);
        world.Agenda.Add(new PhaseStep(new AttackStageOperation(AttackStageKind.DealAttackDamage),
            Round: 1, Number: 5, Subject: villain.ObjectId, Seat: 1));
        var events = new List<GameEvent>();
        var first = Sequence.Work(world, world.Facts, runner, events)!;
        Sequence.Answer(world, world.Facts, runner, first,
            Decision.Take(Assert.Single(first.Affordances).Id), events);
        var recovery = Sequence.Work(world, world.Facts, runner, events)!;
        Assert.Equal(hero.ObjectId, Assert.Single(recovery.Affordances).AnchorId);
        Sequence.Answer(world, world.Facts, runner, recovery,
            Decision.Take(recovery.Affordances[0].Id), events);
        var replay = Sequence.Work(world, world.Facts, runner, events);
        Assert.NotNull(replay);
        Assert.Equal(card.ObjectId, Assert.Single(replay.Affordances).AnchorId);
        Sequence.Answer(world, world.Facts, runner, replay,
            Decision.Take(replay.Affordances[0].Id), events);
        Assert.Null(Sequence.Work(world, world.Facts, runner, events));
        Assert.Equal(held + 1, world.Seats[1].Hand.Cards.Count);
        Assert.Equal(0, hero.Damage);
        Assert.Equal(DeckType.DiscardPile, card.Area.Type);
        Assert.False(hero.Ready);
        Assert.False(world.Agenda.IsBusy);
    }

}
