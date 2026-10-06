using Godot;
using Marvel.Client;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Owns passive seat context, recovery notices and the complete-choice entry.</summary>
internal static class TableSidebarContent
{
    private const string HistoryPath = "Margin/Shell/Content/Play/Prompt/Margin/Stack/Workbench/History";

    internal static void Render(Main main, BoardRenderResult result, Prompt? prompt, DisplayedSeatSelection selection)
    {
        Control history = main.GetNode<Control>(HistoryPath);
        VBoxContainer content = EnsureContent(history);
        foreach (Node child in content.GetChildren())
        {
            content.RemoveChild(child);
            child.QueueFree();
        }
        WorldDescriptor world = main.CurrentGame!.World!;
        var entry = new Button
        {
            Name = "CompleteChoiceSheet", Text = "Complete choices",
            Disabled = prompt is null, TooltipText = "See all available choices.",
            Visible = world.Outcome == Outcome.Unfinished,
            ThemeTypeVariation = GodotThemeVariations.ChoiceButton,
            CustomMinimumSize = new Vector2(0, 44),
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        content.AddChild(entry);
        TableCompactButtonStyle.Apply(entry);
        result.RegisterCompleteChoices(entry);
        TableContextDescriptor? table = world.Table;
        string own = table?.ViewedPrivateSeat is { } seat
            ? $"You control {PendingSituationPresentation.SeatName(world, seat)}."
            : "Shared table view.";
        string active = table is null ? "" : $" Active player: {PendingSituationPresentation.SeatName(world, table.ActivePlayer)}.";
        string inspecting = $" Inspecting: {PendingSituationPresentation.SeatName(world, selection.ExpandedSeat)}.";
        content.AddChild(Copy("SeatContext", own + active + inspecting));
        content.AddChild(Copy("TableOperationalNotice", ""));
        result.RegisterLastResult(TableHistoryDrawer.EnsureLatestResult(main, history));
        RefreshProgress(main);
    }

    internal static void ConfigureInvitation(Main main)
    {
        // The invitation belongs to the fixed-width table sidebar.
        TableCompactButtonStyle.Apply(main.invitationCopy);
        main.invitationCopy.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        main.invitationCopy.CustomMinimumSize = new Vector2(0, 44);
        main.invitationOffer.GetNode<Label>("Margin/Row/Copy")
            .AddThemeFontSizeOverride("font_size", 14);
    }

    internal static void RefreshProgress(Main main)
    {
        if (main.promptPanel.FindChild("TableOperationalNotice", true, false) is not Label notice)
            return;
        GameProgressPresentation? progress = main.currentProgress;
        bool ordinary = progress?.Kind is null or GameProgressKind.AwaitingDecision
            or GameProgressKind.WaitingForOtherPlayer;
        notice.Text = ordinary ? "Refresh to receive other players' latest actions."
            : $"{progress!.Title}\n{progress.Description}";
        notice.TooltipText = notice.Text;
    }

    private static VBoxContainer EnsureContent(Control history)
    {
        if (history.GetNodeOrNull<VBoxContainer>("TableSidebar") is { } existing) return existing;
        var content = new VBoxContainer
        {
            Name = "TableSidebar", ThemeTypeVariation = GodotThemeVariations.TightStack,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        };
        history.AddChild(content);
        history.MoveChild(content, 0);
        return content;
    }

    private static Label Copy(string name, string text) => new()
    {
        Name = name, Text = text, ThemeTypeVariation = GodotThemeVariations.Caption,
        AutowrapMode = TextServer.AutowrapMode.WordSmart,
        SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
    };
}
