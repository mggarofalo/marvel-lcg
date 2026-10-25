using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class CaptainAmericaHitSquadTests
{
    [Theory]
    [InlineData(0, false)]
    [InlineData(1, false)]
    [InlineData(0, true)]
    [InlineData(1, true)]
    [Rule("rr:in-player-order")]
    [Rule("rr:boost-boost-icon.2")]
    public void EachPlayersDamageUsesOnlyTheirDiscardAndNeverExecutesBoostText(int first, bool tough)
    {
        // 03027: "In player order, each player discards the top card of the encounter deck"
        // and takes one damage per boost icon. rr:in-player-order starts with the first player.
        // rr:boost-boost-icon.2: a star is "not itself considered a boost icon".
        var world = new World(CardCatalogData, players: 2) { FirstPlayer = first };
        foreach (string face in new[] { "03001a", "01010b" })
        {
            var seat = world.CreateSeat(face);
            seat.IdentityCard = world.CreateCard(face, seat.Hero);
        }
        world.CreateCard("01094", world.AreaOf(DeckType.VillainArea));
        var source = world.CreateCard("03027", world.AreaOf(DeckType.SideSchemesArea));
        var deck = world.AreaOf(DeckType.EncounterDeck);
        world.CreateCard("01101", deck);
        var star = world.CreateCard("01121", deck);
        var twoIcons = world.CreateCard("01100", deck);
        if (tough) Statuses.Give(world, world.Seats[first].IdentityCard, Statuses.Tough);
        var runner = AuthoredCards.Runner();
        runner.ExecuteReveal(world, source, 0);
        Sequence.Finish(world, world.Facts, runner, []);
        Assert.Equal(tough ? 0 : 2, world.Seats[first].IdentityCard.Damage);
        Assert.Equal(0, world.Seats[1 - first].IdentityCard.Damage);
        Assert.False(Statuses.Has(world, world.Seats[first].IdentityCard, Statuses.Tough));
        Assert.Equal(DeckType.EncounterDiscardPile, twoIcons.Area.Type);
        Assert.Equal(DeckType.EncounterDiscardPile, star.Area.Type);
        Assert.Single(deck.Cards);
        Assert.Empty(world.Areas.Where(area => area.Type == DeckType.EngagedEnemiesArea).SelectMany(area => area.Cards));
    }
}
