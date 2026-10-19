using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Cards.Run;

/// <summary>Derives live trait grants without storing a snapshot on either card.</summary>
internal static class EnvironmentTraitGrants
{
    internal static IEnumerable<ContinuousEffect> For(World world, Card recipient)
    {
        // Printed Absorbing Man text: "gains the trait of each environment in play."
        // The constant-effect fixed point supplies the prior simultaneous pass
        // while Traits.Of reads other grants and losses on each environment.
        foreach (var environment in world.Cards.Where(card => DeckTypes.IsInPlay(card.Area.Type)
                     && !world.Effects.IsDeparting(card)
                     && EffectiveCards.Kind(card, world.Facts) == CardKind.Environment))
        {
            foreach (string trait in Traits.Of(world, environment, world.Facts))
            {
                yield return new ContinuousEffect(EffectSource.ConstantAbility,
                    Traits.Granted + trait, Card: recipient.ObjectId, Affects: recipient.ObjectId,
                    Lasts: Duration.WhileInPlay);
            }
        }
    }
}
