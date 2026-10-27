using Marvel.Protocol;
using Marvel.Rules.Events;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Xunit;

namespace Marvel.Godot.Tests;

internal static class BeatCopNativeAssertions
{
    internal static void Verify(string contract, NativeContractServer server, EngineRequest[] mutations)
    {
        Assert.Equal(2, mutations.Length);
        bool damage = contract == "beat-cop-damage";
        VerifyInitial(server, damage);
        var world = server.PreparedGame.State;
        var cop = world.Cards.Single(card => card.FaceId == "10029");
        var intermediate = server.Responses.Single(response => response.Revision == 1 && response.Prompt is not null);
        Assert.Contains(intermediate.Events.OfType<FieldSet>(), change => change.Card == cop.ObjectId
            && change.Field == "is_exhaust" && change.From == 0 && change.To == 1);
        if (damage) VerifyDamage(server, mutations);
        else VerifyMove(world, cop);
        Assert.DoesNotContain(server.Responses.SelectMany(response => response.Events), e => e.Verb is "Attack" or "Thwart");
        Assert.Empty(server.Responses.SelectMany(response => response.Events).OfType<AttackBoundary>());
        Assert.Equal(PublicDecisionKind.PlayerAction, server.PreparedGame.Pending!.PublicKind);
        TestContext.Current.TestOutputHelper!.WriteLine($"BEAT_COP_AUTHORITY {contract} commits=2 threat={cop.Tokens.GetValueOrDefault("k_threat")} minionDamage={world.Cards.Single(card => card.FaceId == "01131").Damage} supportZone={cop.Area.Type}");
    }

    private static void VerifyInitial(NativeContractServer server, bool damage)
    {
        var initial = server.Responses.First(response => response.World is not null).World!;
        var support = Assert.Single(initial.Areas.Single(area => area.Zone == "SupportsArea").Cards,
            card => card.Face?.Id == "10029");
        Assert.True(support.Ready);
        if (damage) Assert.Equal(3, support.State!.Threat);
        else Assert.Null(support.State!.Threat);
        var intermediate = server.Responses.Single(response => response.Revision == 1 && response.Prompt is not null);
        Assert.DoesNotContain(intermediate.Prompt!.Affordances, offer => offer.AnchorId ==
            initial.Areas.Single(area => area.Zone == "VillainArea").Cards.Single().Id);
    }

    private static void VerifyMove(World world, Card cop)
    {
        Assert.False(cop.Ready);
        Assert.Equal(DeckType.SupportsArea, cop.Area.Type);
        Assert.Equal(1, cop.Tokens["k_threat"]);
        Assert.Equal(0, world.Cards.Single(card => card.FaceId == "01131").Damage);
        Assert.Equal(3, world.TheCardIn(DeckType.MainSchemesArea)!.Tokens["k_threat"]);
    }

    private static void VerifyDamage(NativeContractServer server, EngineRequest[] mutations)
    {
        var world = server.PreparedGame.State;
        var cop = world.Cards.Single(card => card.FaceId == "10029");
        var minion = world.Cards.Single(card => card.FaceId == "01131");
        Assert.Equal(DeckType.DiscardPile, cop.Area.Type);
        Assert.Equal(0, cop.Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(3, minion.Damage);
        Assert.Equal(4, world.TheCardIn(DeckType.MainSchemesArea)!.Tokens["k_threat"]);
        Assert.Equal(minion.ObjectId, mutations[1].Decision!.Affordance);
        var intermediate = server.Responses.Single(response => response.Revision == 1 && response.Prompt is not null);
        Assert.Contains(intermediate.World!.Areas.Single(area => area.Zone == "DiscardPile").Cards,
            card => card.Id == cop.ObjectId);
    }
}
