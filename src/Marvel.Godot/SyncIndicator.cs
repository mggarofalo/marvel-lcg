using Godot;
using Marvel.Client;

namespace Marvel.Godot;

/// <summary>
/// The status bar's connection mark: an icon shown only while the displayed table may differ from the
/// authoritative one, with the client's explanation on hover. A current table shows nothing.
/// </summary>
/// <remarks>Which lifecycle states count as stale, and the icon, are our presentation choices.</remarks>
internal static class SyncIndicator
{
    private const string Icon = "sync_stale";

    /// <summary>Whether the displayed table may not match the authority, or a decision did not reach it.</summary>
    internal static bool Stale(GameProgressKind kind) => kind is GameProgressKind.SynchronizationUnavailable
        or GameProgressKind.DecisionRejected or GameProgressKind.DecisionNotSent or GameProgressKind.Unconfirmed
        or GameProgressKind.ServiceUnavailable or GameProgressKind.SessionUnavailable
        or GameProgressKind.StorageFailure or GameProgressKind.VersionMismatch or GameProgressKind.Unavailable;

    /// <summary>The mark's tooltip: the state's title, then its explanation when it has one.</summary>
    internal static string Tooltip(string title, string description) =>
        string.Join("\n", new[] { title, description }.Where(line => !string.IsNullOrWhiteSpace(line)));

    /// <summary>Shows the mark for a stale state and hides it for a current one.</summary>
    internal static void Apply(Label status, GameProgressPresentation progress)
    {
        if (Stale(progress.Kind)) Show(status, progress.Title, progress.Description);
        else Hide(status);
    }

    /// <summary>Shows the mark with an explanation.</summary>
    internal static void Show(Label status, string title, string description)
    {
        Prepare(status);
        status.Visible = true;
        status.TooltipText = Tooltip(title, description);
        status.AccessibilityName = title;
        status.AccessibilityDescription = status.TooltipText;
    }

    /// <summary>Hides the mark; the table is current.</summary>
    internal static void Hide(Label status)
    {
        Prepare(status);
        status.Visible = false;
        status.TooltipText = string.Empty;
        status.AccessibilityName = string.Empty;
        status.AccessibilityDescription = string.Empty;
    }

    private static void Prepare(Label status)
    {
        status.Text = string.Empty;
        status.MouseFilter = Control.MouseFilterEnum.Stop;
        if (status.GetNodeOrNull("Icon_" + Icon) is not null) return;
        int pixels = 22;
        status.CustomMinimumSize = new Vector2(pixels + 4, pixels + 4);
        TextureRect icon = TableIcon.Create(Icon, pixels, ClientTheme.ToGodot(VisualSystem.Palette.Danger));
        icon.Position = new Vector2(2, 2);
        status.AddChild(icon);
    }
}
