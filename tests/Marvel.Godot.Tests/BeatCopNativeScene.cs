using Marvel.Content.Behavior;
using Marvel.Rules.State;

namespace Marvel.Godot.Tests;

/// <summary>Prepared cross-pack component scenes using actual authored 10029; no public opening claim.</summary>
internal static class BeatCopNativeScene
{
    internal static void Prepare(CanonicalCoreScene scene, string contract)
    {
        scene.Apply(new SetPlayerHand(0, []));
        scene.Apply(new SetSceneCounters(new("01097b"), "threat", 4));
        scene.World.CreateCard("01131", scene.World.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        var support = scene.World.CreateCard("10029", scene.World.AreaOf(DeckType.SupportsArea,
            PlayArea.Of(0), cardOwner: 0));
        // The damage fixture explicitly starts after a ready transition with three stored threat.
        // Preparation ends before Game.Begin; interaction never mutates the fixture.
        if (contract == "beat-cop-damage") support.PlaceTokens("k_threat", 3);
    }
}
