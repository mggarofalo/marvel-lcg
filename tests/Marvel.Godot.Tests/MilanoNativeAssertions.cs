using Marvel.Protocol;
using Xunit;

namespace Marvel.Godot.Tests;

// The host's account of a Milano journey: one payment that spent the first player's Milano.
internal static class MilanoNativeAssertions
{
    internal static void Verify(string contract, NativeContractServer server, EngineRequest[] mutations)
    {
        var initial = server.Responses.First(response => response.World is not null).World!;
        var milano = initial.Areas.SelectMany(area => area.Cards).Single(card => card.Face?.Id == ShipCommandNativeScene.Milano);
        Assert.Equal(0, initial.Table!.FirstPlayer);
        Assert.Equal(0, milano.Location!.Controller);
        Assert.True(milano.Ready);
        // milano-2's second answer passes Spider-Woman's optional Response.
        var decision = mutations[0].Decision!;
        Assert.Contains(milano.Id!.Value, decision.Resources!);
        Assert.Equal(contract == "milano-2" ? 2 : 1, decision.Resources!.Count);
        var final = server.Responses.Last(response => response.World is not null);
        Assert.Equal(mutations.Length, final.Revision);
        var spent = final.World!.Areas.Single(area => area.Cards.Any(card => card.Id == milano.Id));
        Assert.Equal(("SupportsArea", 0), (spent.Zone, spent.Owner));
        Assert.False(spent.Cards.Single(card => card.Id == milano.Id).Ready);
        string played = contract == "milano-2" ? "01011" : "01006";
        Assert.Contains(final.World.Areas.Where(area => area.Owner == ShipCommandNativeScene.Players(contract) - 1)
            .SelectMany(area => area.Cards), card => card.Face?.Id == played
            && card.Location?.Zone is "AlliesArea" or "SupportsArea");
    }
}
