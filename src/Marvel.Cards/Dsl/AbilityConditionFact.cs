using System.Collections.Immutable;
using Marvel.Rules.State;

namespace Marvel.Cards.Dsl;

/// <summary>Supported conditions with fixed authored arguments.</summary>
public enum AbilityConditionFact
{
    /// <summary>The current Special is the last in the ordered group.</summary>
    FinalStep,
    /// <summary>Scheduled work belongs to an enemy attack or its causal descendants.</summary>
    InAttackChain,
    /// <summary>An ally can be played from a discard pile.</summary>
    CanMakeTheCall,
    /// <summary>The completed attack damaged its target.</summary>
    AttackDamaged,
    /// <summary>The completed attack damaged the resolving player's identity.</summary>
    AttackDamagedYou,
    /// <summary>The game was set up in expert mode.</summary>
    InExpertMode,
    /// <summary>The resolver caused the defeat.</summary>
    DefeatedByYou,
    /// <summary>The resolver's identity defended the completed attack.</summary>
    HeroDefended,
    /// <summary>The resolver's identity made a basic defense against the completed attack.</summary>
    HeroBasicDefended,
    /// <summary>The current attack has no defender.</summary>
    UndefendedAttack,
    /// <summary>The defeat was caused by consequential damage.</summary>
    DefeatedByConsequentialDamage,
    /// <summary>The invoking window owns imminent damage from an attack.</summary>
    DamageFromAttack,
    /// <summary>The triggering revelation is from the encounter deck or a dealt queue.</summary>
    RevealedFromEncounterDeck,
}
