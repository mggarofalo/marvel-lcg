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
