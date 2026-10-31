using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Xunit;

namespace Marvel.Acceptance.Tests.Play;

/// <summary>Unarranged public starter games; deterministic policy losses are complete games.</summary>
public sealed class WaspPublicGameTests(ITestOutputHelper output)
{
    private const string Giant = "13001c";

    [Theory]
    [InlineData("rhino", false, 1u)]
    [InlineData("klaw", false, 2u)]
    [InlineData("ultron", false, 3u)]
    [InlineData("crossbones", false, 4u)]
    [InlineData("absorbing_man", false, 5u)]
    [InlineData("rhino", true, 6u)]
    [InlineData("absorbing_man", true, 7u)]
    public void PublicStartersReachAnEndingWithExactDecisionReplay(string scenario, bool multiplayer, uint seed) =>
        PublicStarterGames.Play("wasp", scenario, multiplayer, seed, output);

    [Theory]
    [InlineData("ultron", 3u)]
    [InlineData("wrecking_crew_standard", 8u)]
    public void AGiantStarterDividesItsBasicPowersThroughACompleteReplayedGame(string scenario, uint seed)
    {
        // The policy never changes form, so this game prefers Giant Wasp whenever the
        // turn offers it and spreads each division one point per recipient in turn.
        var seats = PublicStarterGames.Play("wasp", scenario, false, seed, output, PreferGiant);
        Assert.True(seats.Divisions > 0);
    }

    private static Decision? PreferGiant(Game game, Prompt prompt)
    {
        if (prompt.Affordances.SingleOrDefault(offer => offer.Verb == BasicPowerDivision.Verb) is { } divide)
            return Decision.Take(divide.Id) with { Targets = Spread(divide.Targets!) };
        if (prompt.Asking != Question.TurnOption) return null;
        return prompt.Affordances.FirstOrDefault(offer => offer.Verb == Game.ChangeForm && offer.Label == Giant)
            is { } giant ? Decision.Take(giant.Id) : null;
    }

    private static int[] Spread(TargetRequest request)
    {
        var shares = new List<int>();
        while (shares.Count < request.Min)
            foreach (int target in request.Legal.Order())
                if (shares.Count < request.Min && shares.Count(id => id == target)
                    < (request.MaximumOccurrences?.GetValueOrDefault(target) ?? request.Max))
                    shares.Add(target);
        return [.. shares];
    }
}
