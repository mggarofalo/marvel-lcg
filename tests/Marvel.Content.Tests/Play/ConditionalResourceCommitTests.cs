using Marvel.Content;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

// Synthetic source quote; this does not activate any printed card ability.
public sealed class ConditionalResourceCommitTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    public void CommittedHandSourceUsesAcceptedAllocation(int cost)
    {
        var facts = CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));
        var world = new World(facts, 1, 73);
        var seat = world.CreateSeat("p0");
        seat.IdentityCard = world.CreateCard("01001b", seat.Hero);
        for (int i = 0; i < 8; i++) world.CreateCard("01003", seat.Deck);
        var source = world.CreateCard("01072", seat.Hand);
        var quote = new ResourceSource(source.ObjectId, "G")
        { ConditionalBonus = new("G", 1) };
        ResourceAllocation[] allocation = cost == 0 ? [] : [new(source.ObjectId, 0, "GG")];
        var receipt = CardResourcePayments.Spend(world, new NoCardAbilities(), new CardResourcePaymentRequest
        {
            Hands = [seat.Hand], Paying = [source.ObjectId], Cost = cost, Required = "",
            Itself = -1, Payer = 0, Price = new CostOption(0, cost.ToString(), Sources: [quote]),
            Allocations = allocation,
        }, []);
        Assert.True(receipt.IsComplete);
        Assert.Equal(cost == 0 ? "" : "GG", receipt.Paid);
        Assert.Equal(cost == 0 ? "G" : "GG", receipt.Generated);
        Assert.Equal(DeckType.DiscardPile, source.Area.Type);
    }

    [Fact]
    public void AmbiguousPositivePaymentWithoutAllocationFailsBeforeDiscard()
    {
        var facts = CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));
        var world = new World(facts, 1, 73);
        var seat = world.CreateSeat("p0");
        seat.IdentityCard = world.CreateCard("01001b", seat.Hero);
        for (int i = 0; i < 8; i++) world.CreateCard("01003", seat.Deck);
        var source = world.CreateCard("01072", seat.Hand);
        var quote = new ResourceSource(source.ObjectId, "G") { ConditionalBonus = new("G", 1) };
        var other = world.CreateCard("01088", seat.Hand);
        var request = new CardResourcePaymentRequest
        {
            Hands = [seat.Hand], Paying = [source.ObjectId, other.ObjectId], Cost = 1, Required = "",
            Itself = -1, Payer = 0, Price = new CostOption(0, "1", Sources: [quote, new(other.ObjectId, "YY")]),
        };
        Assert.Throws<RulesNotImplementedException>(() => CardResourcePayments.Spend(world, new NoCardAbilities(), request, []));
        Assert.Equal(DeckType.HandsArea, source.Area.Type);
    }
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ChangedOwnerQuoteIsRejectedBeforeHandContribution(bool initiallyConditional)
    {
        var facts = CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));
        var world = new World(facts, 1, 73);
        var seat = world.CreateSeat("p0");
        seat.IdentityCard = world.CreateCard("01001b", seat.Hero);
        var source = world.CreateCard("01072", seat.Hand);
        var owner = world.CreateCard("01069", seat.Hand);
        var abilities = new MutableHandQuote { Conditional = initiallyConditional };
        var accepted = abilities.QuoteHandResources(world, source, owner);
        abilities.Conditional = !initiallyConditional;
        var request = new CardResourcePaymentRequest
        {
            Hands = [seat.Hand], Paying = [source.ObjectId], Cost = 0, Required = "",
            Itself = owner.ObjectId, Payer = 0, PayingFor = owner,
            Price = new CostOption(owner.ObjectId, "0", Sources: [accepted]),
        };
        Assert.Throws<RulesNotImplementedException>(() => CardResourcePayments.Spend(world, abilities, request, []));
        Assert.Same(seat.Hand, source.Area);
    }

    // Synthetic owner quote models a live conditional fact changing after acceptance.
    private sealed class MutableHandQuote : NoCardAbilities, IResourceCardAbilities
    {
        public bool Conditional { get; set; }
        public override ResourceSource QuoteHandResources(World world, Card source, Card? owner, ulong qualifyingComponents = 1) =>
            new(source.ObjectId, "G")
            { ConditionalBonus = Conditional ? new("G", qualifyingComponents) : null };
    }

}
