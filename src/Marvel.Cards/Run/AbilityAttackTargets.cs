using Marvel.Rules.Play;
using Marvel.Rules.State;

namespace Marvel.Cards.Run;

/// <summary>The enemies a card ability's attacking character may attack.</summary>
internal static class AbilityAttackTargets
{
    /// <summary>
    /// <see cref="BasicPowers.Attackable(World, ICardFacts, int, Card)"/> for the
    /// character that performs <paramref name="source"/>'s attack.
    /// </summary>
    internal static IReadOnlyList<Card> For(World world, int player, Card source) =>
        Attacker(world, player, source) is { } attacker
            ? BasicPowers.Attackable(world, world.Facts, player, attacker)
            : BasicPowers.Attackable(world, world.Facts, player);

    /// <summary>The character that would perform <paramref name="source"/>'s attack.</summary>
    /// <remarks>
    /// rr:you-your.12 and .14 make an event's or upgrade's attack one "performed by
    /// that player's identity"; rr:you-your.15 says an ally's attacks "are not
    /// considered to be performed by that player's identity". A prohibition that
    /// binds only an identity (<see cref="AttackPermissions"/>) therefore leaves an
    /// ally's attack untouched. A source no player resolves has no performer.
    /// </remarks>
    internal static Card? Attacker(World world, int player, Card source) =>
        player < 0 || player >= world.Players
            ? null : LabeledAbilities.Performer(world, world.Facts, player, source);
}
