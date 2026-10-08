using System.Text.Json;
using Godot;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Native source layouts built from externally projected, explicitly synthetic Core snapshots.</summary>
internal static class CardSourceSample
{
    internal static bool TryStart(Control owner)
    {
        string? option = OS.GetCmdlineUserArgs().FirstOrDefault(value => value.StartsWith("--marvel-b1-sources=", StringComparison.Ordinal));
        if (option is null) return false;
        owner.SetProcessInput(false);
        foreach (Node child in owner.GetChildren()) if (child is CanvasItem item) item.Hide();
        var root = new Control { Name = "B1SourceFixtures" };
        ClientThemeInstallation.Apply(root);
        owner.AddChild(root);
        BoardAreaPresentation[] areas = JsonSerializer.Deserialize<BoardAreaPresentation[]>(File.ReadAllText(option["--marvel-b1-sources=".Length..]))!;
        foreach (BoardCardPresentation source in CardSourceGroups.Current(areas).Where(CardSourceGroups.IsLocalSource))
        {
            BoardCardPresentation host = areas.SelectMany(area => area.Cards).First(card =>
                source.Persistent!.Relation.Kind == "Attached" ? card.TargetId == source.Persistent.Relation.HostId : card.Kind == "HERO");
            Add(root, host, [source, source with { TargetId = source.TargetId + 1000 }]);
        }
        foreach (BoardCardPresentation host in areas.SelectMany(area => area.Cards))
        {
            BoardCardPresentation[] attached = CardSourceGroups.Attached(areas, host.TargetId ?? -1);
            if (attached.Length > 1) Add(root, host, attached);
        }
        BoardCardPresentation longSource = CardSourceGroups.Controlled(areas, 0)[0] with
        { Title = "Synthetic exceptionally long persistent source title with complete visible words", TargetId = 2000 };
        Add(root, areas.SelectMany(area => area.Cards).First(card => card.Kind == "HERO"),
            [longSource, longSource with { TargetId = 2001 }]);
        Add(root, areas.SelectMany(area => area.Cards).First(card => card.Kind == "HERO"),
            [longSource, longSource with { TargetId = 2001 }], blocked: true);
        Add(root, areas.SelectMany(area => area.Cards).First(card => card.Kind == "HERO"), [longSource]);
        return true;
    }

    private static void Add(Control root, BoardCardPresentation host, BoardCardPresentation[] sources, bool blocked = false)
    {
        BoardCardPresentation source = sources[0];
        var fixture = new Control { Name = $"Source{root.GetChildCount()}", Visible = false };
        root.AddChild(fixture);
        fixture.SetMeta("source_title", source.Title);
        fixture.SetMeta("source_id", source.TargetId ?? -1);
        fixture.SetMeta("second_source_id", sources[Math.Min(1, sources.Length - 1)].TargetId ?? -1);
        fixture.SetMeta("source_count", sources.Length);
        fixture.SetMeta("blocked", blocked);
        if (blocked) fixture.AddChild(new PanelContainer { Name = "PileObstruction",
            Position = new Vector2(80, 300), Size = new Vector2(400, 300) });
        var result = new BoardRenderResult { IsCurrent = () => true };
        result.CardActivated += (card, _) => { root.SetMeta("inspected_id", card.TargetId ?? -1); root.SetMeta("inspected_rules", card.RulesText); };
        CardControl face = CardControl.Create(host, CardDisplaySize.Board,
            SpatialCardMetrics.TableScale(ClientTheme.ConfiguredScale()));
        face.Name = "SourceHost";
        face.Position = new Vector2(100, 100);
        fixture.AddChild(face);
        face.Size = face.CustomMinimumSize;
        if (SpatialTableZones.IsExhausted(host)) { face.PivotOffset = face.Size / 2; face.Rotation = Mathf.Pi / 2; }
        SpatialTableHostAttachments.Add(face, sources, result, ClientTheme.ConfiguredScale());
        VBoxContainer ledger = CardSourceCollection.Create(sources,
            CardSourceStrip.LedgerWidth, result, ClientTheme.ConfiguredScale(), "Source ledger");
        ledger.Name = "SourceLedger";
        ledger.Position = new Vector2(600, 100);
        fixture.AddChild(ledger);
        Callable.From(() =>
        {
            foreach (CardControl card in result.VisibleCardControls())
            {
                card.AddInteractionControl(new Button { Name = "SourceAction", Text = "Action",
                    TooltipText = "Synthetic action control geometry" });
                card.GuiInput += input =>
                {
                    if (input is InputEventMouseButton { Pressed: false }) result.RoutePointer(input);
                };
            }
        }).CallDeferred();
    }
}
