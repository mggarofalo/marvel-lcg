using Marvel.Rules.State;

namespace Marvel.Rules.Play;

/// <summary>Checks printed prerequisites against current identity and controlled characters.</summary>
internal static class PrintedCardPlayPrerequisites
{
    internal static bool Permits(World world, ICardFacts facts, Seat seat, Card card)
    {
        // rr:form-change-form.7: "[type] form only" requires that identity form.
        if (facts.RequiredForm(card.FaceId) is { } form && !Forms.In(world, seat, facts, form)) return false;
        // rr:identity.4: only "the faceup side" is in play; effective traits
        // include continuous grants and losses on that current identity.
        if (facts.RequiredIdentityTrait(card.FaceId) is { } identityTrait
            && !Traits.Has(world, seat.IdentityCard, identityTrait, facts)) return false;
        return facts.RequiredControlledCharacterTrait(card.FaceId) is not { } trait
            || world.Cards.Any(candidate => Qualifies(world, facts, seat, candidate, trait));
    }

    private static bool Qualifies(World world, ICardFacts facts, Seat seat, Card candidate, string trait) =>
        // rr:ownership-and-control.8: "only refers to cards in play currently under that player's control."
        DeckTypes.IsInPlay(candidate.Area.Type)
        && CardControl.ControllerOf(world, candidate) == seat.Index
        // rr:character: "Identities (heroes and alter-egos), allies, villains, and minions are all characters."
        && EffectiveCards.Kind(candidate, facts) is CardKind.Hero or CardKind.AlterEgo
            or CardKind.Ally or CardKind.EncounterVillain or CardKind.Minion
        && Traits.Has(world, candidate, trait, facts);
}
