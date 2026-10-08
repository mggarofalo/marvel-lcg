using System.Globalization;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Selects supplied state labels independently of their native placement.</summary>
internal static class CardStatusEntries
{
    internal static List<(string Name, string Text)> From(BoardCardPresentation card)
    {
        var result = new List<(string Name, string Text)>();
        if (card.Concealed) return result;
        foreach (string state in card.Status.Split('·', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
            if (state != "READY") result.Add((state, Words(state)));
        foreach (var group in card.Statuses.GroupBy(value => value, StringComparer.OrdinalIgnoreCase))
            result.Add((group.Key, (group.Count() > 1 ? $"{group.Count()} × " : "") + Words(group.Key)));
        foreach (BoardFieldPresentation counter in card.Counters)
            result.Add((counter.Name, $"{counter.Value} {Words(counter.Name)}"));
        AddKeywords(card, result);
        string retaliate = CardRetaliateToken.Caption(card);
        if (retaliate.Length > 0) result.Add(("Retaliate", retaliate));
        return result;
    }

    private static void AddKeywords(BoardCardPresentation card, List<(string Name, string Text)> result)
    {
        foreach (string name in new[] { "Acceleration", "Amplify", "Crisis", "Hazard", "Guard", "Patrol", "Steady", "Stalwart" })
        {
            string liveName = name == "Acceleration" ? "ACCELERATION_ICON" : name.ToUpperInvariant();
            BoardFieldPresentation? value = card.Fields.FirstOrDefault(field => field.Name == liveName);
            if (value is null || value.Value == "0") continue;
            result.Add((name, value.Value == "1" ? name : $"{name} {value.Value}"));
        }
    }

    private static string Words(string value) => CultureInfo.InvariantCulture.TextInfo.ToTitleCase(value.ToLowerInvariant());
}
