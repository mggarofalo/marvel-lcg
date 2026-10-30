using System.Globalization;
using Marvel.Rules.Play;
using Marvel.Sim;
using Xunit;

namespace Marvel.Acceptance.Tests;

public sealed class SimulationPolicyErrorTests : SimulationHarnessTestBase
{
    private const int AnswersBeforeFault = 3;

    [Fact]
    public void ARunRecordsAnAnswerTheEngineRejectsAsAPolicyError()
    {
        var record = new StringWriter(CultureInfo.InvariantCulture);
        var summary = new SimulationRunSession(Single(), record, TextWriter.Null) { Policies = Faulting }.Run();

        Assert.Equal(1, summary.Failures);
        var failure = Assert.Single(Lines(record), item => item.GetProperty("type").GetString() == "failure");
        Assert.Equal("policy_error", failure.GetProperty("category").GetString());
        Assert.Equal(AnswersBeforeFault, failure.GetProperty("step").GetInt32());
        Assert.StartsWith("policy answer rejected: ", failure.GetProperty("message").GetString(), StringComparison.Ordinal);
    }

    [Fact]
    public void ReplayReproducesARecordedPolicyError()
    {
        var record = new StringWriter(CultureInfo.InvariantCulture);
        new SimulationRunSession(Single(), record, TextWriter.Null) { Policies = Faulting }.Run();
        string path = Path.Combine(Path.GetTempPath(), $"marvel-sim-policy-error-{Guid.NewGuid():N}.jsonl");
        try
        {
            File.WriteAllText(path, record.ToString());
            var replay = new SimulationReplaySession(new ReplayConfig(path, RepositoryRoot()), TextWriter.Null)
            {
                Policies = Faulting,
            }.Replay();

            Assert.Equal(1, replay.Games);
            Assert.Equal(AnswersBeforeFault, replay.Steps);
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static SimulationConfig Single() => Config(games: 1, seeds: [265], selectionSeed: null);

    private static readonly SimulationPolicyFactory Faulting =
        (facts, seatSeeds) => new FaultingPolicy(ActingPolicy.Create(facts, seatSeeds));

    /// <summary>Plays as the acting policy, then answers one prompt with a target no request offers.</summary>
    private sealed class FaultingPolicy(ISimulationPolicy inner) : ISimulationPolicy
    {
        private int answered;

        public PolicyMetrics Metrics => inner.Metrics;

        public Decision Answer(Game game)
        {
            if (answered++ < AnswersBeforeFault) return inner.Answer(game);
            var option = game.Pending!.Affordances.First(offer => offer.IsLegal);
            return Decision.Take(option.Id, [int.MinValue], []);
        }

        public void DecisionResolved() => inner.DecisionResolved();
    }
}
