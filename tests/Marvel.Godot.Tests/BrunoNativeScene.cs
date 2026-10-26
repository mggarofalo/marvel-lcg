using Marvel.Content.Behavior;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Xunit;

namespace Marvel.Godot.Tests;

/// <summary>Synthetic cross-pack arrangement exercising actual authored Bruno; no Ms Marvel admission.</summary>
internal static class BrunoNativeScene
{
    internal static void Prepare(CanonicalCoreScene scene)
    {
        var world = scene.World;
        var host = world.CreateCard("05007", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        // Synthetic fixture setup: four known physical cards beneath the actual full-book support.
        var stored = world.Seats[0].Hand.Cards.Take(4).ToArray();
        Assert.Equal(4, stored.Length);
        foreach (var card in stored) BeneathCards.Place(world, card, host, false, "synthetic native Bruno fixture", []);
    }
}
