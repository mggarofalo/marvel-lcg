using Marvel.Client;

namespace Marvel.Godot;

/// <summary>Owns chrome that changes when the fixed tabletop enters or leaves play.</summary>
internal static class MainPlayChrome
{
    internal static void Enter(Main main)
    {
        main.eyebrow.Visible = false;
        main.statusPanel.Visible = false;
        main.title.Visible = false;
        main.description.Visible = false;
    }

    internal static void Leave(Main main)
    {
        main.viewedSeat = null;
        main.boardPages.Clear();
        main.statusPanel.Visible = true;
        main.title.Visible = true;
        main.description.Visible = true;
        main.eyebrow.Visible = true;
    }

    internal static void ApplyProgress(
        Main main, GameProgressPresentation progress, bool danger)
    {
        main.statusPanel.Visible = !main.board.Visible;
        if (main.board.Visible && danger)
        {
            main.promptContext.Text = $"{progress.Title} · {progress.Status}";
            main.promptContext.ThemeTypeVariation = GodotThemeVariations.DangerText;
        }
    }
}
