using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Server;
using Marvel.Tests;
using Xunit;

namespace Marvel.Godot.Tests;

public sealed class LightningNativeSceneTests
{
    [Fact]
    public void PublicPreparedThorHasSeparatePrintedAndTypedCostsAndOneDamageResolution()
    {
        // Printed 06006: cost 1; "Spend X [energy] resources"; deal X to the villain.
        var factory = DatasetGameFactory.Load(RepositoryPaths.Root);
        var game = LightningNativeScene.Create(factory, new("rhino", ["thor"], null, 1)).Game;
        var hand = game.State.Seats[0].Hand.Cards;
        var lightning = hand.Single(card => card.FaceId == "06006");
        var offer = Assert.Single(game.Pending!.Affordances, action => action.AnchorId == lightning.ObjectId);
        var cost = Assert.Single(offer.CostOptions);
        Assert.Equal("1+X", cost.Cost);
        Assert.Equal(2, cost.ResourceCosts.Count);
        var values = new Dictionary<string, long> { ["X"] = 2 };
        int energy = hand.Single(card => card.FaceId == "06022").ObjectId;
        int genius = hand.Single(card => card.FaceId == "06023").ObjectId;
        Assert.Null(ResourcePayment.Allocate(cost, [energy], values));
        Assert.Null(ResourcePayment.Allocate(cost, [genius], values));
        var allocations = ResourcePayment.Allocate(cost, [energy, genius], values);
        Assert.NotNull(allocations);
        game.Resolve(new Decision(offer.Id, [], [energy, genius], values, allocations));
        Assert.Equal(2, game.State.TheCardIn(DeckType.VillainArea)!.Damage);
        Assert.Equal(DeckType.DiscardPile, lightning.Area.Type);
        Assert.Single(game.State.Seats[0].Hand.Cards);
        Assert.Equal(PublicDecisionKind.PlayerAction, game.Pending!.PublicKind);
    }
}
