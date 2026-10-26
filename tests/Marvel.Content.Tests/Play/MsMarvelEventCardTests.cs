using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>Actual authored rows exercised through ordinary event payment and windows.</summary>
public sealed class MsMarvelEventCardTests
{
    private static long Observed(Card target, bool thwart) => thwart ? target.Tokens["k_threat"] : target.Damage;

    private static long Delta(long amount, bool thwart) => thwart ? -amount : amount;

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    [Rule("rr:event.5")]
    [Rule("rr:initiating-abilities.step.6")]
    public void ActualEventReceivesItsUpgradeOnlyAfterPaymentBeforeItsBody(bool thwart, bool accept)
    {
        // Big Hands: "Deal 4 damage to an enemy"; Sneak By: "Remove 3 threat
        // from a scheme". rr:event.5: "each of those instances is modified".
        Card? played = null;
        Card? payment = null;
        Card? upgrade = null;
        var (game, world) = Playing(board =>
        {
            played = board.CreateCard(thwart ? "05004" : "05003", board.Seats[0].Hand);
            payment = board.CreateCard("01088", board.Seats[0].Hand);
            upgrade = board.CreateCard(thwart ? "05011" : "05010", board.AreaOf(
                DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
            board.TheCardIn(DeckType.MainSchemesArea)!.PlaceTokens("k_threat", 10);
        }, hero: true, abilities: AuthoredCards.Runner());
        var target = world.TheCardIn(thwart ? DeckType.MainSchemesArea : DeckType.VillainArea)!;
        long before = Observed(target, thwart);
        game.Resolve(PaidDecision.Take(Assert.Single(game.Pending!.Affordances,
            offer => offer.AnchorId == played!.ObjectId), [payment!.ObjectId]));
        Assert.Equal(DeckType.DiscardPile, payment.Area.Type);
        Assert.Equal(before, Observed(target, thwart));
        var interrupt = Assert.Single(game.Pending!.Affordances);
        Assert.Equal(upgrade!.ObjectId, interrupt.AnchorId);
        game.Resolve(accept ? Decision.Take(interrupt.Id) : Decision.Decline);
        game.Resolve(Decision.Take(target.ObjectId));
        long amount = (thwart ? 3 : 4) + (accept ? 2 : 0);
        Assert.Equal(before + Delta(amount, thwart), Observed(target, thwart));
        Assert.Equal(!accept, upgrade.Ready);
        Assert.Equal(DeckType.DiscardPile, played!.Area.Type);
        Assert.DoesNotContain(world.Effects.Registered, effect => effect.Kind is "eventDamage" or "eventThreatRemoval");
        Assert.True(game.IsRootPrompt);
    }

    [Theory]
    [InlineData(false, "wrongTrait")]
    [InlineData(true, "wrongTrait")]
    [InlineData(false, "exhausted")]
    [InlineData(true, "exhausted")]
    [InlineData(false, "otherPlayer")]
    [InlineData(true, "otherPlayer")]
    public void ActualUpgradeRequiresItsPrintedTraitReadyStateAndController(bool thwart, string control)
    {
        Card? played = null;
        Card? payment = null;
        var (game, world) = Playing(board =>
        {
            int owner = control == "otherPlayer" ? 1 : 0;
            string upgradeId = thwart != (control == "wrongTrait") ? "05011" : "05010";
            var upgrade = board.CreateCard(upgradeId, board.AreaOf(
                DeckType.UpgradesArea, PlayArea.Of(owner), cardOwner: owner));
            if (control == "exhausted") upgrade.Exhaust();
            played = board.CreateCard(thwart ? "05004" : "05003", board.Seats[0].Hand);
            payment = board.CreateCard("01088", board.Seats[0].Hand);
            board.TheCardIn(DeckType.MainSchemesArea)!.PlaceTokens("k_threat", 10);
        }, hero: true, heroes: ["spider_man", "captain_marvel"], abilities: AuthoredCards.Runner());
        var target = world.TheCardIn(thwart ? DeckType.MainSchemesArea : DeckType.VillainArea)!;
        long before = Observed(target, thwart);
        game.Resolve(PaidDecision.Take(Assert.Single(game.Pending!.Affordances,
            offer => offer.AnchorId == played!.ObjectId), [payment!.ObjectId]));
        game.Resolve(Decision.Take(target.ObjectId));
        Assert.Equal(before + Delta(thwart ? 3 : 4, thwart), Observed(target, thwart));
        Assert.True(game.IsRootPrompt);
        Assert.DoesNotContain(world.Effects.Registered, effect => effect.Kind is "eventDamage" or "eventThreatRemoval");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:event")]
    [Rule("rr:response.2")]
    public void NormalActualAttackRecoversOnlyItsExactDiscardedCopy(bool accept)
    {
        // rr:event: "places the card in its ownerÃ¢â‚¬â„¢s discard pile after those
        // effects resolve". Ruling 7f69f006cddce07e places Morphogenetics here.
        Card? played = null;
        Card? duplicate = null;
        Card? payment = null;
        var (game, world) = Playing(board =>
        {
            var seat = board.Seats[0];
            World.MoveToTop(seat.IdentityCard, board.AreaOf(DeckType.RemovedArea, PlayArea.Of(0)));
            seat.IdentityCard = board.CreateCard("05001a,05001b", seat.Hero);
            played = board.CreateCard("05003", seat.Hand);
            duplicate = board.CreateCard("05003", board.AreaOf(DeckType.DiscardPile, PlayArea.Of(0), cardOwner: 0));
            payment = board.CreateCard("01088", seat.Hand);
        }, abilities: AuthoredCards.Runner());
        game.Resolve(PaidDecision.Take(Assert.Single(game.Pending!.Affordances,
            offer => offer.AnchorId == played!.ObjectId), [payment!.ObjectId]));
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        game.Resolve(Decision.Take(villain.ObjectId));
        Assert.Equal(4, villain.Damage);
        Assert.Equal(DeckType.DiscardPile, played!.Area.Type);
        var recovery = Assert.Single(game.Pending!.Affordances);
        Assert.Equal(world.Seats[0].IdentityCard.ObjectId, recovery.AnchorId);
        game.Resolve(accept ? Decision.Take(recovery.Id) : Decision.Decline);
        Assert.Equal(accept ? DeckType.HandsArea : DeckType.DiscardPile, played.Area.Type);
        Assert.Equal(DeckType.DiscardPile, duplicate!.Area.Type);
        Assert.Equal(!accept, world.Seats[0].IdentityCard.Ready);
        Assert.True(game.IsRootPrompt);
    }
}
