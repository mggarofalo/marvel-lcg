using Marvel.Decisions;
using Marvel.Protocol;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Xunit;

namespace Marvel.Godot.Tests;

public sealed class PreemptiveStrikeNativeSceneTests
{
    [Fact]
    public void PreparedActualEventPaysOneHandResourceAndFinishesTheAttack()
    {
        // rr:attack-enemy-activation.step.3: "Resolve the following steps one card at a time": 05014 cancels this physical boost's icons.
        var factory = new NativeSceneGameFactory("preemptive-strike");
        Game game = factory.Create(new GameSpecification("rhino", ["spider_man"], null, Seed: 855)).Game;
        Assert.Equal("WhenBoostCardTurnedFaceup", game.Pending!.Trigger);
        var hand = game.State.Seats[0].Hand.Cards;
        int source = Assert.Single(hand, card => card.FaceId == "05014").ObjectId;
        int payment = Assert.Single(hand, card => card.FaceId == "01005").ObjectId;
        var offer = Assert.Single(game.Pending.Affordances, item => item.AnchorId == source);
        var composer = new DecisionComposer(game.Pending);
        composer.SelectAffordance(offer.Id);
        composer.ToggleResource(payment);
        Assert.True(composer.TryBuild(out var decision, out var error), error);
        var events = game.Resolve(decision!).Events;
        Assert.Equal(0, Assert.Single(events.OfType<BoostResolved>()).Icons);
        Assert.Equal(2, Assert.Single(events.OfType<AttackCompleted>()).DamageDealt);
        Assert.Equal(game.State.Seats[0].IdentityCard.ObjectId, Assert.Single(events.OfType<AttackCompleted>()).Defender);
        Assert.Null(game.State.Activation);
        Assert.Null(game.State.Attack);
        Assert.False(game.State.Agenda.IsBusy);
        Assert.Equal(PublicDecisionKind.PlayerAction, game.Pending!.PublicKind);
    }
}
