using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class CaptainAmericaStrikeTests
{
    [Theory]
    [InlineData("physical", false)]
    [InlineData("wild-physical", false)]
    [InlineData("wild-mental", false)]
    [InlineData("mental", false)]
    [InlineData("physical", true)]
    [Rule("rr:wild-resource.1")]
    [Rule("rr:tough.2")]
    public void HeroicStrikeUsesDeclaredPaymentAndStunsDespitePreventedDamage(string payment, bool tough)
    {
        // Players "may specify which resource type (energy, mental, physical, or wild) it is being used as."
        // Tough: "prevent all of that damage and discard a tough status card from that character instead."
        // Printed 03004: six damage, then stun only when a physical resource paid for this card.
        Card? strike = null;
        Card? villain = null;
        Card[] paying = [];
        var (game, world) = CaptainAmericaFixture.PlayingCaptain(board =>
        {
            strike = board.CreateCard("03004", board.Seats[0].Hand);
            string[] faces = payment switch
            {
                "physical" => ["01090", "01093"],
                "mental" => ["01089", "01093"],
                _ => ["01011", "01089"],
            };
            paying = [.. faces.Select(face => board.CreateCard(face, board.Seats[0].Hand))];
            villain = board.TheCardIn(DeckType.VillainArea)!;
            if (tough) Statuses.Give(board, villain, Statuses.Tough);
        });
        var action = Assert.Single(game.Pending!.Affordances, option => option.AnchorId == strike!.ObjectId);
        int[] ids = [.. paying.Select(card => card.ObjectId)];
        IReadOnlyList<ResourceAllocation>? allocations = payment.StartsWith("wild-", StringComparison.Ordinal)
            ? [new(paying[0].ObjectId, 0, payment == "wild-physical" ? "R" : "B"),
                new(paying[1].ObjectId, 0, "BB")]
            : ResourcePayment.Allocate(action.CostOptions[0], ids);
        game.Resolve(new Decision(action.Id, [], ids, Allocations: allocations));
        var target = Assert.Single(game.Pending!.Affordances, option => option.AnchorId == villain!.ObjectId);
        game.Resolve(Decision.Take(target.Id));
        while (game.Pending is { Asking: Question.Opportunity })
            game.Resolve(Decision.Decline);
        Assert.Equal(Question.TurnOption, game.Pending!.Asking);
        Assert.Equal(tough ? 0 : 6, villain!.Damage);
        Assert.False(Statuses.Has(world, villain, Statuses.Tough));
        Assert.Equal(payment is "physical" or "wild-physical", Statuses.Has(world, villain, Statuses.Stunned));
        Assert.Equal(DeckType.DiscardPile, strike!.Area.Type);
        Assert.All(paying, card => Assert.Equal(DeckType.DiscardPile, card.Area.Type));
    }
}
