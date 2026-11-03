using Marvel.Protocol;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Xunit;

namespace Marvel.Godot.Tests;

// The host side of the Milano native journeys, without Godot.
public sealed class ShipCommandNativeSceneTests
{
    [Theory]
    [InlineData("milano-1")]
    [InlineData("milano-2")]
    public void TheJourneyOpensWithTheFirstPlayersMilanoInThePayersPayment(string contract)
    {
        int players = ShipCommandNativeScene.Players(contract);
        var factory = new NativeSceneGameFactory(contract);
        Game game = factory.Create(new GameSpecification("rhino", [.. ShipCommandNativeScene.Heroes.Take(players)], null, Seed: 1)).Game;
        Card milano = ShipCommandNativeScene.MilanoCard(game);
        // 16142: "Setup. ... The first player controls the Milano." It stays the scenario's card.
        Assert.Equal(0, game.State.FirstPlayer);
        Assert.Equal(PlayArea.Of(0), milano.Area.PlayArea);
        Assert.Equal(World.Scenario, milano.Owner);
        Assert.Equal(players - 1, game.Pending!.Player);
        Assert.True(ShipCommandNativeScene.Reached(game, players - 1));
    }
}
