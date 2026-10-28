using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Xunit;

namespace Marvel.View.Tests;

public sealed class AffordanceContextTests
{
    [Fact]
    public void ContextIsCopiedFilteredAndUnavailableToOtherAnsweringScopes()
    {
        World world = VisibilityFixture.Board();
        int publicId = world.TheCardIn(DeckType.VillainArea)!.ObjectId;
        int secret = world.Seats[1].Hand.Cards[0].ObjectId;
        var ids = new[] { publicId, secret };
        var offer = new Affordance(7, "Interrupt", publicId, 0, "Observe")
        { ContextCardIds = ids, DisplayLabel = "Observe secret", CommitLabel = "Commit secret",
            Description = "Heal up to 3 damage; the amount depends on damage present." };
        var prompt = new Prompt(0, Question.Opportunity, TimingPriority.Interrupt, "Synthetic", "Observe", true, [offer]);
        ViewScope own = new RestrictedVisibilityPolicy(0).Authorize(null, 2);
        var visible = WorldProjection.For(world, prompt, [], own);
        Affordance copied = Assert.Single(visible.Prompt!.Affordances);
        Assert.Equal([publicId], copied.ContextCardIds);
        Assert.Null(copied.DisplayLabel);
        Assert.Null(copied.CommitLabel);
        Assert.Equal(offer.Description, copied.Description);
        ids[0] = -999;
        Assert.Equal([publicId], copied.ContextCardIds);
        ((IList<int>)copied.ContextCardIds)[0] = -1;
        Assert.Equal(secret, offer.ContextCardIds[1]);
        Assert.Null(WorldProjection.For(world, prompt, [], ViewScope.None).Prompt);
        ViewScope other = new RestrictedVisibilityPolicy(1).Authorize(null, 2);
        Assert.Null(WorldProjection.For(world, prompt, [], other).Prompt);
    }

    [Fact]
    public void HistoricalAttackLabelSurvivesWithoutAssociatingAHiddenCurrentCopy()
    {
        World world = VisibilityFixture.Board();
        int source = world.TheCardIn(DeckType.VillainArea)!.ObjectId;
        int hidden = world.Seats[1].Hand.Cards[0].ObjectId;
        var offers = new[]
        {
            new Affordance(10, "Response", source, 0, "Respond")
            { ContextCardIds = [hidden], DisplayLabel = "Respond after attacking a secret" },
            new Affordance(11, "Response", source, 0, "Respond")
            { DisplayLabel = "Respond after attacking the public original",
                Description = "Heal up to 3 damage from your identity." },
        };
        var prompt = new Prompt(0, Question.Opportunity, TimingPriority.Response, "Synthetic", "Respond", true, offers);
        var visible = WorldProjection.For(world, prompt, [], new RestrictedVisibilityPolicy(0).Authorize(null, 2));
        var projected = visible.Prompt!.Affordances;
        Assert.Empty(projected[0].ContextCardIds);
        Assert.Null(projected[0].DisplayLabel);
        Assert.Empty(projected[1].ContextCardIds);
        Assert.Equal("Respond after attacking the public original", projected[1].DisplayLabel);
        Assert.Equal(offers[1].Description, projected[1].Description);
    }

    [Fact]
    public void PresentationNamesSameTitleCopiesWithoutChangingTheirSourceOrTargets()
    {
        World world = VisibilityFixture.Board();
        Card first = world.TheCardIn(DeckType.VillainArea)!;
        Card second = world.CreateCard(first.FaceId, first.Area);
        var offers = new[] { first, second }.Select((card, index) =>
            new Affordance(index + 30, "Interrupt", first.ObjectId, 0, "Observe")
            { ContextCardIds = [card.ObjectId] }).ToArray();
        var prompt = new Prompt(0, Question.Opportunity, TimingPriority.Interrupt, "Synthetic", "Observe", true, offers);
        var visible = WorldProjection.For(world, prompt, [], new RestrictedVisibilityPolicy(0).Authorize(null, 2));
        var presented = PromptPresentation.From(visible.Prompt!, visible.World).Affordances;
        Assert.Equal(2, presented.Select(offer => offer.ContextDescription).Distinct().Count());
        Assert.Contains("copy 1 of 2", presented[0].ContextDescription);
        Assert.Contains("copy 2 of 2", presented[1].ContextDescription);
        Assert.All(presented, offer =>
        {
            Assert.Equal(first.ObjectId, offer.AnchorId);
            Assert.Null(offer.TargetRequest);
            Assert.Equal("Observe", offer.Label);
            Assert.DoesNotContain("Object ", offer.ContextDescription);
        });
    }
}
