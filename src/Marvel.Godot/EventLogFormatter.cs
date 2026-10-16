using Marvel.Protocol;
using System.Globalization;
using System.Text;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Formats visibility-safe history and event chronology for Godot rich text.</summary>
internal static class EventLogFormatter
{
    internal static string FormatActions(
        IReadOnlyList<HistoryEntryDescriptor> actions,
        IReadOnlyList<int> undo,
        string accent,
        IReadOnlySet<int>? expanded = null)
    {
        var text = new StringBuilder();
        foreach (HistoryEntryDescriptor entry in actions)
        {
            AppendAction(text, entry, accent, undo.Contains(entry.Cursor),
                expanded is null || expanded.Contains(entry.Cursor), expanded is not null);
        }

        return text.Length == 0 ? "Action in progress." : text.ToString();
    }

    internal static string FormatChronology(
        IReadOnlyList<EventPresentation> entries,
        string accent)
    {
        var text = new StringBuilder();
        for (int index = 0; index < entries.Count; index++)
        {
            text.Append("[color=#")
                .Append(accent)
                .Append(']')
                .Append((index + 1).ToString("000", CultureInfo.InvariantCulture))
                .Append("[/color]  ")
                .AppendLine(entries[index].Summary);
        }

        return text.ToString();
    }

    internal static string FormatChronologySection(
        IReadOnlyList<EventPresentation> entries,
        string accent) => entries.Count == 0
            ? string.Empty
            : $"\n{FormatChronology(entries, accent)}";

    private static void AppendAction(
        StringBuilder text,
        HistoryEntryDescriptor entry,
        string accent,
        bool canUndo,
        bool expanded,
        bool disclosure)
    {
        text.Append("[color=#").Append(accent).Append(']')
            .Append((entry.Cursor + 1).ToString("000", CultureInfo.InvariantCulture))
            .Append("[/color]  ").AppendLine(entry.Summary);
        if (disclosure && entry.Details.Count > 0)
        {
            text.Append("     [url=details:").Append(entry.Cursor.ToString(CultureInfo.InvariantCulture))
                .Append(expanded ? "]< Collapse details[/url]" : "]> Expand details[/url]")
                .AppendLine();
        }
        foreach (string detail in expanded ? entry.Details : [])
        {
            text.Append("     ").AppendLine(detail);
        }
        if (canUndo)
        {
            text.Append("     [url=undo:")
                .Append(entry.Cursor.ToString(CultureInfo.InvariantCulture))
                .AppendLine("]Undo to before this action[/url]");
        }
    }
}
