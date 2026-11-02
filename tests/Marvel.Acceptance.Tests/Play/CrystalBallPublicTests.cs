using Marvel.Protocol;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Acceptance.Tests.Play;

/// <summary>
/// Public starter games with Galactic Artifacts as the modular set in which the defeater of
/// Crystal Ball (16130) plays a card from hand for 3 less. The deal is unarranged; the only
/// steer is that a player thwarts Crystal Ball whenever a turn offers it.
/// </summary>
public sealed class CrystalBallPublicTests(ITestOutputHelper output)
{
    private const string BallPlay = "16130: choose a card to play";

    [Theory]
    [InlineData("spider_man", "absorbing_man", false, 21u, "captain_marvel")]
    [InlineData("thor", "ultron", false, 29u, "captain_marvel")]
    [InlineData("captain_marvel", "risky_business", true, 3u, "she_hulk")]
    public void ACrystalBallPlayReachesAnEndingWithExactDecisionReplay(string hero, string scenario, bool multiplayer, uint seed, string partner)
    {
        // PublicStarterGames replays every answer on a second game and compares each result.
        int played = 0;
        PublicStarterGames.Play(hero, scenario, multiplayer, seed, output, (game, prompt) =>
        {
            if (prompt.Label == BallPlay && prompt.Affordances.Count > 0) played++;
            return ThwartTheBall(game, prompt);
        }, partner, modulars: ["galactic_artifacts"]);
        output.WriteLine($"Crystal Ball plays offered: {played}");
        Assert.True(played > 0);
    }

    [Fact]
    public void ACrystalBallPlaySurvivesAFreshHost()
    {
        // The policy plays to the Crystal Ball prompt; a second session restarts on a fresh
        // host at that prompt and gives the play to both.
        var specification = new GameSpecification("absorbing_man", ["spider_man"], ["galactic_artifacts"], 21);
        var answers = Play(new PublicRestartJourney(specification, "crystal-ball-find"));

        var journey = new PublicRestartJourney(specification, "crystal-ball-restart");
        foreach (var answer in answers.Take(answers.Count - 1)) journey.Answer(answer);
        Assert.Equal(BallPlay, journey.Factory.Game.Pending!.Label);
        var hand = journey.Factory.Game.State.Seats[0].Hand.Cards.Count;
        var fresh = journey.Restart();
        Assert.Equal(BallPlay, fresh.Factory.Game.Pending!.Label);
        fresh.Reject(answers[^1], stale: true);
        journey.Continue(fresh, answers[^1]);

        Assert.True(fresh.Factory.Game.State.Seats[0].Hand.Cards.Count < hand);
        journey.EquivalentState(fresh);
    }

    /// <summary>A thwart of Crystal Ball while a turn offers one, else nothing.</summary>
    private static Decision? ThwartTheBall(Game game, Prompt prompt)
    {
        var ball = game.State.Cards.FirstOrDefault(card => card.FaceId == "16130" && card.Area.Type == DeckType.SideSchemesArea);
        if (ball is null || prompt.Asking != Question.TurnOption) return null;
        return prompt.Affordances.FirstOrDefault(offer => offer.IsLegal && offer.Verb == BasicPowers.ThwartVerb
            && offer.Targets is { } targets && targets.Legal.Contains(ball.ObjectId)) is { } thwart
            ? Decision.Take(thwart.Id, [ball.ObjectId], []) : null;
    }

    /// <summary>The policy's answers up to and including its play at the first Crystal Ball prompt.</summary>
    private static List<Decision> Play(PublicRestartJourney journey)
    {
        var game = journey.Factory.Game;
        var policy = new CoreGamePolicy(game.State.Facts);
        var answers = new List<Decision>();
        while (true)
        {
            var prompt = game.Pending;
            Assert.NotNull(prompt);
            Assert.True(answers.Count < 2000);
            var answer = ThwartTheBall(game, prompt) ?? policy.Answer(game);
            answers.Add(answer);
            if (prompt.Label == BallPlay) return answers;
            journey.Answer(answer);
            game = journey.Factory.Game;
        }
    }
}
