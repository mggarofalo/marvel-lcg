using Godot;
using Marvel.Client;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Owns passive seat context and recovery notices.</summary>
internal static class TableSidebarContent
{
    private const string HistoryPath = "Margin/Shell/Content/Play/Prompt/Margin/Stack/Workbench/History";

    internal static void Render(Main main, BoardRenderResult result, Prompt? prompt, int? inspectedSeat)
    {
        Control history = main.GetNode<Control>(HistoryPath);
        VBoxContainer content = EnsureContent(history);
        foreach (Node child in content.GetChildren())
        {
            content.RemoveChild(child);
            child.QueueFree();
        }
        WorldDescriptor world = main.CurrentGame!.World!;
        content.AddChild(SeatContext(world, inspectedSeat, main.decisions.ReadingScale));
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

    // Which seat you play, whose turn it is and which seat the table shows, as chips named on hover.
    private static HFlowContainer SeatContext(WorldDescriptor world, int? inspectedSeat, InterfaceScale scale)
    {
        var row = new HFlowContainer { Name = "SeatContext", MouseFilter = Control.MouseFilterEnum.Ignore };
        row.AddThemeConstantOverride("h_separation", VisualSystem.Spacing(scale).ExtraSmall);
        row.AddThemeConstantOverride("v_separation", VisualSystem.Spacing(scale).ExtraSmall);
        Wording[] facts = TableWording.SeatContext(world.Table?.ViewedPrivateSeat is { } seat
                ? PendingSituationPresentation.SeatName(world, seat) : null,
            world.Table is { } table ? PendingSituationPresentation.SeatName(world, table.ActivePlayer) : null,
            inspectedSeat is { } inspected ? PendingSituationPresentation.SeatName(world, inspected) : null);
        for (int index = 0; index < facts.Length; index++)
            row.AddChild(Chip.Create($"SeatFact{index}", facts[index], null, scale));
        return row;
    }

    internal static void RefreshProgress(Main main)
    {
        Label? notice = main.promptPanel.FindChild("TableOperationalNotice", true, false) as Label;
        Label? tableNotice = main.boardAreas.FindChild("SpatialOperationalNotice", true, false) as Label;
        GameProgressPresentation? progress = main.currentProgress;
        bool ordinary = progress?.Kind is null or GameProgressKind.AwaitingDecision
            or GameProgressKind.WaitingForOtherPlayer;
        string text = ordinary ? "" : progress!.Kind == GameProgressKind.Resolving
            ? progress.Title : $"{progress.Title}\n{progress.Description}";
        if (InteractionControl.IsUsable(tableNotice)) ShowOnTable(tableNotice!, text);
        if (InteractionControl.IsUsable(notice))
        {
            notice!.Visible = !ordinary && !InteractionControl.IsUsable(tableNotice);
            notice.Text = text;
            notice.AccessibilityDescription = text;
        }
    }

    // On the table the notice is its title, the first line; the explanation is on hover and read aloud.
    private static void ShowOnTable(Label notice, string text)
    {
        notice.Visible = text.Length > 0;
        notice.Text = text.Split('\n')[0];
        notice.TooltipText = text;
        notice.MouseFilter = Control.MouseFilterEnum.Stop;
        notice.AccessibilityDescription = text;
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
