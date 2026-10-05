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
    private readonly BoardContextualInteractionControls contextual;
    private int focusGeneration;
    private BoardInteractionFocusKey? requestedFocus;

    internal BoardCardInteractionControls(Func<bool> isCurrent)
    {
        this.isCurrent = isCurrent;
        contextual = new BoardContextualInteractionControls(isCurrent);
    }

    internal void Track(CardControl card, BoardCardPresentation presentation, bool isHand) =>
        presentations[card] = (presentation, isHand);

    internal void Bind(Func<CardPointerGesture, bool> handler) =>
        activate = handler ?? throw new ArgumentNullException(nameof(handler));

    internal void BindContextual(
        Action<int> handler, Action decline, Action submit, Action cancel,
        Action<int> selectCost, Func<string> commitmentLabel) =>
        contextual.Bind(handler, decline, submit, cancel, selectCost, commitmentLabel);

    internal void RegisterContextualHost(Container host) => contextual.Register(host);

    internal void Present(
        IReadOnlyDictionary<int, List<CardControl>> visible,
        DecisionComposer? composer,
        PromptPresentation? prompt)
    {
        if (!isCurrent()) return;
        if (composer is null || prompt is null)
        {
            checked { focusGeneration++; }
            Clear();
            contextual.Clear();
            foreach (CardControl card in visible.Values.SelectMany(cards => cards).Where(InteractionControl.IsUsable))
                card.SetInteractionCue(CardInteractionCue.None);
            return;
        }

        int generation = checked(++focusGeneration);
        BoardInteractionFocusKey? focused = requestedFocus ?? FocusedKey();
        IReadOnlyDictionary<int, CardInteractionCue> cues =
            BoardInteractionCueProjection.From(composer, prompt);
        foreach ((int id, List<CardControl> cards) in visible)
        {
            foreach (CardControl card in cards.Where(InteractionControl.IsUsable))
            {
                card.SetInteractionCue(cues.GetValueOrDefault(id));
            }
        }
        Clear();
        contextual.Clear();
        if (CardPaymentPresentation.UsesModal(composer)) return;
        var descriptors = BoardInteractionControlProjection.From(composer, prompt).ToList();
        var installedActions = new HashSet<int>();
        foreach (CardInteractionControlDescriptor descriptor in descriptors)
        {
            if (Add(visible, descriptor) && descriptor.Intent == CardInteractionIntent.Action)
                installedActions.Add(descriptor.CardId);
        }
        contextual.Present(composer, prompt, installedActions);
        RestoreFocus(focused, generation);
    }

    private void Clear()
    {
        foreach ((CardControl card, List<Button> attached) in controls)
        {
            foreach (Button button in attached.Where(InteractionControl.IsUsable))
            {
                button.GetParent()?.RemoveChild(button);
                button.QueueFree();
            }
            if (InteractionControl.IsUsable(card))
            {
                card.ClearInteractionControls();
                card.RemoveMeta("spatial_interaction_z");
                card.RemoveMeta("spatial_interaction_rotation");
                if (card.HasMeta("spatial_resting_z"))
                {
                    card.ZIndex = card.GetMeta("spatial_resting_z").AsInt32();
                }
            }
        }
        controls.Clear();
        focusKeys.Clear();
    }

    private bool Add(
        IReadOnlyDictionary<int, List<CardControl>> visible,
        CardInteractionControlDescriptor descriptor)
    {
        CardControl? card = VisibleCard(visible, descriptor.CardId);
        if (card is null)
        {
            return false;
        }
        Button button = CreateButton(descriptor);
        int generation = focusGeneration;
        var key = new BoardInteractionFocusKey(descriptor.CardId, descriptor.Intent);
        focusKeys.Add(button, key);
        button.Pressed += () =>
        {
            if (!isCurrent() || generation != focusGeneration) return;
            requestedFocus = key;
            Activate(card, descriptor.Intent, descriptor.Option);
        };
        if (!CardInteractionControlPlacement.Place(card, button))
        {
            focusKeys.Remove(button);
            button.QueueFree();
            return false;
        }
        button.SetMeta("spatial_control_z", button.ZIndex);
        card.HideRedundantActionCueLabel();
        PresentControlLayer(card, descriptor.Intent);
        if (!controls.TryGetValue(card, out List<Button>? buttons))
        {
            buttons = [];
            controls.Add(card, buttons);
        }
        buttons.Add(button);
        return true;
    }

    private static CardControl? VisibleCard(IReadOnlyDictionary<int, List<CardControl>> visible, int id) =>
        visible.TryGetValue(id, out List<CardControl>? cards)
            ? cards.LastOrDefault(candidate => InteractionControl.IsUsable(candidate) && candidate.IsVisibleInTree())
            : null;

    private static Button CreateButton(CardInteractionControlDescriptor descriptor)
    {
        var button = new Button
        {
            Name = $"Card{descriptor.CardId}{descriptor.Intent}",
            Text = descriptor.Text,
            TooltipText = CardInteractionControlStyle.Tooltip(descriptor.Intent),
            FocusMode = Control.FocusModeEnum.All,
            ZIndex = CardInteractionControlStyle.Layer(descriptor.Intent),
            ZAsRelative = false,
            TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis,
            AccessibilityName = descriptor.Text,
            ThemeTypeVariation = GodotThemeVariations.LegalTargetButton,
        };

        button.TooltipText = descriptor.Description ?? (descriptor.Intent == CardInteractionIntent.Action
            ? descriptor.Text
            : CardInteractionControlStyle.Tooltip(descriptor.Intent));
        if (descriptor.Intent == CardInteractionIntent.Action)
        {
            button.SelfModulate = new Color(1, 1, 1, 0.72f);
            button.MouseEntered += () => button.SelfModulate = Colors.White;
            button.MouseExited += () => button.SelfModulate = button.HasFocus()
                ? Colors.White : new Color(1, 1, 1, 0.72f);
            button.FocusEntered += () => button.SelfModulate = Colors.White;
            button.FocusExited += () => button.SelfModulate = new Color(1, 1, 1, 0.72f);
        }
        return button;
    }

    private static void PresentControlLayer(CardControl card, CardInteractionIntent intent)
    {
        if (!card.HasMeta("spatial_hand_index"))
            card.ZIndex = Math.Max(card.ZIndex,
                intent == CardInteractionIntent.Submit ? 120 : 100);
        card.SetMeta("spatial_interaction_z", card.ZIndex);
        if (intent == CardInteractionIntent.Submit)
        {
            card.MoveToFront();
        }
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
        if (candidate is not null)
        {
            candidate.GrabFocus();
            InteractionControl.ResetDisabledScrollAncestors(candidate);
            candidate.GetTree().CreateTimer(0.05).Timeout += () =>
                ConfirmFocusAfterLayout(key, generation);
        }
    }

    private void ConfirmFocusAfterLayout(BoardInteractionFocusKey key, int generation)
    {
        if (!isCurrent() || generation != focusGeneration) return;
        Button? candidate = focusKeys
            .Where(entry => entry.Value == key && InteractionControl.IsUsable(entry.Key))
            .Select(entry => entry.Key)
            .FirstOrDefault();
        candidate?.GrabFocus();
        if (candidate is not null)
        {
            InteractionControl.ResetDisabledScrollAncestors(candidate);
        }
        if (candidate?.HasFocus() == true) requestedFocus = null;
    }

    private void Activate(CardControl card, CardInteractionIntent intent, int? option)
    {
        if (!isCurrent() || !InteractionControl.IsUsable(card)
            || !presentations.TryGetValue(card, out var presentation))
        {
            return;
        }
        activate?.Invoke(new CardPointerGesture(
            presentation.Card, card, presentation.IsHand,
            card.GetGlobalRect().GetCenter(), intent, option));
    }
}
