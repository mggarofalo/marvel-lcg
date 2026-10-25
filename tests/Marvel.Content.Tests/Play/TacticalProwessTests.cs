using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>Canonical 07031 in arranged worlds; no public scenario admission.</summary>
public sealed class TacticalProwessTests
{
    [Theory]
    [InlineData(0, false)]
    [InlineData(1, false)]
    [InlineData(0, true)]
    [InlineData(1, true)]
    [Rule("rr:surge.1")]
    public void MovesAllLeastToMostAndSurgesOnlyWithoutDestinationResponse(int player, bool response)
    {
        // 07031: "Move all threat from the side scheme with the least threat"
        // "to the side scheme with the most threat." Surge deals "1 facedown encounter card".
        var (world, groups, source) = WreckingEncounterCardFixture.Create("07031", 0, players: 2);
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        int[] threat = [2, 4, response ? 9 : 6, 3];
        for (int index = 0; index < groups.Length; index++) groups[index].Scheme.PlaceTokens("k_threat", threat[index]);
        runner.ExecuteReveal(world, source, player);
        WreckingSchemeFixture.Finish(world, runner);
        Assert.Equal(0, ThreatOn(groups[0].Scheme));
        Assert.Equal(4, ThreatOn(groups[1].Scheme));
        Assert.Equal(response ? 3 : 8, ThreatOn(groups[2].Scheme));
        Assert.Equal(3, ThreatOn(groups[3].Scheme));
        Assert.Equal(response ? 0 : 1, Dealt(world, player));
        Assert.Equal(0, Dealt(world, 1 - player));
        Assert.Equal(response ? 2 : 1, groups[1].Deck.Cards.Count);
        Assert.Equal(2, groups[0].Deck.Cards.Count);
        Assert.Same(groups[1], world.EncounterTopology!.Active);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 0)]
    [InlineData(0, 7)]
    [InlineData(1, 7)]
    public void ZeroThreatSourceMovesNothingAndSurges(int player, int maximum)
    {
        // 07031: "If that scheme's 'Forced Response' ability is not triggered this way, this card gains surge."
        var (world, groups, source) = WreckingEncounterCardFixture.Create("07031", 2, players: 2);
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        for (int index = 1; index < groups.Length; index++) groups[index].Scheme.PlaceTokens("k_threat", maximum);
        runner.ExecuteReveal(world, source, player);
        WreckingSchemeFixture.Finish(world, runner);
        Assert.Equal(new[] { 0, maximum, maximum, maximum }, groups.Select(group => ThreatOn(group.Scheme)));
        Assert.Equal(1, Dealt(world, player));
        Assert.Equal(0, Dealt(world, 1 - player));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void EverySideSchemeParticipatesEvenWithoutAPairedVillain(bool extraIsSource)
    {
        // The printed endpoints are "side scheme", without a Wrecking Crew qualifier.
        var (world, groups, source) = WreckingEncounterCardFixture.Create("07031", 0);
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        int[] threat = extraIsSource ? [2, 3, 4, 5] : [1, 2, 3, 4];
        for (int index = 0; index < groups.Length; index++) groups[index].Scheme.PlaceTokens("k_threat", threat[index]);
        var extra = world.CreateCard(extraIsSource ? "07004" : "01108", world.AreaOf(DeckType.SideSchemesArea));
        extra.PlaceTokens("k_threat", extraIsSource ? 1 : 12);
        runner.ExecuteReveal(world, source, 0);
        WreckingSchemeFixture.Finish(world, runner);
        Assert.Equal(extraIsSource ? 0 : 13, ThreatOn(extra));
        Assert.Equal(DeckType.SideSchemesArea, extra.Area.Type);
        Assert.Equal(extraIsSource ? 2 : 0, ThreatOn(groups[0].Scheme));
        Assert.Equal(extraIsSource ? 6 : 4, ThreatOn(groups[3].Scheme));
        Assert.Equal(1, Dealt(world, 0));
    }

    [Fact]
    public void DestinationResponseChoiceCompletesBeforeSurgeCheckWithoutMovingAgain()
    {
        // Pile It On! discards the highest-cost upgrade/support and resets to 3.
        // 07031 tests whether that response triggered, even after its threat decreases.
        var (world, groups, source) = WreckingEncounterCardFixture.Create("07031", 0);
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        int[] threat = [2, 4, 9, 3];
        for (int index = 0; index < groups.Length; index++) groups[index].Scheme.PlaceTokens("k_threat", threat[index]);
        var first = world.CreateCard("01056", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        var second = world.CreateCard("01092", first.Area);
        runner.ExecuteReveal(world, source, 0);
        var prompt = Sequence.Work(world, world.Facts, runner, []);
        Assert.NotNull(prompt);
        Assert.Equal(new[] { first.ObjectId, second.ObjectId }.Order(), prompt.Affordances.Select(offer => offer.Id).Order());
        Assert.Equal(0, ThreatOn(groups[0].Scheme));
        Assert.Equal(11, ThreatOn(groups[2].Scheme));
        Assert.Equal(0, Dealt(world, 0));
        Sequence.Answer(world, world.Facts, runner, prompt, Decision.Take(second.ObjectId), []);
        WreckingSchemeFixture.Finish(world, runner);
        Assert.Equal(0, ThreatOn(groups[0].Scheme));
        Assert.Equal(3, ThreatOn(groups[2].Scheme));
        Assert.Equal(DeckType.DiscardPile, second.Area.Type);
        Assert.Equal(DeckType.SupportsArea, first.Area.Type);
        Assert.Equal(0, Dealt(world, 0));
        Assert.Equal(2, groups[1].Deck.Cards.Count);
    }

    private static int ThreatOn(Card card) => checked((int)card.Tokens.GetValueOrDefault("k_threat"));
    private static int Dealt(World world, int player) =>
        world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(player)).Cards.Count;
}
