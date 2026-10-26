using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class PrintedTextBlankingReviewTests : CoreCardTestBase
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:initiating-abilities.step.1")]
    public void UnsupportedConstantEndingLeavesTheBlankingPaymentUnspent(bool query)
    {
        // "Check play restrictions" precedes costs. A permanent attachment
        // prevents the ally's health-loss defeat from being committed.
        var runner = new AbilityRunner(AbilityCatalog.Parse("""
            {"cards":[{"card":"05028","abilities":[
              {"trigger":{"timing":"Constant","subject":"this"},
               "effect":{"grant":{"card":{"titled":"Black Cat"},"keyword":"health","amount":2}}},
              {"trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"this"},
               "cost":{"spend":"B"},"effect":{"blankPrintedTextUntilEndOfPhase":"this"}}
            ]}]}
            """));
        var world = Hero("01001a");
        world.Abilities = runner;
        var source = world.CreateCard("05028", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        var ally = world.CreateCard("01002", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        world.CreateCard("27189a", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), ally.ObjectId));
        var payment = world.CreateCard("01089", world.Seats[0].Hand);
        var action = Assert.Single(runner.Actions(world, 0), option => option.Card == source.ObjectId);
        ally.TakeDamage(2);
        string before = world.Digest().Canonical();
        var effects = world.Effects.Registered.ToArray();
        var active = world.Effects.Active().ToArray();
        var pending = world.Agenda.Outstanding.ToArray();
        var random = world.Random.Generator.GetState();

        if (query)
        {
            Assert.Throws<RulesNotImplementedException>(() => runner.Actions(world, 0));
            Assert.Throws<RulesNotImplementedException>(() => runner.Actions(world, 0));
        }
        else
        {
            Assert.Throws<RulesNotImplementedException>(() =>
                runner.ExecuteAction(world, new PlayerAction(action, [payment.ObjectId], [])));
        }

        Assert.Same(world.Seats[0].Hand, payment.Area);
        Assert.Equal(before, world.Digest().Canonical());
        Assert.Equal(effects, world.Effects.Registered);
        Assert.Equal(active, world.Effects.Active());
        Assert.Equal(4, DamagePlacement.Health(world, world.Facts, ally));
        Assert.False(PrintedText.IsBlanked(world, source));
        Assert.Equal(pending, world.Agenda.Outstanding);
        Assert.Equal(random.Index, world.Random.Generator.GetState().Index);
        Assert.Equal(random.Words, world.Random.Generator.GetState().Words);
    }

    [Fact]
    [Rule("rr:hit-points.3.1")]
    public void SupportedConstantEndingSpendsTheSameCostAndRepeatedQueriesPreserveExecution()
    {
        // When a +X hit-point ability ends and damage reaches hit points,
        // "that ally or minion is defeated". These synthetic constants isolate
        // preflight; both worlds use the same mental payment as the refusal.
        var probed = Prepared();
        var control = Prepared();
        string before = probed.World.Digest().Canonical();
        var effects = probed.World.Effects.Registered.ToArray();
        var active = probed.World.Effects.Active().ToArray();
        var pending = probed.World.Agenda.Outstanding.ToArray();
        var random = probed.World.Random.Generator.GetState();

        for (int query = 0; query < 3; query++)
            Assert.Contains(probed.Runner.Actions(probed.World, 0),
                action => action.Card == probed.Source.ObjectId);

        Assert.Equal(before, probed.World.Digest().Canonical());
        Assert.Equal(effects, probed.World.Effects.Registered);
        Assert.Equal(active, probed.World.Effects.Active());
        Assert.Equal(pending, probed.World.Agenda.Outstanding);
        Assert.Equal(random.Index, probed.World.Random.Generator.GetState().Index);
        Assert.Equal(random.Words, probed.World.Random.Generator.GetState().Words);
        foreach (var fixture in new[] { probed, control })
        {
            var action = Assert.Single(fixture.Game.Pending!.Affordances,
                option => option.Verb == Game.ActionVerb && option.AnchorId == fixture.Source.ObjectId);
            fixture.Game.Resolve(PaidDecision.Take(action, [fixture.Payment.ObjectId]));
            Assert.Equal(DeckType.DiscardPile, fixture.Payment.Area.Type);
            Assert.Equal(DeckType.DiscardPile, fixture.Ally.Area.Type);
            Assert.True(PrintedText.IsBlanked(fixture.World, fixture.Source));
            Assert.Equal(Question.TurnOption, fixture.Game.Pending!.Asking);
            Assert.False(fixture.World.Agenda.IsBusy);
        }
        Assert.Equal(control.World.Digest().Canonical(), probed.World.Digest().Canonical());
        Assert.Equal(control.World.Effects.Registered, probed.World.Effects.Registered);
        Assert.Equal(control.World.Agenda.Outstanding, probed.World.Agenda.Outstanding);
        Assert.Equal(control.World.Random.Generator.GetState().Index,
            probed.World.Random.Generator.GetState().Index);
        Assert.Equal(control.World.Random.Generator.GetState().Words,
            probed.World.Random.Generator.GetState().Words);
    }

    private static (Game Game, World World, AbilityRunner Runner, Card Source, Card Ally, Card Payment) Prepared()
    {
        var runner = new AbilityRunner(AbilityCatalog.Parse("""
            {"cards":[{"card":"05028","abilities":[
              {"trigger":{"timing":"Constant","subject":"this"},
               "effect":{"grant":{"card":{"titled":"Black Cat"},"keyword":"health","amount":2}}},
              {"trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"this"},
               "cost":{"spend":"B"},"effect":{"blankPrintedTextUntilEndOfPhase":"this"}}
            ]}]}
            """));
        Card? source = null, ally = null, payment = null;
        var (game, world) = Playing(board =>
        {
            source = board.CreateCard("05028", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            ally = board.CreateCard("01002", board.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
            payment = board.CreateCard("01089", board.Seats[0].Hand);
            ally.TakeDamage(2);
        }, hero: true, abilities: runner);
        return (game, world, runner, source!, ally!, payment!);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:text-box.1")]
    public void BlankedPrintedDefeatTextDoesNotBlockALabelledPowerContinuation(bool includeAuthored)
    {
        // A text-box reference "only references the printed abilities";
        // a blanked When Defeated ability supplies no defeat-window work.
        // The synthetic support action tests the shared projection path.
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
            {"attack":{"target":{"query":"villain"},"effect":{"seq":[
              {"dealAttackDamage":{"cards":{"titled":"Hydra Soldier"},"amount":4}},
              {"heal":{"card":"you","amount":1}}
            ]}}}
            """, cost: """{"exhaust":"this"}""", includeAuthored: includeAuthored);
        var world = Hero("01001a");
        world.Abilities = runner;
        world.CreateCard("01094", world.AreaOf(DeckType.VillainArea));
        var soldier = world.CreateCard("01182", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        var source = InPlay(world, AuthoredCards.AuntMay);
        var identity = world.Seats[0].IdentityCard;
        identity.TakeDamage(2);
        world.Effects.Register(PrintedText.UntilEndOfPhase(world, identity, soldier));
        Assert.True(world.Facts.HasWhenDefeated(soldier.FaceId));
        Assert.Empty(runner.Waiting(world,
            new Occurrence(1, [Steps.CardDefeated], Subject: soldier.ObjectId, Player: 0),
            WindowKind.Interrupt));

        var action = Assert.Single(runner.Actions(world, 0), option => option.Card == source.ObjectId);
        var events = runner.ExecuteAction(world, new PlayerAction(action, [], [])).Events.ToList();
        Sequence.Finish(world, Cards, runner, events);

        Assert.False(source.Ready);
        Assert.Equal(DeckType.EncounterDiscardPile, soldier.Area.Type);
        Assert.Empty(world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(0)).Cards);
        Assert.Equal(1, identity.Damage);
        Assert.Null(Sequence.Work(world, Cards, runner, events));
    }
}
