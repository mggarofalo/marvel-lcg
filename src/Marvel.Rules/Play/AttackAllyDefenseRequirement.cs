using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>An attacked player's ally requirement owned by one enemy activation.</summary>
public static class AttackAllyDefenseRequirement
{
    private const string Kind = "requireAttackedPlayerAllyDefender";

    /// <summary>Install the printed requirement for the current attack and player.</summary>
    public static void Install(World world, Card source)
    {
        if (world.Attack is not { } attack || world.Activation is not { Attacking: true, Id: >= 0 } activation)
            throw new RulesNotImplementedException("an ally defense requirement needs a current attack activation");
        // The engine chooses an activation id, enemy id and identity id as the
        // persistent scope; another activation cannot borrow this restriction.
        world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect,
            Kind, Amount: attack.Enemy, Card: source.ObjectId,
            Affects: world.Seats[attack.Player].IdentityCard.ObjectId,
            Lasts: Duration.UntilEndOf(TimingPoints.EndOfActivation))
        { ActivationId = activation.Id });
    }

    internal static IReadOnlyList<Card> RequiredAllies(World world)
    {
        if (world.Attack is not { } attack || world.Activation is not { } activation
            || !world.Effects.Active().Any(effect => effect.Kind == Kind
                && effect.ActivationId == activation.Id && effect.Amount == attack.Enemy
                && effect.Affects == world.Seats[attack.Player].IdentityCard.ObjectId)) return [];
        return [.. AttackDefenderCandidates.For(world, world.Facts, attack.Player)
            .Where(card => EffectiveCards.Kind(card, world.Facts) == CardKind.Ally)];
    }

    internal static bool Allows(World world, Card defender)
    {
        var required = RequiredAllies(world);
        // A card ability can declare an exhausted ally (rr:defend-defense.3.3).
        // Normal declarations still get their readiness filter from Defenders.
        return required.Count == 0 || (defender.Area.Type == DeckType.AlliesArea
            && defender.Area.PlayArea == required[0].Area.PlayArea
            && EffectiveCards.Kind(defender, world.Facts) == CardKind.Ally);
    }
}
