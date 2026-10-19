using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class HydraConcussionTests : CoreCardTestBase
{
    [Theory]
    [InlineData(true, false, 1)]
    [InlineData(true, true, 2)]
    [InlineData(false, false, 1)]
    [InlineData(false, true, 2)]
    public void ConcussionChecksStatusBeforeGivingIt(bool hero, bool prior, int amount)
    {
        var world = Hero("01001a,01001b");
        if (!hero) Forms.Change(world.Seats[0], Cards);
        world.Abilities = AuthoredCards.Runner();
        var identity = world.Seats[0].IdentityCard;
        string status = hero ? Statuses.Stunned : Statuses.Confused;
        if (prior) Statuses.Give(world, identity, status);
        var main = world.CreateCard("01097b", world.AreaOf(DeckType.MainSchemesArea));
        var card = world.CreateCard("04151", world.AreaOf(DeckType.RevealingArea));
        AuthoredCards.Runner().ExecuteReveal(world, card, 0);
        Sequence.Finish(world, Cards, AuthoredCards.Runner(), []);
        Assert.True(Statuses.Has(world, identity, status));
        Assert.Equal(hero ? amount : 0, identity.Damage);
        Assert.Equal(hero ? 0 : amount, main.Tokens.GetValueOrDefault("k_threat"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void ViperReducesOnlyTheEngagedPlayersHandSize(int player)
    {
        var world = Hero("01001a", 2);
        world.Abilities = AuthoredCards.Runner();
        var before = world.Seats.Select(seat => Modified(world, seat.IdentityCard, "hand_size")).ToArray();
        var viper = world.CreateCard("04054", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(player)));
        Assert.Equal(before[player] - 1, Modified(world, world.Seats[player].IdentityCard, "hand_size"));
        Assert.Equal(before[1-player], Modified(world, world.Seats[1-player].IdentityCard, "hand_size"));
        Discard.Card(world, viper, "test", []);
        Assert.Equal(before[player], Modified(world, world.Seats[player].IdentityCard, "hand_size"));
    }
}

