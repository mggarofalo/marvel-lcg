using System.Collections.Immutable;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class CaptainAmericaBaronZemoTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:cannot")]
    [Rule("rr:you-your.12")]
    [Rule("rr:you-your.15")]
    public void EngagedIdentityCannotThwartButItsAllyCan(bool confused)
    {
        // 03028: "While Baron Zemo is engaged with you, you cannot thwart."
        // rr:you-your.12: event thwarts "are also considered to be performed by that player's identity."
        // rr:you-your.15: ally thwarts "are not considered to be performed by that player's identity."
        Card? ally = null;
        Card? thwart = null;
        var (game, world) = CaptainAmericaFixture.PlayingCaptain(board =>
        {
            board.CreateCard("03028", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            ally = board.CreateCard("03014", board.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
            thwart = board.CreateCard("01060", board.Seats[0].Hand);
            board.CreateCard("03021", board.Seats[0].Hand);
            board.TheCardIn(DeckType.MainSchemesArea)!.PlaceTokens("k_threat", 5);
            if (confused) Statuses.Give(board, board.Seats[0].IdentityCard, Statuses.Confused);
        });
        var hero = world.Seats[0].IdentityCard;
        var scheme = world.TheCardIn(DeckType.MainSchemesArea)!;
        Assert.DoesNotContain(game.Pending!.Affordances, offer =>
            offer.AnchorId == hero.ObjectId && offer.Verb == BasicPowers.ThwartVerb);
        Assert.DoesNotContain(game.Pending!.Affordances, offer => offer.AnchorId == thwart!.ObjectId && offer.PlaysCard);
        string digest = world.Digest().Canonical();
        Assert.Throws<RulesNotImplementedException>(() => BasicPowers.BasicThwart(world, world.Facts, 0, scheme, []));
        Assert.Equal(digest, world.Digest().Canonical());
        var offered = Assert.Single(game.Pending!.Affordances, offer =>
            offer.AnchorId == ally!.ObjectId && offer.Verb == BasicPowers.ThwartVerb);
        game.Resolve(new Decision(offered.Id, [scheme.ObjectId]));
        Assert.Equal(4, scheme.Tokens["k_threat"]);
        Assert.Equal(1, ally!.Damage);
        Assert.Equal(confused, Statuses.Has(world, hero, Statuses.Confused));
    }

    [Fact]
    public void ReengagementAndDepartureImmediatelyMoveAndRemoveTheProhibition()
    {
        Card? zemo = null;
        var (_, world) = CaptainAmericaFixture.PlayingCaptain(board =>
            zemo = board.CreateCard("03028", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0))), twoPlayers: true);
        Assert.False(ThwartPermissions.CanThwart(world, world.Seats[0].IdentityCard));
        Assert.True(ThwartPermissions.CanThwart(world, world.Seats[1].IdentityCard));
        World.MoveToTop(zemo!, world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1)));
        Assert.True(ThwartPermissions.CanThwart(world, world.Seats[0].IdentityCard));
        Assert.False(ThwartPermissions.CanThwart(world, world.Seats[1].IdentityCard));
        Discard.Card(world, zemo!, "test", []);
        Assert.True(ThwartPermissions.CanThwart(world, world.Seats[1].IdentityCard));
    }

    [Fact]
    public void ABlankMinionProfileSuppressesThePrintedProhibition()
    {
        Card? zemo = null;
        var (_, world) = CaptainAmericaFixture.PlayingCaptain(board =>
            zemo = board.CreateCard("03028", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0))));
        Assert.False(ThwartPermissions.CanThwart(world, world.Seats[0].IdentityCard));
        zemo!.AssignProfile(new EffectiveCardProfile("test-blank", "Blank minion", CardKind.Minion,
            [], ImmutableDictionary<string, long>.Empty.Add("HP", 5)));
        Assert.True(ThwartPermissions.CanThwart(world, world.Seats[0].IdentityCard));
    }

    [Fact]
    public void NonThwartThreatRemovalRemainsLegalWhileEngaged()
    {
        Card? ally = null;
        var (game, world) = CaptainAmericaFixture.PlayingCaptain(board =>
        {
            board.CreateCard("03028", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            board.TheCardIn(DeckType.MainSchemesArea)!.PlaceTokens("k_threat", 5);
            ally = board.CreateCard("03002", board.Seats[0].Hand);
            board.CreateCard("03021", board.Seats[0].Hand);
            board.CreateCard("03022", board.Seats[0].Hand);
        });
        var play = Assert.Single(game.Pending!.Affordances, offer => offer.AnchorId == ally!.ObjectId && offer.PlaysCard);
        int[] payment = world.Seats[0].Hand.Cards.Where(card => card.FaceId is "03021" or "03022").Select(card => card.ObjectId).ToArray();
        game.Resolve(new Decision(play.Id, [world.Seats[0].IdentityCard.ObjectId], payment, Allocations: ResourcePayment.Allocate(play.CostOptions[0], payment)));
        var response = Assert.Single(game.Pending!.Affordances, offer => offer.AnchorId == ally!.ObjectId);
        game.Resolve(Decision.Take(response.Id));
        var scheme = world.TheCardIn(DeckType.MainSchemesArea)!;
        var choice = Assert.Single(game.Pending!.Affordances);
        game.Resolve(Decision.Take(choice.Id));
        Assert.Equal(3, scheme.Tokens["k_threat"]);
    }
}
