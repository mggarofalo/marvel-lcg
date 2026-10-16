using System.Text.Json;
using Marvel.Cards.Run;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class ActionInvocationIsolationTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CompletedAndForeignInvocationsCannotEnterAnAction(bool foreign)
    {
        // Synthetic ownership contract: a simple counter effect makes any
        // mutation before rejecting an invalid execution handle observable.
        var runner = new AbilityRunner(GameOwnershipProgram("""
            {"placeCounters":{"card":"this","counter":"test","count":1}}
            """));
        var (world, source) = FixedCountBoard(runner);
        var (other, otherSource) = FixedCountBoard(runner);
        var ability = new PendingAbility(source.ObjectId, AbilityType.Action, 0);
        var owner = foreign ? other : world;
        AbilityExecution invocation = owner.Agenda.Executions.Begin(
            new Occurrence(1, [Steps.TurnAction], Subject: source.ObjectId, Player: 0), ability);
        if (!foreign) invocation.Finish([]);
        string before = Snapshot(world);
        string otherBefore = Snapshot(other);

        Assert.Throws<InvalidOperationException>(() =>
            runner.Act(world, invocation, Decision.Take(source.ObjectId)));
        Assert.Equal(before, Snapshot(world));
        Assert.Equal(otherBefore, Snapshot(other));

        AbilityExecutionResult accepted = runner.ExecuteAction(world, new PlayerAction(ability, [], []));
        Assert.True(accepted.IsComplete);
        Assert.Equal(1, source.Tokens["c_test"]);
        if (foreign)
        {
            Assert.True(runner.Act(other, invocation, Decision.Take(otherSource.ObjectId)).IsComplete);
            Assert.Equal(1, otherSource.Tokens["c_test"]);
        }
    }

    private static string Snapshot(World world) => JsonSerializer.Serialize(new
    {
        Digest = world.Digest().Canonical(),
        Random = world.Random.Generator.GetState(), world.Random.Generator.WordsConsumed,
        world.Agenda.Stage,
        Frames = world.Agenda.Outstanding.Select(step => new { step.What, step.Index, step.Subject }),
    });
}
