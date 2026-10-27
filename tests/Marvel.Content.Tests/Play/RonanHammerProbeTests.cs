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

/// <summary>Synthetic composition diagnostics for printed 90004; no runtime admission.</summary>
public sealed class RonanHammerProbeTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:boost-boost-icon.2")]
    [Rule("rr:activation.7")]
    public void DelayedOccurrenceDefeatPredicateCannotDistinguishLethalAttack(bool survives)
    {
        // Printed 90004: "If this activation defeats a character, deal the first
        // player 1 facedown encounter card." This is a diagnostic of existing
        // composition, not an accepted interpretation of that printed ability.
        // The synthetic choice retains the exact ally before its departure.
        var runner = Runner();
        var (world, ally) = Board(runner, survives);
        var events = Finish(world, runner, ally);

        Assert.Equal(survives ? DeckType.AlliesArea : DeckType.DiscardPile, ally.Area.Type);
        var completed = Assert.Single(events.OfType<AttackCompleted>());
        Assert.Equal(5, completed.DamageDealt);
        Assert.Equal(1, world.Seats[0].IdentityCard.Tokens.GetValueOrDefault("c_probeCompleted"));
        Assert.Equal(5, world.Seats[0].IdentityCard.Tokens.GetValueOrDefault("c_probeDamage"));
        Assert.Equal(1,
            world.Seats[0].IdentityCard.Tokens.GetValueOrDefault("c_probeChosenExists"));
        Assert.Empty(world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(0)).Cards);
        Assert.False(world.IsOver);
        Assert.Null(world.Activation);
        Assert.Empty(world.Agenda.Outstanding);
    }

    [Fact]
    [Rule("rr:boost-boost-icon.2")]
    public void UnconditionalDelayedDealingControlReachesFirstPlayerOnceAfterAllyDefeat()
    {
        // Only the boost text beneath the divider is active. This positive
        // control proves delayed completion and first-player dealing independently
        // of the missing qualifying predicate; it is not the actual card rule.
        var runner = Runner(unconditional: true);
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

    private static AbilityRunner Runner(bool unconditional = false)
    {
        const string deal = """{"dealEncounterCards":{"player":"firstPlayer","count":1}}""";
        string condition = unconditional ? deal
            : """{"if":{"test":{"wasDefeated":"chosen"},"then":DEAL}}"""
                .Replace("DEAL", deal, StringComparison.Ordinal);
        return new AbilityRunner(AbilityCatalog.Parse("""
            {"cards":[{"card":"90004","abilities":[{
              "trigger":{"event":"WhenCardRevealed","timing":"Boost","subject":"this"},
              "effect":{"chooseCard":{"from":{"query":"alliesYouControl"},"effect":{
                "afterActivation":{"effect":{"seq":[
                  {"placeCounters":{"card":"you","counter":"probeCompleted","count":1}},
                  {"placeCounters":{"card":"you","counter":"probeDamage","count":{"result":"activationDamage"}}},
                  {"if":{"test":{"exists":"chosen"},"then":{
                    "placeCounters":{"card":"you","counter":"probeChosenExists","count":1}}}},
                  CONDITION
                ]}}}}}
            }]}]}
            """.Replace("CONDITION", condition, StringComparison.Ordinal)));
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

    private static List<GameEvent> Finish(World world, AbilityRunner runner, Card ally)
    {
        var events = new List<GameEvent>();
        for (int index = 0; index < 20; index++)
        {
            var prompt = Sequence.Work(world, world.Facts, runner, events);
            if (prompt is null) return events;
            var offer = prompt.Affordances.SingleOrDefault(item => item.AnchorId == ally.ObjectId);
            var decision = prompt.Asking == Question.Defender && offer is not null
                ? Decision.Take(offer.Id)
                : prompt.Cancellable ? Decision.Decline : Decision.Take(ally.ObjectId);
            Sequence.Answer(world, world.Facts, runner, prompt, decision, events);
        }
        Assert.Fail("Synthetic Hammer diagnostic did not finish within twenty prompts.");
        return events;
    }
}
