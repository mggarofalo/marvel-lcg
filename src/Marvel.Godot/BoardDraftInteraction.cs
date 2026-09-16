using Marvel.Decisions;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Resolves stable visible ids into one legal operation on a shared draft.</summary>
internal sealed class BoardDraftInteraction
{
    private readonly DecisionComposer composer;
    private readonly TableDraftBinding operations;
    private readonly IReadOnlyList<AffordancePresentation> affordances;

    internal BoardDraftInteraction(
        DecisionComposer composer,
        TableDraftBinding operations,
        IReadOnlyList<AffordancePresentation> affordances)
    {
        this.composer = composer ?? throw new ArgumentNullException(nameof(composer));
        this.operations = operations ?? throw new ArgumentNullException(nameof(operations));
        this.affordances = affordances ?? throw new ArgumentNullException(nameof(affordances));
    }

    internal BoardDraftMutation TryActivate(int? id, bool isHandCard)
    {
        if (isHandCard || id is null)
        {
            return BoardDraftMutation.None;
        }

        if (operations.TryToggleTarget(id.Value))
        {
            return BoardDraftMutation.Target;
        }

        if (operations.TryToggleGenerator(id.Value))
        {
            return BoardDraftMutation.Generator;
        }

        int[] actions = composer.Prompt.Affordances
            .Where(option => option.IsLegal && IsVisibleCardAnchor(option.Id, id.Value))
            .Select(option => option.Id)
            .ToArray();
        return actions.Length == 1 && operations.TrySelectAffordance(actions[0])
            ? BoardDraftMutation.Affordance
            : BoardDraftMutation.None;
    }

    internal BoardDraftMutation TryToggleGenerator(int? id)
    {
        if (id is null || !VisibleGenerator(id.Value))
        {
            return BoardDraftMutation.None;
        }
        return operations.TryToggleGenerator(id.Value)
            ? BoardDraftMutation.Generator
            : BoardDraftMutation.None;
    }

    internal BoardDraftMutation TryToggleTarget(int? id)
    {
        if (id is null || composer.Selected?.Targets is not { } request
            || !request.Legal.Contains(id.Value))
        {
            return BoardDraftMutation.None;
        }
        return operations.TryToggleTarget(id.Value)
            ? BoardDraftMutation.Target
            : BoardDraftMutation.None;
    }

    internal IReadOnlyList<AffordancePresentation> VisibleActions(int cardId) =>
        [.. affordances.Where(affordance => affordance.Illegal is null
            && affordance.CardAnchorId == cardId)];

    internal BoardDraftMutation TrySelectAction(int affordanceId, int cardId) =>
        VisibleActions(cardId).Any(affordance => affordance.Id == affordanceId)
            && operations.TrySelectAffordance(affordanceId)
                ? BoardDraftMutation.Affordance
                : BoardDraftMutation.None;

    internal BoardDraftMutation TryPlay(int? id, bool isHandCard, bool droppedOnPlayerLane)
    {
        if (!isHandCard || !droppedOnPlayerLane || id is null)
        {
            return BoardDraftMutation.None;
        }

        int[] plays = composer.Prompt.Affordances
            .Where(option => option.IsLegal
                && IsVisibleCardAnchor(option.Id, id.Value)
                && string.Equals(option.Verb, "Play", StringComparison.Ordinal))
            .Select(option => option.Id)
            .ToArray();
        return plays.Length == 1 && operations.TrySelectAffordance(plays[0])
            ? BoardDraftMutation.Affordance
            : BoardDraftMutation.None;
    }

    internal bool CanPlay(int? id, bool isHandCard)
    {
        if (!isHandCard || id is null)
        {
            return false;
        }
        return composer.Prompt.Affordances.Count(option => option.IsLegal
            && IsVisibleCardAnchor(option.Id, id.Value)
            && string.Equals(option.Verb, "Play", StringComparison.Ordinal)) == 1;
    }

    private bool IsVisibleCardAnchor(int affordanceId, int cardId) =>
        affordances.SingleOrDefault(affordance => affordance.Id == affordanceId) is { } affordance
            && affordance.CardAnchorId == cardId;

    private bool VisibleGenerator(int id) => composer.SelectedCost >= 0
        && composer.Selected is { } selected
        && composer.SelectedCost < selected.CostOptions.Count
        && selected.CostOptions[composer.SelectedCost].Generators
            .Any(generator => generator.Effect == id)
        && affordances.Any(affordance => affordance.Id == selected.Id);
}
