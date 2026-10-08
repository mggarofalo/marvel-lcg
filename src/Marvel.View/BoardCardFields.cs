using System.Globalization;
using Marvel.Rules.State;

namespace Marvel.View;

/// <summary>Formats supplied card fields while excluding separate status and trait bookkeeping.</summary>
internal static class BoardCardFields
{
    internal static IEnumerable<BoardFieldPresentation> Present(
        CardDescriptor card,
        bool inPlay)
    {
        IEnumerable<BoardFieldPresentation> fields = card.Face!.Fields
            .Where(field => VisibleField(field, inPlay, card.Face.Kind))
            .OrderBy(field => field.Key, StringComparer.Ordinal)
            .Select(field => new BoardFieldPresentation(
                FieldName(field.Key), FieldValue(field, card.Face.Damage)));
        foreach (BoardFieldPresentation field in fields)
        {
            yield return field;
        }
        if (card.State?.Threat is { } threat
            && card.Face.Kind is CardKind.MainScheme or CardKind.EncounterSideScheme
            && !card.Face.Fields.ContainsKey("k_threat"))
        {
            yield return new BoardFieldPresentation(
                "THREAT", threat.ToString(CultureInfo.InvariantCulture));
        }
    }

    private static bool VisibleField(KeyValuePair<string, long> field, bool inPlay, CardKind kind)
    {
        if (field.Key.StartsWith("t_", StringComparison.Ordinal) || field.Key == "is_exhaust") return false;
        if (field.Key == "k_threat" && kind is not (CardKind.MainScheme or CardKind.EncounterSideScheme)) return false;
        // A supplied in-play zero is an authoritative current value, distinct
        // from an absent field. Presentation must not erase suppression.
        return inPlay || field.Value != 0;
    }

    private static string FieldName(string key) => BoardCardPresentationFactory.Humanize(key.StartsWith("k_", StringComparison.Ordinal) ? key[2..] : key, false).ToUpperInvariant();
    private static string FieldValue(KeyValuePair<string, long> field, long damage) => field.Key == "health"
        ? $"{field.Value.ToString(CultureInfo.InvariantCulture)}/{(field.Value + damage).ToString(CultureInfo.InvariantCulture)}"
        : field.Value.ToString(CultureInfo.InvariantCulture);
}
