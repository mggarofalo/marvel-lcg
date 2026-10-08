using System.Text.Json;
using Godot;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Renders externally projected diagnostic faces without loading game content in the renderer.</summary>
internal static class CardFaceSample
{
    internal static bool TryStart(Control owner)
    {
        string? option = OS.GetCmdlineUserArgs().FirstOrDefault(value => value.StartsWith("--marvel-b1-faces=", StringComparison.Ordinal));
        if (option is null) return false;
        owner.SetProcessInput(false);
        foreach (Node child in owner.GetChildren())
            if (child is CanvasItem item) item.Hide();
        try
        {
            BoardCardPresentation[] cards = JsonSerializer.Deserialize<BoardCardPresentation[]>(
                File.ReadAllText(option["--marvel-b1-faces=".Length..]))!;
            var root = new Control { Name = "B1FaceFixtures" };
            ClientThemeInstallation.Apply(root);
            owner.AddChild(root);
            foreach (BoardCardPresentation card in cards)
                foreach (CardDisplaySize size in new[] { CardDisplaySize.Board, CardDisplaySize.Full })
                {
                    InterfaceScale scale = ClientTheme.ConfiguredScale();
                    if (size == CardDisplaySize.Board) scale = SpatialCardMetrics.TableScale(scale);
                    Control face = CardControl.Create(card, size, scale);
                    face.SetMeta("fixture_id", card.FaceId ?? "synthetic");
                    face.SetMeta("fixture_size", size.ToString());
                    face.SetMeta("expected_marks", JsonSerializer.Serialize(card.PrintedMarks));
                    face.SetMeta("expected_cost", card.Cost ?? "");
                    face.SetMeta("expected_effective", JsonSerializer.Serialize(card.EffectiveValues));
                    root.AddChild(face);
                }
            GD.Print($"B1_FACE_FIXTURES count={cards.Length}");
        }
        catch (Exception error)
        {
            GD.PushError(error.ToString());
            owner.GetTree().Quit(1);
        }
        return true;
    }
}
