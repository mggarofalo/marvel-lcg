using Marvel.Rules.Play;
using Marvel.Rules.State;

namespace Marvel.Rules.Timing;

/// <summary>Resolves captured physical recipients while rechecking live keyword eligibility.</summary>
internal static class RetaliationEligibility
{
    internal static Card? Character(World world, RetaliationRecipient recipient)
    {
        Card original = world.Cards[recipient.Attack.Card];
        if (original.Incarnation != recipient.Attack.Incarnation) return null;
        if (DeckTypes.IsInPlay(original.Area.Type)) return original;
        if (!recipient.Attack.Facts.IsVillain) return null;
        var successors = Stages(world, original).Where(card => card.Area.Id == recipient.Area
            && DeckTypes.IsInPlay(card.Area.Type)
            && CardKinds.IsVillain(EffectiveCards.Kind(card, world.Facts))
            && world.Facts.Title(card.FaceId) == world.Facts.Title(original.FaceId)).ToArray();
        return successors.Length switch
        {
            0 => null,
            1 => successors[0],
            _ => throw new RulesNotImplementedException("retaliate requires one same-character villain successor"),
        };
    }

    private static IEnumerable<Card> Stages(World world, Card original)
    {
        if (world.EncounterTopology is not { } topology) return world.Cards;
        // rr:villain-defeat.3.1: "The new stage of the villain is considered
        // the same character as the defeated stage for purposes of card abilities."
        // Physical stage membership identifies that villain independently of titles.
        return topology.Groups.SingleOrDefault(group => group.Stages.Contains(original))?.Stages
            ?? throw new RulesNotImplementedException("retaliate recipient has no configured villain stages");
    }

    internal static Card? Attacker(World world, RetaliationContext context)
    {
        var attacker = world.Cards[context.Attacker];
        return attacker.Incarnation == context.AttackerIncarnation
            && DeckTypes.IsInPlay(attacker.Area.Type) ? attacker : null;
    }

    internal static long Amount(World world, Card character) => StateFields.Modified(
        world, character, "retaliate", world.Facts, world.Players);
}
