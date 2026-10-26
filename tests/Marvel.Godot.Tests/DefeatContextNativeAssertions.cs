using Marvel.Protocol;
using Marvel.Rules.Prompts;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Xunit;

namespace Marvel.Godot.Tests;

internal static class DefeatContextNativeAssertions
{
    internal static void Verify(NativeContractServer server, EngineRequest[] mutations)
    {
        EngineResponse initial = server.Responses.First(response => response.World is not null);
        var offers = initial.Prompt!.Affordances;
        Assert.Equal(2, offers.Count);
        Assert.Single(offers.Select(offer => offer.AnchorId).Distinct());
        int first = Assert.Single(offers[0].ContextCardIds);
        int second = Assert.Single(offers[1].ContextCardIds);
        Assert.NotEqual(first, second);
        var answer = Assert.Single(mutations).Decision!;
        Assert.Equal(offers[1].Id, answer.Affordance);
        Assert.Empty(answer.Targets);
        int payment = Assert.Single(answer.Resources!);
        EngineResponse final = server.Responses.Last(response => response.World is not null);
        Assert.Equal(1, final.Revision);
        Assert.Equal(PublicDecisionKind.Interrupt, final.Prompt!.PublicKind);
        Assert.Equal([first], Assert.Single(final.Prompt.Affordances).ContextCardIds);
        Assert.Contains(final.World!.Areas.Single(area => area.Zone == "DiscardPile").Cards,
            card => card.Id == payment && card.Face!.Title == "Energy");
        var guards = final.World.Areas.Single(area => area.Zone == "EngagedEnemiesArea").Cards
            .Where(card => card.Face?.Title == "Hydra Bomber").ToArray();
        Assert.Equal(new[] { first, second }, guards.Select(card => card.Id!.Value));
        Assert.All(guards, card => Assert.Equal(10, card.State!.Damage));
        Assert.Equal(1, Assert.Single(server.PreparedGame.State.AreaOf(Marvel.Rules.State.DeckType.MainSchemesArea).Cards)
            .Tokens.GetValueOrDefault(EncounterDeck.AccelerationToken));
    }
}
