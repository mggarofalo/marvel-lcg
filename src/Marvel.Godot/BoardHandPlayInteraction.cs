using Marvel.Decisions;
using Marvel.Rules.Prompts;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Matches hand-card play destinations to explicit visible engine offers.</summary>
internal sealed class BoardHandPlayInteraction(DecisionComposer composer,
    TableDraftBinding operations, IReadOnlyList<AffordancePresentation> visible)
{
    internal IReadOnlyList<AffordancePresentation> SourceOffers(int source) =>
        [.. visible.Where(offer => offer.CardAnchorId == source && EnginePlay(offer.Id) is not null)];

    internal IReadOnlyList<AffordancePresentation> Matches(int source, int? destination,
        bool isHand, bool onPlayerLane) => !isHand ? []
            : [.. SourceOffers(source).Where(offer => AcceptsDestination(
                EnginePlay(offer.Id)!, destination, onPlayerLane))];

    internal bool TrySelect(int offerId, int source, int? destination, bool isHand, bool onPlayerLane) =>
        Matches(source, destination, isHand, onPlayerLane).Any(offer => offer.Id == offerId)
            && operations.TrySelectAffordance(offerId);

    private Affordance? EnginePlay(int id) => composer.Prompt.Affordances
        .SingleOrDefault(offer => offer.Id == id && offer.IsLegal && offer.PlaysCard);

    private bool AcceptsDestination(Affordance offer, int? destination, bool onPlayerLane)
    {
        // A fresh play gesture expresses initiation, not an assertion that the
        // object underneath is a legal recipient. An existing target draft is
        // different: an invalid destination must preserve its selections.
        if (composer.Selected?.Targets is not null)
            return false;
        return onPlayerLane || destination is not null || offer.PlaysCard;
    }
}
