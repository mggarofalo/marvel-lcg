using Godot;
using Marvel.Decisions;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Owns the explicit controls attached to prompt-addressable visible cards.</summary>
internal sealed class BoardCardInteractionControls
{
    private readonly Dictionary<CardControl, List<Button>> controls = [];
    private readonly Dictionary<CardControl, (BoardCardPresentation Card, bool IsHand)> presentations = [];
    private readonly Func<bool> isCurrent;
    private Func<CardPointerGesture, bool>? activate;

    internal BoardCardInteractionControls(Func<bool> isCurrent) => this.isCurrent = isCurrent;

    internal void Track(CardControl card, BoardCardPresentation presentation, bool isHand) =>
        presentations[card] = (presentation, isHand);

    internal void Bind(Func<CardPointerGesture, bool> handler) =>
        activate = handler ?? throw new ArgumentNullException(nameof(handler));

    internal void Present(
        IReadOnlyDictionary<int, List<CardControl>> visible,
        DecisionComposer? composer,
        PromptPresentation? prompt)
    {
        IReadOnlyDictionary<int, CardInteractionCue> cues =
            BoardInteractionCueProjection.From(composer, prompt);
        foreach ((int id, List<CardControl> cards) in visible)
        {
            foreach (CardControl card in cards)
            {
                card.SetInteractionCue(cues.GetValueOrDefault(id));
            }
        }
        Clear();
        foreach (CardInteractionControlDescriptor descriptor in
                 BoardInteractionControlProjection.From(composer, prompt))
        {
            Add(visible, descriptor);
        }
    }

    private void Clear()
    {
        foreach (List<Button> buttons in controls.Values)
        {
            foreach (Button button in buttons)
            {
                button.GetParent()?.RemoveChild(button);
                button.QueueFree();
            }
        }
        controls.Clear();
    }

    private void Add(
        IReadOnlyDictionary<int, List<CardControl>> visible,
        CardInteractionControlDescriptor descriptor)
    {
        if (!visible.TryGetValue(descriptor.CardId, out List<CardControl>? cards))
        {
            return;
        }
        foreach (CardControl card in cards.Where(InteractionControl.IsUsable))
        {
            if (card.GetParent() is not BoxContainer parent)
            {
                continue;
            }
            var button = new Button
            {
                Name = $"Card{descriptor.CardId}{descriptor.Intent}",
                Text = descriptor.Text,
                TooltipText = descriptor.Intent == CardInteractionIntent.Generator
                    ? "Use this offered resource generator."
                    : "Choose this card's offered action.",
                FocusMode = Control.FocusModeEnum.All,
            };
            button.Pressed += () => Activate(card, descriptor.Intent);
            parent.AddChild(button);
            parent.MoveChild(button, Math.Min(parent.GetChildCount() - 1, card.GetIndex() + 1));
            if (!controls.TryGetValue(card, out List<Button>? buttons))
            {
                buttons = [];
                controls.Add(card, buttons);
            }
            buttons.Add(button);
        }
    }

    private void Activate(CardControl card, CardInteractionIntent intent)
    {
        if (!isCurrent() || !InteractionControl.IsUsable(card)
            || !presentations.TryGetValue(card, out var presentation))
        {
            return;
        }
        activate?.Invoke(new CardPointerGesture(
            presentation.Card, card, presentation.IsHand,
            card.GetGlobalRect().GetCenter(), intent));
    }
}
