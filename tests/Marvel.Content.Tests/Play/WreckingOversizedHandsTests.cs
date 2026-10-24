using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class WreckingOversizedHandsTests
{
    [Theory]
    [InlineData(0, false)]
    [InlineData(1, false)]
    [InlineData(0, true)]
    [InlineData(1, true)]
    [Rule("rr:permanent.4.1")]
    public void RevealDiscardsHighestEligibleSupportWithoutTouchingForeignCards(int player, bool permanent)
    {
        // 07042: "Discard the support you control with the highest cost."
        // Permanent: "instead targets the non-permanent card that fits its criteria."
        var (world, groups, source) = WreckingEncounterCardFixture.Create("07042", 2, players: 2);
        world.CreateCard("01088", world.Seats[1].Deck);
        var area = world.AreaOf(DeckType.SupportsArea, PlayArea.Of(player), cardOwner: player);
        var mansion = world.CreateCard("01091", area);
        var helicarrier = world.CreateCard("01092", area);
        var foreign = world.CreateCard("01091",
            world.AreaOf(DeckType.SupportsArea, PlayArea.Of(1 - player), cardOwner: 1 - player));
        if (permanent) world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect,
            "permanent", Amount: 1, Affects: mansion.ObjectId));
        var runner = AuthoredCards.Runner();
        runner.ExecuteReveal(world, source, player);
        Assert.Null(Sequence.Work(world, world.Facts, runner, []));
        Assert.Equal(DeckType.DiscardPile, (permanent ? helicarrier : mansion).Area.Type);
        Assert.Equal(area, (permanent ? mansion : helicarrier).Area);
        Assert.Equal(DeckType.SupportsArea, foreign.Area.Type);
        Assert.All(groups, group => Assert.Equal(0, group.Scheme.Tokens.GetValueOrDefault("k_threat")));
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, true)]
    [Rule("rr:permanent.4")]
    public void NoEligibleSupportPlacesThreatOnlyOnActivePair(int player, bool permanent)
    {
        // 07042: "If no support was discarded this way, place 2 threat on the active villain's scheme."
        // Permanent excludes effects "that would cause the permanent card to leave play".
        var (world, groups, source) = WreckingEncounterCardFixture.Create("07042", 2, "01001b", 2);
        if (permanent)
        {
            var support = world.CreateCard("01091",
                world.AreaOf(DeckType.SupportsArea, PlayArea.Of(player), cardOwner: player));
            world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect,
                "permanent", Amount: 1, Affects: support.ObjectId));
        }
        var runner = AuthoredCards.Runner();
        runner.ExecuteReveal(world, source, player);
        Assert.Null(Sequence.Work(world, world.Facts, runner, []));
        for (int index = 0; index < groups.Length; index++)
            Assert.Equal(index == 1 ? 2 : 0, groups[index].Scheme.Tokens.GetValueOrDefault("k_threat"));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:first-player.1")]
    public void RevealTiesAndBoostChoicesKeepTheirProperChooserAndCandidates(bool boost)
    {
        // The first player chooses among "multiple eligible targets" for the highest-cost reveal.
        // The boost instead instructs the resolving player: "Discard a support you control."
        var (world, groups, source) = WreckingEncounterCardFixture.Create("07042", 2, players: 2);
        world.CreateCard("01088", world.Seats[1].Deck);
        var area = world.AreaOf(DeckType.SupportsArea, PlayArea.Of(1), cardOwner: 1);
        var first = world.CreateCard("01091", area);
        var second = world.CreateCard(boost ? "01092" : "01091", area);
        var runner = AuthoredCards.Runner();
        if (boost) runner.ExecuteBoost(world, source, 1);
        else runner.ExecuteReveal(world, source, 1);
        var prompt = Assert.IsType<Marvel.Rules.Prompts.Prompt>(Sequence.Work(world, world.Facts, runner, []));
        Assert.Equal(boost ? 1 : 0, prompt.Player);
        Assert.Equal([first.ObjectId, second.ObjectId], prompt.Affordances.Select(offer => offer.Id));
        Sequence.Answer(world, world.Facts, runner, prompt, Decision.Take(second.ObjectId), []);
        Assert.Null(Sequence.Work(world, world.Facts, runner, []));
        Assert.Equal(area, first.Area);
        Assert.Equal(DeckType.DiscardPile, second.Area.Type);
        Assert.All(groups, group => Assert.Equal(0, group.Scheme.Tokens.GetValueOrDefault("k_threat")));
    }
}
