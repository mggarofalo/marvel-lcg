using static Marvel.Rules.Play.Attack;
using static Marvel.Rules.Play.AttackCompletion;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>Establishes and validates defenders named by card abilities.</summary>
internal static class AttackAbilityDefense
{
    /// <summary>Whether a player may begin resolving a defense-labeled ability.</summary>
    /// <remarks>
    /// Outside an attack the label changes no roles —
    /// <c>rr:defend-defense.4.8</c> — so the ability remains legal. During an
    /// attack, <c>.4.6</c> locks defense abilities to the player already
    /// defending, whether the defender is that player's identity or ally.
    /// </remarks>
    internal static bool CanUseDefenseAbility(World world, int player)
    {
        ArgumentNullException.ThrowIfNull(world);
        if (world.Attack is not { Defender: >= 0 } attack)
        {
            return true;
        }

        return world.Cards[attack.Defender].Area.PlayArea == PlayArea.Of(player);
    }

    /// <summary>Establish the roles created by a defense-labeled ability.</summary>
    /// <remarks>
    /// This runs before the ability's effect. It neither exhausts the identity
    /// nor marks a basic defense, so DEF is not applied —
    /// <c>rr:defend-defense.4.1</c>, <c>.4.3</c>, and <c>.4.4</c>.
    /// </remarks>
    internal static void BeginDefenseAbility(World world, int player)
        => BeginDefenseAbility(world, player, world.Seats[player].IdentityCard);

    /// <summary>Establish the roles for a defense performed by an attributed card.</summary>
    internal static void BeginDefenseAbility(World world, int player, Card performer)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(performer);
        if (world.Attack is not { } attack)
        {
            // `rr:defend-defense.4.8`: the label may resolve outside an attack
            // without making an identity the defender of anything.
            return;
        }

        if (!CanUseDefenseAbility(world, player))
        {
            throw new RulesNotImplementedException(
                $"player {player} cannot defend an attack already defended by another player");
        }

        if (attack.Defender >= 0)
        {
            // `rr:defend-defense.4.7`: a defense ability remains legal while
            // this player's ally defends, but the identity does not replace it.
            return;
        }

        bool character = EffectiveCards.Kind(performer, world.Facts) is
            CardKind.Hero or CardKind.AlterEgo or CardKind.Ally;
        if (!character)
        {
            // `rr:support.3` excludes support defenses from the identity. A
            // support is not a character, so it performs the labeled effect
            // without becoming the defending character of the attack.
            return;
        }

        world.Attack = attack with
        {
            Defender = performer.ObjectId,
            Target = performer.ObjectId,
            Player = player,
            BasicDefense = false,
        };
        if (world.Activation is { Attacking: true } activation)
        {
            world.Activation = activation with { Player = player };
        }
    }

    /// <summary>Whether a card instruction can declare this character the defender.</summary>
    /// <remarks>
    /// Card-declared defenders do not use the ordinary step-2 readiness check:
    /// <c>rr:defend-defense.2.2</c> and <c>.3.3</c> expressly permit an ability
    /// that declares without exhausting to name an exhausted hero or ally.
    /// A defense-labeled ability may already have established the same
    /// character as a non-basic defender before its printed effect reaches the
    /// declaration, so naming that same character remains legal.
    /// </remarks>
    internal static bool CanDeclareByAbility(
        World world, ICardFacts facts, Card defender, int replaceableDefender = -1)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(defender);

        if (world.Attack is not { } attack
            || !DeckTypes.IsInPlay(defender.Area.Type)
            || defender.Area.PlayArea.Player < 0)
        {
            return false;
        }

        var kind = EffectiveCards.Kind(defender, facts);
        return kind is CardKind.Hero or CardKind.Ally
            && (attack.Defender < 0
                || attack.Defender == defender.ObjectId
                || attack.Defender == replaceableDefender);
    }

    /// <summary>Apply a card instruction that declares a hero or ally the defender.</summary>
    /// <remarks>
    /// <c>rr:defend-defense.2.1</c> makes a card-declared hero a basic
    /// defender, including its DEF reduction. <c>.3.2</c> makes a
    /// card-declared ally the defender without a DEF reduction. Exhaustion is
    /// deliberately not performed here: it is a separate printed instruction,
    /// and <c>.2.2</c>/<c>.3.3</c> allow declarations that explicitly happen
    /// without exhausting even when the character is already exhausted.
    /// </remarks>
    internal static void DeclareByAbility(
        World world, ICardFacts facts, Card defender, int replaceableDefender = -1)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(defender);

        if (!CanDeclareByAbility(world, facts, defender, replaceableDefender))
        {
            throw new RulesNotImplementedException(
                $"card {defender.ObjectId} cannot be declared the defender of the current attack");
        }

        var attack = Current(world);
        if (attack.Defender >= 0
            && attack.Defender == replaceableDefender
            && attack.Defender != defender.ObjectId)
        {
            // Mutant Protectors is the first printed shape: its defense label
            // makes the identity the defender, then its text declares an ally.
            // The official ruling retains that identity as the non-basic
            // defender if the ally leaves before damage. A bounded effect keeps
            // that provenance saveable without adding a second defender role.
            world.Effects.Register(new ContinuousEffect(
                EffectSource.LastingEffect,
                Kind: DefenseFallback,
                Amount: attack.Defender,
                Affects: defender.ObjectId,
                Lasts: Duration.UntilEndOf(TimingPoints.EndOfAttack)));
        }
        int player = defender.Area.PlayArea.Player;
        world.Attack = attack with
        {
            Defender = defender.ObjectId,
            Target = defender.ObjectId,
            Player = player,
            BasicDefense = EffectiveCards.Kind(defender, facts) == CardKind.Hero,
        };
        if (world.Activation is { Attacking: true } activation)
        {
            world.Activation = activation with { Player = player };
        }
    }

}
