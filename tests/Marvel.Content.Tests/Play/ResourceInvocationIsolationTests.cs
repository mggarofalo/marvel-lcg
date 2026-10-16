using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Play;
using Marvel.Rules.Timing;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class ResourceInvocationIsolationTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void InvalidResourceEntryCannotPayCostsOrPublishResources(bool foreign)
    {
        // Engineering contract: an entry handle is authority only in its active game.
        var runner = new AbilityRunner(AbilityCatalog.Parse("""
            {"cards":[{"card":"01006","abilities":[{
              "trigger":{"event":"WhenActionTriggered","timing":"Resource","subject":"game"},
              "cost":{"exhaust":"this"},"effect":{"generate":"Y"}
            }]}]}
            """));
        var (world, source) = FixedCountBoard(runner);
        var (other, otherSource) = FixedCountBoard(runner);
        var owner = foreign ? other : world;
        var entry = owner.Agenda.Executions.BeginBatch(new Occurrence(0, [Steps.TurnAction]));
        if (!foreign) entry.Finish([]);
        string before = world.Digest().Canonical();
        string otherBefore = other.Digest().Canonical();

        Assert.Throws<InvalidOperationException>(() => runner.UseResource(world, entry, 0, source.ObjectId));

        Assert.Equal(before, world.Digest().Canonical());
        Assert.Equal(otherBefore, other.Digest().Canonical());
        Assert.True(source.Ready);
        Assert.True(otherSource.Ready);
        var accepted = runner.ExecuteResource(world, 0, source.ObjectId);
        Assert.True(accepted.Execution.IsComplete);
        Assert.Equal("Y", accepted.Generation.Resources);
        Assert.False(source.Ready);
        if (foreign)
        {
            var independent = runner.UseResource(other, entry, 0, otherSource.ObjectId);
            Assert.True(independent.Execution.IsComplete);
            Assert.Equal("Y", independent.Generation.Resources);
            Assert.False(otherSource.Ready);
        }
    }
}
