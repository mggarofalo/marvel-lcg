using System.Globalization;
using System.Security.Cryptography;
using System.Text;
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

    /// <summary>The canonical text. This is what gets compared.</summary>
    public string Canonical()
    {
        var builder = new StringBuilder();
        builder.Append("{\"v\":").Append(Version).Append(",\"cards\":[");
        for (int i = 0; i < Cards.Count; i++)
        {
            if (i > 0)
            {
                builder.Append(',');
            }

            Cards[i].WriteTo(builder);
        }

        builder.Append("]}");
        return builder.ToString();
    }

    /// <summary>SHA-256 of the canonical text.</summary>
    public string Fingerprint() => Sha256(Canonical());

    /// <summary>SHA-256 of a string, lowercase hex.</summary>
    public static string Sha256(string text) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(text)));

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
    /// Escapes a string the way Python's <c>json.dumps(ensure_ascii=True)</c>
    /// does, which is what the fixture was written with.
    /// </summary>
    /// <remarks>
    /// Hand-written rather than delegated to a JSON writer because the writers
    /// disagree in exactly the places that matter here. .NET's default encoder
    /// escapes <c>&amp;</c>, <c>&lt;</c> and <c>+</c>; its
    /// <c>UnsafeRelaxedJsonEscaping</c> leaves non-ASCII unescaped. Python
    /// escapes neither the first set nor leaves the second alone. A card id or
    /// trait outside ASCII has to encode as <c>\uXXXX</c> identically in both
    /// engines or the byte comparison fails on a card nobody has touched.
    /// </remarks>
    internal static void WriteJsonString(StringBuilder builder, string value)
    {
        builder.Append('"');
        foreach (char c in value)
        {
            switch (c)
            {
                case '"': builder.Append("\\\""); break;
                case '\\': builder.Append("\\\\"); break;
                case '\b': builder.Append("\\b"); break;
                case '\f': builder.Append("\\f"); break;
                case '\n': builder.Append("\\n"); break;
                case '\r': builder.Append("\\r"); break;
                case '\t': builder.Append("\\t"); break;
                default:
                    // Python escapes control characters and everything above
                    // ASCII, and nothing else. Notably it does *not* escape
                    // the forward slash.
                    if (c < 0x20 || c > 0x7E)
                    {
                        builder.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                    }
                    else
                    {
                        builder.Append(c);
                    }

                    break;
            }
        }

        builder.Append('"');
    }
}
