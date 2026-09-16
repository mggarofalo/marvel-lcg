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
    private Action<int>? activateContextual;
    private Container? contextualHost;
    private int focusGeneration;
    private BoardInteractionFocusKey? requestedFocus;

    internal BoardCardInteractionControls(Func<bool> isCurrent) => this.isCurrent = isCurrent;

    internal void Track(CardControl card, BoardCardPresentation presentation, bool isHand) =>
        presentations[card] = (presentation, isHand);

    internal void Bind(Func<CardPointerGesture, bool> handler) =>
        activate = handler ?? throw new ArgumentNullException(nameof(handler));

    internal void BindContextual(Action<int> handler) =>
        activateContextual = handler ?? throw new ArgumentNullException(nameof(handler));

    internal void RegisterContextualHost(Container host) => contextualHost = host;

    internal void Present(
        IReadOnlyDictionary<int, List<CardControl>> visible,
        DecisionComposer? composer,
        PromptPresentation? prompt)
    {
        if (!isCurrent() || composer is null || prompt is null)
        {
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
        ClearContextualActions();
        var descriptors = BoardInteractionControlProjection.From(composer, prompt).ToList();
        AddDecline(descriptors, visible, composer, prompt);
        foreach (CardInteractionControlDescriptor descriptor in descriptors)
        {
            Add(visible, descriptor);
        }
        AddContextualActions(prompt);
        RestoreFocus(focused, generation);
    }

    private void AddContextualActions(PromptPresentation? prompt)
    {
        if (prompt is null || !InteractionControl.IsUsable(contextualHost))
        {
            return;
        }
        foreach (AffordancePresentation affordance in prompt.Affordances.Where(candidate =>
                     candidate.CardAnchorId is null && candidate.Illegal is null))
        {
            var action = new Button
            {
                Name = $"ContextAction{affordance.Id}",
                Text = $"◇ {affordance.Label}",
                TooltipText = affordance.Description ?? affordance.Label,
                FocusMode = Control.FocusModeEnum.All,
                CustomMinimumSize = new Vector2(164, 44),
                ThemeTypeVariation = GodotThemeVariations.LegalTargetButton,
            };
            int id = affordance.Id;
            action.Pressed += () => activateContextual?.Invoke(id);
            contextualHost!.AddChild(action);
        }
    }

    private void ClearContextualActions()
    {
        if (!InteractionControl.IsUsable(contextualHost))
        {
            return;
        }
        foreach (Node child in contextualHost!.GetChildren())
        {
            contextualHost.RemoveChild(child);
            child.QueueFree();
        }
    }

    private static void AddDecline(
        List<CardInteractionControlDescriptor> descriptors,
        IReadOnlyDictionary<int, List<CardControl>> visible,
        DecisionComposer? composer,
        PromptPresentation? prompt)
    {
        if (composer?.Prompt.Cancellable != true || prompt is null)
        {
            return;
        }
        int? host = prompt.Affordances
            .Where(affordance => affordance.Illegal is null)
            .Select(affordance => affordance.Source?.CardId)
            .FirstOrDefault(id => id is not null && visible.ContainsKey(id.Value))
            ?? visible.Keys.OrderBy(id => id).Cast<int?>().FirstOrDefault();
        if (host is { } cardId)
        {
            descriptors.Add(new CardInteractionControlDescriptor(
                cardId, CardInteractionIntent.Decline, "PASS", CardInteractionCue.OfferedAction));
        }
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
        var button = new Button
        {
            Name = $"Card{descriptor.CardId}{descriptor.Intent}",
            Text = descriptor.Text,
            TooltipText = CardInteractionControlStyle.Tooltip(descriptor.Intent),
            FocusMode = Control.FocusModeEnum.All,
            ZIndex = CardInteractionControlStyle.Layer(descriptor.Intent),
            ZAsRelative = false,
        };
        var key = new BoardInteractionFocusKey(descriptor.CardId, descriptor.Intent);
        button.SetMeta("spatial_control_z", button.ZIndex);
        focusKeys.Add(button, key);
        button.Pressed += () =>
        {
            requestedFocus = key;
            Activate(card, descriptor.Intent, descriptor.Option);
        };
        if (!CardInteractionControlPlacement.Place(card, button, controls))
        {
            focusKeys.Remove(button);
            button.QueueFree();
            return;
        }
        card.HideRedundantActionCueLabel();
        card.ZIndex = Math.Max(
            card.ZIndex,
            descriptor.Intent == CardInteractionIntent.Submit ? 120 : 100);
        card.SetMeta("spatial_interaction_z", card.ZIndex);
        if (descriptor.Intent == CardInteractionIntent.Submit)
        {
            card.MoveToFront();
        }
        if (!controls.TryGetValue(card, out List<Button>? buttons))
        {
            buttons = [];
            controls.Add(card, buttons);
        }
        buttons.Add(button);
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
