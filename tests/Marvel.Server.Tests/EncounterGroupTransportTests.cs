using System.Text.Json;
using Marvel.Protocol;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.View;
using Xunit;

namespace Marvel.Server.Tests;

public sealed class EncounterGroupTransportTests
{
    [Fact]
    public void WireSnapshotPreservesExactRelationshipsAndOwnsNestedStageStorage()
    {
        var source = Response();
        var restored = EngineJson.Snapshot(source);
        Assert.Equal(37, restored.Version);
        var group = Assert.Single(restored.World!.EncounterGroups);
        Assert.Equal(source.World!.EncounterGroups[0], group with { Stages = source.World.EncounterGroups[0].Stages });
        Assert.Equal([10, 11], group.Stages);
        Assert.Equal(5, group.Deck);
        Assert.Equal(6, group.Discard);
        Assert.True(group.Active);
        Assert.DoesNotContain(99, group.Stages);
        Assert.IsType<int[]>(source.World.EncounterGroups[0].Stages)[0] = 77;
        Assert.Equal(10, group.Stages[0]);
        Assert.IsAssignableFrom<IList<int>>(group.Stages)[0] = 88;
        Assert.Equal(77, source.World.EncounterGroups[0].Stages[0]);
    }

    [Theory]
    [InlineData("concealed-stage")]
    [InlineData("foreign-villain")]
    [InlineData("duplicate-stage")]
    [InlineData("missing-pile")]
    [InlineData("wrong-pile")]
    [InlineData("active-defeated")]
    public void InvalidGroupRelationshipsAreRejectedBeforeClientPublication(string corruption)
    {
        var response = Response();
        var group = response.World!.EncounterGroups[0];
        var invalid = corruption switch
        {
            "concealed-stage" => group with { Stages = [10, 99] },
            "foreign-villain" => group with { Villain = 12 },
            "duplicate-stage" => group with { Stages = [10, 10] },
            "missing-pile" => group with { Deck = 500 },
            "wrong-pile" => group with { Deck = 6, Discard = 5 },
            "active-defeated" => group with { Villain = null, Defeated = true },
            _ => throw new ArgumentException(corruption),
        };
        var broken = response with { World = response.World with { EncounterGroups = [invalid] } };
        Assert.False(EncounterGroupContract.IsValid(broken.World));
        Assert.Throws<JsonException>(() => EngineJson.ReadResponse(EngineJson.Write(broken)));
        Assert.True(EncounterGroupContract.IsValid(response.World));
    }

    private static EngineResponse Response()
    {
        CardDescriptor Card(int id) => new(id, CardBack.Encounter, true, true, -1,
            new CardFaceDescriptor($"card{id}", $"Title {id}", "", CardKind.EncounterVillain,
                new Dictionary<string, long>()));
        var world = new WorldDescriptor([], [
            new AreaDescriptor(1, "VillainArea", -1, -1, [Card(10), Card(11), Card(12)], []),
            new AreaDescriptor(5, "EncounterDeck", -1, -1,
                [new CardDescriptor(null, CardBack.Encounter, false, true, -1, null)], []),
            new AreaDescriptor(6, "EncounterDiscardPile", -1, -1, [], [])], [], Outcome.Unfinished)
        {
            EncounterGroups = [new EncounterGroupDescriptor(10, "Title 10", 10, null, 5, 6)
                { Active = true, Stages = new[] { 10, 11 } }],
        };
        return new EngineResponse(EngineProtocol.Version, "groups", "game", null, null, [], world);
    }
}
