using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>Independent actual-card probes in prepared nonpublic engine states.</summary>
public sealed class MsMarvelBrunoStatusIndependentTests
{
    [Fact]
    public void NonFirstBrunoControllerStoresFromTheirHandRatherThanPhysicalOwnersHand()
    {
        Card host = null!;
        var runner = AuthoredCards.Runner();
        var (_, world) = Playing(board => host = board.CreateCard("05007",
            board.AreaOf(DeckType.SupportsArea, PlayArea.Of(1), cardOwner: 0)),
            heroes: ["spider_man", "captain_marvel"], abilities: runner);
        var own = world.Seats[1].Hand.Cards[0];
        var foreign = world.Seats[0].Hand.Cards[0];
        var action = Assert.Single(runner.Actions(world, 1), a => a.Card == host.ObjectId);
        Assert.DoesNotContain(runner.Actions(world, 0), a => a.Card == host.ObjectId);
        runner.ExecuteAction(world, new PlayerAction(action, [], []));
        var continuation = Assert.Single(world.Agenda.Outstanding);
        var prompt = runner.Choosing(world, continuation)!;
        Assert.Equal(1, prompt.Player);
        Assert.Equal(world.Seats[1].Hand.Cards.Select(c => c.ObjectId), prompt.Affordances.Select(a => a.AnchorId));
        Assert.DoesNotContain(prompt.Affordances, a => a.AnchorId == foreign.ObjectId);
        runner.Chose(world, continuation, Decision.Take(own.ObjectId));
        Assert.Same(own, Assert.Single(BeneathCards.Of(world, host)));
        Assert.False(own.FaceUp);
        Assert.False(DeckTypes.IsInPlay(own.Area.Type));
        Assert.Same(world.Seats[0].Hand, foreign.Area);
        Assert.Equal(1, own.Owner);
        Assert.Equal(0, host.Owner);
        Assert.False(host.Ready);
    }

    [Fact]
    public void NonFirstBrunoControllerRetrievesForeignOwnedCardsIntoTheirOwnHand()
    {
        Card host = null!;
        Card stored = null!;
        var runner = AuthoredCards.Runner();
        var (_, world) = Playing(board =>
        {
            host = board.CreateCard("05007", board.AreaOf(DeckType.SupportsArea, PlayArea.Of(1), cardOwner: 0));
            stored = board.Seats[0].Hand.Cards[0];
            BeneathCards.Place(board, stored, host, false, "fixture", []);
            board.Seats[1].IdentityCard.TurnTo("01010a");
        }, heroes: ["spider_man", "captain_marvel"], abilities: runner);
        int firstHand = world.Seats[0].Hand.Cards.Count;
        int secondHand = world.Seats[1].Hand.Cards.Count;
        var action = Assert.Single(runner.Actions(world, 1), a => a.Card == host.ObjectId);
        runner.ExecuteAction(world, new PlayerAction(action, [], []));
        var continuation = Assert.Single(world.Agenda.Outstanding);
        var prompt = runner.Choosing(world, continuation)!;
        Assert.Equal(1, prompt.Player);
        Assert.Equal(new[] { stored.ObjectId }, Assert.Single(prompt.Affordances).Targets!.Legal);
        runner.Chose(world, continuation, new Decision(Assert.Single(prompt.Affordances).Id, [stored.ObjectId]));
        Assert.Same(world.Seats[1].Hand, stored.Area);
        Assert.Equal(0, stored.Owner);
        Assert.True(stored.FaceUp);
        Assert.Equal(firstHand, world.Seats[0].Hand.Cards.Count);
        Assert.Equal(secondHand + 1, world.Seats[1].Hand.Cards.Count);
        Assert.Empty(BeneathCards.Of(world, host));
        Assert.False(host.Ready);
    }

    [Theory]
    [InlineData("05015", "stunned", false)]
    [InlineData("05015", "stunned", true)]
    [InlineData("05031", "confused", false)]
    [InlineData("05031", "confused", true)]
    [Rule("rr:retaliate-x.1")]
    [Rule("rr:stun-stunned.5")]
    public void StatusOnlyAttackRetaliatesUnlessStunnedHeroReplacesTheEntireAbility(string face, string status, bool stunned)
    {
        // Retaliate: "After this character is attacked, deal X damage to the attacker."
        // Stun: "Costs associated with the attack attempt ... must still be paid."
        Card played = null!;
        Card[] payment = [];
        var (game, world) = Playing(board =>
        {
            played = board.CreateCard(face, board.Seats[0].Hand);
            payment = [board.CreateCard("01011", board.Seats[0].Hand), board.CreateCard("01089", board.Seats[0].Hand)];
            var enemy = board.TheCardIn(DeckType.VillainArea)!;
            board.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect,
                Kind: "retaliate", Amount: 1, Card: enemy.ObjectId, Affects: enemy.ObjectId));
            if (stunned) Statuses.Give(board, board.Seats[0].IdentityCard, "stunned");
        }, hero: true, abilities: AuthoredCards.Runner());
        var target = world.TheCardIn(DeckType.VillainArea)!;
        var action = Assert.Single(game.Pending!.Affordances, a => a.AnchorId == played.ObjectId);
        game.Resolve(new Decision(action.Id, [], payment.Select(c => c.ObjectId).ToArray(), Allocations:
            [new(payment[0].ObjectId, 0, "B"), new(payment[1].ObjectId, 0, "BB")]));
        game.Resolve(Decision.Take(target.ObjectId));
        Assert.Equal(!stunned, Statuses.Has(world, target, status));
        Assert.Equal(0, target.Damage);
        Assert.Equal(stunned ? 0 : 1, world.Seats[0].IdentityCard.Damage);
        Assert.False(Statuses.Has(world, world.Seats[0].IdentityCard, "stunned"));
        Assert.Equal(DeckType.DiscardPile, played.Area.Type);
        Assert.All(payment, card => Assert.Equal(DeckType.DiscardPile, card.Area.Type));
        Assert.True(game.IsRootPrompt);
    }
}
