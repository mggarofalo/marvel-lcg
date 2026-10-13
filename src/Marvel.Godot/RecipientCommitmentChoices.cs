using Godot;
using Marvel.Decisions;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Source-local explicit recipient commitments for one current engine offer.</summary>
internal static class RecipientCommitmentChoices
{
    internal static bool Show(Control source, AffordancePresentation offer,
        Func<bool> isCurrent, Action<int> commit)
    {
        if (!offer.DirectRecipientCommit || offer.TargetRequest is not { Details: { } details } request)
            return false;
        // These are display entries in the existing source-local surface. Their keys
        // are recipient IDs, never replacement engine offer handles.
        AffordancePresentation[] entries = [.. request.Legal.Where(details.ContainsKey).Select(id => offer with
        {
            Id = id, DisplayLabel = details[id], Description = null,
            CostDescription = null, CostOptions = [], Costs = [],
        })];
        BoardActionChoiceSurface.Show(source, entries, isCurrent, commit, recipientCommitments: true);
        return entries.Length > 0;
    }

    internal static bool Select(DecisionComposer composer, TableDraftBinding binding, int offerId, int recipient)
    {
        var offer = composer.Prompt.Affordances.SingleOrDefault(candidate => candidate.Id == offerId);
        if (offer is not { DirectRecipientCommit: true, CostOptions.Count: 0, Targets: { } request }
            || !request.Allows([recipient]) || !binding.IsCurrent()) return false;
        return binding.TrySelectAffordance(offerId) && binding.TryAddTarget(recipient)
            && composer.Targets.Count == 1 && composer.TryBuild(out _, out _);
    }
}
