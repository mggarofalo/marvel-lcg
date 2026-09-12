using Marvel.Rules.Play;
using Marvel.Rules.Timing;

namespace Marvel.Cards.Dsl;

/// <summary>The players an ability can name.</summary>
/// <remarks>
/// Shared between the trigger's <c>player</c> and the effect tree's, so the
/// same phrase means the same seat wherever a card writes it.
/// </remarks>
public static class AbilityPlayers
{
    /// <summary>The seat the occurrence happened to.</summary>
    public const string TriggerPlayer = "trigger.player";

    /// <summary>The seat resolving the ability.</summary>
    public const string You = "you";

    /// <summary>The seat that controls the ability's card.</summary>
    public const string Controller = "controller";
}
