using Marvel.Protocol;
using Xunit;

namespace Marvel.Godot.Tests;

internal static class BrunoNativeAssertions
{
    internal static void Verify(string contract, NativeContractServer server, EngineRequest[] mutations)
    {
        int count = int.Parse(contract["bruno-".Length..], System.Globalization.CultureInfo.InvariantCulture);
        var initial = server.Responses.First(response => response.World is not null).World!;
        var pending = server.Responses.Single(response => response.Revision == 1 && response.Prompt is not null);
        var offer = Assert.Single(pending.Prompt!.Affordances);
        Assert.Equal(0, offer.Targets!.Min);
        Assert.Equal(3, offer.Targets.Max);
        Assert.Equal(4, offer.Targets.Legal.Count);
        var selected = mutations[1].Decision!.Targets;
        Assert.Equal(count, selected.Count);
        Assert.All(selected, target => Assert.Contains(target, offer.Targets.Legal));
        var final = server.Responses.Last(response => response.World is not null).World!;
        int initialHand = initial.Areas.Single(area => area.Zone == "HandsArea").Cards.Count;
        var finalHand = final.Areas.Single(area => area.Zone == "HandsArea").Cards;
        Assert.Equal(initialHand + count, finalHand.Count);
        Assert.All(selected, target => Assert.Contains(finalHand, card => card.Id == target));
        var bruno = Assert.Single(final.Areas.SelectMany(area => area.Cards), card => card.Face?.Id == "05007");
        Assert.False(bruno.State!.Ready);
        Assert.All(offer.Targets.Legal.Except(selected), target =>
            Assert.DoesNotContain(final.Areas.SelectMany(area => area.Cards), card => card.Id == target));
        Assert.All(final.Areas.Single(area => area.Zone == "PlayerDeck").Cards, card => Assert.Null(card.Face));
    }
}
