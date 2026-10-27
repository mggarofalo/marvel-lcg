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

/// <summary>Synthetic activation-owned defeat composition for printed 90004; no runtime admission.</summary>
public sealed class ActivationDefeatOutcomeTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:boost-boost-icon.2")]
    [Rule("rr:activation.7")]
    public void DelayedReceiptDistinguishesEqualDamageLethalAndSurvivingAttacks(bool survives)
    {
        // Printed 90004: "If this activation defeats a character, deal the first
        // player 1 facedown encounter card." The equal five damage in both
        // cases distinguishes actual defeat from a positive damage total.
        var runner = Runner();
        var (world, ally) = Board(runner, survives);
        var events = Finish(world, runner, ally);

        Assert.Equal(survives ? DeckType.AlliesArea : DeckType.DiscardPile, ally.Area.Type);
        var completed = Assert.Single(events.OfType<AttackCompleted>());
        Assert.Equal(5, completed.DamageDealt);
        Assert.Equal(1, world.Seats[0].IdentityCard.Tokens.GetValueOrDefault("c_probeCompleted"));
        Assert.Equal(5, world.Seats[0].IdentityCard.Tokens.GetValueOrDefault("c_probeDamage"));
        Assert.Equal(survives ? 0 : 1,
            world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(0)).Cards.Count);
        Assert.False(world.IsOver);
        Assert.Null(world.Activation);
        Assert.Empty(world.Agenda.Outstanding);
    }

    [Fact]
    [Rule("rr:boost-boost-icon.2")]
    public void DelayedReceiptReachesDistinctFirstPlayerOnceAfterAllyDefeat()
    {
        // rr:boost-boost-icon.2: only boost text beneath the divider is active.
        // The first player differs from the attacked player and chosen identity.
        var runner = Runner(suspended: true);
        var (world, ally) = Board(runner, survives: false);
        world.FirstPlayer = 1;
        var events = Finish(world, runner, ally);

        Assert.Equal(DeckType.DiscardPile, ally.Area.Type);
        Assert.Single(events.OfType<AttackCompleted>());
        Assert.Empty(world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(0)).Cards);
        var dealt = Assert.Single(world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(1)).Cards);
        Assert.False(dealt.FaceUp);
        Assert.Equal(1, world.Seats[0].IdentityCard.Tokens.GetValueOrDefault("c_probeCompleted"));
        Assert.Null(Sequence.Work(world, world.Facts, runner, events));
        Assert.Single(world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(1)).Cards);
        Assert.False(world.IsOver);
        Assert.Null(world.Activation);
        Assert.Empty(world.Agenda.Outstanding);
    }

    [Fact]
    [Rule("rr:scheme-enemy-activation")]
    public void DelayedProbeAlsoCompletesAfterSchemeWithoutClaimingDefeat()
    {
        // "There are two types of enemy activations: an attack activation and
        // a scheme activation" (rr:activation). No attack is manufactured here.
        var runner = Runner();
        var (world, ally) = Board(runner, survives: true);
        world.Agenda.Abandon();
        world.Agenda.Add(new PhaseStep(new EnemySchemeOperation(), 1, 0,
            Subject: world.TheCardIn(DeckType.VillainArea)!.ObjectId, Seat: 0));
        var events = Finish(world, runner, ally);

        Assert.Empty(events.OfType<AttackCompleted>());
        Assert.Equal(DeckType.AlliesArea, ally.Area.Type);
        Assert.Equal(0, ally.Damage);
        Assert.Equal(1, world.Seats[0].IdentityCard.Tokens.GetValueOrDefault("c_probeCompleted"));
        Assert.Equal(0, world.Seats[0].IdentityCard.Tokens.GetValueOrDefault("c_probeDamage"));
        Assert.Empty(world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(0)).Cards);
        Assert.False(world.IsOver);
        Assert.Null(world.Activation);
        Assert.Empty(world.Agenda.Outstanding);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:boost-boost-icon.3")]
    public void BoostAbilityDefeatDoesNotQualifyDuringAttackOrScheme(bool scheme)
    {
        // Boost damage "is not considered to be damage dealt by the activation".
        var runner = Runner(before: """{"dealDamage":{"cards":{"titled":"Mockingbird"},"amount":3}},""");
        var (world, ally) = Board(runner, survives: false);
        if (scheme)
        {
            world.Agenda.Abandon();
            world.Agenda.Add(new PhaseStep(new EnemySchemeOperation(), 1, 0,
                Subject: world.TheCardIn(DeckType.VillainArea)!.ObjectId, Seat: 0));
        }
        Finish(world, runner, ally);
        Assert.Equal(DeckType.DiscardPile, ally.Area.Type);
        Assert.Equal(1, world.Seats[0].IdentityCard.Tokens.GetValueOrDefault("c_probeCompleted"));
        Assert.Empty(world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(0)).Cards);
        Assert.False(world.IsOver);
        Assert.Empty(world.Agenda.Outstanding);
    }

    [Fact]
    [Rule("rr:player-elimination.1")]
    public void IdentityDefeatSurvivesEarlyAttackCompletionAndDealsToSurvivingFirstPlayer()
    {
        // "If a player's identity is defeated, that player is eliminated from the game."
        var runner = Runner();
        var (world, ally) = Board(runner, survives: true);
        world.FirstPlayer = 1;
        world.Seats[0].IdentityCard.TakeDamage(8);
        Finish(world, runner, ally, defend: false);
        Assert.True(world.Seats[0].Eliminated);
        Assert.Single(world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(1)).Cards);
        Assert.False(world.IsOver);
        Assert.Empty(world.Agenda.Outstanding);
    }

    [Fact]
    [Rule("rr:activation.8")]
    public void QueuedActivationDefeatCannotQualifyTheOriginalActivation()
    {
        // The newly initiated activation resolves "after the current activation has finished resolving".
        var runner = Runner(before: """{"enemyAttacks":{"enemies":{"query":"minions"},"against":"you"}},""");
        var (world, ally) = Board(runner, survives: true);
        world.CreateCard("01102", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        world.Seats[0].IdentityCard.TakeDamage(4);
        world.FirstPlayer = 1;
        var events = Finish(world, runner, ally, defend: false);
        Assert.Equal(2, events.OfType<AttackCompleted>().Count());
        Assert.True(world.Seats[0].Eliminated);
        Assert.Empty(world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(1)).Cards);
        Assert.False(world.IsOver);
        Assert.Empty(world.Agenda.Outstanding);
    }

    [Fact]
    [Rule("rr:damage.step.6")]
    public void WouldDefeatRecoveryDoesNotQualifyDespiteLethalDamagePlacement()
    {
        // Step 6 resolves "when [character] would be defeated" before actual defeat.
        var runner = Runner(recover: true);
        var (world, ally) = Board(runner, survives: false);
        var events = Finish(world, runner, ally);
        Assert.Equal(5, Assert.Single(events.OfType<AttackCompleted>()).DamageDealt);
        Assert.Equal(DeckType.AlliesArea, ally.Area.Type);
        Assert.Equal(0, ally.Damage);
        Assert.Equal(1, world.Seats[0].IdentityCard.Tokens.GetValueOrDefault("c_probeCompleted"));
        Assert.Empty(world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(0)).Cards);
        Assert.False(world.IsOver);
        Assert.Empty(world.Agenda.Outstanding);
    }

    [Fact]
    [Rule("rr:overkill.1")]
    [Rule("rr:overkill.2")]
    public void TwoOverkillDefeatsRetainTheirCountButDealOnlyOneEncounterCard()
    {
        // Overkill's excess is "considered damage from an attack". Printed
        // 90004 conditions one deal on a defeat, rather than dealing per defeat.
        var runner = Runner(suspended: true);
        var (world, ally) = Board(runner, survives: false);
        world.FirstPlayer = 1;
        world.Seats[0].IdentityCard.TakeDamage(9);
        world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect,
            Keywords.Overkill, Affects: world.TheCardIn(DeckType.VillainArea)!.ObjectId));
        var events = Finish(world, runner, ally);

        Assert.Equal(DeckType.RemovedArea, ally.Area.Type);
        Assert.True(world.Seats[0].Eliminated);
        Assert.Single(events.OfType<AttackCompleted>());
        Assert.Equal(2, world.Seats[0].IdentityCard.Tokens.GetValueOrDefault("c_probeDefeats"));
        Assert.Single(world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(1)).Cards);
        Assert.False(world.IsOver);
        Assert.Null(world.Activation);
        Assert.Empty(world.Agenda.Outstanding);
    }

    [Fact]
    [Rule("rr:damage.step.6")]
    public void DefeatFromWouldDefeatInterruptCannotQualifyTheRecoveredAttackRecipient()
    {
        // Step 6 resolves "when [character] would be defeated" before actual
        // defeat. Damage from its interrupt is a separate causal effect.
        var runner = Runner(recover: true, collateral: true);
        var (world, ally) = Board(runner, survives: false);
        var collateral = world.CreateCard("01002", world.AreaOf(DeckType.AlliesArea,
            PlayArea.Of(0), cardOwner: 0));
        var events = Finish(world, runner, ally);

        Assert.Equal(5, Assert.Single(events.OfType<AttackCompleted>()).DamageDealt);
        Assert.Equal(DeckType.AlliesArea, ally.Area.Type);
        Assert.Equal(0, ally.Damage);
        Assert.Equal(DeckType.DiscardPile, collateral.Area.Type);
        Assert.Equal(0, world.Seats[0].IdentityCard.Tokens.GetValueOrDefault("c_probeDefeats"));
        Assert.Equal(1, world.Seats[0].IdentityCard.Tokens.GetValueOrDefault("c_probeCompleted"));
        Assert.Empty(world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(0)).Cards);
        Assert.False(world.IsOver);
        Assert.Empty(world.Agenda.Outstanding);
    }

    private static AbilityRunner Runner(string before = "", bool suspended = false, bool recover = false,
        bool collateral = false)
    {
        const string condition = """
            {"seq":[
              {"placeCounters":{"card":"you","counter":"probeDefeats","count":{"result":"activationDefeatedCharacters"}}},
              {"if":{"test":{"atLeast":{"value":{"result":"activationDefeatedCharacters"},"count":1}},
                "then":{"dealEncounterCards":{"player":"firstPlayer","count":1}}}}
            ]}
            """;
        string body = suspended
            ? """{"chooseCard":{"from":{"query":"identities"},"effect":BODY}}""".Replace("BODY", condition, StringComparison.Ordinal)
            : condition;
        return new AbilityRunner(AbilityCatalog.Parse("""
            {"cards":[{"card":"90004","abilities":[{
              "trigger":{"event":"WhenCardRevealed","timing":"Boost","subject":"this"},
              "effect":{"seq":[
                {"afterActivation":{"effect":{"seq":[
                  {"placeCounters":{"card":"you","counter":"probeCompleted","count":1}},
                  {"placeCounters":{"card":"you","counter":"probeDamage","count":{"result":"activationDamage"}}},
                  BODY
                ]}}}
                BEFORE
              ]}
            }]}EXTRA]}
            """.Replace("EXTRA", recover ? """ ,{"card":"01083","abilities":[{"trigger":{"event":"WhenCardWouldBeDefeated","timing":"ForcedInterrupt","subject":"this"},"effect":{"seq":[COLLATERAL{"heal":{"card":"trigger.subject","amount":10}}]}}]} """.Replace(
                "COLLATERAL", collateral ? """{"dealDamage":{"cards":{"titled":"Black Cat"},"amount":3}},""" : "", StringComparison.Ordinal)
                : "", StringComparison.Ordinal).Replace("BODY", body, StringComparison.Ordinal).Replace("BEFORE", before.Length == 0 ? "" : "," + before.TrimEnd(','), StringComparison.Ordinal)));
    }

    private static (World World, Card Ally) Board(AbilityRunner runner, bool survives)
    {
        var (_, world) = Playing(_ => { }, hero: true,
            heroes: ["spider_man", "captain_marvel"], abilities: runner);
        var ally = world.CreateCard("01083", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        if (survives) world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect,
            "health", Amount: 10, Affects: ally.ObjectId));
        var encounter = world.AreaOf(DeckType.EncounterDeck);
        for (int index = 0; index < 12; index++) world.CreateCard("01087", encounter);
        world.CreateCard("90004", encounter);
        world.Agenda.Abandon();
        world.Agenda.Add(new PhaseStep(new EnemyAttackOperation(), 1, 0,
            Subject: world.TheCardIn(DeckType.VillainArea)!.ObjectId, Seat: 0));
        return (world, ally);
    }

    private static List<GameEvent> Finish(World world, AbilityRunner runner, Card ally, bool defend = true)
    {
        var events = new List<GameEvent>();
        for (int index = 0; index < 20; index++)
        {
            var prompt = Sequence.Work(world, world.Facts, runner, events);
            if (prompt is null) return events;
            var offer = prompt.Affordances.SingleOrDefault(item => item.AnchorId == ally.ObjectId);
            var decision = prompt.Asking == Question.Defender && defend && offer is not null
                ? Decision.Take(offer.Id)
                : prompt.Cancellable && prompt.Asking != Question.Element ? Decision.Decline : Decision.Take(
                    prompt.Affordances.Any(item => item.AnchorId == ally.ObjectId) ? ally.ObjectId : prompt.Affordances[0].AnchorId);
            Sequence.Answer(world, world.Facts, runner, prompt, decision, events);
        }
        Assert.Fail("Synthetic Hammer diagnostic did not finish within twenty prompts.");
        return events;
    }
}
