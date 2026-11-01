using System.Text.Json;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Xunit;

namespace Marvel.View.Tests;

/// <summary>Prepared nonpublic continuation meaning, separate from public pack admission.</summary>
public sealed class InvocationContinuationMeaningTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void PaidOriginAndIndependentConsequencesRemainThroughRecipientChoice(bool master)
    {
        var (world, game, deck, eventCard) = InvocationInteractionFixture.Create("09034");
        var top = deck.PublicTop!;
        var source = master ? eventCard : world.Seats[1].IdentityCard;
        var action = Assert.Single(game.Pending!.Affordances, item => item.AnchorId == source.ObjectId && item.Verb == Game.ActionVerb);
        game.Resolve(InvocationInteractionFixture.Pay(world, action, top));
        string digest = world.Digest().Canonical();
        string random = JsonSerializer.Serialize(world.Random.Generator.GetState());
        var current = world.Agenda.Current!.Value;
        var (prompt, presentation) = InvocationInteractionFixture.Project(world, game);
        Assert.Contains(master ? "Master of the Mystic Arts" : "Spell Mastery", presentation.Frame.Instruction);
        Assert.Contains("Already paid: 1 resource for Seven Rings of Raggadorr's Special", presentation.Frame.Instruction);
        Assert.Contains("Seven Rings of Raggadorr's Special discards this Invocation and reveals the next Invocation, even if its other effects do not apply.", presentation.Frame.Instruction);
        Assert.Contains("Closing this choice does not refund these costs", presentation.Frame.Instruction);
        Assert.Contains("none selected", presentation.Frame.Instruction);
        Assert.Null(Assert.Single(presentation.Affordances).Description);
        Assert.Contains("Give a tough status card", Assert.Single(presentation.Affordances).TargetRequest!.Details!.Values);
        Assert.DoesNotContain("Its instructions", presentation.Frame.Instruction);
        Assert.Equal(master, presentation.Frame.Instruction.Contains("return that exact physical Invocation faceup", StringComparison.Ordinal));
        Assert.Equal(!master, presentation.Frame.Instruction.Contains("was exhausted for this cost", StringComparison.Ordinal));
        Assert.Equal(master, presentation.Frame.Instruction.Contains("event cost is also paid", StringComparison.Ordinal));
        Assert.Null(WorldProjection.For(world, game.Pending, [], new RestrictedVisibilityPolicy(0).Authorize(null, 2)).Prompt);
        Assert.Equal(digest, world.Digest().Canonical());
        Assert.Equal(random, JsonSerializer.Serialize(world.Random.Generator.GetState()));
        Assert.Equal(current, world.Agenda.Current);
        Assert.Equal(1, prompt.Player);
        Assert.False(prompt.Cancellable);
        var choice = Assert.Single(game.Pending!.Affordances);
        game.Resolve(Decision.Take(choice.Id));
        Assert.Throws<InvalidOperationException>(() => EnclosingAbilityContinuations.For(world.Agenda, current));
        Assert.DoesNotContain("Already paid", InvocationInteractionFixture.Project(world, game).Presentation.Frame.Instruction);
        Assert.Equal(master, ReferenceEquals(top, deck.PublicTop));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void BothVaporsChoicesRetainTheSameAcceptedParent(bool master)
    {
        var (world, game, deck, eventCard) = InvocationInteractionFixture.Create("09035");
        var status = Statuses.Give(world, world.Seats[0].IdentityCard, Statuses.Stunned);
        var source = master ? eventCard : world.Seats[1].IdentityCard;
        var action = Assert.Single(game.Pending!.Affordances, item => item.AnchorId == source.ObjectId && item.Verb == Game.ActionVerb);
        game.Resolve(InvocationInteractionFixture.Pay(world, action, deck.PublicTop!));
        var firstPrompt = InvocationInteractionFixture.Project(world, game).Presentation;
        string first = firstPrompt.Frame.Instruction;
        Assert.All(firstPrompt.Affordances, choice => Assert.False(string.IsNullOrWhiteSpace(choice.Description)));
        game.Resolve(Decision.Take(Assert.Single(game.Pending!.Affordances, item => item.AnchorId == status.ObjectId).Id));
        var secondPrompt = InvocationInteractionFixture.Project(world, game).Presentation;
        string second = secondPrompt.Frame.Instruction;
        Assert.Collection(secondPrompt.Affordances,
            choice => Assert.Equal("Replace with tough", choice.Description),
            choice => Assert.Equal("Replace with confused", choice.Description));
        foreach (string text in new[] { first, second })
        {
            Assert.Contains(master ? "Master of the Mystic Arts" : "Spell Mastery", text);
            Assert.Contains("Already paid: 0 resources for Vapors of Valtorr's Special", text);
            Assert.Equal(master, text.Contains("return that exact physical Invocation faceup", StringComparison.Ordinal));
        }
    }

    [Fact]
    public void SyntheticNonDiscardingSpecialDoesNotAcquireADiscardPromise()
    {
        var (world, game, deck, eventCard) = InvocationInteractionFixture.Create("09034", syntheticNoDiscard: true);
        var action = Assert.Single(game.Pending!.Affordances, item => item.AnchorId == eventCard.ObjectId && item.Verb == Game.ActionVerb);
        game.Resolve(InvocationInteractionFixture.Pay(world, action, deck.PublicTop!));
        string text = InvocationInteractionFixture.Project(world, game).Presentation.Frame.Instruction;
        Assert.Contains("return that exact physical Invocation faceup", text);
        Assert.DoesNotContain("discards this Invocation", text);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void ConcealedOrderCannotAlterNestedMeaningOrPublishedSnapshot(int viewer)
    {
        var (world, game, deck, eventCard) = InvocationInteractionFixture.Create("09034");
        var action = Assert.Single(game.Pending!.Affordances, item => item.AnchorId == eventCard.ObjectId && item.Verb == Game.ActionVerb);
        game.Resolve(InvocationInteractionFixture.Pay(world, action, deck.PublicTop!));
        var scope = new RestrictedVisibilityPolicy(viewer).Authorize(null, 2);
        var before = WorldProjection.For(world, game.Pending, [], scope);
        string saved = JsonSerializer.Serialize(before.Prompt);
        var top = deck.PublicTop!;
        World.MoveToTop(deck.Deck.Cards.First(card => !card.FaceUp), deck.Deck);
        World.MoveToTop(top, deck.Deck);
        top.TurnFaceUp();
        var after = WorldProjection.For(world, game.Pending, [], scope);
        Assert.Equal(saved, JsonSerializer.Serialize(after.Prompt));
        if (viewer == 1) Assert.Contains("Already paid", after.Prompt!.Description);
        else Assert.Null(after.Prompt);
        deck.DiscardTop(top, "synthetic snapshot lifetime probe", []);
        Assert.Equal(saved, JsonSerializer.Serialize(before.Prompt));
        Assert.All(after.World.Areas.Single(area => area.Id == deck.Deck.Id).Cards.Where(card => card.Face is null), card => Assert.Null(card.Id));
    }
}
