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

/// <summary>Synthetic probes for damage causally owned by a boost activation.</summary>
public sealed class ActivationBoostPlacementTests
{

    [Theory]
    [InlineData(false, 3)]
    [InlineData(true, 2)]
    [Rule("rr:attack-enemy-activation.step.3.b")]
    [Rule("rr:damage.step.5")]
    public void SuspendedBoostDamageCountsPlacementAfterPreventionOnce(bool prevent, long expected)
    {
        // "Resolve any Boost abilities" belongs to the activation; damage step 5
        // places only damage remaining after prevention.
        var runner = Runner(Thresholds, """
            ,{"card":"01154","abilities":[{
              "trigger":{"event":"WhenCardRevealed","timing":"Boost","subject":"this"},
              "effect":{"dealDamage":{"cards":"you","amount":1}}
            }]},
            {"card":"01092","abilities":[{
              "trigger":{"event":"WhenDamageWouldBeDealt","timing":"ForcedInterrupt","target":"you"},
              "effect":{"choose":{"options":[{"preventDamage":"trigger.target"},
                {"draw":{"player":"you","count":1}}]}}
            }]}
            """);
        var world = Board(runner);
        InPlay(world, "01092");
        world.CreateCard("01154", world.AreaOf(DeckType.EncounterDeck));
        int held = world.Seats[0].Hand.Cards.Count;
        var events = Reveal(world, runner);
        var defense = Sequence.Work(world, world.Facts, runner, events)!;
        Sequence.Answer(world, world.Facts, runner, defense, Decision.Decline, events);
        var boost = Sequence.Work(world, world.Facts, runner, events)!;
        Assert.Equal(Question.Option, boost.Asking);
        Assert.Equal(0, world.Activation!.DamagePlaced);
        Sequence.Answer(world, world.Facts, runner, boost, Decision.Take(boost.Affordances[prevent ? 0 : 1].Id), events);
        var attack = Sequence.Work(world, world.Facts, runner, events)!;
        Assert.Equal(Question.Option, attack.Asking);
        Assert.Equal(prevent ? 0 : 1, world.Activation!.DamagePlaced);
        Sequence.Answer(world, world.Facts, runner, attack, Decision.Take(attack.Affordances[1].Id), events);
        Finish(world, runner, events);
        Assert.Equal(expected, world.Seats[0].IdentityCard.Damage);
        Assert.Equal(held + (prevent ? 1 : 3), world.Seats[0].Hand.Cards.Count);
        Assert.Equal(2, Assert.Single(events.OfType<AttackCompleted>()).DamageDealt);
    }

    [Fact]
    public void PlayerInterruptDamageDoesNotJoinTheActivationReceipt()
    {
        var runner = Runner(Thresholds, """
            ,{"card":"01001a","abilities":[{
              "trigger":{"event":"WhenAttackInitiated","timing":"ForcedInterrupt","actor":"villain","player":"you"},
              "effect":{"dealDamage":{"cards":"you","amount":1}}
            }]}
            """);
        var world = Board(runner);
        int held = world.Seats[0].Hand.Cards.Count;
        var events = Reveal(world, runner);
        Finish(world, runner, events);
        Assert.Equal(3, world.Seats[0].IdentityCard.Damage);
        Assert.Equal(held, world.Seats[0].Hand.Cards.Count);
        Assert.Equal(2, Assert.Single(events.OfType<AttackCompleted>()).DamageDealt);
    }

    [Fact]
    public void BoostChoiceRetainsCapturedOwnership()
    {
        var runner = Runner(Thresholds, """
            ,{"card":"01154","abilities":[{
              "trigger":{"event":"WhenCardRevealed","timing":"Boost","subject":"this"},
              "effect":{"choose":{"options":[{"dealDamage":{"cards":"you","amount":1}},
                {"draw":{"player":"you","count":3}}]}}
            }]}
            """);
        var world = Board(runner);
        world.CreateCard("01154", world.AreaOf(DeckType.EncounterDeck));
        int held = world.Seats[0].Hand.Cards.Count;
        var events = Reveal(world, runner);
        var defense = Sequence.Work(world, world.Facts, runner, events)!;
        Sequence.Answer(world, world.Facts, runner, defense, Decision.Decline, events);
        var choice = Sequence.Work(world, world.Facts, runner, events)!;
        Assert.Equal(Question.Option, choice.Asking);
        Sequence.Answer(world, world.Facts, runner, choice, Decision.Take(choice.Affordances[0].Id), events);
        Finish(world, runner, events);
        Assert.Equal(3, world.Seats[0].IdentityCard.Damage);
        Assert.Equal(held + 1, world.Seats[0].Hand.Cards.Count);
        Assert.Equal(2, Assert.Single(events.OfType<AttackCompleted>()).DamageDealt);
    }

    private const string Thresholds = """
        {"enemyAttacks":{"enemies":{"query":"villain"}}},
        {"if":{"test":{"atLeast":{"value":{"result":"activationDamagePlaced"},"count":3}},
          "then":{"draw":{"player":"you","count":1}}}},
        {"if":{"test":{"atLeast":{"value":{"result":"activationDamagePlaced"},"count":4}},
          "then":{"draw":{"player":"you","count":3}}}}
        """;

    private static AbilityRunner Runner(string effect, string extra) => new(AbilityCatalog.Parse(
        "{\"cards\":[{\"card\":\"02031\",\"abilities\":[{\"trigger\":{\"event\":\"WhenCardRevealed\",\"timing\":\"WhenRevealed\",\"subject\":\"this\"},\"effect\":{\"seq\":["
        + effect + "]}}]}" + extra + "]}"));

    private static World Board(AbilityRunner runner)
    {
        var (_, world) = Playing(_ => { }, hero: true, abilities: runner);
        for (int i = 0; i < 12; i++) world.CreateCard("01087", world.AreaOf(DeckType.EncounterDeck));
        return world;
    }

    private static List<GameEvent> Reveal(World world, AbilityRunner runner)
    {
        runner.ExecuteReveal(world, world.CreateCard("02031", world.AreaOf(DeckType.RevealingArea)), 0);
        return [];
    }

    private static void Finish(World world, AbilityRunner runner, List<GameEvent> events)
    {
        for (int i = 0; i < 30; i++)
        {
            var prompt = Sequence.Work(world, world.Facts, runner, events);
            if (prompt is null) break;
            Sequence.Answer(world, world.Facts, runner, prompt, Decision.Decline, events);
        }
        Assert.False(world.IsOver);
        Assert.Empty(world.Agenda.Outstanding);
        Assert.Null(world.Activation);
    }
}
