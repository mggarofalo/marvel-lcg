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
        if (gesture.Card.TargetId is not { } source) return false;
        int? destination = board.CardAt(gesture.Position, gesture.Source);
        var targeting = new BoardSourceTargetInteraction(composer,
            CurrentOperations(panel, composer), Affordances(panel, composer));
        if (destination is { } target && targeting.Matches(source, target).Count > 0)
            return DragOnCard(panel, composer, gesture, source, target);
        return DragPlay(panel, composer, board, gesture, source, destination);
    }

    private static bool DragPlay(DecisionPanel panel, DecisionComposer composer,
        BoardRenderResult board, CardPointerGesture gesture, int source, int? destination)
    {
        var play = new BoardHandPlayInteraction(composer, CurrentOperations(panel, composer),
            Affordances(panel, composer));
        bool onLane = board.IsDroppedOnLivePlayerLane(composer.Prompt.Player, gesture.Position);
        IReadOnlyList<Marvel.View.AffordancePresentation> matches = play.Matches(source, destination,
            gesture.IsHandCard, onLane);
        int generation = panel.GetRenderGeneration();
        bool Select(int id)
        {
            if (!play.TrySelect(id, source, destination, gesture.IsHandCard, onLane)) return false;
            panel.RaiseDraftStarted();
            BoardInteractionBinder.RefreshDraft(panel, composer, generation, source);
            return true;
        }
        if (matches.Count == 0) return false;
        if (matches.Count == 1) return Select(matches[0].Id);
        BoardActionChoiceSurface.Show(gesture.Source, matches,
            () => panel.IsCurrentDraft(composer, generation), id => Select(id));
        return true;
    }

    internal static void PreviewDrag(DecisionPanel panel, DecisionComposer composer,
        BoardRenderResult board, CardPointerGesture gesture)
    {
        if (gesture.Card.TargetId is not { } source) return;
        IReadOnlyList<Marvel.View.AffordancePresentation> offers = Affordances(panel, composer);
        board.PresentDestinations(offers.Where(offer => offer.CardAnchorId == source && offer.Illegal is null)
            .SelectMany(offer => offer.TargetRequest?.Legal ?? []));
        int? destination = board.CardAt(gesture.Position, gesture.Source);
        var interaction = new BoardSourceTargetInteraction(composer, CurrentOperations(panel, composer), offers);
        IReadOnlyList<Marvel.View.AffordancePresentation> matches = destination is { } target
            ? interaction.Matches(source, target) : [];
        if (matches.Count > 0)
        {
            board.PresentGestureFeedback(string.Join(" or ", matches.Select(DecisionCopy.ActionSummary)));
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
        board.PresentGestureFeedback(matches.Count == 0
            ? "No offered action matches this drop. Your choices are unchanged."
            : string.Join(" or ", matches.Select(offer => DecisionCopy.ActionSummary(offer)
                + (offer.DeferredTargetSelection ? " Start play; choose a target after payment." : ""))));
    }

    private static bool DragOnCard(DecisionPanel panel, DecisionComposer composer,
        CardPointerGesture gesture, int source, int destination)
    {
        var interaction = new BoardSourceTargetInteraction(composer,
            CurrentOperations(panel, composer), Affordances(panel, composer));
        IReadOnlyList<Marvel.View.AffordancePresentation> matches = interaction.Matches(source, destination);
        int generation = panel.GetRenderGeneration();
        void Select(int id)
        {
            if (!interaction.TrySelect(id, source, destination)) return;
            panel.RaiseDraftStarted();
            BoardInteractionBinder.RefreshDraft(panel, composer, generation, source);
        }
        if (matches.Count == 0) return false;
        if (matches.Count == 1) Select(matches[0].Id);
        else BoardActionChoiceSurface.Show(gesture.Source, matches,
            () => panel.IsCurrentDraft(composer, generation), Select);
        return true;
    }

    internal static bool CanDrag(DecisionPanel panel, DecisionComposer composer,
        CardPointerGesture gesture)
    {
        IReadOnlyList<Marvel.View.AffordancePresentation> offers = Affordances(panel, composer);
        TableDraftBinding operations = CurrentOperations(panel, composer);
        return gesture.Card.TargetId is { } source
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
