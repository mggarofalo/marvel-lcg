using System.Globalization;

namespace Marvel.View;

/// <summary>Describes current authorized source state without calculating an action outcome.</summary>
public static class CardStatePresentation
{
    /// <summary>Formats only the supplied readable state, optionally with its title.</summary>
    public static string Summary(BoardCardPresentation card, bool includeTitle = true)
    {
        ArgumentNullException.ThrowIfNull(card);
        if (card.Concealed) return card.Title;
        var parts = new List<string>();
        if (includeTitle) parts.Add(card.Title);
        BoardFieldPresentation? health = card.Fields.FirstOrDefault(field => field.Name == "HEALTH");
        if (health is not null) parts.Add($"HP {health.Value}");
        if (!string.IsNullOrWhiteSpace(card.Status))
            parts.Add(CultureInfo.InvariantCulture.TextInfo.ToTitleCase(card.Status.ToLowerInvariant()));
        parts.AddRange(card.Counters.Select(counter =>
            $"{counter.Value} {counter.Name.ToLowerInvariant()} counters"));
        return string.Join(" · ", parts);
    }
}
