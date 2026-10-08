namespace Marvel.Rules.State;

/// <summary>Reads current physical relationships and authored persistent semantics without changing the world.</summary>
public static class PersistentCardFacts
{
    /// <summary>Returns nothing outside play; departing copies have no live relationship.</summary>
    public static PersistentCardDescription? Describe(World world, Card card)
    {
        if (!DeckTypes.IsInPlay(card.Area.Type)) return null;
        PersistentAbilityDescription abilities = world.Abilities.DescribePersistent(card);
        int controller = CardControl.ControllerOf(world, card);
        // These strings are a presentation contract chosen here, not rulebook vocabulary.
        bool attached = abilities.HasAttachmentInstruction || EffectiveCards.Kind(card, world.Facts) == CardKind.Attachment;
        var relation = attached && card.Area.Host >= 0
            ? new PersistentCardRelation("Attached", card.Area.Host, controller >= 0 ? controller : null)
            : new PersistentCardRelation(controller >= 0 ? "Controlled" : "Shared", null,
                controller >= 0 ? controller : null);
        return new(CardSourceSnapshot.Capture(card, world.Facts), relation,
            abilities.Abilities, abilities.HasUnresolvedAbilities);
    }
}
