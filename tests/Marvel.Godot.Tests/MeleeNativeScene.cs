using Marvel.Content.Behavior;
using Marvel.Rules.State;

namespace Marvel.Godot.Tests;

/// <summary>Prepared cross-pack component scene; actual full-book Melee without public Ms Marvel admission.</summary>
internal static class MeleeNativeScene
{
    internal static void Prepare(CanonicalCoreScene scene)
    {
        scene.Apply(new SetPlayerHand(0, [new("01088"), new("01005"), new("01002"), new("01006"), new("01007")]));
        scene.World.CreateCard("05030", scene.World.Seats[0].Hand);
        scene.Apply(new MoveSceneCard(new("01101"), new(SceneZone.EngagedMinion, 0)));
    }
}
