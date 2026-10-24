using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class WreckingEscapePlanTests
{
    [Theory]
    [InlineData(0, false)]
    [InlineData(1, false)]
    [InlineData(2, false)]
    [InlineData(3, false)]
    [InlineData(0, true)]
    [InlineData(1, true)]
    [InlineData(2, true)]
    [InlineData(3, true)]
    [Rule("rr:status-cards.1")]
    public void BoostChecksExistingToughBeforeGivingItAndUsesActivePair(int active, bool tough)
    {
        // Printed Boost: "Give the active villain a tough status card. If they already
        // have a tough status card, place 2 threat on their scheme."
        // rr:status-cards.1: "A character cannot have more than one status card of each type".
        var (world, groups, source) = WreckingEncounterCardFixture.Create("07043", 2);
        world.EncounterTopology!.SelectActive(groups[active].Villain, "setup", []);
        if (tough) Statuses.Give(world, groups[active].Villain, Statuses.Tough);
        var runner = AuthoredCards.Runner();
        runner.ExecuteBoost(world, source, 0);
        Sequence.Finish(world, world.Facts, runner, []);
        for (int index = 0; index < groups.Length; index++)
        {
            Assert.Equal(index == active, Statuses.Has(world, groups[index].Villain, Statuses.Tough));
            Assert.Equal(index == active && tough ? 2 : 0, groups[index].Scheme.Tokens.GetValueOrDefault("k_threat"));
        }
    }

    [Theory]
    [InlineData(false, false, 0)]
    [InlineData(false, true, 0)]
    [InlineData(true, false, 0)]
    [InlineData(true, true, 0)]
    [InlineData(false, false, 1)]
    [InlineData(true, false, 1)]
    [Rule("rr:you-your.5")]
    public void RevealConfusesIdentityOrMakesOnlyPiledriverScheme(bool confused, bool alterEgo, int player)
    {
        // Printed When Revealed: "You are confused. If you are already confused, Piledriver schemes."
        // rr:you-your.5 places a status card on "the identity card of the player resolving the card".
        var (world, groups, source) = WreckingEncounterCardFixture.Create("07043", 2, alterEgo ? "01001b" : "01001a", 2);
        if (confused) Statuses.Give(world, world.Seats[player].IdentityCard, Statuses.Confused);
        var runner = AuthoredCards.Runner();
        runner.ExecuteReveal(world, source, player);
        Sequence.Finish(world, world.Facts, runner, []);
        Assert.True(Statuses.Has(world, world.Seats[player].IdentityCard, Statuses.Confused));
        Assert.False(Statuses.Has(world, world.Seats[1 - player].IdentityCard, Statuses.Confused));
        for (int index = 0; index < groups.Length; index++)
        {
            Assert.Equal(index == 2 && confused ? 2 : 0, groups[index].Scheme.Tokens.GetValueOrDefault("k_threat"));
            // pack:mc03:new-rules: "When the villain is dealt a boost card, it is dealt from the active villain's deck."
            Assert.Equal(index == 1 && confused ? 1 : 2, groups[index].Deck.Cards.Count);
        }
        Assert.Same(groups[1], world.EncounterTopology!.Active);
    }
}
