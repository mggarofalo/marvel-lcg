using Marvel.Protocol;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Xunit;

namespace Marvel.Godot.Tests;

internal static class PreemptiveStrikeNativeAssertions
{
    internal static void Verify(NativeContractServer server, EngineRequest[] mutations)
    {
        var initial = server.Responses.First(response => response.World is not null);
        Assert.Equal("WhenBoostCardTurnedFaceup", initial.Prompt!.Trigger);
        Assert.Equal(PublicDecisionKind.Interrupt, initial.Prompt.PublicKind);
        Assert.Equal(0, initial.Prompt.Player);
        Assert.True(initial.Prompt.Cancellable);
        var hand = initial.World!.Areas.Single(area => area.Zone == "HandsArea").Cards;
        var source = Assert.Single(hand, card => card.Face?.Id == "05014");
        var resource = Assert.Single(hand, card => card.Face?.Id == "01005");
        var hero = Assert.Single(initial.World.Areas.SelectMany(area => area.Cards), card => card.Face?.Id == "01001a");
        var villain = Assert.Single(initial.World.Areas.Single(area => area.Zone == "VillainArea").Cards);
        var decision = Assert.Single(mutations).Decision!;
        Assert.Equal(Assert.Single(initial.Prompt.Affordances, offer => offer.AnchorId == source.Id).Id, decision.Affordance);
        Assert.Equal([resource.Id!.Value], decision.Resources);
        Assert.Equal(1, Assert.Single(decision.Allocated).PaidAs.Length);
        var final = server.Responses.Last(response => response.World is not null);
        Assert.Equal(1, final.Revision);
        Assert.Equal(2, Assert.Single(final.World!.Areas.Single(area => area.Zone == "VillainArea").Cards).State!.Damage);
        Assert.Equal(2, Assert.Single(final.World.Areas.SelectMany(area => area.Cards), card => card.Id == hero.Id).State!.Damage);
        var discarded = final.World.Areas.Single(area => area.Zone == "DiscardPile").Cards;
        Assert.Contains(discarded, card => card.Id == source.Id);
        Assert.Contains(discarded, card => card.Id == resource.Id);
        var boost = Assert.Single(final.Events.OfType<BoostResolved>());
        Assert.Equal(0, boost.Icons);
        Assert.True(boost.Attacking);
        Assert.Equal(villain.Id, boost.Enemy);
        var attack = Assert.Single(final.Events.OfType<AttackCompleted>());
        Assert.Equal(hero.Id, attack.Target);
        Assert.Equal(hero.Id, attack.Defender);
        Assert.Equal(2, attack.DamageDealt);
        Assert.Null(final.World.ActiveAttack);
        Assert.Null(server.PreparedGame.State.Activation);
        Assert.Null(server.PreparedGame.State.Attack);
        Assert.False(server.PreparedGame.State.Agenda.IsBusy);
        Assert.Equal(Outcome.Unfinished, final.World.Outcome);
        Assert.Equal(PublicDecisionKind.PlayerAction, final.Prompt!.PublicKind);
    }
}
