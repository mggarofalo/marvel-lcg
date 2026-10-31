using Marvel.Rules.Events;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>Readies the captured defending hero after its exact attack causes no damage.</summary>
public static class UndamagedDefenseReady
{
    /// <summary>Engine-chosen delayed instruction spelling.</summary>
    public const string Kind = "ReadyUndamagedDefender";

    /// <summary>Whether this occurrence describes this player's committed hero defense.</summary>
    public static bool CanInstall(World world, Occurrence occurrence, int player) =>
        player >= 0 && player < world.Seats.Count
        && occurrence.Is(HeroDefenseTiming.Defends)
        && world.Activation is { Attacking: true } activation
        && world.Agenda.items.Any(item => ReferenceEquals(item.Occurrence, occurrence)
            && item.Step.Operation is HeroDefenseOperation defense && defense.Activation == activation.Id)
        && MatchesHero(world, occurrence, player);

    private static bool MatchesHero(World world, Occurrence occurrence, int player)
    {
        var hero = world.Seats[player].IdentityCard;
        return world.Attack is { } attack
            && occurrence.Player == player && occurrence.Actor == hero.ObjectId
            && occurrence.ActorFacts?.Incarnation == hero.Incarnation
            && occurrence.Target == attack.Enemy
            && occurrence.TargetFacts?.Incarnation == world.Cards[attack.Enemy].Incarnation
            && attack.Defender == hero.ObjectId
            && EffectiveCards.Kind(hero, world.Facts) == CardKind.Hero;
    }

    /// <summary>Retains the physical hero and activation through the existing effect registry.</summary>
    public static void Install(World world, Occurrence occurrence, Card source, int player)
    {
        if (!CanInstall(world, occurrence, player))
            throw new RulesNotImplementedException("delayed undamaged defense ready requires a committed hero defense");
        var hero = world.Seats[player].IdentityCard;
        world.Effects.Register(new(EffectSource.DelayedEffect, Kind,
            Card: source.ObjectId, Affects: hero.ObjectId,
            Lasts: Duration.NextTime(Steps.AttackEnds))
        { TargetIncarnation = hero.Incarnation });
    }

    internal static void Resolve(World world, ContinuousEffect effect, string condition, List<GameEvent> events)
    {
        if (UndamagedHero(world, effect) is not { } hero) return;
        var source = world.Cards[effect.Card!.Value];
        if (!CardReadying.CanReady(world, hero, source)) return;
        // rr:delayed-effect.1 resolves this "before responses to that point or
        // condition may be used"; an observed would-ready window is scheduled
        // ahead of the attack-end response window for the same reason.
        CardReadying.Ready(world, new CardReadyBatch([hero], source, condition), events);
    }

    /// <summary>The captured hero, if it is still in play and its exact attack placed no damage on it.</summary>
    internal static Card? UndamagedHero(World world, ContinuousEffect effect)
    {
        if (world.Activation?.Id != effect.ActivationId || world.Attack is not { } attack
            || effect.Affects is not { } id || id < 0 || id >= world.Cards.Count) return null;
        var hero = world.Cards[id];
        // Printed 09015 says "from that attack"; boost/ability damage is not an attack placement.
        if (hero.Incarnation != effect.TargetIncarnation || !DeckTypes.IsInPlay(hero.Area.Type)
            || TookDamage(attack, effect, id)) return null;
        return hero;
    }

    private static bool TookDamage(EnemyAttack attack, ContinuousEffect effect, int id) =>
        attack.ReceivedDamage.Any(item => item.Recipient.Card == id
            && item.Recipient.Incarnation == effect.TargetIncarnation);
}
