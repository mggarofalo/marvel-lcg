using Marvel.Content.Setup;
using Marvel.Rules.Events;
using Marvel.Rules.Setup;
using Marvel.Rules.State;
using Xunit;
using static Marvel.Content.Tests.Setup.DoctorStrangeSetupFixture;

namespace Marvel.Content.Tests.Setup;

public sealed class DoctorStrangeSetupIndependentReviewTests
{
    [Fact]
    public void RecipeSnapshotsCallerStorageAndPreservesAuthoredOrder()
    {
        // Engine input contract: authored ordering controls physical object allocation.
        string[] supplied = [.. Invocations.AsEnumerable().Reverse()];
        var recipe = new InvocationDeckRecipe(supplied);
        supplied[0] = "01001a";
        Assert.Equal(Invocations.AsEnumerable().Reverse(), recipe.Cards);
        Assert.False(recipe.Cards is string[]);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void InvocationBlockBeforeAnotherSeatPreservesPhysicalBinding(int owner)
    {
        // mc08: the separate deck is placed "next to your identity card".
        string[] heroes = owner == 0
            ? ["doctor_strange", "spider_man", "she_hulk"]
            : ["spider_man", "doctor_strange", "she_hulk"];
        var setup = SetupCatalog.Parse(Root().ToJsonString());
        var blueprints = Marvel.Rules.Setup.Blueprints.From(
            Dealer.DealOrder(setup, "rhino", heroes, facts: PrintedCards), PrintedCards);
        var world = WorldSetup.DealWithoutCardAbilities(PrintedCards, blueprints, heroes, 9381);
        var invocation = Assert.IsType<Marvel.Rules.Play.InvocationDeck>(world.Seats[owner].InvocationDeck);
        var expectedIds = blueprints.Select((card, index) => (card, index))
            .Where(item => item.card.Slot == SetupSlot.InvocationDeck).Select(item => item.index).ToArray();
        Assert.Equal(Invocations, invocation.Deck.Cards.Select(card => card.FaceId).Order(StringComparer.Ordinal));
        Assert.Equal(expectedIds, invocation.Deck.Cards.Select(card => card.ObjectId).Order());
        Assert.Equal(SetupSlot.Identity, blueprints[expectedIds[^1] + 1].Slot);
        Assert.All(invocation.Deck.Cards, card => Assert.Equal(owner, card.Owner));
        Assert.All(world.Seats.Where(seat => seat.Index != owner), seat => Assert.Null(seat.InvocationDeck));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(2)]
    public void CompleteDeckWithInvalidOwnerRejectsBeforeSetupEvents(int owner)
    {
        var blueprints = DoctorStrangeSetupFixture.Blueprints(2)
            .Select(card => card.Slot == SetupSlot.InvocationDeck ? card with { Seat = owner } : card).ToArray();
        var events = new List<GameEvent>();
        Assert.Throws<ArgumentException>(() => WorldSetup.DealWithoutCardAbilities(
            PrintedCards, blueprints, Heroes(2), 9381, events));
        Assert.Empty(events);
    }

    [Theory]
    [InlineData("09001a", false)]
    [InlineData("09001b", false)]
    [InlineData("09001a", true)]
    [InlineData("09001b", true)]
    public void PartialIdentityCannotBypassInvocationComposition(string identityFace, bool includeDeck)
    {
        // mc08: Doctor Strange "begins each game with a special, five-card Invocation deck".
        var blueprints = DoctorStrangeSetupFixture.Blueprints(1)
            .Where(card => includeDeck || card.Slot != SetupSlot.InvocationDeck)
            .Select(card => card.Slot == SetupSlot.Identity ? card with { Spec = identityFace } : card).ToArray();
        var events = new List<GameEvent>();
        Assert.Throws<ArgumentException>(() => WorldSetup.DealWithoutCardAbilities(
            PrintedCards, blueprints, ["Strange"], 9381, events));
        Assert.Empty(events);
    }
}
