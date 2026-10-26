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

/// <summary>Synthetic review probes for the activation-placement contract.</summary>
public sealed class ActivationPlacementIndependentTests
{
    [Fact]
    public void BoostRequestedMinionAndOuterContinuationKeepSeparatePlacement()
    {
        var runner = Runner("""
            {"enemyAttacks":{"enemies":{"query":"villain"}}},
            {"if":{"test":{"atLeast":{"value":{"result":"activationDamagePlaced"},"count":3}},
              "then":{"draw":{"player":"you","count":1}}}}
            """, """
            ,{"card":"01154","abilities":[{
              "trigger":{"event":"WhenCardRevealed","timing":"Boost","subject":"this"},
              "effect":{"seq":[{"enemyAttacks":{"enemies":{"query":"minions"}}},
                {"if":{"test":{"atLeast":{"value":{"result":"activationDamagePlaced"},"count":1}},
                  "then":{"draw":{"player":"you","count":1}}}},
                {"if":{"test":{"atLeast":{"value":{"result":"activationDamagePlaced"},"count":2}},
                  "then":{"draw":{"player":"you","count":3}}}}]}
            }]}
            """);
        var world = Board(runner);
        world.CreateCard("01101", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        World.MoveToTop(world.CreateCard("01154", world.AreaOf(DeckType.EncounterDeck)), world.AreaOf(DeckType.EncounterDeck));
        int held = world.Seats[0].Hand.Cards.Count;
        var events = Reveal(world, runner);
        Finish(world, runner, events);
        Assert.Equal(3, world.Seats[0].IdentityCard.Damage);
        Assert.Equal(held + 1, world.Seats[0].Hand.Cards.Count);
        Assert.Equal(2, events.OfType<AttackCompleted>().Count());
    }

    [Theory]
    [InlineData(false, false, 2)]
    [InlineData(false, true, 0)]
    [InlineData(true, false, 2)]
    [InlineData(true, true, 0)]
    public void SuspendedDamagePreventionRecordsOnce(bool indirect, bool prevent, long expected)
    {
        var runner = Runner("""
            {"enemyAttacks":{"enemies":{"query":"villain"}}},
            {"if":{"test":{"atLeast":{"value":{"result":"activationDamagePlaced"},"count":2}},
              "then":{"draw":{"player":"you","count":1}}}},
            {"if":{"test":{"atLeast":{"value":{"result":"activationDamagePlaced"},"count":3}},
              "then":{"draw":{"player":"you","count":3}}}}
            """, """
            ,{"card":"01092","abilities":[{
              "trigger":{"event":"WhenDamageWouldBeDealt","timing":"ForcedInterrupt","target":"you"},
              "effect":{"choose":{"options":[{"preventDamage":"trigger.target"},
                {"draw":{"player":"you","count":1}}]}}
            }]}
            """);
        var world = Board(runner);
        InPlay(world, "01092");
        if (indirect) world.CreateCard("01020", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        int held = world.Seats[0].Hand.Cards.Count;
        var events = Reveal(world, runner);
        var defense = Sequence.Work(world, world.Facts, runner, events)!;
        Assert.Equal(Question.Defender, defense.Asking);
        if (indirect) Attack.MakeIndirect(world);
        Sequence.Answer(world, world.Facts, runner, defense, Decision.Decline, events);
        var prompt = Sequence.Work(world, world.Facts, runner, events)!;
        if (indirect)
        {
            var offer = Assert.Single(prompt.Affordances);
            Sequence.Answer(world, world.Facts, runner, prompt,
                Decision.Take(offer.Id, [.. Enumerable.Repeat(world.Seats[0].IdentityCard.ObjectId, offer.Targets!.Min)], []), events);
            prompt = Sequence.Work(world, world.Facts, runner, events)!;
        }
        Assert.Equal(Question.Option, prompt.Asking);
        Assert.Equal(0, world.Activation!.DamagePlaced);
        Assert.Equal(0, world.Seats[0].IdentityCard.Damage);
        Sequence.Answer(world, world.Facts, runner, prompt, Decision.Take(prompt.Affordances[prevent ? 0 : 1].Id), events);
        Finish(world, runner, events);
        Assert.Equal(expected, world.Seats[0].IdentityCard.Damage);
        Assert.Equal(held + (prevent ? 0 : 2), world.Seats[0].Hand.Cards.Count);
        Assert.Equal(2, Assert.Single(events.OfType<AttackCompleted>()).DamageDealt);
    }

    [Fact]
    [Rule("rr:attack-enemy-activation.step.3.b")]
    [Rule("rr:damage.step.5")]
    public void OverconfidenceThresholdCountsItsActivationsConcussiveBlastPlacement()
    {
        var runner = Runner("""
            {"enemyAttacks":{"enemies":{"query":"villain"}}},
            {"if":{"test":{"atLeast":{"value":{"result":"activationDamagePlaced"},"count":3}},
              "then":{"draw":{"player":"you","count":1}}}}
            """, """
            ,{"card":"01154","abilities":[{
              "trigger":{"event":"WhenCardRevealed","timing":"Boost","subject":"this"},
              "effect":{"dealDamage":{"cards":{"query":"charactersYouControl"},"amount":1}}
            }]}
            """);
        var world = Board(runner);
        world.CreateCard("01154", world.AreaOf(DeckType.EncounterDeck));
        int held = world.Seats[0].Hand.Cards.Count;
        var events = Reveal(world, runner);
        Finish(world, runner, events);
        Assert.Equal(3, world.Seats[0].IdentityCard.Damage);
        // Printed 02031 counts damage "placed by this activation"; resolving
        // this boost is activation step 3.b, before the attack's step-5 damage.
        Assert.Equal(held + 1, world.Seats[0].Hand.Cards.Count);
        Assert.Equal(2, Assert.Single(events.OfType<AttackCompleted>()).DamageDealt);
    }

    [Theory]
    [InlineData(false, 1)]
    [InlineData(true, 0)]
    public void IndirectRecipientSharesSumAfterEachRecipientsPrevention(bool toughAlly, int drawn)
    {
        var runner = Runner("""
            {"enemyAttacks":{"enemies":{"query":"villain"}}},
            {"if":{"test":{"atLeast":{"value":{"result":"activationDamagePlaced"},"count":2}},
              "then":{"draw":{"player":"you","count":1}}}}
            """, "");
        var world = Board(runner);
        var hero = world.Seats[0].IdentityCard;
        var ally = world.CreateCard("01020", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        if (toughAlly) Statuses.Give(world, ally, Statuses.Tough);
        int held = world.Seats[0].Hand.Cards.Count;
        var events = Reveal(world, runner);
        var defense = Sequence.Work(world, world.Facts, runner, events)!;
        Assert.Equal(Question.Defender, defense.Asking);
        Attack.MakeIndirect(world);
        Sequence.Answer(world, world.Facts, runner, defense, Decision.Decline, events);
        var assign = Sequence.Work(world, world.Facts, runner, events)!;
        var offer = Assert.Single(assign.Affordances);
        Assert.Equal(2, offer.Targets!.Min);
        Sequence.Answer(world, world.Facts, runner, assign,
            Decision.Take(offer.Id, [hero.ObjectId, ally.ObjectId], []), events);
        Finish(world, runner, events);
        Assert.Equal(1, hero.Damage);
        Assert.Equal(toughAlly ? 0 : 1, ally.Damage);
        Assert.Equal(held + drawn, world.Seats[0].Hand.Cards.Count);
        Assert.Equal(2, Assert.Single(events.OfType<AttackCompleted>()).DamageDealt);
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(2, 0)]
    public void SchemeReceiptUsesPreventedThreatAndClearsEarlierDamage(int threshold, int drawn)
    {
        var runner = Runner("""
            {"enemyAttacks":{"enemies":{"query":"villain"}}},
            {"enemySchemes":{"enemies":{"query":"villain"}}},
            {"if":{"test":{"atLeast":{"value":{"result":"activationDamagePlaced"},"count":1}},
              "then":{"draw":{"player":"you","count":3}}}},
            {"if":{"test":{"atLeast":{"value":{"result":"activationThreat"},"count":THRESHOLD}},
              "then":{"draw":{"player":"you","count":1}}}}
            """.Replace("THRESHOLD", threshold.ToString()), """
            ,{"card":"01008","abilities":[{
              "trigger":{"event":"WhenThreatWouldBePlaced","timing":"ForcedInterrupt","subject":"game"},
              "effect":{"preventThreat":1}
            }]}
            """);
        var world = Board(runner);
        InPlay(world, "01008");
        world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect, "scheme", Amount: 1,
            Affects: world.TheCardIn(DeckType.VillainArea)!.ObjectId));
        var scheme = world.TheCardIn(DeckType.MainSchemesArea)!;
        long before = scheme.Tokens.GetValueOrDefault("k_threat");
        int held = world.Seats[0].Hand.Cards.Count;
        var events = Reveal(world, runner);
        Finish(world, runner, events);
        Assert.Equal(before + 1, scheme.Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(held + drawn, world.Seats[0].Hand.Cards.Count);
        Assert.Equal(2, world.Seats[0].IdentityCard.Damage);
    }

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
