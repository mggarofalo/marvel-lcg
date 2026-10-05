using Marvel.Decisions;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Matches a source/destination intention to explicit offered target identities.</summary>
internal sealed class BoardSourceTargetInteraction
{
    private readonly TableDraftBinding operations;
    private readonly IReadOnlyList<AffordancePresentation> offers;
    private readonly DecisionComposer composer;

    internal BoardSourceTargetInteraction(DecisionComposer composer, TableDraftBinding operations,
        IReadOnlyList<AffordancePresentation> offers)
    {
        this.composer = composer;
        this.operations = operations;
        this.offers = offers;
    }

    internal IReadOnlyList<AffordancePresentation> Matches(int source, int destination) =>
        [.. offers.Where(offer => offer.Illegal is null && offer.CardAnchorId == source
            && offer.TargetRequest?.Legal.Contains(destination) == true)];

    internal bool CanDrag(int source) => offers.Any(offer => offer.Illegal is null
        && offer.CardAnchorId == source && offer.TargetRequest is { Legal.Count: > 0 });
    private bool CanCompose(int offerId, int destination)
    {
        var trial = new DecisionComposer(composer.Prompt);
        var trialOperations = new TableDraftOperations(trial);
        if (!trialOperations.TrySelectAffordance(offerId)) return false;
        if (composer.Selected?.Id == offerId) trial.SelectTargets(composer.Targets);
        return AlreadySelected(trial, destination) || trial.Selected!.Targets!.IsGrouped
            || trialOperations.TryAddTarget(destination);
    }

    private static bool AlreadySelected(DecisionComposer draft, int destination) =>
        draft.Selected?.Targets?.AllowRepeated == false && draft.Targets.Contains(destination);

    internal bool TrySelect(int offerId, int source, int destination)
    {
        if (!Matches(source, destination).Any(offer => offer.Id == offerId)
            || !CanCompose(offerId, destination)
            || (composer.Selected?.Id != offerId && !operations.TrySelectAffordance(offerId))) return false;
        if (AlreadySelected(composer, destination)) return true;
        // A grouped offer still needs its complete ordered group from the same composer.
        if (composer.Selected!.Targets!.IsGrouped) return true;
        return operations.TryAddTarget(destination);
    }
}
