using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class PrintedTextBlankingReviewTests : CoreCardTestBase
{
    [Fact]
    [Rule("rr:initiating-abilities.step.1")]
    public void UnsupportedConstantEndingLeavesTheBlankingPaymentUnspent()
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
        ally.TakeDamage(2);
        var payment = world.CreateCard("01089", world.Seats[0].Hand);
        var action = Assert.Single(runner.Actions(world, 0), option => option.Card == source.ObjectId);
        string before = world.Digest().Canonical();
        var effects = world.Effects.Registered.ToArray();
        var pending = world.Agenda.Outstanding.ToArray();
        var random = world.Random.Generator.GetState();

        Assert.Throws<RulesNotImplementedException>(() =>
            runner.ExecuteAction(world, new PlayerAction(action, [payment.ObjectId], [])));

        Assert.Same(world.Seats[0].Hand, payment.Area);
        Assert.Equal(before, world.Digest().Canonical());
        Assert.Equal(effects, world.Effects.Registered);
        Assert.Equal(pending, world.Agenda.Outstanding);
        Assert.Equal(random.Index, world.Random.Generator.GetState().Index);
        Assert.Equal(random.Words, world.Random.Generator.GetState().Words);
    }

    [Fact]
    [Rule("rr:text-box.1")]
    public void BlankedPrintedDefeatTextDoesNotBlockALabelledPowerContinuation()
    {
        // A text-box reference "only references the printed abilities";
        // a blanked When Defeated ability supplies no defeat-window work.
        // The synthetic support action tests the shared projection path.
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
            {"attack":{"target":{"query":"villain"},"effect":{"seq":[
              {"dealAttackDamage":{"cards":{"titled":"Hydra Soldier"},"amount":4}},
              {"heal":{"card":"you","amount":1}}
            ]}}}
            """, cost: """{"exhaust":"this"}""", includeAuthored: true);
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
