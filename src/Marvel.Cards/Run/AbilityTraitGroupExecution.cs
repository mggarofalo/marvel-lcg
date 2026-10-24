using Marvel.Cards.Dsl;
using Marvel.Rules.Timing;
using Marvel.Rules.State;

namespace Marvel.Cards.Run;

/// <summary>Registers data-backed live trait sets rather than current recipients.</summary>
internal static class AbilityTraitGroupExecution
{
    internal static bool HasRecipients(AbilityEffect.GrantTraitCharacters grant, World world)
    {
        var scope = new ContinuousEffect(EffectSource.LastingEffect, "attack",
            Scope: ContinuousEffect.CharactersWithTrait + grant.Trait);
        return world.Cards.Any(card => scope.AppliesTo(world, card));
    }

    internal static AbilityImmediateResult Run(
        AbilityEffect.GrantTraitCharacters grant, AbilityImmediateContext context)
    {
        foreach (string field in grant.Fields)
            context.Admission.World.Effects.Register(new ContinuousEffect(
                EffectSource.LastingEffect, field, Amount: grant.Amount,
                Card: context.Admission.Source.ObjectId,
                Lasts: Duration.UntilEndOf(grant.Until),
                Scope: ContinuousEffect.CharactersWithTrait + grant.Trait));
        return new(true, true);
    }
}
