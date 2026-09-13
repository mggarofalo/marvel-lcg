using Marvel.Rules.Events;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;

namespace Marvel.Rules.Timing;

/// <summary>What a card is waiting to do, and what happens when it does.</summary>
/// <remarks>
/// The seam between the timing rules and the cards. Everything in
/// <see cref="Offering"/> is the Rules Reference and none of it knows what any
/// card says.
/// </remarks>
public interface IAbilityDescriptions
{
    /// <summary>How to describe one ability to a player who may take it.</summary>
    /// <param name="world">The world.</param>
    /// <param name="ability">The ability being offered.</param>
    Affordance Describe(World world, PendingAbility ability);
}
