using Godot;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Keeps one prompt's visible cause beside the choice it produced.</summary>
internal sealed class MainResolutionStageController
{
    private const string Root = "Margin/Shell/Content/Play/Prompt/Margin/Stack/ActiveResolution";
    private readonly Main main;
    private readonly PanelContainer stage;
    private readonly HBoxContainer cards;
    private readonly Label hint;
    private readonly Label kind;
    private readonly Label summary;

    internal MainResolutionStageController(Main main)
    {
        this.main = main;
        stage = main.GetNode<PanelContainer>(Root);
        cards = main.GetNode<HBoxContainer>($"{Root}/Margin/Copy/Focus/Cards");
        kind = main.GetNode<Label>($"{Root}/Margin/Copy/Kind");
        summary = main.GetNode<Label>($"{Root}/Margin/Copy/Focus/Narrative/Summary");
        hint = main.GetNode<Label>($"{Root}/Margin/Copy/Focus/Narrative/Hint");
    }

    internal void Render(PromptPresentation view)
    {
        ClearCards();
        RenderCards(view.ContextCards);
        BoardCardPresentation? subject = view.ContextCards.Count > 0
            ? view.ContextCards[0]
            : null;
        kind.Text = Heading(view.ResolutionKind, subject is not null);
        summary.Text = Resolution(view, subject);
        hint.Text = subject is null
            ? string.Empty
            : "Click the card to inspect its full printed text while deciding.";
        stage.Visible = subject is not null
            || !string.IsNullOrWhiteSpace(summary.Text);
    }

    private void RenderCards(IReadOnlyList<BoardCardPresentation> contextCards)
    {
        foreach (BoardCardPresentation card in contextCards)
        {
            CardControl control = CardControl.Create(
                card, CardDisplaySize.Hand, main.interfaceScale, main.art);
            control.SetPresented(true);
            cards.AddChild(control);
            main.boardRender?.TrackCard(control, card);
            if (card.TargetId is { } id)
            {
                main.boardRender?.Register(id, control);
            }
        }

    }

    private static string Resolution(
        PromptPresentation view, BoardCardPresentation? subject)
    {
        string resolution = view.Resolution;
        if (string.IsNullOrWhiteSpace(resolution) && subject is not null)
        {
            resolution = view.ResolutionKind switch
            {
                "Interrupt" =>
                    $"Before {subject.Title} resolves, use an interrupt or let its reveal continue.",
                "Response" => $"{subject.Title} has resolved. Choose a response or continue.",
                _ => $"Resolve {subject.Title}.",
            };
        }
        return resolution;
    }

    private static string Heading(string resolutionKind, bool hasSubject) =>
        resolutionKind == "Interrupt" && hasSubject
            ? "RESOLVING CARD  /  INTERRUPT WINDOW"
            : resolutionKind == "Interrupt"
            ? "CURRENT RESOLUTION  /  INTERRUPT WINDOW"
            : hasSubject
            ? "RESOLVING CARD"
            : "CURRENT RESOLUTION";

    internal void Clear()
    {
        ClearCards();
        stage.Visible = false;
        summary.Text = string.Empty;
        hint.Text = string.Empty;
    }

    private void ClearCards()
    {
        foreach (Node child in cards.GetChildren())
        {
            cards.RemoveChild(child);
            child.QueueFree();
        }
    }
}
