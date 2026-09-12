using Marvel.Rules.Play;
using Marvel.Rules.State;
using static Marvel.Content.Behavior.CoreSceneStateMutation;
using static Marvel.Content.Behavior.CoreSceneValidation;

namespace Marvel.Content.Behavior;

/// <summary>A legal Core Set deal from which one behavioral transcript begins.</summary>

/// <summary>
/// Deals a complete legal Core Set game, then applies a small invariant-checked state vocabulary.
/// </summary>
/// <remarks>
/// This is specification infrastructure, not a second rules engine. The setup dataset and
/// <see cref="WorldSetup"/> decide which physical cards exist and where a legal game begins;
/// scene._operations can only rearrange those cards. They cannot allocate a card, transfer ownership,
/// replace an identity's signature set, or silently manufacture a boundary state.
/// </remarks>
internal static class CoreSceneStateMutation
{
    internal static void Damage(this CanonicalCoreScene scene, Card card, long damage)
    {
        if (damage < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(damage), "damage must not be negative");
        }

        if (!DeckTypes.IsInPlay(card.Area.Type)
            || !CardKinds.IsCharacter(scene.World.Facts.Kind(card.FaceId)))
        {
            throw new InvalidOperationException("damage can be arranged only on an in-play character");
        }

        long health = Rules.Play.DamagePlacement.Health(scene.World, scene.World.Facts, card);
        if (damage >= health)
        {
            throw new InvalidOperationException(
                $"{damage} damage would defeat '{card.FaceId}' with {health} health");
        }

        card.TakeDamage(damage - card.Damage);
    }

    internal static void Counters(this CanonicalCoreScene scene, Card card, string type, long count)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(type);
        string normalized = type.ToLowerInvariant();

        if (count < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(count), "token count must not be negative");
        }

        CardKind cardKind = scene.World.Facts.Kind(card.FaceId);
        string key = IsSchemeThreat(normalized, cardKind)
            ? ThreatCounterKey(scene, card, cardKind, count)
            : PrintedCounterKey(scene, card, normalized, count);
        long held = card.Tokens.GetValueOrDefault(key, 0);
        card.PlaceTokens(key, count - held);
    }

    private static string ThreatCounterKey(
        CanonicalCoreScene scene, Card card, CardKind kind, long count)
    {
        ValidateThreat(scene, card, kind, count);
        return "k_threat";
    }

    private static string PrintedCounterKey(
        CanonicalCoreScene scene, Card card, string type, long count)
    {
        if (!scene.World.Facts.CounterTypes(card.FaceId).Contains(type, StringComparer.Ordinal))
        {
            throw new InvalidOperationException($"'{card.FaceId}' does not print {type} counters");
        }
        ValidateUses(scene, card, type, count);
        if (scene.World.Facts.CounterMaximum(card.FaceId, type) is { } maximum
            && count > maximum)
        {
            throw new InvalidOperationException(
                $"'{card.FaceId}' cannot hold more than its printed {maximum} {type} counters");
        }
        return "c_" + type;
    }

    private static void ValidateUses(
        CanonicalCoreScene scene, Card card, string type, long count)
    {
        var uses = scene.World.Abilities.CounterPool(scene.World, card);
        if (uses?.Uses != true
            || !string.Equals(uses.Type, type, StringComparison.OrdinalIgnoreCase)
            || !DeckTypes.IsInPlay(card.Area.Type)
            || count > 0 && count <= uses.Starting)
        {
            return;
        }
        throw new InvalidOperationException(
            count == 0
                ? $"'{card.FaceId}' would be discarded with no {type} uses remaining"
                : $"'{card.FaceId}' cannot hold more than its printed {uses.Starting} {type} uses");
    }

    private static bool IsSchemeThreat(string type, CardKind kind) =>
        type == "threat"
        && kind is CardKind.MainScheme or CardKind.EncounterSideScheme or CardKind.PlayerSideScheme;

    private static void ValidateThreat(
        CanonicalCoreScene scene, Card card, CardKind kind, long count)
    {
        if (!DeckTypes.IsInPlay(card.Area.Type))
        {
            throw new InvalidOperationException("threat can be arranged only on an in-play scheme");
        }
        if (kind is CardKind.EncounterSideScheme or CardKind.PlayerSideScheme && count == 0)
        {
            throw new InvalidOperationException($"'{card.FaceId}' would be defeated at zero threat");
        }
        long target = scene.World.Facts.PrintedValue(
            card.FaceId, "TargetThreat", scene.World.Players, long.MaxValue);
        if (kind == CardKind.MainScheme && count >= target)
        {
            throw new InvalidOperationException(
                $"'{card.FaceId}' would advance at its target threat of {target}");
        }
    }

    internal static void AccelerationTokens(this CanonicalCoreScene scene, long count)
    {
        if (count < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(count),
                "acceleration token count must not be negative");
        }

        Card scheme = scene.World.TheCardIn(DeckType.MainSchemesArea)
            ?? throw new InvalidOperationException("the scene has no faceup main scheme");
        long held = scheme.Tokens.GetValueOrDefault(EncounterDeck.AccelerationToken);
        scheme.PlaceTokens(EncounterDeck.AccelerationToken, count - held);
    }

    internal static void Form(this CanonicalCoreScene scene, SetSceneForm operation)
    {
        Seat seat = scene.Player(operation.Seat);
        if (!seat.IdentityCard.Faces.Contains(operation.FaceId, StringComparer.Ordinal)
            || scene.World.Facts.Kind(operation.FaceId) is not (CardKind.Hero or CardKind.AlterEgo))
        {
            throw new ArgumentException(
                $"'{operation.FaceId}' is not a printed identity face for seat {operation.Seat}");
        }

        seat.IdentityCard.TurnTo(operation.FaceId);
    }

    internal static void Ready(Card card, bool ready)
    {
        if (!DeckTypes.IsInPlay(card.Area.Type))
        {
            throw new InvalidOperationException("readiness can be arranged only on an in-play card");
        }

        if (ready)
        {
            card.Refresh();
        }
        else
        {
            card.Exhaust();
        }
    }

    internal static void Status(this CanonicalCoreScene scene, Card host, string status)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(status);
        if (status is not (Statuses.Tough or Statuses.Stunned or Statuses.Confused))
        {
            throw new ArgumentException(
                $"'{status}' is not a rules-provided status card", nameof(status));
        }

        if (!DeckTypes.IsInPlay(host.Area.Type)
            || !CardKinds.IsCharacter(scene.World.Facts.Kind(host.FaceId)))
        {
            throw new InvalidOperationException(
                "a status card can be arranged only on an in-play character");
        }

        Card created = Statuses.Inflict(scene.World, scene.World.Facts, host, status)
            ?? throw new InvalidOperationException(
                $"'{host.FaceId}' cannot receive another {status} status card");
        scene._accountedOwners.Add(created.ObjectId, created.Owner);
    }

}
