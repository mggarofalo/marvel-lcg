using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Play;
using Marvel.Rules.Timing;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class ResourceReceiptAuthorityTests
{
    [Fact]
    public void AReceiptPublishesOnlyOnceFromItsExactLiveOwner()
    {
        // Engineering contract: an unpublished receipt is not a resource contribution.
        var (world, _) = FixedCountBoard(new AbilityRunner(AbilityCatalog.Parse("{\"cards\":[]}")));
        var execution = world.Agenda.Executions.BeginBatch(new Occurrence(0, [Steps.TurnAction]));
        var receipt = new ResourceGenerationReceipt(world, execution);
        Assert.False(receipt.IsComplete);
        Assert.Throws<InvalidOperationException>(() => receipt.Resources);
        var other = world.Agenda.Executions.BeginBatch(new Occurrence(1, [Steps.TurnAction]));
        Assert.Throws<InvalidOperationException>(() => receipt.Complete(world, other, "Y"));
        other.Finish([]);
        Assert.False(receipt.IsComplete);

        receipt.Complete(world, execution, "Y");
        Assert.True(receipt.IsComplete);
        Assert.Equal("Y", receipt.Resources);
        Assert.Throws<InvalidOperationException>(() => receipt.Complete(world, execution, "YY"));
        execution.Finish([]);
        Assert.Equal("Y", receipt.Resources);
    }

    [Fact]
    public void AFinishedInvocationCannotPublishAnUnfinishedReceipt()
    {
        var (world, _) = FixedCountBoard(new AbilityRunner(AbilityCatalog.Parse("{\"cards\":[]}")));
        var execution = world.Agenda.Executions.BeginBatch(new Occurrence(0, [Steps.TurnAction]));
        var receipt = new ResourceGenerationReceipt(world, execution);
        execution.Finish([]);

        Assert.Throws<InvalidOperationException>(() => receipt.Complete(world, execution, "Y"));
        Assert.False(receipt.IsComplete);
        Assert.Throws<InvalidOperationException>(() => receipt.Resources);
    }

    [Fact]
    public void ARepeatedGeneratorIsRejectedBeforeEitherContributionMutatesTheBoard()
    {
        var runner = new AbilityRunner(AbilityCatalog.Parse("{\"cards\":[]}"));
        var (world, source) = FixedCountBoard(runner);
        var probe = new PaymentResourceProbe(source, "Y", () => source.Exhaust());
        string before = world.Digest().Canonical();

        Assert.Throws<RulesNotImplementedException>(() => CardPlay.Spend(
            world, world.Facts, probe, [world.Seats[0].Hand],
            [source.ObjectId, source.ObjectId], 2, "", -1, 0, []));

        Assert.Equal(0, probe.Uses);
        Assert.Equal(before, world.Digest().Canonical());
        Assert.False(world.Agenda.IsBusy);
    }

}
