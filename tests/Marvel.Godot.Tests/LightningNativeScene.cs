using Marvel.Protocol;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Server;
using Xunit;

namespace Marvel.Godot.Tests;

/// <summary>Public Thor factory with explicitly prepared hero form and three-card payment hand.</summary>
internal static class LightningNativeScene
{
    internal static OpenedGame Create(DatasetGameFactory factory, GameSpecification specification)
    {
        Assert.Equal("thor", Assert.Single(specification.Heroes));
        OpenedGame opened = factory.Create(specification);
        Game game = opened.Game;
        var world = game.State;
        var seat = world.Seats[0];
        // Fixture preparation only; the native journey owns every subsequent draft and commitment.
        seat.IdentityCard.TurnTo("06001a");
        foreach (var card in seat.Hand.Cards.ToArray()) World.MoveToTop(card, seat.Deck);
        foreach (string face in new[] { "06006", "06022", "06023" })
            World.MoveToTop(world.Cards.First(card => card.FaceId == face), seat.Hand);
        game.Resolve(Decision.Take(Assert.Single(game.Pending!.Affordances).Id));
        Assert.Equal(PublicDecisionKind.PlayerAction, game.Pending!.PublicKind);
        Assert.Contains(game.Pending.Affordances, offer => offer.AnchorId == seat.Hand.Cards.Single(card => card.FaceId == "06006").ObjectId);
        return opened;
    }
}
