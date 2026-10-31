using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;
using static Marvel.Content.Tests.Play.KangObligationFixture;

namespace Marvel.Content.Tests.Play;

// Each engine site that reads Stolen Memories' filtered hand-discard cost,
// held to the printed mental filter independently of the others.
public sealed class KangFilteredHandDiscardTests : CoreCardTestBase
{
    private const string StolenMemories = "11019";

    [Fact]
    [Rule("rr:cost")]
    public void PayabilityCountsOnlyMatchingHandCards()
    {
        var (world, f, memories, ability) = Board(out var program);

        Assert.False(AbilityPaymentRules.Payable(world, memories, 1, ability.Cost, program, f.Runner));
        var genius = InHand(world, "01089", 1);
        Assert.True(AbilityPaymentRules.Payable(world, memories, 1, ability.Cost, program, f.Runner));
        Assert.NotNull(genius);
    }

    [Fact]
    [Rule("rr:cost")]
    public void AdmissionFindsNoReceiptWithoutAMatchingHandCard()
    {
        var (world, f, memories, ability) = Board(out var program);
        var queries = new AbilityOfferQueries(program, f.Runner);

        Assert.Null(queries.ActionAdmission(world, memories, ability, 1, occurrence: null));
        InHand(world, "01089", 1);
        Assert.NotNull(queries.ActionAdmission(world, memories, ability, 1, occurrence: null));
    }

    [Fact]
    [Rule("rr:initiating-abilities")]
    public void TheCardRequestOffersOnlyMatchingHandCards()
    {
        // The prompt asks the payer to choose among printed mental cards only.
        var (world, _, memories, ability) = Board(out _);
        var genius = InHand(world, "01089", 1);

        var request = AbilityCostSelection.Ask(world, memories, 1, ability.Cost)!;

        Assert.Equal([genius.ObjectId], request.Legal);
        Assert.Equal(1, request.Min);
        Assert.Equal(1, request.Max);
    }

    [Fact]
    [Rule("rr:initiating-abilities")]
    public void AForgedNonMatchingSelectionIsRefusedBeforePayment()
    {
        // The offer is legal because a Genius is in hand; naming the Energy card
        // instead is refused, and nothing moves.
        var (world, f, memories, _) = Board(out _);
        InHand(world, "01089", 1);
        var energy = world.Seats[1].Hand.Cards.Single(card => card.FaceId == "01088");
        var action = Assert.Single(f.Runner.Actions(world, 1), offer => offer.Card == memories.ObjectId);

        Assert.Throws<RulesNotImplementedException>(() =>
            f.Runner.ExecuteAction(world, new PlayerAction(action, [], [energy.ObjectId])));
        Assert.Equal(DeckType.HandsArea, energy.Area.Type);
        Assert.Equal(DeckType.ObligationsArea, memories.Area.Type);
    }

    [Fact]
    [Rule("rr:initiating-abilities")]
    public void PaymentValidationRefusesANonMatchingCard()
    {
        var (world, f, memories, ability) = Board(out var program);
        var genius = InHand(world, "01089", 1);
        var energy = world.Seats[1].Hand.Cards.Single(card => card.FaceId == "01088");
        var validation = new AbilityPaymentValidation(world, memories, 1, program, f.Runner);

        validation.Validate(ability.Cost, [], [genius.ObjectId], null);
        Assert.Throws<RulesNotImplementedException>(() =>
            validation.Validate(ability.Cost, [], [energy.ObjectId], null));
    }

    private static (World World, RedSkullFixture F, Card Memories, CompiledCardAbility Ability) Board(
        out AbilityProgram program)
    {
        var (world, f) = KangTemporalFixture.Board(players: 2);
        var memories = Obligation(world, StolenMemories, 1);
        AlterEgo(world, 1);
        InHand(world, "01088", 1);
        program = AbilityLowering.Book(Marvel.Content.Tests.Cards.AuthoredCards.Book);
        var ability = Assert.Single(program.On(StolenMemories), candidate => candidate.Cost is not null);
        return (world, f, memories, ability);
    }
}
