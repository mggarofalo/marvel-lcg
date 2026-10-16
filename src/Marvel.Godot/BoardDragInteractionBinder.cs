using Godot;
using Marvel.Decisions;

namespace Marvel.Godot;

/// <summary>Stages drag intentions through exact prompt-bound offers and target operations.</summary>
internal static class BoardDragInteractionBinder
{
    internal static bool Drag(
        DecisionPanel panel,
        DecisionComposer composer,
        BoardRenderResult board,
        CardPointerGesture gesture)
    {
        if (panel.PaymentModalOpen || gesture.Card.TargetId is not { } source) return false;
        var targeting = new BoardSourceTargetInteraction(composer,
            CurrentOperations(panel, composer), Affordances(panel, composer));
        int? destination = board.TargetSnap.At(board, gesture, targeting.Destinations(source));
        if (destination is { } target && targeting.Matches(source, target).Count > 0)
            return DragOnCard(panel, composer, board, gesture.Source, source, target);
        return DragPlay(panel, composer, board, gesture, source, destination);
    }

    private static bool DragPlay(DecisionPanel panel, DecisionComposer composer,
        BoardRenderResult board, CardPointerGesture gesture, int source, int? destination)
        => ChoosePlay(panel, composer, gesture.Source, new BoardPlayChoiceIntent(source, destination,
            gesture.IsHandCard, board.IsDroppedOnLivePlayerLane(composer.Prompt.Player, gesture.Position)));

    private static bool ChoosePlay(DecisionPanel panel, DecisionComposer composer, Control sourceControl,
        BoardPlayChoiceIntent intent)
    {
        var play = new BoardHandPlayInteraction(composer, CurrentOperations(panel, composer),
            Affordances(panel, composer));
        IReadOnlyList<Marvel.View.AffordancePresentation> matches = play.Matches(intent.Source, intent.Destination,
            intent.IsHandCard, intent.OnPlayerLane);
        if (!panel.BoardChoices.Matches(matches)) return false;
        int generation = panel.GetRenderGeneration();
        bool Select(int id)
        {
            if (!play.TrySelect(id, intent.Source, intent.Destination, intent.IsHandCard, intent.OnPlayerLane)) return false;
            panel.RaiseDraftStarted();
            BoardInteractionBinder.RefreshDraft(panel, composer, generation, intent.Source);
            return true;
        }
        if (matches.Count == 0) return false;
        if (matches.Count == 1) return Select(matches[0].Id);
        panel.BoardChoices.Show(sourceControl, composer, intent.Source, matches, id => Select(id),
            replacement => ChoosePlay(panel, composer, replacement, intent));
        return true;
    }

    internal static void PreviewDrag(DecisionPanel panel, DecisionComposer composer,
        BoardRenderResult board, CardPointerGesture gesture)
    {
        if (panel.PaymentModalOpen || gesture.Card.TargetId is not { } source) return;
        IReadOnlyList<Marvel.View.AffordancePresentation> offers = Affordances(panel, composer);
        var interaction = new BoardSourceTargetInteraction(composer, CurrentOperations(panel, composer), offers);
        int[] destinations = [.. interaction.Destinations(source)];
        int? destination = board.TargetSnap.At(board, gesture, destinations);
        board.PresentDestinations(destinations);
        IReadOnlyList<Marvel.View.AffordancePresentation> matches = destination is { } target
            ? interaction.Matches(source, target) : [];
        if (matches.Count > 0)
        {
            board.PresentDragDestination(gesture, true);
            board.PresentGestureFeedback(string.Join(" or ", matches.Select(offer =>
                DecisionCopy.ActionSummary(offer) + (offer.DeferredTargetSelection
                    ? " Activate first; choose the recipient next." : ""))));
            return;
        }
        PreviewPlay(panel, composer, board, gesture, source, destination, offers);
    }

    private static void PreviewPlay(DecisionPanel panel, DecisionComposer composer,
        BoardRenderResult board, CardPointerGesture gesture, int source, int? destination,
        IReadOnlyList<Marvel.View.AffordancePresentation> offers)
    {
        var play = new BoardHandPlayInteraction(composer, CurrentOperations(panel, composer), offers);
        IReadOnlyList<Marvel.View.AffordancePresentation> matches = play.Matches(source, destination,
            gesture.IsHandCard, board.IsDroppedOnLivePlayerLane(composer.Prompt.Player, gesture.Position));
        board.PresentDragDestination(gesture, matches.Count > 0);
        board.PresentGestureFeedback(matches.Count == 0
            ? "No offered action matches this drop. Your choices are unchanged."
            : string.Join(" or ", matches.Select(offer => DecisionCopy.ActionSummary(offer)
                + (offer.DeferredTargetSelection ? " Start play; choose a target after payment." : ""))));
    }

    private static bool DragOnCard(DecisionPanel panel, DecisionComposer composer,
        BoardRenderResult board, Control sourceControl, int source, int destination)
    {
        var interaction = new BoardSourceTargetInteraction(composer,
            CurrentOperations(panel, composer), Affordances(panel, composer));
        IReadOnlyList<Marvel.View.AffordancePresentation> matches = interaction.Matches(source, destination);
        if (!panel.BoardChoices.Matches(matches)) return false;
        int generation = panel.GetRenderGeneration();
        void Select(int id)
        {
            if (!interaction.TrySelect(id, source, destination)) return;
            Control recipient = board.VisibleCardControls().FirstOrDefault(card => card.TargetId == destination)
                ?? (Control)sourceControl;
            board.AnchorDraft(recipient.GetGlobalRect().GetCenter(), id);
            panel.RaiseDraftStarted();
            BoardInteractionBinder.RefreshDraft(panel, composer, generation, source);
        }
        if (matches.Count == 0) return false;
        if (matches.Count == 1) Select(matches[0].Id);
        else panel.BoardChoices.Show(sourceControl, composer, source, matches, Select,
            replacement => ReopenTargetChoice(panel, composer, replacement, source, destination));
        return true;
    }

    private static void ReopenTargetChoice(DecisionPanel panel, DecisionComposer composer,
        Control sourceControl, int source, int destination)
    {
        if (panel.CurrentBoard is { } board) DragOnCard(panel, composer, board, sourceControl, source, destination);
    }

    internal static bool CanDrag(DecisionPanel panel, DecisionComposer composer,
        CardPointerGesture gesture)
    {
        IReadOnlyList<Marvel.View.AffordancePresentation> offers = Affordances(panel, composer);
        TableDraftBinding operations = CurrentOperations(panel, composer);
        return !panel.PaymentModalOpen && gesture.Card.TargetId is { } source
            && ((gesture.IsHandCard && new BoardHandPlayInteraction(composer, operations, offers)
                    .SourceOffers(source).Count > 0)
                || new BoardSourceTargetInteraction(composer, operations, offers).CanDrag(source));
    }

    private static TableDraftBinding CurrentOperations(DecisionPanel panel, DecisionComposer composer) =>
        panel.BindTableDraft(composer, panel.GetRenderGeneration());

    private static IReadOnlyList<Marvel.View.AffordancePresentation> Affordances(
        DecisionPanel panel, DecisionComposer composer) =>
        Marvel.View.PromptPresentation.From(composer.Prompt, panel.world!).Affordances;
}
