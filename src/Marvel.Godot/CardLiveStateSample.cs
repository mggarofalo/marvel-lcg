using System.Text.Json;
using Godot;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Exercises state placement using externally projected diagnostic snapshots.</summary>
internal static class CardLiveStateSample
{
    internal static bool TryStart(Control owner)
    {
        string? option = OS.GetCmdlineUserArgs().FirstOrDefault(value => value.StartsWith("--marvel-b1-state=", StringComparison.Ordinal));
        if (option is null) return false;
        owner.SetProcessInput(false);
        foreach (Node child in owner.GetChildren()) if (child is CanvasItem item) item.Hide();
        var root = new Control { Name = "B1StateFixtures" };
        ClientThemeInstallation.Apply(root);
        owner.AddChild(root);
        BoardCardPresentation[] cards = JsonSerializer.Deserialize<BoardCardPresentation[]>(File.ReadAllText(option["--marvel-b1-state=".Length..]))!;
        foreach (BoardCardPresentation card in cards) Add(root, card);
        for (int seat = 0; seat < 2; seat++)
            foreach (int count in new[] { 0, 1, 4 })
            {
                var sample = new Control { Name = $"Pending{seat}_{count}", Visible = false };
                sample.SetMeta("waiting_count", count);
                root.AddChild(sample);
                PendingEncounterIndicator.Add(sample, new Rect2(80, 80, 260, 64), count, seat);
            }
        return true;
    }

    private static void Add(Control root, BoardCardPresentation card)
    {
        var fixture = new Control { Name = $"State{root.GetChildCount()}", Visible = false };
        fixture.SetMeta("state_card", true);
        fixture.SetMeta("title", card.Title);
        fixture.SetMeta("concealed", card.Concealed);
        fixture.SetMeta("modified_maximum", CardProgressValue.From(card)?.MaximumModified ?? false);
        fixture.SetMeta("description", CardLiveStateRendering.Description(card));
        fixture.SetMeta("expected_entries", CardStatusEntries.From(card).Count);
        root.AddChild(fixture);
        var table = new Control { Name = "TableSample", Position = new Vector2(100, 100) };
        fixture.AddChild(table);
        CardControl face = CardControl.Create(card, CardDisplaySize.Board,
            SpatialCardMetrics.TableScale(ClientTheme.ConfiguredScale()));
        face.Name = "StateCard";
        table.AddChild(face);
        face.Size = face.CustomMinimumSize;
        if (SpatialTableZones.IsExhausted(card)) { face.PivotOffset = face.Size / 2; face.Rotation = Mathf.Pi / 2; }
        SpatialCardSidecar.State(face, card);
        CardTargetControlSample.Add(fixture, face, card);
        Control detail = CardStateDetails.Wrap(CardControl.Create(card, CardDisplaySize.Full,
            ClientTheme.ConfiguredScale()), card, beside: true, inspect: source =>
            {
                root.SetMeta("opened_source_title", source.Title);
                root.SetMeta("opened_source_target", source.TargetId ?? -1);
            });
        detail.Name = "StateInspection";
        detail.Position = new Vector2(500, 80);
        fixture.AddChild(detail);
    }
}
