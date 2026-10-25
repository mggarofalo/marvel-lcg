using Marvel.Rules.Play;
using Marvel.Rules.State;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class WreckingRemainingSchemeTests
{
    private static readonly int[] ThreatCases = [8, 9, 13];
    public static IEnumerable<object[]> Cases() => Enumerable.Range(0, 4).SelectMany(group =>
        Enumerable.Range(1, 4).SelectMany(players => ThreatCases
            .Select(threat => new object[] { group, players, threat })));

    [Theory]
    [MemberData(nameof(Cases))]
    public void PrintedThresholdEffectsReachEveryFriendlySeatAndResetExactlyToThree(int group, int players, int threat)
    {
        // 07004/19/34/48: "After threat is placed here, if there is 10 or more threat here"
        // followed by the signature penalty and "Remove all but 3 threat from this scheme."
        var (world, groups, _) = WreckingEncounterCardFixture.Create("07037", 0, "01001b", players);
        var runner = Marvel.Content.Tests.Cards.AuthoredCards.Runner();
        world.Abilities = runner;
        var allies = new List<Card>();
        var supports = new List<Card>();
        for (int player = 0; player < players; player++)
        {
            var seat = world.Seats[player];
            allies.Add(world.CreateCard("01050", world.AreaOf(DeckType.AlliesArea, seat.Hero.PlayArea, cardOwner: player)));
            supports.Add(world.CreateCard("01092", world.AreaOf(DeckType.SupportsArea, seat.Hero.PlayArea, cardOwner: player)));
            for (int count = 0; count < 12; count++) world.CreateCard("01088", seat.Deck);
        }
        var initialDecks = world.Seats.Select(seat => seat.Deck.Cards.Count).ToArray();
        groups[group].Scheme.PlaceTokens("k_threat", threat);
        Threat.Schedule(world, new([groups[group].Scheme], null, 1, ThreatCause.CardAbility, "test"));
        WreckingRemainingSchemeFixture.Finish(world, runner);
        bool fired = threat >= 9;
        Assert.Equal(fired ? 3 : threat + 1, groups[group].Scheme.Tokens["k_threat"]);
        Assert.Equal(DeckType.SideSchemesArea, groups[group].Scheme.Area.Type);
        AssertPenalties(world, allies, supports, initialDecks, group, fired);
        Assert.All(groups, item => Assert.Equal(0, item.Villain.Damage));
    }

    private static void AssertPenalties(World world, List<Card> allies, List<Card> supports,
        int[] initialDecks, int group, bool fired)
    {
        int damage = fired && group == 0 ? 2 : 0;
        bool stunned = fired && group == 1;
        var supportArea = fired && group == 2 ? DeckType.DiscardPile : DeckType.SupportsArea;
        int discarded = fired && group == 3 ? 10 : 0;
        for (int seat = 0; seat < world.Players; seat++)
        {
            Assert.Equal(damage, world.Seats[seat].IdentityCard.Damage);
            Assert.Equal(damage, allies[seat].Damage);
            Assert.Equal(stunned, Statuses.Has(world, world.Seats[seat].IdentityCard, Statuses.Stunned));
            Assert.Equal(stunned, Statuses.Has(world, allies[seat], Statuses.Stunned));
            Assert.Equal(supportArea, supports[seat].Area.Type);
            Assert.Equal(initialDecks[seat] - discarded, world.Seats[seat].Deck.Cards.Count);
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void EachSignatureSchemeRemainsAtZeroWhileItsOwnVillainIsPresent(int group)
    {
        // pack:mc03:signature-side-schemes: "These side schemes are not discarded
        // when they have no threat on them."
        var (world, groups, source) = WreckingEncounterCardFixture.Create("07037", 0);
        world.Abilities = Marvel.Content.Tests.Cards.AuthoredCards.Runner();
        var scheme = groups[group].Scheme;
        Assert.False(Discard.EffectCanRemove(world, world.Facts, source, scheme));
        Defeat.Scheme(world, world.Facts, scheme, "test", []);
        Discard.Card(world, scheme, "test", []);
        Assert.Equal(DeckType.SideSchemesArea, scheme.Area.Type);
        World.MoveToTop(groups[group].Villain, world.AreaOf(DeckType.AsideDeck));
        Assert.True(Discard.EffectCanRemove(world, world.Facts, source, scheme));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void PairedSchemeSurvivesExtremePromotionAndLeavesOnlyOnFinalDefeat(int group)
    {
        // pack:mc03:adjustable-difficulty: version B enters after A is defeated.
        // pack:mc03:new-rules: the corresponding scheme leaves on the villain's defeat.
        var (world, groups, _) = WreckingProjectionFixture.Create();
        var runner = Marvel.Content.Tests.Cards.AuthoredCards.Runner();
        world.Abilities = runner;
        world.EncounterTopology!.SelectActive(groups[(group + 1) % 4].Villain, "test", []);
        var scheme = groups[group].Scheme;
        Defeat.FinalizeCharacter(world, world.Facts, groups[group].Villain, "test", []);
        WreckingRemainingSchemeFixture.Finish(world, runner);
        Assert.Same(groups[group].Stages[1], groups[group].CurrentVillain);
        Assert.Equal(DeckType.SideSchemesArea, scheme.Area.Type);
        Assert.False(SideSchemeDeparture.Allowed(world, scheme));
        Defeat.FinalizeCharacter(world, world.Facts, groups[group].Stages[1], "test", []);
        WreckingRemainingSchemeFixture.Finish(world, runner);
        Assert.Null(groups[group].CurrentVillain);
        Assert.Equal(DeckType.RemovedArea, scheme.Area.Type);
        Assert.All(groups.Where((_, index) => index != group), item =>
            Assert.Equal(DeckType.SideSchemesArea, item.Scheme.Area.Type));
    }
}
