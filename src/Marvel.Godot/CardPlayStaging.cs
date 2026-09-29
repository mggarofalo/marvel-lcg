using Godot;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Moves only the visible draft representation out of the hand until payment is committed or cancelled.</summary>
internal sealed class CardPlayStaging : IDisposable
{
    private readonly Control? source;
    private readonly bool sourceWasVisible;
    private readonly VBoxContainer? stage;

    internal CardPlayStaging(DecisionPanel panel, Control overlay)
    {
        Main? main = MainFor(panel);
        int id = panel.composer!.Selected!.AnchorId;
        BoardCardPresentation? card = FindCard(main, id);
        if (main is null || card is null) return;
        main.boardController.HideCardInspector();
        source = main.boardRender?.ControlFor(id);
        sourceWasVisible = source?.Visible == true;
        if (source is not null) source.Visible = false;
        stage = new VBoxContainer { Name = "StagedCard", MouseFilter = Control.MouseFilterEnum.Ignore };
        stage.AddChild(DecisionPanel.Text("Ready to play", GodotThemeVariations.Heading));
        stage.AddChild(CardControl.Create(card, CardDisplaySize.Full, InterfaceScale.Percent60, main.art));
        stage.AddChild(DecisionPanel.Text("Not paid yet", GodotThemeVariations.Caption));
        overlay.AddChild(stage);
        CardInspectorFocus.IgnoreMouseRecursively(stage, interactiveRules: false);
    }

    internal void Fit(Rect2 payment, bool wide)
    {
        if (stage is null) return;
        stage.Visible = wide;
        stage.Position = new Vector2(payment.Position.X - 280, payment.Position.Y + 60);
        stage.Size = new Vector2(256, payment.Size.Y - 120);
    }

    public void Dispose()
    {
        if (InteractionControl.IsUsable(source)) source!.Visible = sourceWasVisible;
    }

    private static BoardCardPresentation? FindCard(Main? main, int id) =>
        main?.boardPresentation?.Areas.SelectMany(area => area.Cards)
            .FirstOrDefault(card => card.TargetId == id && !card.Concealed);

    private static Main? MainFor(Node node)
    {
        for (Node? parent = node; parent is not null; parent = parent.GetParent())
            if (parent is Main main) return main;
        return null;
    }
}
