using Godot;

namespace Marvel.Godot;

/// <summary>Distinguishes deliberate keyboard inspection from returning focus after dismissal.</summary>
internal static class CardFocusPreview
{
    private const string Restoring = "restoring_inspector_focus";

    internal static void Bind(Control control, Action enter, Action leave)
    {
        control.FocusEntered += () => { if (!control.HasMeta(Restoring)) enter(); };
        control.FocusExited += leave;
    }

    internal static void Restore(Control control)
    {
        control.SetMeta(Restoring, true);
        control.GrabFocus();
        control.RemoveMeta(Restoring);
    }
}
