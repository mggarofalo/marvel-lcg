using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;

namespace Marvel.Rules.Timing;

/// <summary>Settles state-based health changes caused by continuous effects.</summary>
internal static class ContinuousEffectHealth
{
    /// <summary>Maximum hit points before a state change can alter constants.</summary>
    internal static IReadOnlyDictionary<int, long> CaptureCharacterHealthCore(this ContinuousEffects effects) => effects.world.Cards
        .Where(card => DeckTypes.IsInPlay(card.Area.Type)
            && CardKinds.IsCharacter(FacedownDrones.Kind(card, effects.world.Facts)))
        .OrderBy(card => card.ObjectId)
        .ToDictionary(
            card => card.ObjectId,
            card => Play.DamagePlacement.Health(effects.world, effects.world.Facts, card));

    /// <summary>Defeat characters made lethal by a health modifier ending.</summary>
    /// <remarks>
    /// <c>rr:hit-points.2.3</c> and <c>rr:hit-points.3.1</c> apply when a
    /// conditional constant switches off just as they do when its source
    /// leaves play. The before-image makes causality explicit: an unrelated
    /// state change does not rediscover a character that was already at zero.
    /// </remarks>
    internal static bool SettleLostHealthCore(
        this ContinuousEffects effects,
        IReadOnlyDictionary<int, long> before, string trigger,
        List<GameEvent> events)
    {
        ArgumentNullException.ThrowIfNull(before);
        ArgumentNullException.ThrowIfNull(trigger);
        ArgumentNullException.ThrowIfNull(events);

        bool suspended = false;
        foreach (var (id, formerHealth) in before.OrderBy(pair => pair.Key))
        {
            if (id < 0 || id >= effects.world.Cards.Count)
            {
                continue;
            }
            var card = effects.world.Cards[id];
            if (!DeckTypes.IsInPlay(card.Area.Type)
                || !CardKinds.IsCharacter(FacedownDrones.Kind(card, effects.world.Facts)))
            {
                continue;
            }

            long currentHealth = Play.DamagePlacement.Health(effects.world, effects.world.Facts, card);
            if (currentHealth < formerHealth
                && card.Damage < formerHealth
                && card.Damage >= currentHealth)
            {
                suspended |= effects.SettleHealthDefeat(card, trigger, events);
            }
        }
        return suspended;
    }

    internal static bool SettleHealthDefeat(this ContinuousEffects effects, Card card, string trigger, List<GameEvent> events)
    {
        if (StateFields.Modified(
                effects.world, card, "is_infinite_health", effects.world.Facts, effects.world.Players) > 0
            || card.Damage < Play.DamagePlacement.Health(effects.world, effects.world.Facts, card))
        {
            return false;
        }

        if (effects.healthDefeatPending.Contains(card.ObjectId))
        {
            return true;
        }

        // The Rules Reference names the condition but no event-stream verb;
        // the engine chooses this spelling to distinguish it from damage.
        const string verb = "Hit_Points_Reduced";
        var occurrence = effects.world.Agenda.Occurrence;
        if (!effects.world.DamageAbilities.WouldBeDefeated(
                effects.world, card, card, trigger, verb, by: -1,
                events: events, recordDefeatOn: occurrence))
        {
            effects.healthDefeatPending.Add(card.ObjectId);
            return true;
        }

        if (card.Damage >= Play.DamagePlacement.Health(effects.world, effects.world.Facts, card))
        {
            Defeat.Character(
                effects.world, effects.world.Facts, card, trigger, events,
                how: verb, recordOn: occurrence);
        }
        return false;
    }

    /// <summary>Marks a suspended health-loss defeat procedure as settled.</summary>
    internal static void CompleteHealthDefeatCore(this ContinuousEffects effects, Card card) =>
        effects.healthDefeatPending.Remove(card.ObjectId);
}
