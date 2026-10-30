using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Xunit;

namespace Marvel.Content.Tests.Play;

// Synthetic conditional quote; no printed Power ability is activated.
public sealed class ConditionalResourceCaptureReviewTests
{
    [Fact]
    public void SuspendedGeneratorRetainsAcceptedConditionalQuoteAndAllocation()
    {
        var runner = new AbilityRunner(AbilityCatalog.Parse("""
            {"cards":[
              {"card":"01030","abilities":[{
                "trigger":{"event":"WhenActionTriggered","timing":"Resource","subject":"game"},
                "cost":{"dealDamage":{"cards":"this","amount":2}},"effect":{"generate":"Y"}}]},
              {"card":"01092","abilities":[{
                "trigger":{"event":"WhenCardWouldBeDefeated","timing":"Interrupt","subject":"game"},
                "effect":{"draw":{"player":"you","count":1}}}]}
            ]}
            """));
        Card? generator = null;
        Card? hand = null;
        var (_, world) = Playing(board =>
        {
            generator = board.CreateCard("01030", board.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
            generator.TakeDamage(2);
            hand = board.CreateCard("01072", board.Seats[0].Hand);
            InPlay(board, "01092");
        }, hero: true, abilities: runner);
        var sources = new List<ResourceSource>
        {
            new(generator!.ObjectId, "Y"),
            new(hand!.ObjectId, "G") { ConditionalBonus = new("G", 1) },
        };
        var allocations = new List<ResourceAllocation>
        {
            new(generator.ObjectId, 0, "Y"), new(hand.ObjectId, 0, "GG"),
        };
        var events = new List<GameEvent>();
        var receipt = CardResourcePayments.Spend(world, runner, new CardResourcePaymentRequest
        {
            Hands = [world.Seats[0].Hand], Paying = [generator.ObjectId, hand.ObjectId],
            Cost = 3, Required = "", Itself = -1, Payer = 0,
            Price = new CostOption(0, "3", Sources: sources), Allocations = allocations,
        }, events);
        Assert.False(receipt.IsComplete);
        Assert.Throws<InvalidOperationException>(() => receipt.Generated);
        var asked = Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, runner, events));
        Assert.Equal(Question.Opportunity, asked.Asking);
        Assert.Equal(DeckType.HandsArea, hand.Area.Type);
        sources[1] = new(hand.ObjectId, "G") { ConditionalBonus = new("B", 0) };
        allocations.Clear();
        Sequence.Answer(world, world.Facts, runner, asked, Decision.Decline, events);
        Assert.Null(Sequence.Work(world, world.Facts, runner, events));
        Assert.True(receipt.IsComplete);
        Assert.Equal("YGG", receipt.Paid);
        Assert.Equal("YGG", receipt.Generated);
        Assert.Equal(DeckType.DiscardPile, hand.Area.Type);
        Assert.False(world.Agenda.IsBusy);
    }
}

