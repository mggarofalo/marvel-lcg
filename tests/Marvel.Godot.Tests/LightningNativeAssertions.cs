using Marvel.Protocol;
using Marvel.Rules.Prompts;
using Xunit;

namespace Marvel.Godot.Tests;

internal static class LightningNativeAssertions
{
    private static readonly string[] DiscardedFaces = ["06006", "06022", "06023"];

    internal static void Verify(NativeContractServer server, EngineRequest[] mutations)
    {
        var decision = Assert.Single(mutations).Decision!;
        Assert.Equal(2, decision.Values!["X"]);
        Assert.Equal(2, decision.Resources!.Count);
        Assert.Equal(2, decision.Allocated.Count);
        var initial = server.Responses.First(response => response.World is not null).World!;
        var hand = initial.Areas.Single(area => area.Zone == "HandsArea").Cards;
        int energy = hand.Single(card => card.Face!.Id == "06022").Id!.Value;
        int genius = hand.Single(card => card.Face!.Id == "06023").Id!.Value;
        var typed = Assert.Single(decision.Allocated, allocation => allocation.Cost == 1);
        Assert.Equal(energy, typed.Source);
        Assert.Equal("YY", typed.PaidAs);
        var printed = Assert.Single(decision.Allocated, allocation => allocation.Cost == 0);
        Assert.Equal(genius, printed.Source);
        Assert.Single(printed.PaidAs);
        var final = server.Responses.Last(response => response.World is not null);
        Assert.Equal(1, final.Revision);
        Assert.Equal(PublicDecisionKind.PlayerAction, final.Prompt!.PublicKind);
        Assert.Equal(2, Assert.Single(final.World!.Areas.Single(area => area.Zone == "VillainArea").Cards).State!.Damage);
        Assert.Single(final.World.Areas.Single(area => area.Zone == "HandsArea").Cards);
        Assert.Equal(DiscardedFaces, final.World.Areas.Single(area => area.Zone == "DiscardPile").Cards.Select(card => card.Face!.Id).Order().ToArray());
    }
}
