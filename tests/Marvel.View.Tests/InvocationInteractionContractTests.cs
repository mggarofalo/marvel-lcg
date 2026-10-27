using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Xunit;

namespace Marvel.View.Tests;

/// <summary>Concrete prepared engine prompts, distinct from native or public-pack acceptance.</summary>
public sealed class InvocationInteractionContractTests
{
    [Theory]
    [InlineData("09032", "Crimson Bands of Cyttorak", "2", "3")]
    [InlineData("09033", "Images of Ikonn", "1", "2")]
    [InlineData("09034", "Seven Rings of Raggadorr", "1", "2")]
    [InlineData("09035", "Vapors of Valtorr", "0", "1")]
    [InlineData("09036", "Winds of Watoomb", "0", "1")]
    public void CurrentTopCostsAndDistinctConsequencesSurviveAuthorizedProjection(
        string face, string title, string specialCost, string combinedCost)
    {
        var (world, game, deck, master) = InvocationInteractionFixture.Create(face);
        string before = world.Digest().Canonical();
        string random = System.Text.Json.JsonSerializer.Serialize(world.Random.Generator.GetState());
        var pending = game.Pending;
        var (prompt, presentation) = InvocationInteractionFixture.Project(world, game);
        Assert.Equal(1, prompt.Player);
        Assert.Contains("p1", presentation.Context);
        var spell = Assert.Single(presentation.Affordances, offer => offer.AnchorId == world.Seats[1].IdentityCard.ObjectId
            && offer.Verb == Game.ActionVerb);
        var paid = Assert.Single(presentation.Affordances, offer => offer.AnchorId == master.ObjectId);
        foreach (var offer in new[] { spell, paid })
        {
            Assert.Equal(1, offer.AnchorPlayer);
            Assert.Equal(new[] { deck.PublicTop!.ObjectId }, offer.TargetRequest!.Legal);
            Assert.Equal(1, offer.TargetRequest.Min);
            Assert.Equal(1, offer.TargetRequest.Max);
            Assert.Contains($"Pay {specialCost} resources for {title}'s Special", offer.CostDescription);
            Assert.DoesNotContain("resolve", offer.CostDescription!, StringComparison.OrdinalIgnoreCase);
            Assert.Contains(title, offer.Description);
            Assert.Contains($"{title}'s Special discards this Invocation and reveals the next Invocation", offer.Description);
            Assert.Contains("Later choices and interrupts", offer.Description);
        }
        Assert.Contains("Exhaust Doctor Strange", spell.CostDescription);
        Assert.DoesNotContain("Exhaust", paid.CostDescription!);
        Assert.False(spell.PlaysCard);
        Assert.True(paid.PlaysCard);
        Assert.Equal(specialCost, Assert.Single(spell.CostOptions).Cost);
        Assert.Equal(combinedCost, Assert.Single(paid.CostOptions).Cost);
        Assert.DoesNotContain("return", spell.Description!, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("return that exact physical Invocation faceup", paid.Description);
        var other = WorldProjection.For(world, game.Pending, [], new RestrictedVisibilityPolicy(0).Authorize(null, 2));
        Assert.Null(other.Prompt);
        Assert.Equal(before, world.Digest().Canonical());
        Assert.Equal(random, System.Text.Json.JsonSerializer.Serialize(world.Random.Generator.GetState()));
        Assert.Same(pending, game.Pending);
    }

    [Fact]
    public void SyntheticSpecialWithoutDiscardDoesNotAdvertiseDiscard()
    {
        var (world, game, _, master) = InvocationInteractionFixture.Create("09036", syntheticNoDiscard: true);
        var (_, presentation) = InvocationInteractionFixture.Project(world, game);
        var offer = Assert.Single(presentation.Affordances, item => item.AnchorId == master.ObjectId);
        Assert.Contains("Winds of Watoomb", offer.Description);
        Assert.DoesNotContain("discard", offer.Description!, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("return that exact physical Invocation", offer.Description);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SevenRingsRetainsOneCompleteSubsetAndItsIndependentCommitment(bool master)
    {
        var (world, game, deck, eventCard) = InvocationInteractionFixture.Create("09034");
        var source = master ? eventCard : world.Seats[1].IdentityCard;
        var offer = Assert.Single(game.Pending!.Affordances, item => item.AnchorId == source.ObjectId
            && item.Verb == Game.ActionVerb);
        game.Resolve(InvocationInteractionFixture.Pay(world, offer, deck.PublicTop!));
        var (prompt, presentation) = InvocationInteractionFixture.Project(world, game);
        Assert.Equal(1, prompt.Player);
        Assert.False(prompt.Cancellable);
        Assert.Contains("Seven Rings", Assert.Single(presentation.ContextCards).Title);
        Assert.Contains("none selected", presentation.Resolution);
        var group = Assert.Single(presentation.Affordances).TargetRequest!;
        Assert.Equal(0, group.Min);
        Assert.Equal(3, group.Max);
        Assert.False(group.AllowRepeated);
        Assert.True(group.Allows([]));
        Assert.False(group.Allows([group.Legal[0], group.Legal[0]]));
        Assert.Contains(world.TheCardIn(DeckType.VillainArea)!.ObjectId, group.Legal);
        Assert.All(group.Details!.Values, detail => Assert.Contains("tough", detail));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void VaporsKeepsStatusAndReplacementAsSeparateAuthorizedPrompts(bool master)
    {
        var (world, game, deck, eventCard) = InvocationInteractionFixture.Create("09035");
        var old = Statuses.Give(world, world.Seats[0].IdentityCard, Statuses.Stunned);
        var source = master ? eventCard : world.Seats[1].IdentityCard;
        var offer = Assert.Single(game.Pending!.Affordances, item => item.AnchorId == source.ObjectId
            && item.Verb == Game.ActionVerb);
        game.Resolve(InvocationInteractionFixture.Pay(world, offer, deck.PublicTop!));
        var (prompt, presentation) = InvocationInteractionFixture.Project(world, game);
        Assert.Equal(1, prompt.Player);
        Assert.False(prompt.Cancellable);
        Assert.Contains("Vapors", Assert.Single(presentation.ContextCards).Title);
        Assert.Contains(presentation.Affordances, item => item.AnchorId == old.ObjectId);
        game.Resolve(Decision.Take(Assert.Single(prompt.Affordances, item => item.AnchorId == old.ObjectId).Id));
        (prompt, presentation) = InvocationInteractionFixture.Project(world, game);
        Assert.Equal(1, prompt.Player);
        Assert.False(prompt.Cancellable);
        Assert.Collection(presentation.Affordances,
            item => Assert.Equal("Replace with tough", item.Description),
            item => Assert.Equal("Replace with confused", item.Description));
        Assert.Contains("Vapors", Assert.Single(presentation.ContextCards).Title);
    }
}
