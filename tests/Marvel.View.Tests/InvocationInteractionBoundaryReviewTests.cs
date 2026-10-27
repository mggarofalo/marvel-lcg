using System.Text.Json;
using Marvel.Rules.State;
using Xunit;

namespace Marvel.View.Tests;

/// <summary>Independent prepared-engine visibility and typed-condition controls; no public admission.</summary>
public sealed class InvocationInteractionBoundaryReviewTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void HiddenInvocationOrderCannotChangeAuthorizedExplanation(int viewer)
    {
        var (world, game, deck, _) = InvocationInteractionFixture.Create("09032");
        var scope = new RestrictedVisibilityPolicy(viewer).Authorize(null, 2);
        var before = WorldProjection.For(world, game.Pending, [], scope);
        string prompt = JsonSerializer.Serialize(before.Prompt);
        var hidden = deck.Deck.Cards.First(card => !card.FaceUp);
        // Synthetic perturbation of concealed order, preserving the actual public top.
        World.MoveToTop(hidden, deck.Deck);
        World.MoveToTop(world.Cards.Single(card => card.FaceId == "09032"), deck.Deck);
        deck.PublicTop!.TurnFaceUp();
        var after = WorldProjection.For(world, game.Pending, [], scope);

        Assert.Equal(prompt, JsonSerializer.Serialize(after.Prompt));
        var pile = Assert.Single(after.World.Areas, area => area.Id == deck.Deck.Id);
        var exposed = Assert.Single(pile.Cards, card => card.Face is not null);
        Assert.Equal(deck.PublicTop!.ObjectId, exposed.Id);
        Assert.All(pile.Cards.Where(card => card.Face is null), card => Assert.Null(card.Id));
        Assert.Equal(4, pile.Cards.Count(card => card.Face is null));
        Assert.Equal(viewer == 1, after.Prompt is not null);
    }

    [Fact]
    public void CapturedPromptAndBoardKeepTheOfferedTopAfterItsDeparture()
    {
        var (world, game, deck, master) = InvocationInteractionFixture.Create("09032");
        var visible = WorldProjection.For(world, game.Pending, [],
            new RestrictedVisibilityPolicy(1).Authorize(null, 2));
        string captured = JsonSerializer.Serialize(visible);
        var original = deck.PublicTop!;
        // Synthetic post-publication movement challenges live-state lookup by the consumer.
        deck.DiscardTop(original, "independent snapshot probe", []);
        Assert.NotSame(original, deck.PublicTop);

        Assert.Equal(captured, JsonSerializer.Serialize(visible));
        var presentation = PromptPresentation.From(visible.Prompt!, visible.World);
        var offer = Assert.Single(presentation.Affordances, item => item.AnchorId == master.ObjectId);
        Assert.Contains("Crimson Bands of Cyttorak", offer.Description);
        Assert.Contains("Pay 2 resources for Crimson Bands of Cyttorak's Special", offer.CostDescription);
        Assert.Equal(new[] { original.ObjectId }, offer.TargetRequest!.Legal);
        Assert.Contains("Later choices and interrupts", offer.Description);
    }

    [Fact]
    public void SyntheticFailureOnlyReturnDoesNotPromiseMastersSuccessfulReturn()
    {
        var (world, game, _, master) = InvocationInteractionFixture.Create("09036",
            syntheticReturnOtherwise: true);
        var (_, presentation) = InvocationInteractionFixture.Project(world, game);
        var offer = Assert.Single(presentation.Affordances, item => item.AnchorId == master.ObjectId);
        Assert.DoesNotContain("return that exact physical Invocation", offer.Description ?? string.Empty);
        var ordinary = Assert.Single(presentation.Affordances,
            item => item.AnchorId == world.Seats[1].IdentityCard.ObjectId && item.Verb == "Action");
        Assert.Contains("Winds of Watoomb", ordinary.Description);
        Assert.Contains("Winds of Watoomb's Special discards this Invocation", ordinary.Description);
    }
}
