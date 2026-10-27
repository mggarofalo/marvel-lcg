using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class HulkStarterPaymentReviewTests
{
    [Theory]
    [InlineData("RR", 1, true)]
    [InlineData("RY", 1, false)]
    [InlineData("GG", 1, false)]
    [InlineData("R", 2, true)]
    [InlineData("", 3, false)]
    [Rule("rr:wild-resource.1.1")]
    [Rule("rr:cost.4.1")]
    public void DropKickUsesDeclaredSpentIconsIncludingZeroPrice(string declaration, int reduction, bool bonus)
    {
        // Doubled wild resources "can be declared a different type"; excess resources
        // "were not paid for that cost". Paying no resources does not pay physical.
        var f = new HulkStarterFixture();
        var source = f.Held("10014");
        var power = f.Held("10017");
        CardPayment.ReduceNextCardCost(f.World, f.Hero, 1, reduction);
        var action = Assert.Single(f.Runner.Actions(f.World, 1), a => a.Card == source.ObjectId);
        int deck = f.World.Seats[1].Deck.Cards.Count;
        f.Events.AddRange(f.Runner.ExecuteAction(f.World, new PlayerAction(action,
            declaration.Length == 0 ? [] : [power.ObjectId], [f.Villain.ObjectId],
            Allocations: declaration.Length == 0 ? [] : [new(power.ObjectId, 0, declaration)])).Events);
        f.Finish();
        Assert.Equal(4, f.Villain.Damage);
        Assert.Equal(bonus, Statuses.Has(f.World, f.Villain, Statuses.Stunned));
        Assert.Equal(deck - (bonus ? 1 : 0), f.World.Seats[1].Deck.Cards.Count);
        Assert.Equal(declaration.Length == 0 ? DeckType.HandsArea : DeckType.DiscardPile, power.Area.Type);
        Assert.Equal(DeckType.DiscardPile, source.Area.Type);
    }

    [Fact]
    public void DropKickDrawsAfterItsChosenMinionIsDefeated()
    {
        // 10014 says "stun that enemy and draw 1 card", without a dependent "then".
        var f = new HulkStarterFixture();
        var minion = f.World.CreateCard("01121", f.World.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1)));
        int deck = f.World.Seats[1].Deck.Cards.Count;
        f.PlayEvent("10014", minion, f.Held("10022"), f.Held("10002"));
        f.Finish();
        Assert.Equal(DeckType.EncounterDiscardPile, minion.Area.Type);
        Assert.Equal(deck - 1, f.World.Seats[1].Deck.Cards.Count);
        Assert.False(Statuses.Has(f.World, minion, Statuses.Stunned));
        Assert.Equal(0, f.Villain.Damage);
    }

    [Fact]
    [Rule("rr:stun-stunned.5")]
    public void StunnedDropKickPaysAndCancelsItsDamageStunAndDraw()
    {
        // "discard the stunned card instead. Costs associated with the attack
        // attempt ... must still be paid."
        var f = new HulkStarterFixture();
        Statuses.Give(f.World, f.Hero, Statuses.Stunned);
        int deck = f.World.Seats[1].Deck.Cards.Count;
        var physical = f.Held("10022");
        var single = f.Held("10002");
        var source = f.PlayEvent("10014", f.Villain, physical, single);
        f.Finish();
        Assert.False(Statuses.Has(f.World, f.Hero, Statuses.Stunned));
        Assert.False(Statuses.Has(f.World, f.Villain, Statuses.Stunned));
        Assert.Equal(0, f.Villain.Damage);
        Assert.Equal(deck, f.World.Seats[1].Deck.Cards.Count);
        Assert.All(new[] { source, physical, single }, card => Assert.Equal(DeckType.DiscardPile, card.Area.Type));
    }
}
