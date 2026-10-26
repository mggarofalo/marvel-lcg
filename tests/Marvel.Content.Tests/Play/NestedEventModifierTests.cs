using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class NestedEventModifierTests
{
    // Synthetic instructions isolate event modifier routing without admitting
    // Embiggen!, Big Hands or Wiggle Room as executable pack content.
    private static AbilityRunner Runner(string trait = "ATTACK") => new(AbilityCatalog.Parse("""
        {"cards":[{"card":"05010","abilities":[{
          "trigger":{"event":"WhenEventPlayInitiated","timing":"Interrupt","subject":"you","form":"hero"},
          "when":{"hasTrait":{"card":"trigger.subject","trait":"TRAIT"}},
          "cost":{"exhaust":"this"},"effect":{"modifyPlayedEvent":{"quantity":"damage","amount":2}}
        }]},{"card":"05003","abilities":[{
          "trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"this","form":"hero"},
          "effect":{"seq":[{"draw":{"player":"you","count":1}},{"dealDamage":{"cards":{"query":"villain"},"amount":3}}]}
        }]},{"card":"05005","abilities":[{
          "trigger":{"event":"WhenDamageWouldBeDealt","timing":"Interrupt","subject":"you","form":"hero"},
          "effect":{"seq":[{"draw":{"player":"you","count":1}},{"dealDamage":{"cards":{"query":"villain"},"amount":3}}]}
        }]}]}
        """.Replace("TRAIT", trait, StringComparison.Ordinal)));

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
        Assert.Equal(held, world.Seats[0].Hand.Cards.Count);
        Assert.Equal(5, villain.Damage);
        Assert.DoesNotContain(world.Effects.Registered, effect => effect.Kind == "eventDamage");
        Assert.Equal(DeckType.DiscardPile, played.Area.Type);
        Assert.False(upgrade.Ready);
        Assert.Equal(1, hero.Damage);
        Assert.False(world.Agenda.IsBusy);
    }
}
