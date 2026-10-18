using Marvel.Rules.State;

namespace Marvel.View;

/// <summary>Projects an explicitly public catalog face without creating live game authority.</summary>
public static class CatalogCardProjection
{
    /// <summary>Supplies the same complete printed face used by game inspection, with no target handle.</summary>
    public static BoardCardPresentation From(ICardFacts facts, string id)
    {
        var face = PrintedCardFaceProjection.From(facts, id);
        var back = facts.Kind(id) is CardKind.Ally or CardKind.Event or CardKind.Resource or CardKind.Support
            or CardKind.Upgrade or CardKind.Hero or CardKind.AlterEgo or CardKind.PlayerSideScheme ? CardBack.Player : CardBack.Encounter;
        var descriptor = new CardDescriptor(null, back, true, true, -1, face);
        return BoardCardPresentationFactory.Present(descriptor, "DiscardPile") with
        {
            Fields = [], EffectiveValues = new Dictionary<string, CardEffectiveValue>(),
        };
    }
}
