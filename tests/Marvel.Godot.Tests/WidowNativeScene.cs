using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content;
using Marvel.Content.Behavior;
using Marvel.Content.Setup;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Server;
using Xunit;

namespace Marvel.Godot.Tests;

/// <summary>Prepared cross-pack component worlds using unmodified authored Widow rows; no public setup claim.</summary>
internal static class WidowNativeScene
{
    internal static OpenedGame Create(string contract, CardCatalog cards, SetupCatalog setup, AbilityRunner abilities)
    {
        var scene = CanonicalCoreScene.Deal(new CoreSceneRequest(
            "prepared-native:" + contract, "rhino", ["black_widow"], 855), setup, cards, abilities);
        scene.Apply(new SetPlayerHand(0, []));
        World world = scene.World;
        var seat = world.Seats[0];
        if (contract == "widow-safe-house")
        {
            seat.IdentityCard.TurnTo("08001b");
            World.MoveToTop(world.Cards.Single(card => card.FaceId == "08005"), world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
            var discard = world.AreaOf(DeckType.DiscardPile, PlayArea.Of(0), cardOwner: 0);
            foreach (var copy in world.Cards.Where(card => card.FaceId == "08010").ToArray()) World.MoveToTop(copy, discard);
            World.MoveToTop(world.Cards.Single(card => card.FaceId == "08020"), discard);
        }
        else
        {
            seat.IdentityCard.Exhaust();
            seat.IdentityCard.TurnTo("08001a");
            World.MoveToTop(world.Cards.Single(card => card.FaceId == "08009"), world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
            World.MoveToTop(world.Cards.First(card => card.FaceId == "08010"), world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
        }
        Game game = Game.Begin(world, cards, abilities);
        Assert.Equal(PublicDecisionKind.OpeningHand, game.Pending!.PublicKind);
        if (contract == "widow-preparation")
        {
            var minion = world.Cards.Single(card => card.FaceId == "01103");
            World.MoveToTop(minion, world.AreaOf(DeckType.RevealingArea));
            EncounterEntry.Schedule(world, EncounterEntryRequest.For(minion, 0, new CardEntryPlacement()), new EffectCompletion(1, 1));
        }
        game.Resolve(Decision.Take(Assert.Single(game.Pending.Affordances).Id));
        Assert.Equal(contract == "widow-safe-house" ? PublicDecisionKind.PlayerAction : PublicDecisionKind.Response,
            game.Pending!.PublicKind);
        return new(game, []);
    }
}
