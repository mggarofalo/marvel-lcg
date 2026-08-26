using System.Text.Json;
using Marvel.Content.Setup;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.State;

/// <summary>
/// The opening board, byte for byte against the recorded digest.
/// </summary>
/// <remarks>
/// <para>
/// The whole state model and none of the rules. <c>datasets/digest/vectors.json</c>
/// holds the canonical serialisation of every step of
/// <c>rhino / spider_man / 12345</c>; step 0 is the board as it stands before
/// anybody has decided anything, and reproducing it exactly means the id
/// allocator, every area, the seeded shuffle, the card data pipeline and the
/// field merge all agree with the engine that generated the corpus.
/// </para>
/// <para>
/// It is a byte comparison because that is the contract
/// (<c>docs/state-digest-v2.md</c>). When it fails, the assertion message names
/// the first card that differs rather than printing two 11 KB strings — which is
/// the entire reason the digest is a document and not a hash.
/// </para>
/// </remarks>
public sealed class OpeningBoardTests
{
    private const string Campaign = "rhino";
    private const uint Seed = 12345;
    private static readonly string[] Heroes = ["spider_man"];

    private static readonly SetupCatalog Setup =
        SetupCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("setup", "setup.json")));

    private static readonly CardCatalog Cards =
        CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));

    private static World Deal() => WorldSetup.Deal(
        Cards,
        Blueprints.From(Dealer.DealOrder(Setup, Campaign, Heroes), Cards),
        // The seat's name is the hero's printed name, which the setup dataset
        // records: `spider_man` seats "Spider-Man". It reaches nothing in the
        // digest and everything in a prompt label.
        [.. Heroes.Select(hero => Setup.Hero(hero).Name)],
        Seed);

    private static string RecordedStep(int step)
    {
        using var vectors = JsonDocument.Parse(
            File.ReadAllText(RepositoryPaths.Dataset("digest", "vectors.json")));
        var board = vectors.RootElement.GetProperty("cases")[0];
        Assert.Equal(Campaign, board.GetProperty("campaign").GetString());
        Assert.Equal((int)Seed, board.GetProperty("seed").GetInt32());
        return board.GetProperty("step_digests")[step].GetString()!;
    }

    [Fact]
    public void TheOpeningBoardIsByteIdenticalToTheRecording()
    {
        string recorded = RecordedStep(0);
        string dealt = Deal().Digest().Canonical();

        // Guard against a vacuous pass: two empty documents are also equal.
        Assert.Equal(81, dealt.Split("\"id\":").Length - 1);
        Assert.True(recorded.Length > 10_000, $"the recording is {recorded.Length} bytes");

        if (recorded != dealt)
        {
            Assert.Fail(DigestDiff.Describe(recorded, dealt));
        }
    }

    [Fact]
    public void TheDealtBoardHasEveryRecordedIdInPlace()
    {
        // The same claim without the fields, so a field bug and a placement bug
        // fail as two different tests rather than one.
        var recorded = Parse(RecordedStep(0));
        var dealt = Parse(Deal().Digest().Canonical());

        Assert.Equal(recorded.Count, dealt.Count);
        foreach (var id in recorded.Keys)
        {
            var (card, zone, owner, index, host, faceUp) = recorded[id];
            Assert.Equal((card, zone, owner, index, host, faceUp), Placement(dealt[id]));
        }
    }

    private static (string, string, int, int, int, bool) Placement(
        (string Card, string Zone, int Owner, int Index, int Host, bool FaceUp) record) =>
        (record.Card, record.Zone, record.Owner, record.Index, record.Host, record.FaceUp);

    private static Dictionary<int, (string Card, string Zone, int Owner, int Index, int Host, bool FaceUp)>
        Parse(string digest)
    {
        var cards = new Dictionary<int, (string, string, int, int, int, bool)>();
        using var document = JsonDocument.Parse(digest);
        foreach (var card in document.RootElement.GetProperty("cards").EnumerateArray())
        {
            cards[card.GetProperty("id").GetInt32()] = (
                card.GetProperty("card").GetString()!,
                card.GetProperty("zone").GetString()!,
                card.GetProperty("owner").GetInt32(),
                card.GetProperty("index").GetInt32(),
                card.GetProperty("host").GetInt32(),
                card.GetProperty("face_up").GetBoolean());
        }

        return cards;
    }
}
