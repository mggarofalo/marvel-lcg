using System.Text.Json;
using Marvel.Cards.Run;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Xunit;

namespace Marvel.View.Tests;

/// <summary>Independent controls for the live nested query, separate from public admission.</summary>
public sealed class InvocationContinuationReviewTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RepeatedNestedQueriesPreserveExecutionAndTheNextAcceptedOutcome(bool master)
    {
        AbilityRunner runner = null!;
        var probe = InvocationInteractionFixture.Create("09034", captureRunner: value => runner = value);
        var control = InvocationInteractionFixture.Create("09034");
        foreach (var scene in new[] { probe, control })
        {
            int source = master ? scene.Master.ObjectId : scene.World.Seats[1].IdentityCard.ObjectId;
            var action = Assert.Single(scene.Game.Pending!.Affordances,
                item => item.AnchorId == source && item.Verb == Game.ActionVerb);
            scene.Game.Resolve(InvocationInteractionFixture.Pay(scene.World, action, scene.Deck.PublicTop!));
        }
        string digest = probe.World.Digest().Canonical();
        string random = JsonSerializer.Serialize(probe.World.Random.Generator.GetState());
        var pending = probe.World.Agenda.Outstanding.ToArray();
        var current = probe.World.Agenda.Current!.Value;
        var lineage = EnclosingAbilityContinuations.For(probe.World.Agenda, current).ToArray();
        for (int count = 0; count < 3; count++)
        {
            var prompt = runner.Choosing(probe.World, current)!;
            Assert.Contains(master ? "Master of the Mystic Arts" : "Spell Mastery", prompt.Description);
            Assert.Contains("Already paid: 1 resource", prompt.Description);
            Assert.Equal(lineage, EnclosingAbilityContinuations.For(probe.World.Agenda, current));
        }
        Assert.Equal(digest, probe.World.Digest().Canonical());
        Assert.Equal(random, JsonSerializer.Serialize(probe.World.Random.Generator.GetState()));
        Assert.Equal(pending, probe.World.Agenda.Outstanding);
        foreach (var scene in new[] { probe, control })
            scene.Game.Resolve(new Decision(Assert.Single(scene.Game.Pending!.Affordances).Id,
                [scene.World.Seats[0].IdentityCard.ObjectId]));
        Assert.Equal(control.World.Digest().Canonical(), probe.World.Digest().Canonical());
        Assert.Equal(JsonSerializer.Serialize(control.Game.Pending), JsonSerializer.Serialize(probe.Game.Pending));
        Assert.Equal(JsonSerializer.Serialize(control.World.Random.Generator.GetState()),
            JsonSerializer.Serialize(probe.World.Random.Generator.GetState()));
    }

    [Fact]
    public void DepartedPhysicalInvocationCannotBorrowTheNewPublicTopsPaidContext()
    {
        AbilityRunner runner = null!;
        var (world, game, deck, master) = InvocationInteractionFixture.Create("09034",
            captureRunner: value => runner = value);
        var original = deck.PublicTop!;
        var action = Assert.Single(game.Pending!.Affordances, item => item.AnchorId == master.ObjectId);
        game.Resolve(InvocationInteractionFixture.Pay(world, action, original));
        var current = world.Agenda.Current!.Value;
        Assert.Contains("Already paid", runner.Choosing(world, current)!.Description);
        // Synthetic departure while a choice is suspended tests causal identity, not legal play.
        deck.DiscardTop(original, "independent departure probe", []);
        Assert.NotSame(original, deck.PublicTop);
        Assert.DoesNotContain("Already paid", runner.Choosing(world, current)!.Description ?? string.Empty);
    }

    [Fact]
    public void FailureOnlyReturnDoesNotBecomeASuccessPromiseInsideTheSpecial()
    {
        var (world, game, deck, master) = InvocationInteractionFixture.Create("09034",
            syntheticReturnOtherwise: true);
        var action = Assert.Single(game.Pending!.Affordances, item => item.AnchorId == master.ObjectId);
        game.Resolve(InvocationInteractionFixture.Pay(world, action, deck.PublicTop!));
        string text = InvocationInteractionFixture.Project(world, game).Presentation.Frame.Instruction;
        Assert.Contains("Already paid", text);
        Assert.DoesNotContain("return that exact physical Invocation", text);
    }
}
