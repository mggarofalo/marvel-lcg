using Godot;

namespace Marvel.Godot;

/// <summary>One decision entry, with an explicit accepted-prompt path during prior-table playback.</summary>
internal sealed class BoardCompleteChoicesEntry(Func<bool> isCurrent)
{
    private Button? entry;
    private Action<Control>? open;
    private Action<Control>? pending;
    private bool opened;
    private bool available;
    private string priorVariation = string.Empty;
    private string disclosureLabel = "All choices";

    internal void Register(Button button)
    {
        entry = button;
        disclosureLabel = button.Text;
        button.Pressed += () => Open(button);
    }

    internal void Bind(Action<Control> action) => open = action;

    internal void Open(Control source)
    {
        if (!InteractionControl.IsUsable(source)) return;
        if (pending is not null) pending(source);
        else if (isCurrent()) open?.Invoke(source);
    }

    internal void SetOpen(bool value, bool hasDraft)
    {
        opened = value;
        available = hasDraft;
        Refresh();
    }

    internal void PresentPending(string heading, Action<Control> action)
    {
        if (pending is null && InteractionControl.IsUsable(entry)) priorVariation = entry!.ThemeTypeVariation;
        pending = action;
        if (!InteractionControl.IsUsable(entry)) return;
        SetVariation(GodotThemeVariations.ChoiceButton);
        entry!.Text = heading;
        entry.AccessibilityName = heading;
        entry.SetMeta("accepted_decision_entry", true);
        Refresh();
    }

    internal void ClearPending()
    {
        if (pending is not null && InteractionControl.IsUsable(entry)) SetVariation(priorVariation);
        pending = null;
        if (!InteractionControl.IsUsable(entry)) return;
        entry!.Text = disclosureLabel;
        entry.AccessibilityName = entry.Text;
        entry.RemoveMeta("accepted_decision_entry");
        Refresh();
    }

    private void Refresh()
    {
        if (InteractionControl.IsUsable(entry))
            entry!.Disabled = opened || !available && pending is null;
    }

    private void SetVariation(string variation)
    {
        entry!.ThemeTypeVariation = variation;
        foreach (string state in new[] { "normal", "hover", "pressed", "hover_pressed", "disabled" })
            entry.RemoveThemeStyleboxOverride(state);
        TableCompactButtonStyle.Apply(entry);
    }
}
