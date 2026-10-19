using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>Resolves a keyword for an attack's actor, source and basic-power identity.</summary>
public static class AttackKeywords
{
    /// <summary>Internal effect spelling; scoped grants never become global character keywords.</summary>
    public const string ScopedPrefix = "attackKeyword:";

    /// <summary>Whether this particular attack carries the keyword.</summary>
    public static bool Has(World world, Card attacker, Card source, string keyword, bool basic = false)
    {
        if (Keywords.Has(world, attacker, keyword, world.Facts)
            || source != attacker && Keywords.Has(world, source, keyword, world.Facts)) return true;
        var active = world.Effects.Active();
        if (active.Any(effect => Matches(effect))) return true;
        // An event's attack-scoped instruction describes its resolution, including
        // the offered attack before the event leaves hand. It grants nothing to its hero.
        return EffectiveCards.Kind(source, world.Facts) == CardKind.Event
            && world.ConstantAbilities.Constant(world, source).Any(effect =>
                effect.AttackRestriction is AttackKeywordRestriction.Source && Matches(effect));

        bool Matches(ContinuousEffect effect) => effect.Kind == ScopedPrefix + keyword
            && effect.AttackRestriction switch
            {
                AttackKeywordRestriction.Source => effect.AppliesTo(world, source),
                AttackKeywordRestriction.Basic => basic && effect.AppliesTo(world, attacker),
                AttackKeywordRestriction.SourceTrait trait => effect.AppliesTo(world, attacker)
                    && Traits.Has(world, source, trait.Trait, world.Facts),
                _ => false,
            };
    }
}
