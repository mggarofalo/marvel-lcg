using Godot;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Presents the ordered consequences of the latest response without playback.</summary>
internal sealed class EventCueSurface(Main main) : IDisposable
{
    private VBoxContainer? account;
    private Button? toggle;
    private ScrollContainer? scroll;
    private VBoxContainer? rows;

    internal void Present(IReadOnlyList<EventPresentation> entries)
    {
        EnsureAccount();
        foreach (Node child in rows!.GetChildren())
        {
            rows.RemoveChild(child);
            child.QueueFree();
        }
        for (int index = 0; index < entries.Count; index++)
        {
            EventPresentation entry = entries[index];
            rows.AddChild(new Label
            {
                Name = $"ResultStep{index}", Text = $"{index + 1}. {entry.Summary}",
                AutowrapMode = TextServer.AutowrapMode.Word,
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                ThemeTypeVariation = GodotThemeVariations.Caption,
            });
        }
        main.eventCueSummary.Text = entries.Count == 0 ? string.Empty
            : entries[EventCuePlanner.SettledCueIndex(entries)].CueSummary
                ?? entries[EventCuePlanner.SettledCueIndex(entries)].Summary;
        SetExpanded(false);
        RefreshVisibility(entries.Count > 0);
    }

    private void EnsureAccount()
    {
        if (account is not null) return;
        Control history = main.eventLog.GetParent<Control>();
        account = new VBoxContainer
        {
            Name = "ResultAccount", ThemeTypeVariation = GodotThemeVariations.TightStack,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        };
        history.AddChild(account);
        history.MoveChild(account, main.eventLog.GetIndex());
        toggle = new Button
        {
            Name = "ToggleResultDetails", Text = "> Result details",
            CustomMinimumSize = new Vector2(0, 44),
            Alignment = HorizontalAlignment.Left,
        };
        TableCompactButtonStyle.Apply(toggle);
        toggle.Pressed += () => SetExpanded(!scroll!.Visible);
        account.AddChild(toggle);
        scroll = new ScrollContainer
        {
            Name = "ResultChronology", CustomMinimumSize = new Vector2(0, 160),
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
            VerticalScrollMode = ScrollContainer.ScrollMode.Auto,
        };
        account.AddChild(scroll);
        rows = new VBoxContainer
        {
            Name = "ResultSteps", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            ThemeTypeVariation = GodotThemeVariations.TightStack,
        };
        scroll.AddChild(rows);
        rows.SortChildren += () =>
        {
            if (InteractionControl.IsUsable(scroll))
                scroll.CustomMinimumSize = new Vector2(0, Math.Clamp(rows.GetCombinedMinimumSize().Y, 44, 180));
        };
    }

    private void SetExpanded(bool expanded)
    {
        scroll!.Visible = expanded;
        toggle!.Text = expanded ? "< Result details" : "> Result details";
        toggle.TooltipText = expanded ? "Collapse result details" : "Expand ordered result details";
    }

    internal void Clear()
    {
        main.eventCueSummary.Text = string.Empty;
        main.eventCue.Visible = false;
        if (account is not null) account.Visible = false;
    }

    internal void RefreshVisibility(bool active)
    {
        main.eventCue.Visible = false;
        main.GetNode<Control>("StatusBar/Spacer").CustomMinimumSize = Vector2.Zero;
        if (account is not null) account.Visible = active && !CardPaymentWorkspaceLayout.Active(main);
    }

    public void Dispose()
    {
        if (InteractionControl.IsUsable(account))
        {
            account!.GetParent().RemoveChild(account);
            account.QueueFree();
        }
        account = null;
        toggle = null;
        rows = null;
        scroll = null;
    }
}
