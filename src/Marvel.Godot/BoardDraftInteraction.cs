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

    private bool IsVisibleCardAnchor(int affordanceId, int cardId) =>
        affordances.SingleOrDefault(affordance => affordance.Id == affordanceId)
            ?.Source?.CardId == cardId;
}
