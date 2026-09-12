using Marvel.Decisions;
using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Tests;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Session;
using Marvel.View;
using Xunit;

namespace Marvel.Server.Tests;
public sealed class EngineHostRestartQuarantinesTheFirstDivergentAuthorityRecoTests : EngineHostTestBase
{
    [Theory]
    [InlineData("rng")]
    [InlineData("digest")]
    [InlineData("prompt")]
    [InlineData("compatibility")]
    public void RestartQuarantinesTheFirstDivergentAuthorityRecord(string field)
    {
        var store = new MemorySessionStore();
        var factory = DatasetGameFactory.Load(RepositoryPaths.Root);
        var host = new EngineHost(factory, new SequenceCapabilities("divergence-owner"), store: store);
        _ = host.Exchange(EngineRequest.OpenGame("open", "divergence-table", new GameSpecification("rhino", ["spider_man"], [], Seed: 73)));
        StoredSession stored = Assert.Single(store.Load());
        SessionSave changed = field switch
        {
            "rng" => stored.Save with
            {
                Initial = stored.Save.Initial with
                {
                    RngWords = stored.Save.Initial.RngWords + 1,
                },
            },
            "digest" => stored.Save with
            {
                Initial = stored.Save.Initial with
                {
                    StateDigest = "changed"
                },
            },
            "prompt" => stored.Save with
            {
                CurrentPrompt = null
            },
            "compatibility" => stored.Save with
            {
                Compatibility = stored.Save.Compatibility with
                {
                    ReplayContract = "future-contract",
                },
            },
            _ => throw new InvalidOperationException(field),
        };
        AssertQuarantined(factory, stored with { Save = changed }, "divergence-table", "divergence-owner");
    }

    [Theory]
    [InlineData("event")]
    [InlineData("rng")]
    [InlineData("state")]
    public void RestartQuarantinesTheFirstDivergentCommittedDecision(string field)
    {
        var store = new MemorySessionStore();
        var factory = DatasetGameFactory.Load(RepositoryPaths.Root);
        var host = new EngineHost(factory, new SequenceCapabilities("step-owner"), store: store);
        EngineResponse opened = host.Exchange(EngineRequest.OpenGame("open", "step-table", new GameSpecification("rhino", ["spider_man"], [], Seed: 73)));
        _ = host.Exchange(EngineRequest.ResolveGame("resolve", "step-table", opened.Capability!, TakeOnly(opened), opened.Revision));
        StoredSession stored = Assert.Single(store.Load());
        JournalUnit unit = Assert.Single(stored.Save.Units);
        JournalStep step = Assert.Single(unit.Decisions);
        JournalStep changedStep = field switch
        {
            "event" => step with
            {
                Events = [JournalJson.Event(new FieldSet(0, "damage", 0, 1))],
            },
            "rng" => step with
            {
                RngWords = step.RngWords + 1
            },
            "state" => step with
            {
                StateFingerprint = "changed"
            },
            _ => throw new InvalidOperationException(field),
        };
        SessionSave changed = stored.Save with
        {
            Units = [unit with
            {
                Decisions = [changedStep]
            }

            ],
        };
        AssertQuarantined(factory, stored with { Save = changed }, "step-table", "step-owner");
    }

    [Fact]
    public void DrawingDuringAUnitAdvancesItsPersistedInformationFrontier()
    {
        var store = new MemorySessionStore();
        var host = new EngineHost(DatasetGameFactory.Load(RepositoryPaths.Root), new SequenceCapabilities("frontier-owner"), store: store);
        EngineResponse opened = host.Exchange(EngineRequest.OpenGame("open", "frontier-table", new GameSpecification("rhino", ["spider_man"], [], Seed: 73)));
        Prompt prompt = Assert.IsType<Prompt>(opened.Prompt);
        Affordance mulligan = Assert.Single(prompt.Affordances);
        int card = Assert.IsType<TargetRequest>(mulligan.Targets).Legal[0];
        EngineResponse resolved = host.Exchange(EngineRequest.ResolveGame("mulligan", "frontier-table", opened.Capability!, new EngineDecision(mulligan.Id, [card]), opened.Revision));
        Assert.Null(resolved.Error);
        SessionSave save = Assert.Single(store.Load()).Save;
        Assert.Equal(1, save.EditFrontier);
        JournalUnit unit = Assert.Single(save.Units);
        InformationExposure draw = Assert.Single(unit.Exposures, exposure => exposure.Reason == InformationFrontier.Draw);
        Assert.Equal([0], draw.Seats);
        StoredSession stored = Assert.Single(store.Load());
        IDurableGameFactory factory = DatasetGameFactory.Load(RepositoryPaths.Root);
        AssertQuarantined(factory, new StoredSession(save with { EditFrontier = 0 }, stored.Authorities), "frontier-table", "frontier-owner");
        AssertQuarantined(factory, stored with { Save = save with { Units = [unit with { Exposures = [new InformationExposure(InformationFrontier.Search, [0]), ], }, ], }, }, "frontier-table", "frontier-owner");
        var migrationStore = new MigrationSessionStore(stored with { Save = save with { Schema = 2, }, });
        var restarted = new EngineHost(DatasetGameFactory.Load(RepositoryPaths.Root), store: migrationStore);
        StoredSession migrated = Assert.Single(migrationStore.Load());
        Assert.Equal(1, migrationStore.Commits);
        Assert.Equal(SessionSave.CurrentSchema, migrated.Save.Schema);
        Assert.Equal(1, migrated.Save.EditFrontier);
        Assert.Equal(InformationFrontier.Draw, Assert.Single(Assert.Single(migrated.Save.Units).Exposures).Reason);
        Assert.Null(restarted.Exchange(EngineRequest.SyncGame("sync", "frontier-table", opened.Capability!)).Error);
    }

    [Fact]
    public void FailedSchemaTwoMigrationLeavesThePredecessorAuthoritative()
    {
        var source = new MemorySessionStore();
        IDurableGameFactory factory = DatasetGameFactory.Load(RepositoryPaths.Root);
        var first = new EngineHost(factory, new SequenceCapabilities("migration-failure-owner"), store: source);
        EngineResponse opened = first.Exchange(EngineRequest.OpenGame("open", "migration-failure-table", new GameSpecification("rhino", ["spider_man"], [], Seed: 73)));
        StoredSession current = Assert.Single(source.Load());
        var predecessor = new MigrationSessionStore(current with { Save = current.Save with { Schema = 2 }, });
        var restarted = new EngineHost(factory, store: new FailingSessionStore(predecessor, failAtCommit: 1));
        Assert.Equal(2, Assert.Single(predecessor.Load()).Save.Schema);
        Assert.Equal(0, predecessor.Commits);
        Assert.Equal("session_not_found", restarted.Exchange(EngineRequest.SyncGame("sync", "migration-failure-table", opened.Capability!)).Error?.Code);
    }

    [Fact]
    public void DivergentSchemaTwoMigrationIsRejectedBeforeCommit()
    {
        var source = new MemorySessionStore();
        IDurableGameFactory factory = DatasetGameFactory.Load(RepositoryPaths.Root);
        var first = new EngineHost(factory, new SequenceCapabilities("migration-divergence-owner"), store: source);
        EngineResponse opened = first.Exchange(EngineRequest.OpenGame("open", "migration-divergence-table", new GameSpecification("rhino", ["spider_man"], [], Seed: 73)));
        StoredSession current = Assert.Single(source.Load());
        var predecessor = new MigrationSessionStore(current with { Save = current.Save with { Schema = 2, CurrentPrompt = current.Save.CurrentPrompt!with { Label = "changed" }, }, });
        var restarted = new EngineHost(factory, store: predecessor);
        Assert.Equal(0, predecessor.Commits);
        Assert.Equal("session_not_found", restarted.Exchange(EngineRequest.SyncGame("sync", "migration-divergence-table", opened.Capability!)).Error?.Code);
    }

    [Fact]
    public void InvalidAuthorityDoesNotDisplaceTheSchemaTwoGeneration()
    {
        var source = new MemorySessionStore();
        IDurableGameFactory factory = DatasetGameFactory.Load(RepositoryPaths.Root);
        var first = new EngineHost(factory, new SequenceCapabilities("migration-authority-owner"), store: source);
        _ = first.Exchange(EngineRequest.OpenGame("open", "migration-authority-table", new GameSpecification("rhino", ["spider_man"], [], Seed: 73)));
        StoredSession current = Assert.Single(source.Load());
        var predecessor = new MigrationSessionStore(current with { Save = current.Save with { Schema = 2 }, Authorities = [current.Authorities[0] with { Seats = [1] }], });
        _ = new EngineHost(factory, store: predecessor);
        Assert.Equal(0, predecessor.Commits);
        Assert.Equal(2, Assert.Single(predecessor.Load()).Save.Schema);
    }

    [Fact]
    public void DuplicateAuthorityDoesNotDisplaceTheSchemaTwoGeneration()
    {
        var source = new MemorySessionStore();
        IDurableGameFactory factory = DatasetGameFactory.Load(RepositoryPaths.Root);
        var first = new EngineHost(factory, new SequenceCapabilities("migration-first-owner", "migration-second-owner"), store: source);
        _ = first.Exchange(EngineRequest.OpenGame("open-first", "migration-first-table", new GameSpecification("rhino", ["spider_man"], [], Seed: 73)));
        _ = first.Exchange(EngineRequest.OpenGame("open-second", "migration-second-table", new GameSpecification("rhino", ["spider_man"], [], Seed: 79)));
        StoredSession restored = source.Load().Single(session => session.Save.Session.Label == "migration-first-table");
        StoredSession current = source.Load().Single(session => session.Save.Session.Label == "migration-second-table");
        var predecessor = new MigrationSessionStore(restored, current with { Save = current.Save with { Schema = 2 }, Authorities = [current.Authorities[0] with { Verifier = restored.Authorities[0].Verifier, }, ], });
        _ = new EngineHost(factory, store: predecessor);
        Assert.Equal(0, predecessor.Commits);
        Assert.Equal(2, predecessor.Load().Single(session => session.Save.Session.Label == "migration-second-table").Save.Schema);
    }

    [Fact]
    public void ChoosingNoMulliganCardsBeforeAnotherPlayersPromptRevealsNothingNew()
    {
        var store = new MemorySessionStore();
        var host = new EngineHost(DatasetGameFactory.Load(RepositoryPaths.Root), new SequenceCapabilities("seat-zero", "invite-one", "seat-one"), store: store);
        EngineResponse opened = host.Exchange(EngineRequest.OpenGame("open", "zero-mulligan-frontier", new GameSpecification("rhino", ["captain_marvel", "spider_man"], [], Seed: 73)));
        EngineResponse resolved = host.Exchange(EngineRequest.ResolveGame("keep", "zero-mulligan-frontier", RequiredCapability(opened), TakeOnly(opened), opened.Revision));
        Assert.Null(resolved.Error);
        SessionSave save = Assert.Single(store.Load()).Save;
        Assert.Equal(0, save.EditFrontier);
        Assert.Empty(Assert.Single(save.Units).Exposures);
        Assert.Equal(1, resolved.Prompt?.Player);
        Assert.True(Assert.Single(resolved.Prompt!.Affordances).Targets?.IsSearch);
    }
}
