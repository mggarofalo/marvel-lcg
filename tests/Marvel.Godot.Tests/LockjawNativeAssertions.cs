using Marvel.Protocol;
using Marvel.Rules.Events;
using Marvel.Rules.Prompts;
using Xunit;

namespace Marvel.Godot.Tests;

internal static class LockjawNativeAssertions
{
    internal static void Verify(NativeContractServer server, EngineRequest[] mutations)
    {
        var initial = server.Responses.First(response => response.World is not null).World!;
        var lockjaw = Assert.Single(initial.Areas.Single(area => area.Zone == "DiscardPile").Cards,
            card => card.Face?.Id == "05018");
        var decision = Assert.Single(mutations).Decision!;
        Assert.DoesNotContain(lockjaw.Id!.Value, decision.Resources!);
        Assert.Equal(2, decision.Resources!.Count);
        Assert.Equal(4, decision.Allocated.Sum(allocation => allocation.PaidAs.Length));
        var final = server.Responses.Last(response => response.World is not null);
        Assert.Equal(1, final.Revision);
        Assert.Contains(final.World!.Areas.Single(area => area.Zone == "AlliesArea").Cards,
            card => card.Id == lockjaw.Id && card.Face?.Id == "05018");
        Assert.DoesNotContain(final.World.Areas.Single(area => area.Zone == "DiscardPile").Cards,
            card => card.Id == lockjaw.Id);
        Assert.All(decision.Resources, id => Assert.Contains(
            final.World.Areas.Single(area => area.Zone == "DiscardPile").Cards, card => card.Id == id));
        Assert.Contains(final.Events.OfType<CardsMoved>(), move =>
            move.From.Zone == "DiscardPile" && move.To.Zone == "RevealingArea" && move.Cards.Any(card => card.Card == lockjaw.Id));
        Assert.Contains(final.Events.OfType<CardsMoved>(), move =>
            move.From.Zone == "RevealingArea" && move.To.Zone == "AlliesArea" && move.Cards.Any(card => card.Card == lockjaw.Id));
        Assert.Equal(PublicDecisionKind.PlayerAction, final.Prompt!.PublicKind);
    }
}
