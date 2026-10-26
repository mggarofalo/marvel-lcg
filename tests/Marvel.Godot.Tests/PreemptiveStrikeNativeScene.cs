using Marvel.Content.Behavior;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Xunit;

namespace Marvel.Godot.Tests;

/// <summary>Prepared cross-pack attack with actual full-book 05014; no public Ms Marvel admission.</summary>
internal static class PreemptiveStrikeNativeScene
{
    internal static void Prepare(CanonicalCoreScene scene)
    {
        scene.Apply(new SetPlayerHand(0, [new("01005"), new("01006"), new("01007"), new("01008")]));
        scene.Apply(new StackEncounterDeck([new("01102"), new("01105")]));
        scene.World.CreateCard("05014", scene.World.Seats[0].Hand);
    }

    internal static void Advance(Game game)
    {
        // Prepared prelude: end the player turn and explicitly decline earlier windows.
        int source = Assert.Single(game.State.Seats[0].Hand.Cards, card => card.FaceId == "05014").ObjectId;
        game.Resolve(Decision.Decline);
        for (int answer = 0; answer < 25; answer++)
        {
            var prompt = Assert.IsType<Prompt>(game.Pending);
            if (prompt.Trigger == "WhenBoostCardTurnedFaceup")
            {
                Assert.Equal(2, game.State.Agenda.Occurrence!.BoostIcons!.Remaining);
                Assert.Contains(prompt.Affordances, offer => offer.AnchorId == source);
                return;
            }
            game.Resolve(prompt.Cancellable ? Decision.Decline :
                Decision.Take(Assert.Single(prompt.Affordances).Id));
        }
        throw new InvalidOperationException("Prepared attack did not reach the individual boost faceup interrupt.");
    }
}
