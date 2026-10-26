using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class MorphogeneticsTests
{
    private static void Identity(World world, int player)
    {
        var seat = world.Seats[player];
        World.MoveToTop(seat.IdentityCard, world.AreaOf(DeckType.RemovedArea, PlayArea.Of(player)));
        seat.IdentityCard = world.CreateCard("05001a,05001b", seat.Hero);
    }
    // Nonpublic printed 05001a interpretation. Supporting event instructions
    // isolate event-completion plumbing; they do not admit Ms. Marvel content.
    private static AbilityRunner Runner(string binding = "trigger.discardedEvent") => new(AbilityCatalog.Parse("""
        {"cards":[{"card":"05001a","abilities":[{
          "trigger":{"event":"WhenCardPlayed","timing":"Response","subject":"you","form":"hero"},
          "when":{"and":[{"exists":"BINDING"},{"or":[
            {"hasTrait":{"card":"trigger.subject","trait":"ATTACK"}},
            {"hasTrait":{"card":"trigger.subject","trait":"THWART"}},
            {"hasTrait":{"card":"trigger.subject","trait":"DEFENSE"}}
          ]}]},
          "cost":{"exhaust":"this"},"effect":{"returnToHand":"BINDING"}
        }]},{"card":"05003","abilities":[{
          "trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"this","form":"hero"},
          "effect":{"draw":{"player":"you","count":1}}
        }]},{"card":"05005","abilities":[{
          "trigger":{"event":"WhenDamageWouldBeDealt","timing":"Interrupt","subject":"you","form":"hero"},
          "effect":{"seq":[{"preventDamage":{"card":"trigger.target","amount":3}},{"draw":{"player":"you","count":1}}]}
        }]}]}
        """.Replace("BINDING", binding, StringComparison.Ordinal)));

    [Theory]
    [InlineData("trigger.subject")]
    [InlineData("trigger.discardedEvent")]
    [Rule("rr:event")]
    [Rule("rr:response.2")]
    public void NormalActionRecoversOnlyTheExactPlayedPhysicalCopy(string binding)
    {
        // rr:event: "then places the card in its owner's discard pile after
        // those effects resolve". Ruling 7f69f006cddce07e places recovery here.
        var runner = Runner(binding);
        Card? played = null;
        Card? duplicate = null;
        Card? payment = null;
        var (game, world) = Playing(board =>
        {
            Identity(board, 0);
            played = board.CreateCard("05003", board.Seats[0].Hand);
            duplicate = board.CreateCard("05003", board.AreaOf(DeckType.DiscardPile, PlayArea.Of(0), cardOwner: 0));
            payment = board.CreateCard("01088", board.Seats[0].Hand);
        }, abilities: runner);
        int held = world.Seats[0].Hand.Cards.Count;
        var action = Assert.Single(game.Pending!.Affordances, offer => offer.AnchorId == played!.ObjectId);
        game.Resolve(PaidDecision.Take(action, [payment!.ObjectId]));
        Assert.Equal(DeckType.DiscardPile, played!.Area.Type);
        Assert.Equal(held - 1, world.Seats[0].Hand.Cards.Count);
        var response = Assert.Single(game.Pending!.Affordances);
        Assert.Equal(world.Seats[0].IdentityCard.ObjectId, response.AnchorId);
        game.Resolve(Decision.Take(response.Id));
        Assert.Same(world.Seats[0].Hand, played.Area);
        Assert.Equal(DeckType.DiscardPile, duplicate!.Area.Type);
        Assert.False(world.Seats[0].IdentityCard.Ready);
        Assert.True(game.IsRootPrompt);
    }

    [Fact]
    [Rule("rr:response.4")]
    public void DecliningRecoveryLeavesThePlayedEventInDiscard()
    {
        // "Further responses to that instance ... cannot be used."
        var runner = Runner();
        Card? played = null;
        Card? payment = null;
        var (game, world) = Playing(board =>
        {
            Identity(board, 0);
            played = board.CreateCard("05003", board.Seats[0].Hand);
            payment = board.CreateCard("01088", board.Seats[0].Hand);
        }, abilities: runner);
        game.Resolve(PaidDecision.Take(Assert.Single(game.Pending!.Affordances,
            offer => offer.AnchorId == played!.ObjectId), [payment!.ObjectId]));
        game.Resolve(Decision.Decline);
        Assert.Equal(DeckType.DiscardPile, played!.Area.Type);
        Assert.True(world.Seats[0].IdentityCard.Ready);
        Assert.True(game.IsRootPrompt);
    }

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

    [Fact]
    public void OffTurnWindowEventFinishesItsEffectsBeforeExactRecovery()
    {
        // Rulings b144656def387a1e and 1086649ada076b70 permit off-turn
        // recovery after the event fully resolves, while the outer window lives.
        var runner = Runner();
        var (_, world) = Playing(board => Identity(board, 1),
            heroes: ["spider_man", "captain_marvel"], abilities: runner);
        var card = world.CreateCard("05005", world.Seats[1].Hand);
        var duplicate = world.CreateCard("05005", world.AreaOf(DeckType.DiscardPile, PlayArea.Of(1), cardOwner: 1));
        int held = world.Seats[1].Hand.Cards.Count;
        var occurrence = new Occurrence(91, [Steps.DamageWouldBeDealt], Player: 1,
            Target: world.Seats[1].IdentityCard.ObjectId);
        var interrupt = Assert.Single(runner.Waiting(world, occurrence, WindowKind.Interrupt));
        runner.ResolveWindow(world, occurrence, interrupt, Decision.Take(interrupt.Card));
        Assert.Equal(DeckType.DiscardPile, card.Area.Type);
        Assert.Equal(held, world.Seats[1].Hand.Cards.Count);
        Assert.Equal(2, runner.WouldTake(world, world.Seats[1].IdentityCard,
            world.Seats[0].IdentityCard, 5, []));
        var events = new List<GameEvent>();
        var prompt = Sequence.Work(world, world.Facts, runner, events)!;
        var recovery = Assert.Single(prompt.Affordances);
        Assert.Equal(world.Seats[1].IdentityCard.ObjectId, recovery.AnchorId);
        Sequence.Answer(world, world.Facts, runner, prompt, Decision.Take(recovery.Id), events);
        Sequence.Finish(world, world.Facts, runner, events);
        Assert.Same(world.Seats[1].Hand, card.Area);
        Assert.Equal(DeckType.DiscardPile, duplicate.Area.Type);
        Assert.False(world.Seats[1].IdentityCard.Ready);
        Assert.False(world.Agenda.IsBusy);
    }
}
