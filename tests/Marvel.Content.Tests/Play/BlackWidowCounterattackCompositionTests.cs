using System.Text.Json.Nodes;
using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

// Test-local actual printed interpretation; no public admission or ability-book change.
public sealed class BlackWidowCounterattackCompositionTests
{
    [Theory]
    [InlineData(0, 2)]
    [InlineData(1, 1)]
    [InlineData(2, 0)]
    [Rule("rr:damage.3.2")]
    public void ActualCounterattackReturnsOnlyDamageTakenFromTheEnemyAttack(int prevention, int expected)
    {
        // Prevention modifies "the amount of damage a character takes", not damage dealt.
        // 08030: "After you take damage from an enemy attack, discard Counterattack"
        // and "deal an equal amount of damage to that enemy."
        var book = JsonNode.Parse(AuthoredCards.Text)!;
        book["cards"]!.AsArray().Add(JsonNode.Parse(Row));
        var runner = new AbilityRunner(AbilityCatalog.Parse(book.ToJsonString()));
        var world = ThorActualFixture.Board("06001a", 2);
        var seat = world.Seats[1];
        World.MoveToTop(seat.IdentityCard, world.AreaOf(DeckType.RemovedArea));
        var hero = seat.IdentityCard = world.CreateCard("08001a,08001b", seat.Hero);
        var counter = world.CreateCard("08030",
            world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(1), cardOwner: 1));
        world.Abilities = runner;
        var enemy = world.CreateCard("01103",
            world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1)));
        if (prevention > 0)
            world.Effects.Register(new(EffectSource.LastingEffect, "preventDamage", prevention,
                Affects: hero.ObjectId, Lasts: Duration.NextUses(1)));
        world.Agenda.Add(new PhaseStep(new EnemyAttackOperation(), 1, 2,
            Subject: enemy.ObjectId, Seat: 1));
        var events = new List<GameEvent>();
        bool used = false;
        for (int index = 0; index < 20; index++)
        {
            var prompt = Sequence.Work(world, world.Facts, runner, events);
            if (prompt is null) break;
            var offer = prompt.Affordances.SingleOrDefault(a => a.AnchorId == counter.ObjectId);
            if (offer is not null)
            {
                Assert.False(used);
                Assert.Equal(1, prompt.Player);
                Assert.True(prompt.Cancellable);
                Sequence.Answer(world, world.Facts, runner, prompt, Decision.Take(offer.Id), events);
                used = true;
            }
            else
            {
                Assert.True(prompt.Cancellable);
                Sequence.Answer(world, world.Facts, runner, prompt, Decision.Decline, events);
            }
        }
        Assert.Equal(expected > 0, used);
        Assert.Equal(expected, hero.Damage);
        Assert.Equal(expected, enemy.Damage);
        Assert.Equal(expected > 0 ? DeckType.DiscardPile : DeckType.UpgradesArea, counter.Area.Type);
        Assert.Single(events.OfType<AttackCompleted>());
        Assert.False(world.IsOver);
        Assert.Null(Sequence.Work(world, world.Facts, runner, events));
    }

    internal const string Row = """
        {"card":"08030","abilities":[{
          "trigger":{"event":"WhenDamageDealt","timing":"Response","actor":"enemy","target":"you","form":"hero"},
          "when":{"atLeast":{"value":{"receivedAttackDamage":"game"},"count":1}},
          "labels":["attack"],
          "cost":{"discard":"this"},
          "effect":{"attack":{"target":"trigger.actor","effect":{
            "dealAttackDamage":{"cards":"trigger.actor","amount":{"receivedAttackDamage":"game"}}
          }}}
        }]}
        """;
}
