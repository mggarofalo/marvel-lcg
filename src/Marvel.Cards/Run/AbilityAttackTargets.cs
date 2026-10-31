using Marvel.Rules.Play;
using Marvel.Rules.State;

namespace Marvel.Cards.Run;

/// <summary>The enemies a card ability's attacking character may attack.</summary>
internal static class AbilityAttackTargets
{
    /// <summary>
    /// <see cref="BasicPowers.Attackable(World, ICardFacts, int)"/> narrowed by
    /// <see cref="Permits"/> for the character that performs <paramref name="source"/>'s attack.
    /// </summary>
    internal static IReadOnlyList<Card> For(World world, int player, Card source) =>
        [.. BasicPowers.Attackable(world, world.Facts, player).Where(card => Permits(world, player, source, card))];

    /// <summary>
    /// Whether the character performing <paramref name="source"/>'s attack may attack
    /// <paramref name="enemy"/>. Live, projected and traced attack-target reads all
    /// ask this one question.
    /// </summary>
    internal static bool Permits(World world, int player, Card source, Card enemy) =>
        Attacker(world, player, source) is not { } attacker || AttackPermissions.CanAttack(world, attacker, enemy);

    /// <summary>The character that would perform <paramref name="source"/>'s attack.</summary>
    /// <remarks>
    /// rr:you-your.12 and .14 make an event's or upgrade's attack one "performed by
    /// that player's identity"; rr:you-your.15 says an ally's attacks "are not
    /// considered to be performed by that player's identity". A prohibition that
    /// binds only an identity (<see cref="AttackPermissions"/>) therefore leaves an
    /// ally's attack untouched. A source no player resolves has no performer.
    /// </remarks>
    internal static Card? Attacker(World world, int player, Card source) =>
        player < 0
            ? null : LabeledAbilities.Performer(world, world.Facts, player, source);
}
