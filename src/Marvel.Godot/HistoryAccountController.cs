using System.Globalization;
using Godot;
using Marvel.Server;

namespace Marvel.Godot;

/// <summary>Owns disclosure and scroll continuity for the authorized game account.</summary>
internal sealed class HistoryAccountController(Main main)
{
    private readonly HashSet<int> expanded = [];
    private readonly Dictionary<int, string> identities = [];
    private bool bound;
    private string? gameId;

    internal void Render()
    {
        IReadOnlyList<HistoryEntryDescriptor> actions = main.CurrentGame?.History?.Entries ?? [];
        Synchronize(actions);
        Present(Format(actions));
    }

    private void Synchronize(IReadOnlyList<HistoryEntryDescriptor> actions)
    {
        if (!string.Equals(gameId, main.CurrentGame?.GameId, StringComparison.Ordinal))
        {
            gameId = main.CurrentGame?.GameId;
            expanded.Clear();
            identities.Clear();
        }
        if (!bound)
        {
            main.eventLog.MetaClicked += ToggleDetails;
            bound = true;
        }
        foreach (int cursor in identities.Keys.ToArray())
        {
            if (!actions.Any(entry => entry.Cursor == cursor && entry.Summary == identities[cursor]))
            {
                expanded.Remove(cursor);
                identities.Remove(cursor);
            }
        }
        foreach (HistoryEntryDescriptor entry in actions) identities[entry.Cursor] = entry.Summary;
    }

    private string Format(IReadOnlyList<HistoryEntryDescriptor> actions)
    {
        string accent = ClientTheme.ToGodot(VisualSystem.Palette.Accent).ToHtml(false);
        return actions.Count > 0 || main.CurrentGame?.History?.ActionOpen == true
            ? EventLogFormatter.FormatActions(actions, main.CurrentGame!.History!.Undo, accent, expanded)
                + EventLogFormatter.FormatChronologySection(main.events.Entries, accent)
            : main.events.Entries.Count > 0
                ? EventLogFormatter.FormatChronology(main.events.Entries, accent) : "No events yet.";
    }

    private void Present(string text)
    {
        if (main.eventLog.Text == text) return;
        VScrollBar bar = main.eventLog.GetVScrollBar();
        double offset = bar.Value;
        bool atEnd = offset >= bar.MaxValue - bar.Page - 2;
        main.eventLog.Text = text;
        if (atEnd) main.eventLog.ScrollToLine(main.eventLog.GetLineCount());
        else bar.SetDeferred("value", offset);
    }

    private void ToggleDetails(Variant metadata)
    {
        const string prefix = "details:";
        string value = metadata.AsString();
        if (!value.StartsWith(prefix, StringComparison.Ordinal)
            || !int.TryParse(value.AsSpan(prefix.Length), NumberStyles.None,
                CultureInfo.InvariantCulture, out int cursor) || !identities.ContainsKey(cursor)) return;
        if (!expanded.Add(cursor)) expanded.Remove(cursor);
        double offset = main.eventLog.GetVScrollBar().Value;
        Render();
        main.eventLog.GetVScrollBar().SetDeferred("value", offset);
    }
}
