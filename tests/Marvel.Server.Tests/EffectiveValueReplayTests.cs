using System.Text.Json;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Session;
using Marvel.Tests;
using Marvel.View;
using Xunit;

namespace Marvel.Server.Tests;

public sealed class EffectiveValueReplayTests
{
    [Fact]
    public void LedgerReconstructionRecreatesSourceSnapshotsWithoutPersistingProjectionState()
    {
        // The fixture registers one lasting effect during deterministic setup.
        // Source provenance is reconstructed by registration, just as it is
        // when a saved player decision creates an effect during replay.
        var factory = new SourceFixtureFactory(DatasetGameFactory.Load(RepositoryPaths.Root));
        var store = new MemorySessionStore();
        var first = new EngineHost(factory, store: store);
        EngineResponse opened = first.Exchange(EngineRequest.OpenGame(
            "open", "sources", new GameSpecification("rhino", ["spider_man"], [], Seed: 7)));
        Assert.Null(opened.Error);
        CardEffectiveValue before = VillainAttack(opened.World!);
        CardValueSourceDescriptor origin = Assert.Single(before.Calculation).Source!;
        Assert.Equal("Peter Parker", origin.Title);
        Assert.Equal(TimingPoints.EndOfRound, Assert.Single(before.Calculation).Duration!.Until);
        string save = SessionSaveJson.Write(Assert.Single(store.Load()).Save);
        Assert.DoesNotContain("effective_values", save, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("CardSourceSnapshot", save, StringComparison.Ordinal);

        var restarted = new EngineHost(factory, store: store);
        EngineResponse restored = restarted.Exchange(EngineRequest.SyncGame("sync", "sources", opened.Capability!));

        Assert.Null(restored.Error);
        Assert.Equal(JsonSerializer.Serialize(before), JsonSerializer.Serialize(VillainAttack(restored.World!)));
        Assert.Equal(save, SessionSaveJson.Write(Assert.Single(store.Load()).Save));
    }

    private static CardEffectiveValue VillainAttack(WorldDescriptor world) =>
        Assert.Single(world.Areas.Where(area => area.Zone == nameof(DeckType.VillainArea))
            .SelectMany(area => area.Cards)).Face!.EffectiveValues["ATK"];

    private sealed class SourceFixtureFactory(DatasetGameFactory inner) : IDurableGameFactory
    {
        public SessionCompatibility Compatibility => inner.Compatibility;

        public OpenedGame Create(GameSpecification specification)
        {
            OpenedGame opened = inner.Create(specification);
            World world = opened.Game.State;
            Card target = world.AreaOf(DeckType.VillainArea).Cards.Single();
            Card source = world.Seats[0].IdentityCard;
            world.Effects.Register(new(EffectSource.LastingEffect, "attack", 1, source.ObjectId, target.ObjectId,
                Duration.UntilEndOf(TimingPoints.EndOfRound)));
            return opened;
        }
    }
}
