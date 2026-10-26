using Marvel.Protocol;
using Marvel.Rules.Events;
using Marvel.Rules.Prompts;
using Xunit;

namespace Marvel.Godot.Tests;

internal static class MeleeNativeAssertions
{
    internal static void Verify(NativeContractServer server, EngineRequest[] mutations)
    {
        Assert.Equal(3, mutations.Length);
        var initial = server.Responses.First(response => response.World is not null).World!;
        var guard = Assert.Single(initial.Areas.SelectMany(area => area.Cards), card => card.Face?.Id == "01101");
        var villain = Assert.Single(initial.Areas.Single(area => area.Zone == "VillainArea").Cards);
        var first = server.Responses.Single(response => response.Revision == 1 && response.Prompt is not null);
        Assert.Equal(guard.Id, Assert.Single(first.Prompt!.Affordances).AnchorId);
        Assert.Equal(guard.Id, mutations[1].Decision!.Affordance);
        var second = server.Responses.Single(response => response.Revision == 2 && response.Prompt is not null);
        Assert.Equal("Choose another enemy", second.Prompt!.DisplayQuestion);
        Assert.Equal(villain.Id, Assert.Single(second.Prompt.Affordances).AnchorId);
        Assert.Equal(villain.Id, mutations[2].Decision!.Affordance);
        Assert.Contains(second.World!.Areas.Single(area => area.Zone == "EncounterDiscardPile").Cards, card => card.Id == guard.Id);
        Assert.Contains(second.Events.OfType<FieldSet>(), change => change.Card == guard.Id && change.Field == "health" && change.From == 3 && change.To == 0);
        Assert.Equal(0, Assert.Single(second.World.Areas.Single(area => area.Zone == "VillainArea").Cards).State!.Damage);
        var final = server.Responses.Last(response => response.World is not null);
        Assert.Equal(3, Assert.Single(final.World!.Areas.Single(area => area.Zone == "VillainArea").Cards).State!.Damage);
        Assert.Contains(final.World.Areas.Single(area => area.Zone == "DiscardPile").Cards, card => card.Face?.Id == "05030");
        Assert.Equal(PublicDecisionKind.PlayerAction, final.Prompt!.PublicKind);
        Assert.Equal(2, mutations[0].Decision!.Resources!.Count);
        Assert.Equal(3, mutations[0].Decision!.Allocated.Sum(allocation => allocation.PaidAs.Length));
    }
}
