using Marvel.Rules.Play;
using Marvel.Rules.State;

namespace Marvel.Rules.Timing;

/// <summary>How a continuous effect got into the game, and therefore how it leaves.</summary>
public enum EffectSource
{
    /// <summary>
    /// A constant ability. Active as soon as its card enters play and while it
    /// remains in play — <c>rr:ability</c>, "Constant Abilities".
    /// </summary>
    ConstantAbility,

    /// <summary>
    /// A lasting effect, which persists past the ability that created it for a
    /// stated duration — <c>rr:lasting-effects.1</c>.
    /// </summary>
    LastingEffect,

    /// <summary>
    /// A delayed effect, which resolves once when its timing point or condition
    /// occurs — <c>rr:delayed-effect.1</c>.
    /// </summary>
    DelayedEffect,
}
