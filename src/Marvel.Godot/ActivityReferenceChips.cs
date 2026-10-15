using Godot;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Event references inspect authorized printed faces without registering duplicate physical cards.</summary>
internal static class ActivityReferenceChips
{
    internal static void Present(Container host, Main main, EventPresentation? entry, bool compact)
    {
        foreach (Node child in host.GetChildren()) { host.RemoveChild(child); child.QueueFree(); }
        IEnumerable<ActivitySubject> subjects = entry?.Activity?.Subjects.DistinctBy(subject => subject.Card) ?? [];
        if (compact) subjects = subjects.Where(subject => subject.Reference is not null).Take(2);
        foreach (ActivitySubject subject in subjects)
        {
            var chip = new Button { Text = "▱ " + subject.Name, CustomMinimumSize = new Vector2(compact ? 96 : 0, 32), ClipText = compact,
                TooltipText = subject.Reference is null ? "" : "Pin card details · event reference",
                AccessibilityName = "Event reference: " + subject.Name,
                TextOverrunBehavior = compact ? TextServer.OverrunBehavior.TrimEllipsis : TextServer.OverrunBehavior.NoTrimming,
                MouseFilter = Control.MouseFilterEnum.Stop };
            chip.AddThemeFontSizeOverride("font_size", 12);
            host.AddChild(chip);
            if (subject.Reference is not { } face)
            {
                chip.Disabled = true;
                TooltipPolicy.ClippedCaption(chip, subject.Name);
                continue;
            }
            void Preview()
            {
                if (!main.cardInspectorPinned) main.boardController.ShowCardInspector(face, chip, false);
            }
            chip.MouseEntered += Preview;
            chip.FocusEntered += Preview;
            chip.MouseExited += main.boardController.ScheduleCardInspectorHide;
            chip.FocusExited += main.boardController.ScheduleCardInspectorHide;
            chip.Pressed += () => main.boardController.ShowCardInspector(face, chip, true);
        }
        host.Visible = host.GetChildCount() > 0;
    }
}
