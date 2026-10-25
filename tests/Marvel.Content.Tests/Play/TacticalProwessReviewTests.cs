using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>Independent canonical-card controls in explicitly arranged worlds.</summary>
public sealed class TacticalProwessReviewTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    [Rule("rr:first-player.1")]
    public void OnlyTiedExtremeOffersAChoiceWithoutBroadeningTheOtherPool(bool tiedMinimum)
    {
        // 07031 selects "least threat" and "most threat" independently.
        // "The first player selects among the eligible options."
        var (world, groups, source) = WreckingEncounterCardFixture.Create("07031", 0, players: 2);
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        world.FirstPlayer = 1;
        int[] threat = tiedMinimum ? [2, 2, 5, 4] : [2, 3, 5, 5];
        for (int index = 0; index < groups.Length; index++)
            groups[index].Scheme.PlaceTokens("k_threat", threat[index]);

        runner.ExecuteReveal(world, source, 0);
        var prompt = Sequence.Work(world, world.Facts, runner, []);
        Assert.NotNull(prompt);
        Assert.Equal(1, prompt.Player);
        Assert.False(prompt.Cancellable);
        Assert.Equal(2, prompt.Affordances.Count);
        Assert.All(prompt.Affordances, offer => Assert.True(offer.IsLegal));
        Assert.Equal(threat.Select(value => (long)value),
            groups.Select(group => group.Scheme.Tokens.GetValueOrDefault("k_threat")));

        Sequence.Answer(world, world.Facts, runner, prompt, Decision.Take(1), []);
        WreckingSchemeFixture.Finish(world, runner);
        long[] expected = tiedMinimum ? [2, 0, 7, 4] : [0, 3, 5, 7];
        Assert.Equal(expected, groups.Select(group => group.Scheme.Tokens.GetValueOrDefault("k_threat")));
        Assert.Single(world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(0)).Cards);
        Assert.Empty(world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(1)).Cards);
    }
}
