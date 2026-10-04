using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using static Marvel.Rules.Play.Game;

namespace Marvel.Rules.Play;

/// <summary>Engine-authored basic-power offers and known consequences.</summary>
internal static class GamePowerOffers
{
    /// <summary>Offers a basic power if it has a target or a status to clear.</summary>
    /// <remarks>
    /// Anchored to the character using it, which is the identity for a hero's
    /// own power and the ally for <c>rr:player-turn.4</c>. Two allies attacking
    /// are two options, because <c>rr:ally.2</c> permits "any number".
    /// </remarks>
    internal static void Offer(this Game game,
        List<Affordance> options, Card character, string verb, IReadOnlyList<Card> targets)
    {
        string cancellingStatus = string.Equals(
            verb, BasicPowers.AttackVerb, StringComparison.Ordinal)
                ? Statuses.Stunned
                : Statuses.Confused;
        bool cancelledByStatus = Statuses.Afflicted(
            game.world, game.facts, character, cancellingStatus);
        bool targetlessStatusAttempt = targets.Count == 0 && cancelledByStatus;
        if (targets.Count == 0 && !targetlessStatusAttempt)
        {
            return;
        }

        long power = StateFields.Modified(
            game.world, character,
            string.Equals(verb, BasicPowers.AttackVerb, StringComparison.Ordinal)
                ? "attack"
                : "thwart",
            game.facts, game.world.Players);
        bool ranged = StateFields.Modified(game.world, character, "ranged", game.facts, game.world.Players) > 0;
        options.Add(game.Anchored(verb, character, game.world.Seats[game.Active]) with
        {
            Description = PowerDescription(game, character, verb, power, ranged,
                cancelledByStatus, cancellingStatus),
            // Exactly one target: `rr:attack-player-ability-type.1` and
            // `rr:thwart.1` are each one enemy or one scheme. An ability that
            // hits several is a different thing (`.5`) and is not a basic power.
            Targets = new TargetRequest(
                [.. targets.Select(target => target.ObjectId)],
                Min: targetlessStatusAttempt ? 0 : 1,
                Max: targetlessStatusAttempt ? 0 : 1)
            {
                Details = targets.ToDictionary(target => target.ObjectId,
                    target => PowerTargetDetail(game, character, target, verb, power,
                        cancelledByStatus, cancellingStatus)),
            },
        });
    }

    private static string PowerDescription(Game game, Card character, string verb,
        long power, bool ranged, bool cancelled, string status) =>
        $"{game.facts.Title(character.FaceId)} · {verb} for {power}"
        + $" · Exhaust {game.facts.Title(character.FaceId)}"
        + ConsequentialDescription(game, character, verb, cancelled)
        + (ranged && verb == BasicPowers.AttackVerb ? " · Ranged" : string.Empty)
        + (cancelled ? $" · {status} cancels this attempt and is discarded" : string.Empty);

    private static string PowerTargetDetail(Game game, Card character, Card target,
        string verb, long power, bool cancelled, string status)
    {
        if (!cancelled) return game.BasicPowerTargetDetail(character, target, verb, power);
        string effect = verb == BasicPowers.AttackVerb
            ? "damage will be dealt" : "threat will be removed";
        return $"{status} cancels this attempt; no {effect}";
    }

    internal static string BasicPowerTargetDetail(this Game game,
        Card character, Card target, string verb, long power)
    {
        if (string.Equals(verb, BasicPowers.ThwartVerb, StringComparison.Ordinal))
        {
            long current = target.Tokens.GetValueOrDefault("k_threat");
            long result = Math.Max(0, current - power);
            long threshold = game.facts.PrintedValue(target.FaceId, "TargetThreat", game.world.Players);
            return threshold > 0
                ? $"{current}/{threshold} → {result}/{threshold} threat"
                : $"{current} → {result} threat";
        }

        return Damage.PreviewAttack(game.world, game.facts, character, character, target, power);
    }

    // rr:consequential-damage.1: "Consequential damage is dealt to an ally after resolving abilities"
    // that are triggered by the ally attacking or thwarting.
    // Later windows can change the final amount, so the offered current value is conditional.
    private static string ConsequentialDescription(Game game, Card character, string verb, bool cancelled)
    {
        if (cancelled || game.facts.Kind(character.FaceId) != CardKind.Ally) return string.Empty;
        bool attack = verb == BasicPowers.AttackVerb;
        long damage = game.facts.ConsequentialDamage(character.FaceId, attack ? "ATK" : "THW")
            + StateFields.Modified(game.world, character,
                attack ? "attack_consequential_damage" : "thwart_consequential_damage", game.facts, game.world.Players);
        return $" · {damage} consequential damage to this ally after resolving (before later effects)";
    }
}
