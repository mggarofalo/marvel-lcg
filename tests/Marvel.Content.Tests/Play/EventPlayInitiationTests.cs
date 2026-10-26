using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class EventPlayInitiationTests
{
    // Synthetic draw instructions isolate initiation timing without admitting
    // Embiggen!, Big Hands or Wiggle Room as executable pack content.
    private static AbilityRunner Runner(string trait = "ATTACK") => new(AbilityCatalog.Parse("""
        {"cards":[{"card":"05010","abilities":[{
          "trigger":{"event":"WhenEventPlayInitiated","timing":"Interrupt","subject":"you","form":"hero"},
          "when":{"hasTrait":{"card":"trigger.subject","trait":"TRAIT"}},
          "cost":{"exhaust":"this"},"effect":{"draw":{"player":"you","count":1}}
        }]},{"card":"05003","abilities":[{
          "trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"this","form":"hero"},
          "effect":{"draw":{"player":"you","count":1}}
        }]},{"card":"05005","abilities":[{
          "trigger":{"event":"WhenDamageWouldBeDealt","timing":"Interrupt","subject":"you","form":"hero"},
          "effect":{"draw":{"player":"you","count":1}}
        }]}]}
        """.Replace("TRAIT", trait, StringComparison.Ordinal)));

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    [Rule("rr:initiating-abilities.step.6")]
    [Rule("rr:initiating-abilities.step.7")]
    public void PaidActionPausesBeforeItsEffectsAndResumesOnce(bool accept)
    {
        // "The card commences being played" precedes "its effects resolve".
        var runner = Runner();
        Card? upgrade = null;
        Card? played = null;
        Card? payment = null;
        var (game, world) = Playing(board =>
        {
            upgrade = board.CreateCard("05010", board.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
            played = board.CreateCard("05003", board.Seats[0].Hand);
            payment = board.CreateCard("01088", board.Seats[0].Hand);
        }, hero: true, abilities: runner);
        int held = world.Seats[0].Hand.Cards.Count;
        game.Resolve(PaidDecision.Take(Assert.Single(game.Pending!.Affordances,
            offer => offer.AnchorId == played!.ObjectId), [payment!.ObjectId]));
        Assert.Equal(DeckType.RevealingArea, played!.Area.Type);
        Assert.Equal(DeckType.DiscardPile, payment!.Area.Type);
        Assert.Equal(held - 2, world.Seats[0].Hand.Cards.Count);
        Assert.True(upgrade!.Ready);
        var interrupt = Assert.Single(game.Pending!.Affordances);
        Assert.Equal(upgrade.ObjectId, interrupt.AnchorId);
        game.Resolve(accept ? Decision.Take(interrupt.Id) : Decision.Decline);
        Assert.Equal(DeckType.DiscardPile, played.Area.Type);
        Assert.Equal(held - 1 + (accept ? 1 : 0), world.Seats[0].Hand.Cards.Count);
        Assert.Equal(!accept, upgrade.Ready);
        Assert.True(game.IsRootPrompt);
    }

    [Theory]
    [InlineData("wrongTrait")]
    [InlineData("exhausted")]
    [InlineData("otherPlayer")]
    public void IneligibleUpgradeDoesNotCreateAnInitiationPrompt(string control)
    {
        var runner = Runner(control == "wrongTrait" ? "THWART" : "ATTACK");
        Card? played = null;
        Card? payment = null;
        var (game, world) = Playing(board =>
        {
            int owner = control == "otherPlayer" ? 1 : 0;
            var upgrade = board.CreateCard("05010", board.AreaOf(DeckType.UpgradesArea, PlayArea.Of(owner), cardOwner: owner));
            if (control == "exhausted") upgrade.Exhaust();
            played = board.CreateCard("05003", board.Seats[0].Hand);
            payment = board.CreateCard("01088", board.Seats[0].Hand);
        }, hero: true, heroes: ["spider_man", "captain_marvel"], abilities: runner);
        int held = world.Seats[0].Hand.Cards.Count;
        game.Resolve(PaidDecision.Take(Assert.Single(game.Pending!.Affordances,
            offer => offer.AnchorId == played!.ObjectId), [payment!.ObjectId]));
        Assert.True(game.IsRootPrompt);
        Assert.Equal(DeckType.DiscardPile, played!.Area.Type);
        Assert.Equal(held - 1, world.Seats[0].Hand.Cards.Count);
    }

    [Fact]
    [Rule("rr:initiating-abilities.1")]
    public void NestedWindowEventResumesInsideItsOwningDamageWindow()
    {
        // "that ability may be initiated just before that triggering condition becomes true."
        var runner = Runner("DEFENSE");
        var (_, world) = Playing(_ => { }, hero: true, abilities: runner);
        var upgrade = world.CreateCard("05010", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
        var played = world.CreateCard("05005", world.Seats[0].Hand);
        int held = world.Seats[0].Hand.Cards.Count;
        var hero = world.Seats[0].IdentityCard;
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        world.Activation = new(villain.ObjectId, 0, Attacking: true);
        world.Attack = new(villain.ObjectId, 0, hero.ObjectId, CalculatedDamage: 1);
        world.Agenda.Add(new PhaseStep(new AttackStageOperation(AttackStageKind.DealAttackDamage),
            Round: 1, Number: 5, Subject: villain.ObjectId, Seat: 0));
        var events = new List<GameEvent>();
        var damageWindow = Sequence.Work(world, world.Facts, runner, events)!;
        Sequence.Answer(world, world.Facts, runner, damageWindow,
            Decision.Take(Assert.Single(damageWindow.Affordances).Id), events);
        var initiation = Sequence.Work(world, world.Facts, runner, events)!;
        Assert.Equal(upgrade.ObjectId, Assert.Single(initiation.Affordances).AnchorId);
        Assert.Equal(DeckType.RevealingArea, played.Area.Type);
        Assert.Equal(0, hero.Damage);
        Sequence.Answer(world, world.Facts, runner, initiation,
            Decision.Take(initiation.Affordances[0].Id), events);
        Sequence.Finish(world, world.Facts, runner, events);
        Assert.Equal(held + 1, world.Seats[0].Hand.Cards.Count);
        Assert.Equal(DeckType.DiscardPile, played.Area.Type);
        Assert.False(upgrade.Ready);
        Assert.Equal(1, hero.Damage);
        Assert.False(world.Agenda.IsBusy);
    }
}


