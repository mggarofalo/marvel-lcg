using Marvel.Cards.Run;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class WreckingBulldozerDiscardTests
{
    [Theory]
    [InlineData(0, true, "01005", 3)]
    [InlineData(1, true, "01005", 3)]
    [InlineData(0, false, "01005", 3)]
    [InlineData(1, false, "01005", 3)]
    [InlineData(0, true, "", 0)]
    [InlineData(1, false, "", 0)]
    [InlineData(0, true, "14006", 0)]
    [InlineData(1, false, "01088", 0)]
    public void HeadbuttUsesOnlyRevealingPlayersRandomDiscard(int player, bool hero, string face, int cost)
    {
        var (world, groups, source) = WreckingEncounterCardFixture.Create("07058", 3,
            hero ? "01001a" : "01001b", 2);
        if (player == 1 && !hero)
        {
            World.MoveToTop(world.Seats[1].IdentityCard, world.AreaOf(DeckType.RemovedArea));
            world.Seats[1].IdentityCard = world.CreateCard("01001b", world.Seats[1].Hero);
        }
        if (face.Length > 0) world.CreateCard(face, world.Seats[player].Hand);
        var foreign = world.CreateCard("01005", world.Seats[1 - player].Hand);
        var runner = Runner();
        world.Abilities = runner;
        runner.ExecuteReveal(world, source, player);
        Sequence.Finish(world, world.Facts, runner, []);
        Assert.Empty(world.Seats[player].Hand.Cards);
        Assert.Contains(foreign, world.Seats[1 - player].Hand.Cards);
        Assert.Equal(hero ? cost : 0, world.Seats[player].IdentityCard.Damage);
        Assert.Equal(hero ? 0 : cost, groups[3].Scheme.Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(0, groups[1].Scheme.Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(0, world.Seats[1 - player].IdentityCard.Damage);
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, false)]
    [InlineData(0, true)]
    [InlineData(1, true)]
    public void LeadingChargeCountsDistinctTypesAndUsesBulldozerAttack(int player, bool duplicates)
    {
        var (world, groups, source) = WreckingEncounterCardFixture.Create("07059", 3, players: 2);
        foreach (var seat in world.Seats) world.CreateCard("01088", seat.Deck);
        world.CreateCard("01002", world.AreaOf(DeckType.DiscardPile, PlayArea.Of(player), cardOwner: player));
        string[] faces = duplicates ? ["01005", "01003", "01005"] : ["01005", "01007", "01088"];
        foreach (var face in faces) world.CreateCard(face, world.Seats[player].Deck);
        int before = world.Seats[player].Deck.Cards.Count;
        int foreignBefore = world.Seats[1 - player].Deck.Cards.Count;
        var runner = Runner();
        world.Abilities = runner;
        runner.ExecuteReveal(world, source, player);
        Sequence.Finish(world, world.Facts, runner, []);
        Assert.Equal(before - 3, world.Seats[player].Deck.Cards.Count);
        Assert.Equal(foreignBefore, world.Seats[1 - player].Deck.Cards.Count);
        Assert.Equal(duplicates ? 1 : 3, groups[3].Scheme.Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(0, groups[1].Scheme.Tokens.GetValueOrDefault("k_threat"));
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 0)]
    [InlineData(0, 1)]
    [InlineData(1, 1)]
    [InlineData(0, 2)]
    [InlineData(1, 2)]
    public void LeadingChargeShortDeckCountsOnlyCardsActuallyDiscarded(int player, int count)
    {
        var (world, groups, source) = WreckingEncounterCardFixture.Create("07059", 3, players: 2);
        foreach (var card in world.Seats[player].Deck.Cards.ToArray())
            World.MoveToTop(card, world.AreaOf(DeckType.RemovedArea));
        for (int index = 0; index < count; index++) world.CreateCard("01005", world.Seats[player].Deck);
        var runner = Runner();
        world.Abilities = runner;
        runner.ExecuteReveal(world, source, player);
        Sequence.Finish(world, world.Facts, runner, []);
        Assert.Equal(count == 0 ? 0 : 1, groups[3].Scheme.Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(0, groups[1].Scheme.Tokens.GetValueOrDefault("k_threat"));
    }

    private static AbilityRunner Runner() => Marvel.Content.Tests.Cards.AuthoredCards.Runner();
}
