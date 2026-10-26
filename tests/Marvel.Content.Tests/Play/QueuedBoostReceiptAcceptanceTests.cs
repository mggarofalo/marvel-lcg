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
public sealed class QueuedBoostReceiptAcceptanceTests
{

    [Theory]
    [InlineData(false, false, false)]
    [InlineData(true, false, false)]
    [InlineData(true, true, false)]
    [InlineData(true, false, true)]
    [Rule("rr:activation.7")]
    [Rule("rr:activation.8")]
    [Rule("rr:activation.8.2")]
    public void DeferredBoostDamageCompletesWithoutChangingCompletedActivationReceipts(bool chooseDamage, bool prevent, bool unrelatedLive)
    {
        // rr:activation.7: "considered resolved after that activation has fully resolved".
        // rr:activation.8: "resolves after the current activation has finished resolving".
        // rr:activation.8.2: "All abilities triggered by the initial activation resolve
        // before subsequent activations initiate." This synthetic seq is engine traversal.
        var runner = SyntheticRunner();
        var world = Board(runner);
        var minion = world.CreateCard("01101", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        var boostCard = world.CreateCard("01154", world.AreaOf(DeckType.EncounterDeck));
        int held = world.Seats[0].Hand.Cards.Count;
        var events = Reveal(world, runner);
        Prompt? prompt;
        while ((prompt = Sequence.Work(world, world.Facts, runner, events))?.Asking == Question.Defender)
            Sequence.Answer(world, world.Facts, runner, prompt, Decision.Decline, events);
        Assert.Equal(Question.Option, prompt!.Asking);
        Assert.Equal(3, world.Seats[0].IdentityCard.Damage);
        var attacks = events.OfType<AttackCompleted>().ToArray();
        Assert.Equal(2, attacks.Length);
        Assert.NotEqual(minion.ObjectId, attacks[0].Enemy);
        Assert.Equal(minion.ObjectId, attacks[1].Enemy);
        Assert.Equal(2, attacks[0].DamageDealt);
        Assert.Equal(1, attacks[1].DamageDealt);
        Assert.Null(world.Activation);
        Assert.Null(world.FinishedActivation);
        Assert.Equal(held + 1, world.Seats[0].Hand.Cards.Count);
        // Synthetic unrelated live activation proves late damage never rebinds.
        var unrelated = new EnemyActivation(minion.ObjectId, 0, true, Id: 999);
        if (unrelatedLive) world.Activation = unrelated;
        ResolveLateOption(world, runner, prompt, events, boostCard, chooseDamage, prevent);
        if (unrelatedLive)
        {
            Assert.Same(unrelated, world.Activation);
            Assert.Equal(0, world.Activation!.DamagePlaced);
            world.Activation = null;
        }
        Finish(world, runner, events);
        Assert.Equal(attacks, events.OfType<AttackCompleted>().ToArray());
        Assert.Equal(chooseDamage && !prevent ? 4 : 3, world.Seats[0].IdentityCard.Damage);
        Assert.Equal(held + (chooseDamage ? 2 : 5), world.Seats[0].Hand.Cards.Count);
    }

    private static void ResolveLateOption(World world, AbilityRunner runner, Prompt prompt,
        List<GameEvent> events, Card boostCard, bool chooseDamage, bool prevent)
    {
        if (prevent) InPlay(world, "01092");
        Sequence.Answer(world, world.Facts, runner, prompt,
            Decision.Take(prompt.Affordances[chooseDamage ? 0 : 1].Id), events);
        if (prevent)
        {
            var replacement = Sequence.Work(world, world.Facts, runner, events)!;
            Assert.Equal(Question.Option, replacement.Asking);
            Assert.Equal(boostCard.ObjectId, world.Agenda.Occurrence!.Actor);
            Assert.Equal(3, world.Seats[0].IdentityCard.Damage);
            Sequence.Answer(world, world.Facts, runner, replacement,
                Decision.Take(replacement.Affordances[0].Id), events);
        }
    }

    private static AbilityRunner SyntheticRunner()
    {
        return Runner(Thresholds, """
            ,{"card":"01154","abilities":[{
              "trigger":{"event":"WhenCardRevealed","timing":"Boost","subject":"this"},
              "effect":{"seq":[{"enemyAttacks":{"enemies":{"query":"minions"}}},
                {"if":{"test":{"atLeast":{"value":{"result":"activationDamagePlaced"},"count":1}},
                  "then":{"draw":{"player":"you","count":1}}}},
                {"if":{"test":{"atLeast":{"value":{"result":"activationDamagePlaced"},"count":2}},
                  "then":{"draw":{"player":"you","count":3}}}},
                {"choose":{"options":[{"dealDamage":{"cards":"you","amount":1}},
                  {"draw":{"player":"you","count":3}}]}}]}
            }]},
            {"card":"01092","abilities":[{
              "trigger":{"event":"WhenDamageWouldBeDealt","timing":"ForcedInterrupt","target":"you"},
              "effect":{"choose":{"options":[{"preventDamage":"trigger.target"},
                {"draw":{"player":"you","count":1}}]}}
            }]}
            """);
    }

    private const string Thresholds = """
        {"enemyAttacks":{"enemies":{"query":"villain"}}},
        {"if":{"test":{"atLeast":{"value":{"result":"activationDamagePlaced"},"count":2}},
          "then":{"draw":{"player":"you","count":1}}}},
        {"if":{"test":{"atLeast":{"value":{"result":"activationDamagePlaced"},"count":3}},
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
