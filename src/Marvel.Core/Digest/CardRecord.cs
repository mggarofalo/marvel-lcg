using System.Globalization;
using System.Text;

namespace Marvel.Core.Digest;

/// <summary>
/// One card in the state digest: eight keys, all present, always in this order.
/// </summary>
/// <param name="Id">The card's <c>object_id</c>.</param>
/// <param name="Card"><c>face.paper.card_id</c> of the <b>current</b> face.</param>
/// <param name="Zone">A <c>DeckType</c> member name, optionally <c>/removed</c>.</param>
/// <param name="Owner">Controlling player, or <c>-1</c> for the scenario.</param>
/// <param name="Index">Position within the zone's ordered list, from 0.</param>
/// <param name="Host">The card this one is attached to, or <c>-1</c>.</param>
/// <param name="FaceUp">Whether the card is face up.</param>
/// <param name="Fields">Named live state. Emitted code-point ordered.</param>
/// <remarks>
/// The key order is fixed by <c>docs/state-digest-v2.md</c> and is not
/// alphabetical — it is the table order. An empty <see cref="Fields"/> means
/// the card registers none, not that its zone was skipped.
/// </remarks>
public sealed record CardRecord(
    int Id,
    string Card,
    string Zone,
    int Owner,
    int Index,
    int Host,
    bool FaceUp,
    IReadOnlyDictionary<string, long> Fields)
{
    internal void WriteTo(StringBuilder builder)
    {
        builder.Append("{\"id\":").Append(Id.ToString(CultureInfo.InvariantCulture));

        builder.Append(",\"card\":");
        StateDigest.WriteJsonString(builder, Card);

        builder.Append(",\"zone\":");
        StateDigest.WriteJsonString(builder, Zone);

        builder.Append(",\"owner\":").Append(Owner.ToString(CultureInfo.InvariantCulture));
        builder.Append(",\"index\":").Append(Index.ToString(CultureInfo.InvariantCulture));
        builder.Append(",\"host\":").Append(Host.ToString(CultureInfo.InvariantCulture));
        builder.Append(",\"face_up\":").Append(FaceUp ? "true" : "false");

        builder.Append(",\"fields\":{");
        bool first = true;

        // Ordinal — by code point, which is what the spec says and what
        // Python's `sorted()` does on `str`. A culture-aware comparison would
        // put `t_GENIUS` and `toughness` in the other order on some machines
        // and the same on others, which is the worst possible failure mode for
        // a byte comparison.
        foreach (var field in Fields.OrderBy(f => f.Key, StringComparer.Ordinal))
        {
            if (!first)
            {
                builder.Append(',');
            }

            first = false;
            StateDigest.WriteJsonString(builder, field.Key);
            builder.Append(':').Append(field.Value.ToString(CultureInfo.InvariantCulture));
        }

        builder.Append("}}");
    }
}
