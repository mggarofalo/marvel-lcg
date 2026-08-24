using System.Buffers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace Marvel.Core.Digest;

/// <summary>
/// The canonical state digest, v2 — see <c>docs/state-digest-v2.md</c>.
/// </summary>
/// <remarks>
/// <para>
/// This is the document two engines compare to decide whether they did the same
/// thing. Comparison is <b>byte equality on the canonical string</b>, so the
/// serialisation is not a formatting preference — it is the contract.
/// </para>
/// <para>
/// The reimplementation checklist in the spec runs to ten steps. Steps 1–7
/// populate the records from a live game and need an engine, which does not
/// exist yet. Steps 8 and 9 — serialise exactly, compare as strings and diff
/// only on mismatch — are pure functions over the record model and are what
/// this file implements. They are also the steps most likely to differ between
/// languages, because every JSON writer has opinions about key order,
/// whitespace and non-ASCII.
/// </para>
/// <para>
/// The writer is <see cref="Utf8JsonWriter"/> with
/// <see cref="JavaScriptEncoder.UnsafeRelaxedJsonEscaping"/>, which reproduces
/// Python's <c>json.dumps</c> exactly over the strings a digest can contain —
/// see <see cref="CanonicalOptions"/> for what "can contain" means and why it
/// is a constraint rather than an observation.
/// </para>
/// </remarks>
public sealed class StateDigest
{
    /// <summary>The digest format version.</summary>
    public const int Version = 2;

    /// <summary>The empty document — <b>not</b> the empty string.</summary>
    /// <remarks>
    /// An absent digest and an empty one mean different things to the
    /// comparison: absent means "this scene predates v2 and there is nothing to
    /// compare", empty means "the world held no cards".
    /// </remarks>
    public const string Empty = "{\"v\":2,\"cards\":[]}";

    /// <summary>The cards, ascending by <see cref="CardRecord.Id"/>.</summary>
    public IReadOnlyList<CardRecord> Cards { get; }

    /// <summary>Creates a digest, sorting the records by id.</summary>
    public StateDigest(IEnumerable<CardRecord> cards)
    {
        ArgumentNullException.ThrowIfNull(cards);
        Cards = [.. cards.OrderBy(card => card.Id)];
    }

    /// <summary>
    /// How the canonical text is written, and the one setting that matters.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Python writes the fixture with <c>json.dumps(separators=(",", ":"),
    /// ensure_ascii=True)</c>. No .NET encoder reproduces that for arbitrary
    /// strings, and it is worth being precise about why, because the reason
    /// bounds what this contract can promise.
    /// </para>
    /// <para>
    /// Python emits <c>\u001f</c>; .NET emits <c>\u001F</c>. Hex case is not
    /// configurable on either side, so once a character needs escaping at all,
    /// the two writers disagree — and no combination of settings fixes it.
    /// Agreement is therefore only reachable by keeping digest strings clear of
    /// characters that need escaping.
    /// </para>
    /// <para>
    /// They are clear of them, and not by luck: a digest holds card ids, zone
    /// names and field names, and every one of those is an identifier. Measured
    /// across the whole domain — 3,999 card ids, 96 zone names and 257 field
    /// names, the last including one <c>t_</c> key per trait in the card
    /// database — all 4,352 strings are printable ASCII, and this encoder
    /// reproduces Python byte for byte on every one.
    /// </para>
    /// <para>
    /// <see cref="JavaScriptEncoder.Default"/> does not: it escapes the
    /// apostrophe, and three traits carry one — <c>'POOL</c>,
    /// <c>BATROC'S BRIGADE</c> and <c>CROSSFIRE'S CREW</c>. That is the whole
    /// margin between the two encoders on real data, and it is why the choice
    /// is spelled out here rather than left to the default.
    /// </para>
    /// <para>
    /// The constraint that keeps this true is that no digest string leaves
    /// printable ASCII. It is enforced on the Python side, over the card
    /// database, by <c>test_digest_domain.py</c>; the fixture is checked here.
    /// A card <i>name</i> in a digest would break it — names carry curly
    /// apostrophes and accents — which is a good reason never to put one there.
    /// </para>
    /// </remarks>
    internal static readonly JsonWriterOptions CanonicalOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        Indented = false,
    };

    /// <summary>The canonical text. This is what gets compared.</summary>
    public string Canonical()
    {
        var buffer = new ArrayBufferWriter<byte>();
        using (var writer = new Utf8JsonWriter(buffer, CanonicalOptions))
        {
            writer.WriteStartObject();
            writer.WriteNumber("v", Version);
            writer.WriteStartArray("cards");
            foreach (var card in Cards)
            {
                card.WriteTo(writer);
            }

            writer.WriteEndArray();
            writer.WriteEndObject();
        }

        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }

    /// <summary>SHA-256 of the canonical text.</summary>
    public string Fingerprint() => Sha256(Canonical());

    /// <summary>SHA-256 of a string, lowercase hex.</summary>
    /// <remarks>
    /// <c>Convert.ToHexStringLower</c> would say this in one call, but it
    /// arrived in .NET 9 and this assembly targets .NET 8 so that Godot can
    /// reference it — see <c>Directory.Build.props</c>. It is the only place
    /// the floor costs anything.
    /// </remarks>
    public static string Sha256(string text) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text))).ToLowerInvariant();

    /// <summary>Reads a canonical document back into records.</summary>
    /// <remarks>
    /// Needed for the diff on mismatch, and for the round-trip test that proves
    /// the writer agrees with the recorded fixture. Deliberately strict: a
    /// document this cannot parse is one the comparison cannot explain.
    /// </remarks>
    public static StateDigest Parse(string canonical)
    {
        ArgumentNullException.ThrowIfNull(canonical);
        using var document = JsonDocument.Parse(canonical);
        var root = document.RootElement;

        int version = root.GetProperty("v").GetInt32();
        if (version != Version)
        {
            throw new NotSupportedException($"digest version {version}, expected {Version}");
        }

        var cards = new List<CardRecord>();
        foreach (var element in root.GetProperty("cards").EnumerateArray())
        {
            var fields = new Dictionary<string, long>(StringComparer.Ordinal);
            foreach (var field in element.GetProperty("fields").EnumerateObject())
            {
                fields[field.Name] = field.Value.GetInt64();
            }

            cards.Add(new CardRecord(
                Id: element.GetProperty("id").GetInt32(),
                Card: element.GetProperty("card").GetString() ?? "",
                Zone: element.GetProperty("zone").GetString() ?? "",
                Owner: element.GetProperty("owner").GetInt32(),
                Index: element.GetProperty("index").GetInt32(),
                Host: element.GetProperty("host").GetInt32(),
                FaceUp: element.GetProperty("face_up").GetBoolean(),
                Fields: fields));
        }

        return new StateDigest(cards);
    }

    /// <summary>
    /// Whether <paramref name="value"/> is safe to put in a digest.
    /// </summary>
    /// <remarks>
    /// Printable ASCII, <c>0x20</c> to <c>0x7E</c>. Outside that range this
    /// writer and Python's stop agreeing — see <see cref="CanonicalOptions"/>.
    /// Exposed so the check can be a test rather than a per-step cost: the
    /// domain is fixed by the card database, so it is cheaper to prove once
    /// than to assert on every one of the corpus's 1,773 scenes.
    /// </remarks>
    public static bool IsCanonicalSafe(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        foreach (char c in value)
        {
            if (c < 0x20 || c > 0x7E)
            {
                return false;
            }
        }

        return true;
    }
}
