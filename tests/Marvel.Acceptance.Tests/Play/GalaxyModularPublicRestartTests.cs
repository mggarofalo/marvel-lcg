using Marvel.Protocol;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Acceptance.Tests.Play;

public sealed class GalaxyModularPublicRestartTests
{
    [Theory]
    [InlineData("kree_militant", "she_hulk", "rhino")]
    [InlineData("menagerie_medley", "spider_man", "klaw")]
    [InlineData("space_pirates", "captain_marvel", "rhino")]
    public void TheFirstCardOfTheSetToEnterPlaySurvivesAFreshHost(string set, string hero, string scenario)
    {
        // The policy plays until a card of the set enters play; a second session stops one
        // decision short, restarts on a fresh host and gives that decision to both.
        var faces = GalaxyModularFaces.Of(set);
        var specification = new GameSpecification(scenario, [hero], [set], 1);
        var answers = Play(new PublicRestartJourney(specification, $"{set}-find"), faces);

        var journey = new PublicRestartJourney(specification, $"{set}-restart");
        foreach (var answer in answers.Take(answers.Count - 1)) journey.Answer(answer);
        Assert.False(InPlay(journey.Factory.Game, faces));
        var fresh = journey.Restart();
        fresh.Reject(answers[^1], stale: true);
        journey.Continue(fresh, answers[^1]);

        Assert.True(InPlay(fresh.Factory.Game, faces));
        journey.EquivalentState(fresh);
    }

    /// <summary>The policy's answers up to and including the one after which a card of the set is in play.</summary>
    private static List<Decision> Play(PublicRestartJourney journey, IReadOnlySet<string> faces)
    {
        var game = journey.Factory.Game;
        var policy = new CoreGamePolicy(game.State.Facts);
        var answers = new List<Decision>();
        while (!InPlay(game, faces))
        {
            Assert.NotNull(game.Pending);
            Assert.True(answers.Count < 800);
            var answer = policy.Answer(game);
            answers.Add(answer);
            journey.Answer(answer);
            game = journey.Factory.Game;
        }
        return answers;
    }

    private static bool InPlay(Game game, IReadOnlySet<string> faces) =>
        game.State.Cards.Any(card => faces.Contains(card.FaceId) && DeckTypes.IsInPlay(card.Area.Type));
}
