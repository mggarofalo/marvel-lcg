using Marvel.Content.Behavior;
using Marvel.Rules.State;

namespace Marvel.Godot.Tests;

/// <summary>Prepared cross-pack scene with actual authored Lockjaw; no public Ms Marvel admission.</summary>
internal static class LockjawNativeScene
{
    internal static void Prepare(CanonicalCoreScene scene)
    {
        scene.Apply(new SetPlayerHand(0, [new("01088"), new("01089")]));
        scene.World.CreateCard("05018", scene.World.AreaOf(
            DeckType.DiscardPile, PlayArea.Of(0), cardOwner: 0));
    }
}
