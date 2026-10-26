using System.Text.Json;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class ControlledCharacterPlayRestrictionReviewTests : CoreCardTestBase
{
    [Fact]
    public void CatalogRestrictionInventoryExcludesIncidentalCharacterReferences()
    {
        Assert.Equal(["08018", "08033"], Cards.Faces
            .Where(face => Cards.RequiredControlledCharacterTrait(face) is not null).ToArray());
        Assert.Contains("if you control a PSIONIC character", Cards.Text("34022"));
        Assert.Null(Cards.RequiredControlledCharacterTrait("34022"));
    }

    [Theory]
    [InlineData("Play only if you control a Spy character.", "SPY")]
    [InlineData("Preparation.\nPlay only if you control a Spy character.\nInterrupt: Draw 1 card.", "SPY")]
    [InlineData("Play only if you control a Spy support.", null)]
    [InlineData("Action: Play only if you control a Spy character.", null)]
    [InlineData("Draw 1 card if you control a Spy character.", null)]
    [InlineData("Play only if your identity has the Spy trait.", null)]
    public void ParserRecognizesACompleteRestrictionRatherThanMentionedTraits(string text, string? expected)
    {
        // Synthetic catalog rows distinguish the parser grammar without admitting content.
        var catalog = CardCatalog.Parse(JsonSerializer.Serialize(new
        {
            cards = new[] { new { card_id = "synthetic", text_plain = text } },
        }));
        Assert.Equal(expected, catalog.RequiredControlledCharacterTrait("synthetic"));
    }

    [Theory]
    [InlineData("08018")]
    [InlineData("08033")]
    [Rule("rr:ownership-and-control.5")]
    [Rule("rr:ownership-and-control.8")]
    public void TransferChangesEligibilityForEachActingSeatWithoutChangingOwnership(string face)
    {
        // Clause 5: a character that changes control "is moved to its new controller's play area."
        // Clause 8: "only refers to cards in play currently under that player's control."
        var world = Hero("01001b", 2);
        var first = world.Seats[0];
        var second = world.Seats[1];
        var spy = world.CreateCard("08002", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        var firstCard = world.CreateCard(face, first.Hand);
        var secondCard = world.CreateCard(face, second.Hand);
        var firstControl = world.CreateCard("01006", first.Hand);
        var secondControl = world.CreateCard("01006", second.Hand);
        world.CreateCard("01093", first.Hand);
        world.CreateCard("01093", second.Hand);
        Assert.NotNull(CardPayment.Price(world, Cards, first, firstControl));
        Assert.NotNull(CardPayment.Price(world, Cards, second, secondControl));
        Assert.NotNull(CardPayment.Price(world, Cards, first, firstCard));
        Assert.Null(CardPayment.Price(world, Cards, second, secondCard));

        CardPlay.TakeControl(world, Cards, spy, 1);

        Assert.Equal(0, spy.Owner);
        Assert.Equal(1, CardControl.ControllerOf(world, spy));
        Assert.Null(CardPayment.Price(world, Cards, first, firstCard));
        Assert.NotNull(CardPayment.Price(world, Cards, second, secondCard));

        CardPlay.ReturnToOwnerControl(world, Cards, spy);

        Assert.Equal(0, spy.Owner);
        Assert.NotNull(CardPayment.Price(world, Cards, first, firstCard));
        Assert.Null(CardPayment.Price(world, Cards, second, secondCard));
    }
}
