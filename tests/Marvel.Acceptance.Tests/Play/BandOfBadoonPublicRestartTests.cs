using Marvel.Protocol;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Acceptance.Tests.Play;

public sealed class BandOfBadoonPublicRestartTests
{
    private static readonly string[] Band = ["16117", "16118", "16119", "16120", "16121"];

    [Fact]
    [Rule("rr:engage")]
    public void ABadoonMinionsEngagementSurvivesAFreshHost()
    {
        // Band of Badoon replaces Rhino's modular set. The policy plays She-Hulk until a Badoon
        // minion first engages her; a second session stops one decision short, restarts on a
        // fresh host and gives that decision to both.
        var specification = new GameSpecification("rhino", ["she_hulk"], ["band_of_badoon"], 1);
        var answers = Play(new PublicRestartJourney(specification, "band-of-badoon-find"));

        var journey = new PublicRestartJourney(specification, "band-of-badoon-restart");
        foreach (var answer in answers.Take(answers.Count - 1)) journey.Answer(answer);
        Assert.Empty(Engaged(journey.Factory.Game));
        var fresh = journey.Restart();
        fresh.Reject(answers[^1], stale: true);
        journey.Continue(fresh, answers[^1]);

        Assert.NotEmpty(Engaged(fresh.Factory.Game));
        journey.EquivalentState(fresh);
    }

    /// <summary>The policy's answers up to and including the one after which a Badoon minion is engaged.</summary>
    private static List<Decision> Play(PublicRestartJourney journey)
    {
        var game = journey.Factory.Game;
        var policy = new CoreGamePolicy(game.State.Facts);
        var answers = new List<Decision>();
        while (Engaged(game).Count == 0)
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

    private static List<Card> Engaged(Game game) =>
        [.. game.State.Cards.Where(card => Band.Contains(card.FaceId) && card.Area.Type == DeckType.EngagedEnemiesArea)];
}
