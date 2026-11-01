using Marvel.Protocol;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Acceptance.Tests.Play;

public sealed class PowerStonePublicRestartTests
{
    private const string PowerStone = "16149";

    [Fact]
    [Rule("rr:attachment.1.1")]
    public void ThePowerStonesMoveToAHeroSurvivesAFreshHost()
    {
        // Printed 16149: "Setup. Attach to the villain. ... Forced Response: After a hero or
        // villain deals 3 or more damage to attached character with a single attack, attach
        // Power Stone to the attacking hero or villain." The policy plays She-Hulk (ATK 3)
        // against Rhino until her attack takes the stone; a second session stops one decision
        // short, restarts on a fresh host and gives that decision to both.
        var specification = new GameSpecification("rhino", ["she_hulk"], ["power_stone"], 1);
        var answers = Play(new PublicRestartJourney(specification, "power-stone-find"));

        var journey = new PublicRestartJourney(specification, "power-stone-restart");
        foreach (var answer in answers.Take(answers.Count - 1)) journey.Answer(answer);
        Assert.True(VillainHolds(journey.Factory.Game));
        var fresh = journey.Restart();
        fresh.Reject(answers[^1], stale: true);
        journey.Continue(fresh, answers[^1]);

        var state = fresh.Factory.Game.State;
        var stone = state.Cards.Single(card => card.FaceId == PowerStone);
        Assert.Equal(state.Seats[0].IdentityCard.ObjectId, stone.Area.Host);
        Assert.True(DeckTypes.IsInPlay(stone.Area.Type));
        journey.EquivalentState(fresh);
    }

    /// <summary>The policy's answers up to and including the one that moves the stone to a hero.</summary>
    private static List<Decision> Play(PublicRestartJourney journey)
    {
        var game = journey.Factory.Game;
        var policy = new CoreGamePolicy(game.State.Facts);
        var answers = new List<Decision>();
        while (VillainHolds(game))
        {
            Assert.NotNull(game.Pending);
            Assert.True(answers.Count < 500);
            var answer = policy.Answer(game);
            answers.Add(answer);
            journey.Answer(answer);
            game = journey.Factory.Game;
        }
        return answers;
    }

    private static bool VillainHolds(Game game)
    {
        var stone = game.State.Cards.Single(card => card.FaceId == PowerStone);
        return CardKinds.IsVillain(game.State.Facts.Kind(game.State.Cards[stone.Area.Host].FaceId));
    }
}
