using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>Actual full-book Red Dagger row; ownership differs from control.</summary>
public sealed class MsMarvelRedDaggerTests
{
    [Theory]
    [InlineData("01088", "01089", true)]
    [InlineData("01088", "01088", false)]
    [InlineData("01055", "01055", true)]
    [InlineData("01090", "01089", true)]
    [Rule("rr:wild-resource.1")]
    public void DefeatControllerPaysDifferentTypesThenOwnerReceivesExactAlly(string first, string second, bool payable)
    {
        // Wild may be used as "energy, mental, physical, or wild".
        // Rulings 10293662ffefd5b2 / 2368dda1610740b3 distinguish controller from owner.
        var (world, ally, enemy, payment) = Board(first, second);
        BasicPowerResolution.Consequential(world, world.Facts, ally, true, "Attack", []);
        var prompt = Sequence.Work(world, world.Facts, world.Abilities, []);
        if (!payable)
        {
            Assert.Null(prompt);
            Assert.Equal(DeckType.DiscardPile, ally.Area.Type);
            Assert.Equal(0, enemy.Damage);
            Assert.All(payment, p => Assert.Same(world.Seats[1].Hand, p.Area));
            return;
        }
        Assert.NotNull(prompt);
        Assert.Equal(1, prompt.Player);
        var offer = Assert.Single(prompt.Affordances);
        int[] ids = [.. payment.Select(p => p.ObjectId)];
        var price = Assert.Single(offer.Costs!);
        Assert.True(Assert.Single(price.ResourceCosts).DistinctTypes);
        var allocations = ResourcePayment.Allocate(price, ids)!;
        Sequence.Answer(world, world.Facts, world.Abilities, prompt,
            new Decision(offer.Id, [enemy.ObjectId], ids, Allocations: allocations), []);
        Assert.Null(Sequence.Work(world, world.Facts, world.Abilities, []));
        Assert.Equal(2, enemy.Damage);
        Assert.Same(world.Seats[0].Hand, ally.Area);
        Assert.Equal(0, ally.Damage);
        Assert.All(payment, p => Assert.Equal(DeckType.DiscardPile, p.Area.Type));
        Assert.False(world.Agenda.IsBusy);
    }

    [Fact]
    public void DecliningDefeatInterruptLeavesPaymentAndDefeatsAlly()
    {
        var (world, ally, enemy, payment) = Board("01088", "01089");
        BasicPowerResolution.Consequential(world, world.Facts, ally, true, "Attack", []);
        var prompt = Sequence.Work(world, world.Facts, world.Abilities, [])!;
        Assert.Equal(1, prompt.Player);
        Sequence.Answer(world, world.Facts, world.Abilities, prompt, Decision.Decline, []);
        Assert.Null(Sequence.Work(world, world.Facts, world.Abilities, []));
        Assert.Equal(DeckType.DiscardPile, ally.Area.Type);
        Assert.Equal(0, enemy.Damage);
        Assert.All(payment, p => Assert.Same(world.Seats[1].Hand, p.Area));
    }

    private static (World World, Card Ally, Card Enemy, Card[] Payment) Board(string first, string second)
    {
        var (_, world) = Playing(_ => { }, hero: true,
            heroes: ["spider_man", "captain_marvel"], abilities: AuthoredCards.Runner());
        foreach (var seat in world.Seats)
            foreach (var card in seat.Hand.Cards.ToArray()) Discard.Card(world, card, "test", []);
        var ally = world.CreateCard("05002", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(1), cardOwner: 0));
        ally.TakeDamage(2);
        var enemy = world.TheCardIn(DeckType.VillainArea)!;
        Card[] payment = [world.CreateCard(first, world.Seats[1].Hand), world.CreateCard(second, world.Seats[1].Hand)];
        return (world, ally, enemy, payment);
    }
}
