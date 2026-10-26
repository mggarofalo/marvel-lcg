using Marvel.Protocol;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Xunit;

namespace Marvel.Godot.Tests;

internal static class HeimdallNativeAssertions
{
    internal static void Verify(NativeContractServer server, EngineRequest[] mutations)
    {
        Assert.Equal(4, mutations.Length);
        var looked = server.Responses.Single(response => response.Revision == 2 && response.Prompt is not null);
        int[] group = looked.Prompt!.ContextCardIds.ToArray();
        Assert.Equal(3, group.Length);
        Assert.Equal(group[1], mutations[2].Decision!.Affordance);
        Assert.Equal(new[] { group[2], group[0] }, mutations[3].Decision!.Targets);
        var game = server.PreparedGame;
        Assert.Equal(DeckType.EncounterDiscardPile, game.State.Cards[group[1]].Area.Type);
        Assert.Equal(new[] { group[2], group[0] }, game.State.AreaOf(DeckType.EncounterDeck)
            .Cards.Reverse().Take(2).Select(card => card.ObjectId));
        var final = server.Responses.Last(response => response.World is not null);
        Assert.Equal(4, final.Revision);
        Assert.Equal(PublicDecisionKind.PlayerAction, final.Prompt!.PublicKind);
        Assert.All(final.World!.Areas.Single(area => area.Zone == "EncounterDeck").Cards,
            card => { Assert.Null(card.Id); Assert.Null(card.Face); });
    }
}
