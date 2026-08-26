using System.Buffers;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;

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
/// <see cref="JavaScriptEncoder.UnsafeRelaxedJsonEscaping"/>, normalised
/// afterwards — see <see cref="CanonicalOptions"/> for what a digest string can
/// contain and why that is a constraint rather than an observation.
/// </para>
/// </remarks>
public sealed partial class StateDigest
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

    /// <summary>How the canonical text is written before normalisation.</summary>
    /// <remarks>
    /// The relaxed encoder rather than the default one, because it escapes only
    /// what JSON requires. That leaves <see cref="Normalise"/> with two
    /// mechanical differences to fix instead of a general re-encoding job — and
    /// in particular it does not escape the apostrophe, which three traits
    /// carry (<c>'POOL</c>, <c>BATROC'S BRIGADE</c>, <c>CROSSFIRE'S CREW</c>).
    /// </remarks>
    internal static readonly JsonWriterOptions CanonicalOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        Indented = false,
    };

    // Every non-ASCII UTF-16 code unit. Matching per code unit rather than per
    // rune is deliberate: an astral character is two matches and becomes a
    // surrogate pair, which is what `ensure_ascii=True` emits for it.
    [GeneratedRegex(@"[^\x00-\x7f]")]
    private static partial Regex NonAscii();

    // An escape sequence, or a pair of backslashes. The pair comes first and
    // that ordering is the whole correctness argument: a literal backslash in
    // string content is spelled `\\`, so consuming pairs first means content
    // that reads like `\u0041` can never be mistaken for an escape.
    [GeneratedRegex(@"\\\\|\\u([0-9a-fA-F]{4})")]
    private static partial Regex Escape();

    /// <summary>
    /// Normalises this writer's output to the canonical spelling.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The digest is compared byte for byte, so the spelling of the JSON is the
    /// contract, and the contract picks two things .NET does not offer a switch
    /// for:
    /// </para>
    /// <list type="bullet">
    /// <item><description><b>Hex case is lower.</b> <c>\u001f</c>, where .NET
    /// writes <c>\u001F</c>.</description></item>
    /// <item><description><b>Non-ASCII is escaped.</b> Every character outside
    /// ASCII, where the relaxed encoder leaves most of them
    /// raw.</description></item>
    /// </list>
    /// <para>
    /// <b>Both choices were inherited and neither is now load-bearing.</b> They
    /// made the digest byte-identical to another implementation's; that
    /// implementation is gone, so this is simply the canonical form, and its
    /// only remaining virtue is that it holds still. Dropping the normaliser
    /// would be legitimate and would change every digest ever produced, which
    /// is why it is a decision rather than a cleanup.
    /// </para>
    /// <para>
    /// Both are mechanical, so they are fixed here rather than by hand-writing
    /// an encoder. The platform writer still decides everything structural —
    /// which characters JSON requires escaped, surrogate validity, the short
    /// forms — and this only adjusts the spelling afterwards.
    /// </para>
    /// <para>
    /// Correctness rests on the ordering inside <see cref="Escape"/>; see the
    /// comment there. It was checked against a 420-case fixture, 400 of them
    /// fuzzed over an alphabet of backslashes, <c>u</c>, hex digits and
    /// surrogate halves — because "obviously correct" is exactly the claim that
    /// was wrong the first time. Those cases are gone; the ordering argument
    /// stands on its own, and MARVEL-251 tracks replacing them.
    /// </para>
    /// <para>
    /// On the strings a digest actually contains this does nothing at all: card
    /// ids, zone names and field names are printable ASCII, so neither pass
    /// matches. Measured at about 6 microseconds per digest, which is why there
    /// is no fast path to get wrong.
    /// </para>
    /// </remarks>
    internal static string Normalise(string text)
    {
        string escaped = NonAscii().Replace(
            text, match => "\\u" + ((int)match.Value[0]).ToString("x4", CultureInfo.InvariantCulture));

        return Escape().Replace(
            escaped,
            match => match.Groups[1].Success
                ? "\\u" + match.Groups[1].Value.ToLowerInvariant()
                : match.Value);
    }

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

        return Normalise(Encoding.UTF8.GetString(buffer.WrittenSpan));
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

}
