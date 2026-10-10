using System.Text.Json;
using Godot;
using Marvel.View;
using Marvel.Decisions;
using Marvel.Rules.Prompts;

namespace Marvel.Godot;

/// <summary>Explicit synthetic density fixture using projected Core sources and production table layout.</summary>
internal static class SourceTableauSample
{
    internal static bool TryStart(Control owner)
    {
        string? option = OS.GetCmdlineUserArgs().FirstOrDefault(value => value.StartsWith("--marvel-tableau=", StringComparison.Ordinal));
        if (option is null) return false;
        owner.SetProcessInput(false);
        foreach (Node child in owner.GetChildren()) if (child is CanvasItem item) item.Hide();
        var root = new Control { Name = "TableauFixture", Position = new Vector2(20, 50) };
        ClientThemeInstallation.Apply(root);
        owner.AddChild(root);
        BoardAreaPresentation[] areas = JsonSerializer.Deserialize<BoardAreaPresentation[]>(File.ReadAllText(option["--marvel-tableau=".Length..]))!;
        InterfaceScale scale = SpatialCardMetrics.TableScale(ClientTheme.ConfiguredScale());
        bool narrow = OS.GetCmdlineUserArgs().Contains("--marvel-tableau-narrow");
        root.SetMeta("narrow", narrow);
        var geometry = new AstraTableGeometry(narrow ? 1320 : 1670, 962, ClientTheme.ConfiguredScale() >= InterfaceScale.Percent130,
            PhysicalCardSize: SpatialCardMetrics.Envelope(areas.SelectMany(area => area.Cards), CardDisplaySize.Board, scale),
            HasSourceTableau: true, HasRevealingCard: narrow);
        var result = new BoardRenderResult { IsCurrent = () => true };
        var objects = new SpatialTableObjectRenderer(root, result, geometry, scale, null, false);
        objects.RenderScenario(areas.Where(area => area.Seat < 0).ToArray());
        if (narrow) objects.RenderRevealing([], [areas.SelectMany(area => area.Cards).First(card => card.Kind == "HERO")]);
        objects.RenderPlayer(areas.Where(area => area.Seat == 0).ToArray());
        objects.RenderHosted(areas);
        objects.RenderHand(areas.FirstOrDefault(area => area.Zone == "HandsArea"));
        root.AddChild(new Label { Name = "FixtureContext", Text = "SYNTHETIC DENSE TABLE · 16 physical sources\nCard body inspects · Each action selects that exact copy",
            Position = geometry.Context.Position, ThemeTypeVariation = GodotThemeVariations.Body });
        result.CardActivated += (card, _) => root.SetMeta("inspected_id", card.TargetId ?? -1);
        string path = option["--marvel-tableau=".Length..];
        WorldDescriptor world = JsonSerializer.Deserialize<WorldDescriptor>(File.ReadAllText(path + ".world"))!;
        Prompt prompt = JsonSerializer.Deserialize<Prompt>(File.ReadAllText(path + ".prompt"))!;
        var composer = new DecisionComposer(prompt);
        PromptPresentation presentation = PromptPresentation.From(prompt, world);
        result.BindExplicitInteraction(gesture =>
        {
            var draft = new BoardDraftInteraction(composer, new TableDraftBinding(composer, 0, 0, (_, _) => true), presentation.Affordances);
            bool changed = draft.TryActivate(gesture.Card.TargetId, false) != BoardDraftMutation.None;
            root.SetMeta("selected_id", composer.Selected?.AnchorId ?? -1);
            composer = new DecisionComposer(prompt);
            return changed;
        });
        foreach (CardControl card in result.VisibleCardControls()) card.GuiInput += input =>
        {
            if (input is InputEventMouseButton { Pressed: false }) result.RoutePointer(input);
        };
        result.PresentInteraction(composer, presentation);
        return true;
    }
}
