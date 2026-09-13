using System.Collections.Immutable;
using Marvel.Rules.State;

namespace Marvel.Cards.Dsl;

/// <summary>Supported conditions with fixed authored arguments.</summary>
public enum AbilityConditionFact
{
    /// <summary>The current Special is the last in the ordered group.</summary>
    FinalStep,
    /// <summary>An ally can be played from a discard pile.</summary>
    CanMakeTheCall,
    /// <summary>The completed attack damaged its target.</summary>
    AttackDamaged,
    /// <summary>The game was set up in expert mode.</summary>
    InExpertMode,
    /// <summary>The resolver caused the defeat.</summary>
    DefeatedByYou,
    /// <summary>The resolver's identity defended the completed attack.</summary>
    HeroDefended,
    /// <summary>The current attack has no defender.</summary>
    UndefendedAttack,
    /// <summary>The defeat was caused by consequential damage.</summary>
    DefeatedByConsequentialDamage,
}
