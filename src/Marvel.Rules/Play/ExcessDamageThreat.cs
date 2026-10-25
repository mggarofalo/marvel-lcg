using Marvel.Rules.State;

namespace Marvel.Rules.Play;

/// <summary>A printed continuous instruction routing excess damage to a corresponding scheme.</summary>
public sealed record ExcessDamageThreat(Card Scheme, Card Source)
{
    /// <summary>Engine modifier spelling for the printed excess-damage instruction, not a game keyword.</summary>
    public const string Modifier = "excessDamageAsThreat";

    internal static ExcessDamageThreat? Capture(World world, AttackDamageRequest request)
    {
        // The damage must come from the named character, not a separate attack ability's source.
        if (!ReferenceEquals(request.Source, request.Attacker)) return null;
        return Capture(world, request.Source);
    }

    internal static ExcessDamageThreat? Capture(World world, Card damageSource)
    {
        var effect = world.Effects.Active().FirstOrDefault(effect =>
            effect.Kind == Modifier && effect.AppliesTo(world, damageSource));
        if (effect is null) return null;
        if (effect.Card is not { } source)
            throw new RulesNotImplementedException("excess damage threat routing requires its ability source");
        // Physical setup membership, rather than the active counter, identifies "corresponding".
        return new(EncounterSchemeRelations.Scheme(world, damageSource), world.Cards[source]);
    }

    internal EffectCompletion Schedule(World world, long excess, string trigger) =>
        Threat.Schedule(world, new ThreatBatch([Scheme], Source, excess, ThreatCause.CardAbility, trigger));
}
