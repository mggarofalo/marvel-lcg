using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class PlayedEventModifierLifecycleTests
{
    // Synthetic event and canceling upgrade bodies isolate initiation cancellation;
    // only the recovery instruction follows the printed Morphogenetics text.
    private static AbilityRunner Runner() => new(AbilityCatalog.Parse("""
        {"cards":[{"card":"05010","abilities":[{
          "trigger":{"event":"WhenEventPlayInitiated","timing":"Interrupt","subject":"you","form":"hero"},
          "cost":{"exhaust":"this"},"effect":{"seq":[{"modifyPlayedEvent":{"quantity":"damage","amount":2}},{"cancelOccurrence":"trigger"}]}
        }]},{"card":"05001a","abilities":[{
          "trigger":{"event":"WhenCardPlayed","timing":"Response","subject":"you","form":"hero"},
          "when":{"exists":"trigger.discardedEvent"},
          "cost":{"exhaust":"this"},"effect":{"returnToHand":"trigger.discardedEvent"}
        }]},{"card":"05003","abilities":[{
          "trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"this","form":"hero"},
          "effect":{"draw":{"player":"you","count":1}}
        }]},{"card":"05005","abilities":[{
          "trigger":{"event":"WhenDamageWouldBeDealt","timing":"Interrupt","subject":"you","form":"hero"},
          "effect":{"draw":{"player":"you","count":1}}
        }]}]}
        """));

    private static void Identity(World world)
    {
        var seat = world.Seats[0];
        World.MoveToTop(seat.IdentityCard, world.AreaOf(DeckType.RemovedArea, PlayArea.Of(0)));
        seat.IdentityCard = world.CreateCard("05001a,05001b", seat.Hero);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    [Rule("rr:event.2")]
    public void CancellationPreservesPaidCostsDiscardsEventAndAllowsRecovery(bool cancel)
    {
        // "the card is still considered to have been played, and its costs remain
        // paid. Only the effects are canceled."
        var runner = Runner();
        Card? played = null;
        Card? payment = null;
        Card? upgrade = null;
        var (game, world) = Playing(board =>
        {
            Identity(board);
            upgrade = board.CreateCard("05010", board.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
            played = board.CreateCard("05003", board.Seats[0].Hand);
            payment = board.CreateCard("01088", board.Seats[0].Hand);
        }, abilities: runner);
        int held = world.Seats[0].Hand.Cards.Count;
        game.Resolve(PaidDecision.Take(Assert.Single(game.Pending!.Affordances,
            offer => offer.AnchorId == played!.ObjectId), [payment!.ObjectId]));
        var initiation = Assert.Single(game.Pending!.Affordances);
        Assert.Equal(upgrade!.ObjectId, initiation.AnchorId);
        game.Resolve(cancel ? Decision.Take(initiation.Id) : Decision.Decline);
        Assert.Equal(DeckType.DiscardPile, played!.Area.Type);
        Assert.DoesNotContain(world.Effects.Registered, effect => effect.Kind == "eventDamage");
        Assert.Equal(DeckType.DiscardPile, payment!.Area.Type);
        Assert.Equal(held - 2 + (cancel ? 0 : 1), world.Seats[0].Hand.Cards.Count);
        Assert.Equal(!cancel, upgrade.Ready);
        Assert.Equal(cancel ? ResolutionStatus.Unresolved : ResolutionStatus.Resolved,
            world.Agenda.Occurrence!.Resolution.CardStatus(played.ObjectId));
        var recovery = Assert.Single(game.Pending!.Affordances);
        Assert.Equal(world.Seats[0].IdentityCard.ObjectId, recovery.AnchorId);
        game.Resolve(Decision.Take(recovery.Id));
        Assert.Same(world.Seats[0].Hand, played.Area);
        Assert.DoesNotContain(world.Effects.Registered, effect => effect.Kind == "eventDamage");
        Assert.Equal(held - 1 + (cancel ? 0 : 1), world.Seats[0].Hand.Cards.Count);
        Assert.False(world.Seats[0].IdentityCard.Ready);
        Assert.True(game.IsRootPrompt);
    }

    [Fact]
    [Rule("rr:event.2")]
    public void CanceledNestedEventRecoversBeforeTheEnclosingAttackContinues()
    {
        // "If the effects of an event are canceled, the card is still considered
        // to have been played"; its card-play response follows normal discard.
        var runner = Runner();
        var (_, world) = Playing(Identity, abilities: runner);
        var upgrade = world.CreateCard("05010", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
        var played = world.CreateCard("05005", world.Seats[0].Hand);
        var hero = world.Seats[0].IdentityCard;
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        int held = world.Seats[0].Hand.Cards.Count;
        world.Activation = new(villain.ObjectId, 0, Attacking: true);
        world.Attack = new(villain.ObjectId, 0, hero.ObjectId, CalculatedDamage: 1);
        world.Agenda.Add(new PhaseStep(new AttackStageOperation(AttackStageKind.DealAttackDamage),
            Round: 1, Number: 5, Subject: villain.ObjectId, Seat: 0));
        var events = new List<GameEvent>();
        var window = Sequence.Work(world, world.Facts, runner, events)!;
        Sequence.Answer(world, world.Facts, runner, window,
            Decision.Take(Assert.Single(window.Affordances).Id), events);
        var initiation = Sequence.Work(world, world.Facts, runner, events)!;
        Assert.Equal(upgrade.ObjectId, Assert.Single(initiation.Affordances).AnchorId);
        Sequence.Answer(world, world.Facts, runner, initiation,
            Decision.Take(initiation.Affordances[0].Id), events);
        var recovery = Sequence.Work(world, world.Facts, runner, events)!;
        Assert.Equal(held - 1, world.Seats[0].Hand.Cards.Count);
        Assert.Equal(DeckType.DiscardPile, played.Area.Type);
        Assert.DoesNotContain(world.Effects.Registered, effect => effect.Kind == "eventDamage");
        Assert.Equal(0, hero.Damage);
        Assert.Equal(hero.ObjectId, Assert.Single(recovery.Affordances).AnchorId);
        Sequence.Answer(world, world.Facts, runner, recovery,
            Decision.Take(recovery.Affordances[0].Id), events);
        Assert.Same(world.Seats[0].Hand, played.Area);
        Assert.DoesNotContain(world.Effects.Registered, effect => effect.Kind == "eventDamage");
        Assert.Equal(held, world.Seats[0].Hand.Cards.Count);
        var replay = Sequence.Work(world, world.Facts, runner, events)!;
        Assert.Equal(played.ObjectId, Assert.Single(replay.Affordances).AnchorId);
        Sequence.Answer(world, world.Facts, runner, replay, Decision.Decline, events);
        Sequence.Finish(world, world.Facts, runner, events);
        Assert.False(upgrade.Ready);
        Assert.Equal(1, hero.Damage);
        Assert.False(world.Agenda.IsBusy);
    }
}
