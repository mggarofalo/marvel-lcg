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
internal static class CoreSceneValidation
{
    internal static void AccountCreatedCards(this CanonicalCoreScene scene)
    {
        foreach (var created in scene.World.Cards.Where(card => !scene._accountedOwners.ContainsKey(card.ObjectId)))
        {
            scene._accountedOwners.Add(created.ObjectId, created.Owner);
        }
    }

    internal static void ValidateWorld(this CanonicalCoreScene scene)
    {
        if (scene.World.Cards.Count != scene._accountedOwners.Count)
        {
            throw new InvalidOperationException(
                $"the scene accounts for {scene._accountedOwners.Count} cards and the world contains {scene.World.Cards.Count}");
        }

        int[] membership = new int[scene.World.Cards.Count];
        foreach (var area in scene.World.Areas.OrderBy(area => area.Id))
        {
            ValidateHost(scene, area);
            CountMembership(area, membership);
        }
        ValidateCards(scene, membership);
        ValidateUniqueness(scene);
    }

    private static void ValidateHost(CanonicalCoreScene scene, Area area)
    {
        if (area.Host < 0 || area.Cards.Count == 0 && area.Removed.Count == 0)
        {
            return;
        }
        if (area.Host >= scene.World.Cards.Count)
        {
            throw new InvalidOperationException($"hosted area {area.Id} names missing card {area.Host}");
        }
        Card host = scene.World.Cards[area.Host];
        if (!DeckTypes.IsInPlay(host.Area.Type) || host.Area.PlayArea != area.PlayArea)
        {
            throw new InvalidOperationException(
                $"hosted area {area.Id} does not share an in-play host's play area");
        }
    }

    private static void CountMembership(Area area, int[] membership)
    {
        foreach (Card card in area.Cards.Concat(area.Removed))
        {
            if (card.ObjectId < 0 || card.ObjectId >= membership.Length)
            {
                throw new InvalidOperationException($"area {area.Id} contains an unknown card");
            }
            membership[card.ObjectId]++;
            if (!ReferenceEquals(card.Area, area))
            {
                throw new InvalidOperationException(
                    $"card {card.ObjectId} names area {card.Area.Id} but is held by area {area.Id}");
            }
        }
    }

    private static void ValidateCards(CanonicalCoreScene scene, int[] membership)
    {
        for (int id = 0; id < scene.World.Cards.Count; id++)
        {
            Card card = scene.World.Cards[id];
            if (card.ObjectId != id)
            {
                throw new InvalidOperationException(
                    $"card index {id} contains object id {card.ObjectId}");
            }

            if (membership[id] != 1)
            {
                throw new InvalidOperationException(
                    $"card {id} is accounted for {membership[id]} times; expected exactly once");
            }

            if (card.Owner != scene._accountedOwners[id])
            {
                throw new InvalidOperationException(
                    $"card {id} changed ownership from {scene._accountedOwners[id]} to {card.Owner}");
            }
        }
    }

    private static void ValidateUniqueness(CanonicalCoreScene scene)
    {
        var inPlay = scene.World.Cards.Where(card => DeckTypes.IsInPlay(card.Area.Type)).ToList();
        for (int left = 0; left < inPlay.Count; left++)
        {
            for (int right = left + 1; right < inPlay.Count; right++)
            {
                if (Uniqueness.Matches(scene.World.Facts, inPlay[left], inPlay[right]))
                {
                    throw new InvalidOperationException(
                        $"matching unique cards {inPlay[left].ObjectId} and {inPlay[right].ObjectId} are both in play");
                }
            }
        }
    }

    internal static Seat Player(this CanonicalCoreScene scene, int seat) => seat >= 0 && seat < scene.World.Seats.Count
        ? scene.World.Seats[seat]
        : throw new ArgumentOutOfRangeException(nameof(seat), $"there is no player seat {seat}");

    internal static Card Host(this CanonicalCoreScene scene, int host)
    {
        if (host < 0 || host >= scene.World.Cards.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(host), $"there is no card {host}");
        }

        Card card = scene.World.Cards[host];
        if (!DeckTypes.IsInPlay(card.Area.Type))
        {
            throw new InvalidOperationException($"host card {host} is not in play");
        }

        return card;
    }

    internal static Card UpgradeHost(this CanonicalCoreScene scene, Card card, int player, int requested)
    {
        Seat seat = scene.Player(player);
        var eligible = CardPlayLegality.LegalAttachmentTargets(
            scene.World, scene.World.Facts, seat, card, scene.World.Abilities);
        if (eligible is null)
        {
            int identity = seat.IdentityCard.ObjectId;
            if (requested is not (-1) && requested != identity)
            {
                throw new InvalidOperationException(
                    $"'{card.FaceId}' is an ordinary upgrade and must attach to identity {identity}");
            }

            return seat.IdentityCard;
        }

        if (requested < 0 || !eligible.Contains(requested))
        {
            throw new InvalidOperationException(
                $"card {requested} is not a legal printed host for '{card.FaceId}'");
        }

        return scene.Host(requested);
    }

    internal static Card AttachmentHost(this CanonicalCoreScene scene, Card card, int requested)
    {
        Card host = scene.Host(requested);
        IReadOnlyList<int>? eligible = scene.World.Abilities.AttachmentTargets(scene.World, card);
        if (eligible is null || !eligible.Contains(host.ObjectId))
        {
            throw new InvalidOperationException(
                $"card {requested} is not the printed attach-to host for '{card.FaceId}'");
        }

        return host;
    }

    internal static void RequireHostCanMove(this CanonicalCoreScene scene,
        Card card, PlayArea destination, bool destinationInPlay)
    {
        var hosted = scene.World.Areas
            .Where(area => area.Host == card.ObjectId && area.Cards.Count > 0)
            .OrderBy(area => area.Id)
            .FirstOrDefault();
        if (hosted is null)
        {
            return;
        }

        if (!destinationInPlay || hosted.PlayArea != destination)
        {
            throw new InvalidOperationException(
                $"card {card.ObjectId} cannot move while area {hosted.Id} still holds a hosted card");
        }
    }

    internal static void RequireEntryLimits(this CanonicalCoreScene scene,
        Card card, PlayArea destination, bool destinationInPlay, int requestedHost)
    {
        if (!destinationInPlay || DeckTypes.IsInPlay(card.Area.Type))
        {
            return;
        }
        RequireAllyLimit(scene, card, destination);
        RequireRestrictedLimit(scene, card);
        RequirePerPlayerLimit(scene, card, destination, requestedHost);
    }

    private static void RequireAllyLimit(
        CanonicalCoreScene scene, Card card, PlayArea destination)
    {
        if (scene.World.Facts.Kind(card.FaceId) == CardKind.Ally && destination.IsPlayers)
        {
            int player = destination.Player;
            long limit = StateFields.Modified(
                scene.World,
                scene.World.Seats[player].IdentityCard,
                "ally_limit",
                scene.World.Facts,
                scene.World.Players);
            int held = scene.World.Areas
                .Where(area => area.Type == DeckType.AlliesArea
                    && area.PlayArea == destination)
                .Sum(area => area.Cards.Count);
            if (held >= limit)
            {
                throw new InvalidOperationException(
                    $"seat {player} already controls its ally limit of {limit}");
            }
        }
    }

    private static void RequireRestrictedLimit(CanonicalCoreScene scene, Card card)
    {
        if (StateFields.Modified(
                scene.World, card, "restricted", scene.World.Facts, scene.World.Players) > 0)
        {
            int held = scene.World.Cards.Count(candidate =>
                DeckTypes.IsInPlay(candidate.Area.Type)
                && candidate.Owner == card.Owner
                && StateFields.Modified(
                    scene.World, candidate, "restricted", scene.World.Facts, scene.World.Players) > 0);
            if (held >= StateFieldCatalog.RestrictedLimit)
            {
                throw new InvalidOperationException(
                    $"seat {card.Owner} already controls {held} restricted cards");
            }
        }
    }

    private static void RequirePerPlayerLimit(
        CanonicalCoreScene scene,
        Card card,
        PlayArea destination,
        int requestedHost)
    {
        if (card.Owner >= 0 && destination.IsPlayers)
        {
            IReadOnlyList<int>? targets = requestedHost >= 0 ? [requestedHost] : null;
            if (!CardPlayLegality.WithinPerPlayerLimit(
                    scene.World,
                    scene.World.Facts,
                    scene.World.Seats[destination.Player],
                    card,
                    targets,
                    scene.World.Abilities))
            {
                throw new InvalidOperationException(
                    $"seat {destination.Player} already controls the printed maximum of "
                    + $"'{scene.World.Facts.Title(card.FaceId)}'");
            }
        }
    }

    internal static void RequireStructuralRoleCanMove(this CanonicalCoreScene scene, Card card)
    {
        CardKind kind = scene.World.Facts.Kind(card.FaceId);
        if (kind is CardKind.Hero or CardKind.AlterEgo or CardKind.MainScheme
            or CardKind.EncounterVillain)
        {
            throw new InvalidOperationException(
                $"structural {kind} card {card.ObjectId} cannot be rearranged directly");
        }
    }

    internal static void RequireOwner(Card card, int owner)
    {
        if (card.Owner != owner)
        {
            throw new InvalidOperationException(
                $"card {card.ObjectId} ('{card.FaceId}') is owned by {card.Owner}, not seat {owner}");
        }
    }

    internal static void RequirePlayerKind(this CanonicalCoreScene scene, Card card, int seat, CardKind expected)
    {
        scene.Player(seat);
        RequireOwner(card, seat);
        CardKind actual = scene.World.Facts.Kind(card.FaceId);
        if (actual != expected)
        {
            throw new InvalidOperationException(
                $"'{card.FaceId}' is {actual}, not {expected}");
        }
    }

    internal static void RequirePlayerDeckCard(this CanonicalCoreScene scene, Card card)
    {
        if (scene.World.Facts.Kind(card.FaceId) is not (
            CardKind.Ally or CardKind.Event or CardKind.Resource or CardKind.Support or CardKind.Upgrade))
        {
            throw new InvalidOperationException(
                $"'{card.FaceId}' is not a player deck card");
        }
    }

    internal static void RequireEncounterCard(this CanonicalCoreScene scene, Card card)
    {
        RequireOwner(card, World.Scenario);
        if (scene.World.Facts.Kind(card.FaceId) is not (
            CardKind.Obligation or CardKind.Treachery or CardKind.Minion or CardKind.Attachment
            or CardKind.EncounterSideScheme or CardKind.Environment))
        {
            throw new InvalidOperationException(
                $"'{card.FaceId}' is not an encounter card");
        }
    }

    internal static void RequireScenarioKind(Card card, CardKind actual, CardKind expected)
    {
        RequireOwner(card, World.Scenario);
        if (actual != expected)
        {
            throw new InvalidOperationException(
                $"'{card.FaceId}' is {actual}, not {expected}");
        }
    }
}
