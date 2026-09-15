using Godot;
using Marvel.Decisions;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Owns the explicit controls attached to prompt-addressable visible cards.</summary>
internal sealed class BoardCardInteractionControls
{
    private readonly Dictionary<CardControl, List<Button>> controls = [];
    private readonly Dictionary<CardControl, (BoardCardPresentation Card, bool IsHand)> presentations = [];
    private readonly Dictionary<Button, BoardInteractionFocusKey> focusKeys = [];
    private readonly Func<bool> isCurrent;
    private Func<CardPointerGesture, bool>? activate;
    private int focusGeneration;
    private BoardInteractionFocusKey? requestedFocus;

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
        if (!isCurrent())
        {
            return;
        }

        int generation = checked(++focusGeneration);
        BoardInteractionFocusKey? focused = requestedFocus ?? FocusedKey();
        requestedFocus = null;
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
        RestoreFocus(focused, generation);
    }

    private void Clear()
    {
        foreach (CardControl card in controls.Keys)
        {
            if (InteractionControl.IsUsable(card))
            {
                card.ClearInteractionControls();
            }
        }
        controls.Clear();
        focusKeys.Clear();
    }

    private void Add(
        IReadOnlyDictionary<int, List<CardControl>> visible,
        CardInteractionControlDescriptor descriptor)
    {
        if (!visible.TryGetValue(descriptor.CardId, out List<CardControl>? cards))
        {
            return;
        }
        CardControl? card = cards.LastOrDefault(InteractionControl.IsUsable);
        if (card is null)
        {
            return;
        }
        // Cards remain semantic inspection/drag surfaces. The decision dock
        // owns explicit target and resource toggles; only a standing board
        // action earns a card-local button. Prompt refreshes therefore do not
        // change table geometry merely because a draft asks for inputs.
        if (IsInHand(card) || descriptor.Intent != CardInteractionIntent.Action)
        {
            return;
        }
        var button = new Button
        {
            Name = $"Card{descriptor.CardId}{descriptor.Intent}",
            Text = descriptor.Text,
            TooltipText = Tooltip(descriptor.Intent),
            FocusMode = Control.FocusModeEnum.All,
        };
        var key = new BoardInteractionFocusKey(descriptor.CardId, descriptor.Intent);
        focusKeys.Add(button, key);
        button.Pressed += () =>
        {
            requestedFocus = key;
            Activate(card, descriptor.Intent);
        };
        card.AddInteractionControl(button);
        if (!controls.TryGetValue(card, out List<Button>? buttons))
        {
            buttons = [];
            controls.Add(card, buttons);
        }
        buttons.Add(button);
    }

    private static bool IsInHand(Node node)
    {
        for (Node? ancestor = node.GetParent(); ancestor is not null; ancestor = ancestor.GetParent())
        {
            if (ancestor.Name == "HandShelf")
            {
                return true;
            }
        }

        return false;
    }

    private BoardInteractionFocusKey? FocusedKey()
    {
        foreach (KeyValuePair<Button, BoardInteractionFocusKey> entry in focusKeys)
        {
            if (InteractionControl.IsUsable(entry.Key) && entry.Key.HasFocus())
            {
                return entry.Value;
            }
        }

        return null;
    }

    private void RestoreFocus(BoardInteractionFocusKey? requested, int generation)
    {
        if (requested is not { } prior)
        {
            return;
        }

        BoardInteractionFocusKey? key = BoardInteractionFocus.Restore(prior, focusKeys.Values);
        if (key is not { } next)
        {
            return;
        }

        // Keep only the stable value key across this boundary. The old button
        // is queued for deletion by Clear and must never receive deferred work.
        Callable.From(() => FocusWhenLayoutSettles(next, generation)).CallDeferred();
    }

    private void FocusWhenLayoutSettles(BoardInteractionFocusKey key, int generation)
    {
        if (!isCurrent() || generation != focusGeneration)
        {
            return;
        }

        Button? candidate = focusKeys
            .Where(entry => entry.Value == key && InteractionControl.IsUsable(entry.Key))
            .Select(entry => entry.Key)
            .FirstOrDefault();
        candidate?.GrabFocus();
    }

    private static string Tooltip(CardInteractionIntent intent) => intent switch
    {
        CardInteractionIntent.Target => "Choose this offered target.",
        CardInteractionIntent.Generator => "Use this offered resource generator.",
        _ => "Choose this card's offered action.",
    };

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
