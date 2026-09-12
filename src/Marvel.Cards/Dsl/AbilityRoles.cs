using Marvel.Rules.Play;
using Marvel.Rules.Timing;

namespace Marvel.Cards.Dsl;

/// <summary>Which card may fill an occurrence's actor or target role.</summary>
public static class AbilityRoles
{
    /// <summary>The card carrying the ability.</summary>
    public const string This = "this";

    /// <summary>The card this ability's card is attached to.</summary>
    public const string AttachedTo = "attachedTo";

    /// <summary>
    /// A player-controlled card. A player card also requires the same
    /// controller; an encounter card binds its resolver to this role's
    /// controller.
    /// </summary>
    public const string You = "you";

    /// <summary>A villain.</summary>
    public const string Villain = "villain";

    /// <summary>A minion.</summary>
    public const string Minion = "minion";

    /// <summary>A hero.</summary>
    public const string Hero = "hero";

    /// <summary>An ally.</summary>
    public const string Ally = "ally";

    /// <summary>Any card controlled by a player — <c>rr:friendly</c>.</summary>
    public const string Friendly = "friendly";

    /// <summary>A villain or minion.</summary>
    public const string Enemy = "enemy";

    /// <summary>Every role matcher this vocabulary has.</summary>
    public static IReadOnlySet<string> All { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        This, AttachedTo, You, Villain, Minion, Hero, Ally, Friendly, Enemy,
    };
}
