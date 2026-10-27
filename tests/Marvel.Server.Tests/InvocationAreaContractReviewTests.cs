using System.Text;
using System.Text.Json;
using Marvel.Protocol;
using Marvel.Rules.Play;
using Marvel.View;
using Xunit;

namespace Marvel.Server.Tests;

/// <summary>Independent synthetic contract controls; these do not admit a content pack.</summary>
public sealed class InvocationAreaContractReviewTests
{
    [Fact]
    public void ASecondDistinctPairCannotGiveOneOwnerTwoInvocationDecks()
    {
        var valid = Response();
        var world = valid.World!;
        var duplicate = world with
        {
            InvocationDecks = [world.InvocationDecks[1], new InvocationDeckDescriptor(0, 8, 9)],
        };
        // Repository choice: one typed Invocation deck per seat, even when all area IDs differ.
        Assert.Throws<JsonException>(() => EngineJson.Snapshot(valid with { World = duplicate }));
        Assert.Equal(2, EngineJson.Snapshot(valid).World!.InvocationDecks.Count);
    }

    [Fact]
    public void AnAmbiguousPlayerSeatCannotAuthorizeAnOtherwiseValidPair()
    {
        var valid = Response();
        var world = valid.World!;
        var duplicate = world with { Players = [.. world.Players, world.Players[0]] };
        Assert.Throws<JsonException>(() => EngineJson.Snapshot(valid with { World = duplicate }));
        Assert.Equal(2, EngineJson.Snapshot(valid).World!.Players.Count);
    }

    [Theory]
    [InlineData("card_ids")]
    [InlineData("order")]
    [InlineData("public_top")]
    public void TypedPileWireShapeRejectsUnreviewedIdentityMetadata(string field)
    {
        var valid = Response();
        string json = Encoding.UTF8.GetString(EngineJson.Write(valid));
        using var document = JsonDocument.Parse(json);
        var deck = document.RootElement.GetProperty("world").GetProperty("invocation_decks")[0];
        Assert.Equal("deck,discard,owner",
            string.Join(',', deck.EnumerateObject().Select(property => property.Name).Order()));
        string invalid = json.Replace("\"owner\":1,\"deck\":6", $"\"{field}\":[123],\"owner\":1,\"deck\":6",
            StringComparison.Ordinal);
        Assert.NotEqual(json, invalid);
        Assert.Throws<JsonException>(() => EngineJson.ReadResponse(Encoding.UTF8.GetBytes(invalid)));
        Assert.Equal(valid.World!.InvocationDecks, EngineJson.Snapshot(valid).World!.InvocationDecks);
    }

    [Fact]
    public void EmptyTypedPilesRemainExactWhenGenericPilesShareTheirZones()
    {
        var world = EngineJson.Snapshot(Response()).World!;
        var board = BoardPresentation.From(world);
        foreach (var typed in world.InvocationDecks)
        {
            var deck = Assert.Single(board.Areas, area => area.Id == typed.Deck);
            var discard = Assert.Single(board.Areas, area => area.Id == typed.Discard);
            Assert.Equal("INVOCATION DECK", deck.Title);
            Assert.Equal("INVOCATION DISCARD", discard.Title);
            Assert.Equal(typed.Owner, deck.Seat);
            Assert.Equal(typed.Owner, discard.Seat);
            Assert.Empty(deck.Cards);
            Assert.Empty(discard.Cards);
        }
        Assert.All(board.Areas.Where(area => area.Id is 8 or 9),
            area => Assert.DoesNotContain("INVOCATION", area.Title, StringComparison.Ordinal));
    }

    [Fact]
    public void EarlierSnapshotAndBoardSurviveSourceAndConsumerMutation()
    {
        var source = Response();
        var first = EngineJson.Snapshot(source);
        var board = BoardPresentation.From(first.World!);
        byte[] original = EngineJson.Write(first);
        var sourcePiles = Assert.IsAssignableFrom<IList<InvocationDeckDescriptor>>(source.World!.InvocationDecks);
        sourcePiles[1] = new InvocationDeckDescriptor(0, 8, 9);
        var second = EngineJson.Snapshot(source);
        Assert.Equal(original, EngineJson.Write(first));
        Assert.Equal(8, second.World!.InvocationDecks[1].Deck);
        Assert.Equal("INVOCATION DECK", Assert.Single(board.Areas, area => area.Id == 4).Title);
        Assert.DoesNotContain("INVOCATION", Assert.Single(board.Areas, area => area.Id == 8).Title,
            StringComparison.Ordinal);
        var consumerPiles = Assert.IsAssignableFrom<IList<InvocationDeckDescriptor>>(first.World!.InvocationDecks);
        consumerPiles[0] = consumerPiles[0] with { Deck = 99 };
        Assert.Equal(6, source.World.InvocationDecks[0].Deck);
        Assert.Equal(6, second.World.InvocationDecks[0].Deck);
        Assert.Equal("INVOCATION DECK", Assert.Single(board.Areas, area => area.Id == 6).Title);
    }

    private static EngineResponse Response() => new(EngineProtocol.Version, "review", "table", null, null, [],
        new WorldDescriptor([new PlayerDescriptor(0, "First", false), new PlayerDescriptor(1, "Second", false)],
            [new AreaDescriptor(4, "AdditionalDeck", 0, -1, [], []),
             new AreaDescriptor(5, "AdditionalDiscardPile", 0, -1, [], []),
             new AreaDescriptor(6, "AdditionalDeck", 1, -1, [], []),
             new AreaDescriptor(7, "AdditionalDiscardPile", 1, -1, [], []),
             new AreaDescriptor(8, "AdditionalDeck", 0, -1, [], []),
             new AreaDescriptor(9, "AdditionalDiscardPile", 0, -1, [], [])], [], Outcome.Unfinished)
        {
            InvocationDecks = new[] { new InvocationDeckDescriptor(1, 6, 7), new InvocationDeckDescriptor(0, 4, 5) },
        });
}
