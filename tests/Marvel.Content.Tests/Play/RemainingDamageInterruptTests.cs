using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class RemainingDamageInterruptTests : CoreCardTestBase
{
    // Test-only interpretation of printed 05017; this does not admit the card.
    private static AbilityRunner Runner() => new(AbilityCatalog.Parse("""
        {"cards":[
        {
          "card": "05017",
          "name": "Energy Barrier",
          "note": "Printed Uses (3 reflection counters). Interrupt removes one reflection counter to prevent one damage to your identity and deal one damage to a chosen enemy. Any damage and either form qualify; this is not a defense ability. rr:uses-x-type discards the upgrade when its final counter is removed.",
          "startingCounters": {
            "type": "reflection",
            "count": 3,
            "uses": true
          },
          "abilities": [
            {
              "trigger": {
                "event": "WhenDamageWouldBeDealt",
                "damage": "taken",
                "timing": "Interrupt",
                "target": "you"
              },
              "when": {
                "isYourIdentity": "trigger.target"
              },
              "cost": {
                "removeCounters": {
                  "card": "this",
                  "counter": "reflection",
                  "count": 1
                }
              },
              "effect": {
                "seq": [
                  {
                    "preventDamage": {
                      "card": "trigger.target",
                      "amount": 1
                    }
                  },
                  {
                    "chooseCard": {
                      "from": {
                        "query": "enemies"
                      },
                      "effect": {
                        "dealDamage": {
                          "cards": "chosen",
                          "amount": 1
                        }
                      }
                    }
                  }
                ]
              }
            }
          ]
        }
        ]}
        """));

    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 0)]
    [InlineData(2, 0)]
    [InlineData(1, 1)]
    [InlineData(2, 1)]
    [Rule("rr:prevent.1")]
    [Rule("rr:prevent.1.1")]
    public void BarriersSpendOnlyWhileDamageRemainsAndDoNotLeakIntoTheNextAttack(int amount, int player)
    {
        // faq:05017: "there is no more damage left to trigger the other ones."
        // rr:prevent.1.1: the amount "dealt" is not reduced by prevention.
        var world = Hero("05001a", 2);
        var hero = world.Seats[player].IdentityCard;
        var villain = world.CreateCard("01094", world.AreaOf(DeckType.VillainArea));
        var barriers = Enumerable.Range(0, 2).Select(_ => Barrier(world, player)).ToArray();
        var foreign = Barrier(world, 1 - player);
        var runner = Runner();
        world.Abilities = runner;
        // Rhino I has printed ATK 2; the stored amount is authoritative after reduction.
        Assert.Equal(2, Attack.Amount(world, Cards, new(villain.ObjectId, player, hero.ObjectId)));
        BeginAttack(world, villain, hero, player, amount);
        var events = new List<GameEvent>();
        for (int index = 0; index < amount; index++)
        {
            var prompt = Assert.IsType<Prompt>(Sequence.Work(world, Cards, runner, events));
            Assert.Equal(player, prompt.Player);
            Assert.Equal(2 - index, prompt.Affordances.Count);
            Assert.DoesNotContain(prompt.Affordances, offer => offer.AnchorId == foreign.ObjectId);
            var occurrence = world.Windows.Current!.Value.Occurrence;
            string before = world.Digest().Canonical();
            long random = world.Random.Generator.WordsConsumed;
            var randomState = world.Random.Generator.GetState();
            var pending = world.Agenda.Outstanding.ToArray();
            var effects = world.Effects.Active().ToArray();
            for (int query = 0; query < 3; query++)
                Assert.Equal(2, runner.Waiting(world, occurrence, WindowKind.Interrupt).Count);
            Assert.Equal(before, world.Digest().Canonical());
            Assert.Equal(random, world.Random.Generator.WordsConsumed);
            Assert.Equal(randomState.Index, world.Random.Generator.GetState().Index);
            Assert.Equal(randomState.Words, world.Random.Generator.GetState().Words);
            Assert.Equal(pending, world.Agenda.Outstanding);
            Assert.Equal(effects, world.Effects.Active());
            Assert.Throws<RulesNotImplementedException>(() => Sequence.Answer(world, Cards, runner, prompt,
                Decision.Take(int.MaxValue), events));
            Assert.Equal(before, world.Digest().Canonical());
            Assert.Equal(random, world.Random.Generator.WordsConsumed);
            Sequence.Answer(world, Cards, runner, prompt, Decision.Take(prompt.Affordances[0].Id), events);
            var target = Assert.IsType<Prompt>(Sequence.Work(world, Cards, runner, events));
            Sequence.Answer(world, Cards, runner, target, Decision.Take(villain.ObjectId), events);
        }
        Sequence.Finish(world, Cards, runner, events);
        Assert.False(world.Agenda.IsBusy);
        Assert.Equal(0, hero.Damage);
        Assert.Equal(amount, villain.Damage);
        Assert.Equal(amount, world.Activation!.DamageDealt);
        Assert.Equal(new long[] { 3 - Math.Min(amount, 1), 3 - Math.Max(0, amount - 1) }.Order(),
            barriers.Select(card => card.Tokens["c_reflection"]).Order());
        Assert.Equal(6 - amount, barriers.Sum(card => card.Tokens["c_reflection"]));
        Assert.Equal(3, foreign.Tokens["c_reflection"]);
        Assert.DoesNotContain(world.Effects.Active(), effect => effect.Kind == "preventDamage");

        BeginAttack(world, villain, hero, player, 1);
        var next = Assert.IsType<Prompt>(Sequence.Work(world, Cards, runner, events));
        Assert.Equal(2, next.Affordances.Count);
        Sequence.Answer(world, Cards, runner, next, Decision.Decline, events);
        Sequence.Finish(world, Cards, runner, events);
        Assert.Equal(1, hero.Damage);
        Assert.False(world.Agenda.IsBusy);
    }

    [Fact]
    public void ResolvingAnOfferAfterItsDamageIsFullyPreventedCannotSpendACounter()
    {
        var world = Hero("05001a");
        var hero = world.Seats[0].IdentityCard;
        var villain = world.CreateCard("01094", world.AreaOf(DeckType.VillainArea));
        var first = Barrier(world, 0);
        var second = Barrier(world, 0);
        var runner = Runner();
        world.Abilities = runner;
        BeginAttack(world, villain, hero, 0, 1);
        var events = new List<GameEvent>();
        var prompt = Sequence.Work(world, Cards, runner, events)!;
        var occurrence = world.Windows.Current!.Value.Occurrence;
        var stale = Assert.Single(runner.Waiting(world, occurrence, WindowKind.Interrupt), a => a.Card == second.ObjectId);
        Sequence.Answer(world, Cards, runner, prompt,
            Decision.Take(Assert.Single(prompt.Affordances, a => a.AnchorId == first.ObjectId).Id), events);
        var target = Sequence.Work(world, Cards, runner, events)!;
        Sequence.Answer(world, Cards, runner, target, Decision.Take(villain.ObjectId), events);
        Assert.NotNull(occurrence.Damage);
        Assert.False(DamageWindowEligibility.HasRemainingDamage(world, occurrence));
        string before = world.Digest().Canonical();
        var pending = world.Agenda.Outstanding.ToArray();
        var effects = world.Effects.Registered.ToArray();
        var random = world.Random.Generator.GetState();
        long consumed = world.Random.Generator.WordsConsumed;
        var window = world.Windows.Current;
        var stage = world.Agenda.Stage;
        Assert.Throws<RulesNotImplementedException>(() => runner.PrepareWindow(world, occurrence, stale, Decision.Take(second.ObjectId)));
        Assert.Equal(before, world.Digest().Canonical());
        Assert.Equal(pending, world.Agenda.Outstanding);
        Assert.Equal(effects, world.Effects.Registered);
        Assert.Equal(random.Index, world.Random.Generator.GetState().Index);
        Assert.Equal(random.Words, world.Random.Generator.GetState().Words);
        Assert.Equal(consumed, world.Random.Generator.WordsConsumed);
        Assert.Equal(window, world.Windows.Current);
        Assert.Equal(stage, world.Agenda.Stage);
        Assert.Throws<RulesNotImplementedException>(() => runner.ResolveWindow(world, occurrence, stale, Decision.Take(second.ObjectId)));
        Assert.Equal(before, world.Digest().Canonical());
        Assert.Equal(3, second.Tokens["c_reflection"]);
        Sequence.Finish(world, Cards, runner, events);
        Assert.Equal(0, hero.Damage);
        Assert.Equal(1, villain.Damage);
    }

    private static Card Barrier(World world, int player)
    {
        var card = world.CreateCard("05017", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(player), cardOwner: player));
        card.PlaceTokens("c_reflection", 3);
        return card;
    }

    private static void BeginAttack(World world, Card villain, Card hero, int player, int amount)
    {
        world.Activation = new(villain.ObjectId, player, Attacking: true);
        world.Attack = new(villain.ObjectId, player, hero.ObjectId, CalculatedDamage: amount);
        world.Agenda.Add(new PhaseStep(new AttackStageOperation(AttackStageKind.DealAttackDamage),
            1, 5, Subject: villain.ObjectId, Seat: player));
    }
}
