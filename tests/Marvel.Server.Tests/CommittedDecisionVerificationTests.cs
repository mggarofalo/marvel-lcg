using Marvel.Session;
using Marvel.Tests;
using Xunit;

namespace Marvel.Server.Tests;

/// <summary>
/// The published game reproduces a committed decision exactly as a replay
/// verifies a recorded one, so a record that disagrees with it is refused.
/// </summary>
public sealed class CommittedDecisionVerificationTests
{
    [Fact]
    public void ThePublishedGameReproducesTheCommittedDecision()
    {
        var commit = Commit.Next();

        commit.Verify(commit.Committed);
    }

    [Fact]
    public void AUnitThatStartsElsewhereIsRefused() =>
        Commit.Next().Refuses(unit => unit with { Round = unit.Round + 1 });

    [Fact]
    public void AUnitWhoseCompletionDisagreesIsRefused() =>
        Commit.Next().Refuses(unit => unit with
        {
            Status = unit.Status == "complete" ? "open" : "complete",
        });

    [Fact]
    public void AUnitWhoseExposuresDisagreeIsRefused()
    {
        // The frontier moves with the tampered exposure, so only the exposure
        // record itself disagrees with the published game.
        var commit = Commit.Next();
        JournalUnit last = commit.Committed.Units[^1];
        SessionSave tampered = commit.Committed with
        {
            EditFrontier = commit.Committed.Units.Count,
            Units = [.. commit.Committed.Units.Take(commit.Committed.Units.Count - 1), last with
            {
                Exposures = [.. last.Exposures, new InformationExposure("tampered", [0])],
            }],
        };

        Assert.Throws<ReplayDivergenceException>(() => commit.Verify(tampered));
    }

    [Fact]
    public void ASaveWhoseFrontierDisagreesIsRefused()
    {
        var commit = Commit.Next();

        Assert.Throws<ReplayDivergenceException>(() => commit.Verify(
            commit.Committed with { EditFrontier = commit.Committed.EditFrontier + 1 }));
    }

    [Fact]
    public void ASaveWhoseNextPromptDisagreesIsRefused()
    {
        var commit = Commit.Next();

        Assert.Throws<ReplayDivergenceException>(() => commit.Verify(
            commit.Committed with { CurrentPrompt = null }));
    }

    /// <summary>One accepted decision that starts a unit, with the saves around it.</summary>
    private sealed record Commit(EngineHost Host, SessionSave Live, SessionSave Committed)
    {
        public static Commit Next()
        {
            var store = new MemorySessionStore();
            var host = new EngineHost(DatasetGameFactory.Load(RepositoryPaths.Root), store: store);
            var table = ReplayTable.Open(host, "committed-decision");
            while (true)
            {
                SessionSave live = Assert.Single(store.Load()).Save;
                table.Accept();
                SessionSave committed = Assert.Single(store.Load()).Save;
                if (table.Accepted > 3 && committed.CurrentPrompt is not null
                    && committed.Units[^1].Decisions.Count == 1)
                {
                    return new Commit(host, live, committed);
                }
            }
        }

        public void Verify(SessionSave committed)
        {
            var published = SessionReplay.Verify(Live, Host.compatibility, Host.ReplayOpen);
            var candidate = SessionReplay.BeginCandidate(
                Live, Host.compatibility, Host.ReplayOpen, checkpoint: null);
            candidate.VerifyCommitted(published, committed);
        }

        public void Refuses(Func<JournalUnit, JournalUnit> tamper) =>
            Assert.Throws<ReplayDivergenceException>(() => Verify(Committed with
            {
                Units = [.. Committed.Units.Take(Committed.Units.Count - 1), tamper(Committed.Units[^1])],
            }));
    }
}
