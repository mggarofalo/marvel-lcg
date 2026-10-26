using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class MeleeActualRowReviewTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void PrintedHeroActionRequiresHeroFormWithAffordableControl(bool hero)
    {
        // Printed 05030: "Hero Action (attack)" and cost 3. Helicarrier is
        // a same-cost support, proving payment and card-play availability.
        Card? source = null;
        Card? control = null;
        var (game, _) = Playing(board =>
        {
            source = board.CreateCard("05030", board.Seats[0].Hand);
            control = board.CreateCard("01092", board.Seats[0].Hand);
            board.CreateCard("01088", board.Seats[0].Hand);
            board.CreateCard("01089", board.Seats[0].Hand);
        }, hero: hero, abilities: AuthoredCards.Runner());
        Assert.Contains(game.Pending!.Affordances, option => option.AnchorId == control!.ObjectId && option.IsLegal);
        var offered = game.Pending.Affordances.Where(option => option.AnchorId == source!.ObjectId).ToArray();
        if (!hero) Assert.Empty(offered);
        else Assert.Equal("3", Assert.Single(Assert.Single(offered).Costs!).Cost);
    }

    [Rule("rr:stun-stunned.5")]
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SoleEnemyResolvesOnceOrStunCancelsEntirePaidAttack(bool stunned)
    {
        // "Costs associated with the attack attempt ... must still be paid."
        // Printed 05030's "another enemy" cannot be the sole enemy again.
        // Vendored Melee ruling 2737e9f131b881b5: "one attack with two separate targets".
        Card? source = null;
        Card? payment = null;
        Card? secondPayment = null;
        var (game, world) = Playing(board =>
        {
            source = board.CreateCard("05030", board.Seats[0].Hand);
            payment = board.CreateCard("01088", board.Seats[0].Hand);
            secondPayment = board.CreateCard("01089", board.Seats[0].Hand);
            if (stunned) Statuses.Give(board, board.Seats[0].IdentityCard, Statuses.Stunned);
        }, hero: true, abilities: AuthoredCards.Runner());
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        var offer = Assert.Single(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        Assert.Equal("3", Assert.Single(offer.Costs!).Cost);
        game.Resolve(PaidDecision.Take(offer, [payment!.ObjectId, secondPayment!.ObjectId]));
        game.Resolve(Decision.Take(villain.ObjectId));
        Assert.Equal(stunned ? 0 : 3, villain.Damage);
        Assert.False(Statuses.Has(world, world.Seats[0].IdentityCard, Statuses.Stunned));
        Assert.Equal(DeckType.DiscardPile, source!.Area.Type);
        Assert.Equal(DeckType.DiscardPile, payment.Area.Type);
        Assert.Equal(DeckType.DiscardPile, secondPayment.Area.Type);
        Assert.Equal(Question.TurnOption, game.Pending!.Asking);
        Assert.False(world.Agenda.IsBusy);
    }
}
