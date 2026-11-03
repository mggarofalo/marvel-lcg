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

    /// <summary>States the host's open invitation as a chip naming its seat, explained on hover.</summary>
    internal static void ShowInvitation(Main main, int seat)
    {
        Control row = main.invitationOffer.GetNode<Control>("Margin/Row");
        row.GetNode<Label>("Copy").Visible = false;
        if (row.GetNodeOrNull<Control>("InvitationSeat") is { } prior)
        {
            row.RemoveChild(prior);
            prior.QueueFree();
        }
        PanelContainer chip = Chip.Create("InvitationSeat", SetupWording.Invitation(seat), null, main.decisions.ReadingScale);
        chip.SizeFlagsHorizontal = Control.SizeFlags.ShrinkBegin;
        row.AddChild(chip);
        row.MoveChild(chip, 0);
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
        string text = Notice(progress);
        if (InteractionControl.IsUsable(tableNotice)) ShowOnTable(tableNotice!, TableNotice(progress));
        if (InteractionControl.IsUsable(notice))
        {
            notice!.Visible = !ordinary && !InteractionControl.IsUsable(tableNotice);
            notice.Text = text;
            notice.AccessibilityDescription = text;
        }
    }

    /// <summary>The operational notice: empty for an ordinary decision, the title while resolving, else the title and why.</summary>
    internal static string Notice(GameProgressPresentation? progress) =>
        progress?.Kind is null or GameProgressKind.AwaitingDecision or GameProgressKind.WaitingForOtherPlayer ? ""
            : progress.Kind == GameProgressKind.Resolving ? progress.Title : $"{progress.Title}\n{progress.Description}";

    /// <summary>The notice the table shows: none for a finished game, whose heading and reason chip state it.</summary>
    internal static string TableNotice(GameProgressPresentation? progress) =>
        progress?.Kind is GameProgressKind.PlayersWin or GameProgressKind.VillainWins or GameProgressKind.PlayersLose
            ? "" : Notice(progress);

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
