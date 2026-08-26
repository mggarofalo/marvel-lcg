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
/// <see cref="JavaScriptEncoder.UnsafeRelaxedJsonEscaping"/>, and its output is
/// taken as written — see <see cref="CanonicalOptions"/>.
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

    /// <summary>How the canonical text is written before normalisation.</summary>
    /// <remarks>
    /// <para>
    /// The relaxed encoder rather than the default one, because it escapes only
    /// what JSON requires. The default escapes far more than that for HTML
    /// safety — including the apostrophe, which three traits carry
    /// (<c>'POOL</c>, <c>BATROC'S BRIGADE</c>, <c>CROSSFIRE'S CREW</c>) — and a
    /// digest is not HTML.
    /// </para>
    /// <para>
    /// <b>Whatever this writer emits is the canonical form.</b> No rule decides
    /// how a digest is spelled, so the engine decides, and the decision is to
    /// take the platform writer's answer rather than adjust it: hex case and
    /// non-ASCII escaping are left exactly as .NET writes them. That is our
    /// choice, and the only property it has to keep is holding still.
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
    /// Needed for the diff on mismatch, and for the round-trip that proves
    /// reading a digest and writing it back changes no byte. Deliberately
    /// strict: a document this cannot parse is one the comparison cannot
    /// explain.
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
