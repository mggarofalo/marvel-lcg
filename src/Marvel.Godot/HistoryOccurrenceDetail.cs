using Marvel.Protocol;
using Godot;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Discloses the selected authorized occurrence without creating a second chronology.</summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1001", Justification = "The scene tree owns the native controls.")]
internal sealed class HistoryOccurrenceDetail
{
    private readonly Main main;
    private readonly Button toggle = new() { Text = "▸ Details", CustomMinimumSize = new Vector2(0, 44) };
    private readonly VBoxContainer body = new();
    private readonly Label summary = new() { AutowrapMode = TextServer.AutowrapMode.WordSmart };
    private readonly Label cause = new() { AutowrapMode = TextServer.AutowrapMode.WordSmart };
    private readonly ScrollContainer components = new() { Name = "OccurrenceComponents",
        CustomMinimumSize = new Vector2(0, 160), HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled };
    private readonly VBoxContainer componentRows = new() { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
    private readonly HFlowContainer references = new() { Name = "HistoryEventReferences" };
    private readonly Button undo = new() { Text = "Undo this action", CustomMinimumSize = new Vector2(0, 44) };
    private TimelineOccurrence? occurrence;
    private int undoCursor;
    private bool expanded;

    internal VBoxContainer Root { get; } = new() { Name = "OccurrenceDetails" };

    internal HistoryOccurrenceDetail(Main main)
    {
        this.main = main;
        Root.AddChild(toggle);
        Root.AddChild(body);
        body.AddChild(summary);
        body.AddChild(cause);
        body.AddChild(references);
        components.AddChild(componentRows);
        body.AddChild(components);
        body.AddChild(undo);
        toggle.Pressed += () => { expanded = !expanded; Render(); };
        undo.Pressed += () => main.sessionController.UndoTo(undoCursor);
    }

    internal void Present(TimelineOccurrence? selected)
    {
        if (!ReferenceEquals(occurrence, selected))
        {
            ActivityReferenceChips.Present(references, main, selected?.Event, compact: false);
            PresentComponents(selected?.Event.Details ?? []);
        }
        occurrence = selected;
        Render();
    }

    internal void Expand() { expanded = true; Render(); }

    internal bool Expanded => expanded;

    internal void FitHeight(float available)
    {
        if (!expanded || !components.Visible) return;
        float chrome = Root.GetCombinedMinimumSize().Y - components.CustomMinimumSize.Y;
        float height = Math.Clamp(available - chrome, 80,
            Math.Max(80, componentRows.GetChildCount() * 80));
        var minimum = new Vector2(0, height);
        if (!components.CustomMinimumSize.IsEqualApprox(minimum)) components.CustomMinimumSize = minimum;
    }

    private void PresentComponents(IReadOnlyList<EventPresentation> entries)
    {
        foreach (Node child in componentRows.GetChildren()) { componentRows.RemoveChild(child); child.QueueFree(); }
        components.Visible = entries.Count > 0;
        foreach (EventPresentation entry in entries)
        {
            var row = new HistoryOccurrenceRow { CustomMinimumSize = new Vector2(0, 76),
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, FocusMode = Control.FocusModeEnum.None };
            row.Configure(_ => { });
            componentRows.AddChild(row);
            row.Present(0, entry, false);
        }
    }

    private void Render()
    {
        Root.Visible = occurrence is not null;
        body.Visible = expanded;
        toggle.Text = expanded ? "▾ Details" : "▸ Details";
        if (occurrence is null) return;
        EventPresentation entry = occurrence.Event;
        summary.Visible = cause.Visible = entry.Details.Count == 0;
        summary.Text = entry.Summary;
        cause.Text = string.Join("\n", new[] { entry.Intention, entry.Cause }
            .Where(value => !string.IsNullOrWhiteSpace(value) && value != entry.Summary).Distinct());
        RefreshUndo();
    }
    private void RefreshUndo()
    {
        HistoryEntryDescriptor? action = main.CurrentGame?.History?.Entries
            .LastOrDefault(candidate => candidate.Cursor < occurrence!.Cursor);
        undoCursor = action?.Cursor ?? -1;
        undo.Visible = action is not null;
        undo.Disabled = main.decisions.PaymentModalOpen || !main.lifecycle.CanUndo(undoCursor);
        undo.TooltipText = action?.Summary ?? "";
    }
}
