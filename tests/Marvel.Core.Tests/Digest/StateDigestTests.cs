using System.Text.Json;
using Marvel.Core.Digest;
using Xunit;

namespace Marvel.Core.Tests.Digest;

/// <summary>
/// The canonical serialisation of a board — <c>docs/state-digest-v2.md</c>.
/// </summary>
/// <remarks>
/// <para>
/// A digest is compared as a string, so the <i>spelling</i> is the contract:
/// key order, field ordering, absence of whitespace, and what happens to a
/// character JSON has an opinion about. Two boards that differ only in how they
/// were written down compare as different boards.
/// </para>
/// <para>
/// <b>None of this is a rule.</b> No published rule mentions serialisation, so
/// every claim here is the engine's own choice, and the only property any of
/// them has to keep is holding still. They are pinned for that reason and not
/// because anything external requires them.
/// </para>
/// </remarks>
public sealed class StateDigestTests
{
    [Fact]
    public void TheCanonicalTextSurvivesBeingParsedAndWrittenAgain()
    {
        // The property everything else rests on. If reading a digest and
        // writing it back changed a byte, no two engines could ever agree and
        // the diff on mismatch would report differences that are not there.
        string canonical = Board().Canonical();

        Assert.Equal(canonical, StateDigest.Parse(canonical).Canonical());
    }

    [Fact]
    public void TheKeysComeInTheTableOrderAndNotAlphabetically()
    {
        // `id`, `card`, `zone`, `owner`, `index`, `host`, `face_up`, `fields`.
        // Alphabetical would be `card`, `face_up`, `fields`, `host`, ... -- a
        // perfectly reasonable order that would change every digest ever
        // produced.
        var card = JsonDocument.Parse(Board().Canonical())
            .RootElement.GetProperty("cards")[0];

        Assert.Equal(
            ["id", "card", "zone", "owner", "index", "host", "face_up", "fields"],
            card.EnumerateObject().Select(property => property.Name));
    }

    [Fact]
    public void FieldsAreOrderedByCodePointAndNotByCulture()
    {
        // Ordinal puts every uppercase letter before every lowercase one. Any
        // comparison that reads letters as letters -- culture-aware or merely
        // case-insensitive -- puts `a` before `B` instead. On a byte comparison
        // that is the worst kind of difference, because it can be
        // machine-dependent: the same board digesting two ways on two laptops.
        string[] keys = ["B", "a"];

        // Proves the case discriminates, without depending on which collation
        // the machine happens to carry: reading these two as letters inverts
        // them, so an emitted order of `B, a` can only be ordinal.
        Assert.Equal(["a", "B"], keys.OrderBy(key => key, StringComparer.OrdinalIgnoreCase));

        var fields = keys.ToDictionary(key => key, _ => 1L, StringComparer.Ordinal);
        var written = JsonDocument.Parse(Board(fields).Canonical())
            .RootElement.GetProperty("cards")[0].GetProperty("fields");

        Assert.Equal(
            ["B", "a"],
            written.EnumerateObject().Select(property => property.Name));
    }

    [Fact]
    public void ThereIsNoWhitespaceAnywhere()
    {
        // Indentation is invisible to a reader and fatal to a comparison.
        string canonical = Board().Canonical();

        Assert.DoesNotContain('\n', canonical);
        Assert.DoesNotContain("  ", canonical, StringComparison.Ordinal);
        Assert.DoesNotContain("\": ", canonical, StringComparison.Ordinal);
    }

    [Fact]
    public void TheEmptyDocumentIsAConstantAndNotAnEmptyString()
    {
        // "An absent digest and an empty one mean different things: absent
        // means there is nothing to compare, empty means the world held no
        // cards."
        Assert.Equal(StateDigest.Empty, new StateDigest([]).Canonical());
        Assert.NotEqual("", StateDigest.Empty);
    }

    [Fact]
    public void CardsAreSortedByIdWhateverOrderTheyArriveIn()
    {
        // The digest is a function of the board and not of the order the
        // engine happened to walk it in.
        var ascending = new StateDigest([Record(1), Record(2), Record(3)]);
        var jumbled = new StateDigest([Record(3), Record(1), Record(2)]);

        Assert.Equal(ascending.Canonical(), jumbled.Canonical());
    }

    [Fact]
    public void AVersionThisCannotReadIsNamedRatherThanGuessedAt()
    {
        // A document from a format this does not implement is not a board with
        // a few unknown keys -- it is a board this cannot compare at all.
        string wrong = StateDigest.Empty.Replace("\"v\":2", "\"v\":3", StringComparison.Ordinal);

        var thrown = Assert.Throws<NotSupportedException>(() => StateDigest.Parse(wrong));
        Assert.Contains("version 3", thrown.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("'POOL")]
    [InlineData("BATROC'S CREW")]
    [InlineData("a\\backslash")]
    [InlineData("a\"quote")]
    [InlineData("a\u001fcontrol")]
    [InlineData("caf\u00e9")]
    [InlineData("\ud83c\udfb4 astral")]
    [InlineData("\\u0041 not an escape")]
    public void AnyStringADigestCanCarrySurvivesTheRoundTrip(string awkward)
    {
        // The apostrophes are real: three traits in the pool carry one, and the
        // default .NET encoder escapes them for HTML safety. The rest are the
        // shapes a JSON writer has an opinion about -- and the last is content
        // that *reads* like an escape, which is where a hand-rolled normaliser
        // would go wrong.
        var digest = Board(
            fields: new Dictionary<string, long>(StringComparer.Ordinal) { [awkward] = 1 },
            card: awkward,
            zone: awkward);

        string canonical = digest.Canonical();
        var read = StateDigest.Parse(canonical);

        Assert.Equal(canonical, read.Canonical());
        Assert.Equal(awkward, read.Cards[0].Card);
        Assert.Equal(awkward, read.Cards[0].Zone);
        Assert.Contains(awkward, read.Cards[0].Fields.Keys);
    }

    [Fact]
    public void OnlyWhatJsonRequiresIsEscaped()
    {
        // A round-trip passes under any encoder -- read back, an escaped
        // apostrophe is still an apostrophe. What an encoder changes is the
        // *spelling*, and the spelling is the contract: the same board written
        // two ways is two different digests.
        //
        // The default .NET encoder escapes for HTML safety, which reaches the
        // apostrophe (`'POOL`, `BATROC'S BRIGADE`, `CROSSFIRE'S CREW` all
        // carry one) and every non-ASCII character. A digest is not HTML.
        string canonical = Board(
            fields: new Dictionary<string, long>(StringComparer.Ordinal) { ["t_'POOL"] = 1 },
            card: "caf\u00e9").Canonical();

        Assert.Contains("t_'POOL", canonical, StringComparison.Ordinal);
        Assert.Contains("caf\u00e9", canonical, StringComparison.Ordinal);
        Assert.DoesNotContain("\\u0027", canonical, StringComparison.Ordinal);
        Assert.DoesNotContain("\\u00e9", canonical, StringComparison.OrdinalIgnoreCase);

        // And what JSON *does* require is still escaped.
        string quoted = Board(card: "a\"quote\\and\u001fcontrol").Canonical();
        Assert.Contains("\\\"", quoted, StringComparison.Ordinal);
        Assert.Contains("\\\\", quoted, StringComparison.Ordinal);
        Assert.Contains("\\u001", quoted, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void FuzzedStringsSurviveTheRoundTrip()
    {
        // The eight cases above are the shapes somebody thought of. This is the
        // alphabet they live in -- backslashes, `u`, hex digits and surrogate
        // halves -- because "obviously correct" is not a safe claim about
        // escaping. Seeded, so a failure is reproducible.
        const string alphabet = "\\u0123456789abcdefABCDEF\"'\u001f\u00e9";
        var random = new System.Random(20260826);

        for (int trial = 0; trial < 400; trial++)
        {
            string text = new(
                Enumerable.Range(0, random.Next(1, 24))
                    .Select(_ => alphabet[random.Next(alphabet.Length)])
                    .ToArray());

            var digest = Board(
                fields: new Dictionary<string, long>(StringComparer.Ordinal) { [text] = 1 },
                card: text);

            string canonical = digest.Canonical();
            var read = StateDigest.Parse(canonical);

            Assert.Equal(canonical, read.Canonical());
            Assert.Equal(text, read.Cards[0].Card);
        }
    }

    [Fact]
    public void TheFingerprintIsLowercaseHexAndFollowsTheText()
    {
        // A fingerprint is only useful if it moves with the thing it names.
        var one = Board();
        var two = Board(new Dictionary<string, long>(StringComparer.Ordinal) { ["health"] = 9 });

        Assert.Equal(64, one.Fingerprint().Length);
        Assert.Equal(one.Fingerprint(), one.Fingerprint().ToLowerInvariant());
        Assert.Equal(one.Fingerprint(), StateDigest.Parse(one.Canonical()).Fingerprint());
        Assert.NotEqual(one.Fingerprint(), two.Fingerprint());
    }

    [Fact]
    public void TheFingerprintIsSha256OfTheCanonicalText()
    {
        // Not a private hash: anything that can compute SHA-256 over the
        // canonical bytes gets the same answer.
        var digest = Board();

        Assert.Equal(StateDigest.Sha256(digest.Canonical()), digest.Fingerprint());
    }

    private static CardRecord Record(int id) => new(
        Id: id,
        Card: "01001a",
        Zone: "HeroArea",
        Owner: 0,
        Index: 0,
        Host: -1,
        FaceUp: true,
        Fields: new Dictionary<string, long>(StringComparer.Ordinal) { ["health"] = 10 });

    private static StateDigest Board(
        IReadOnlyDictionary<string, long>? fields = null,
        string card = "01001a",
        string zone = "HeroArea") =>
        new(
        [
            new CardRecord(
                Id: 0,
                Card: card,
                Zone: zone,
                Owner: 0,
                Index: 0,
                Host: -1,
                FaceUp: true,
                Fields: fields
                    ?? new Dictionary<string, long>(StringComparer.Ordinal)
                    {
                        ["health"] = 10,
                        ["is_exhaust"] = 0,
                        ["t_GENIUS"] = 1,
                    }),
        ]);
}
