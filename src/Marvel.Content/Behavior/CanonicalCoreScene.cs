using Marvel.Rules.Play;
using Marvel.Rules.State;

namespace Marvel.Content.Behavior;

/// <summary>A legal Core Set deal from which one behavioral transcript begins.</summary>
public sealed record CoreSceneRequest(
    string Authority,
    string Campaign,
    IReadOnlyList<string> Heroes,
    uint Seed,
    IReadOnlyList<string>? ModularSets = null);

/// <summary>One physical card selected by printed face and zero-based copy number.</summary>
public sealed record SceneCard(string FaceId, int Copy = 0);

/// <summary>The legal places the behavioral state vocabulary may arrange directly.</summary>
public enum SceneZone
{
#pragma warning disable CS1591, SA1602
    PlayerDeck,
    PlayerHand,
    PlayerDiscard,
    Ally,
    Support,
    Upgrade,
    Attachment,
    EngagedMinion,
    Obligation,
    EncounterDeck,
    EncounterDiscard,
    SideScheme,
    Environment,
    SetAside,
#pragma warning restore CS1591, SA1602
}

/// <summary>A typed destination; <see cref="Seat"/> is required for player places.</summary>
public sealed record SceneDestination(SceneZone Zone, int Seat = World.Scenario, int Host = -1);

/// <summary>One deterministic arrangement applied after the legal deal.</summary>
public abstract record CoreSceneOperation
{
    /// <summary>The stable operation name included in a construction failure.</summary>
    public abstract string Name { get; }
}

/// <summary>Moves one existing physical card without changing its ownership.</summary>
public sealed record MoveSceneCard(SceneCard Card, SceneDestination Destination)
    : CoreSceneOperation
{
    /// <inheritdoc />
    public override string Name => "move-card";
}

/// <summary>Stacks selected cards on a player's deck; the first id is the next card drawn.</summary>
public sealed record StackPlayerDeck(int Seat, IReadOnlyList<SceneCard> TopFirst, bool DiscardOthers = false)
    : CoreSceneOperation
{
    /// <inheritdoc />
    public override string Name => "stack-player-deck";
}

/// <summary>Stacks selected encounter cards; the first id is the next card drawn.</summary>
public sealed record StackEncounterDeck(IReadOnlyList<SceneCard> TopFirst)
    : CoreSceneOperation
{
    /// <inheritdoc />
    public override string Name => "stack-encounter-deck";
}

/// <summary>Sets the damage already on an in-play character.</summary>
public sealed record SetSceneDamage(SceneCard Card, long Damage)
    : CoreSceneOperation
{
    /// <inheritdoc />
    public override string Name => "set-damage";
}

/// <summary>Sets scheme threat or one printed all-purpose counter type to an exact value.</summary>
public sealed record SetSceneCounters(SceneCard Card, string Type, long Count)
    : CoreSceneOperation
{
    /// <inheritdoc />
    public override string Name => "set-counters";
}

/// <summary>Shows one of the selected player's two printed identity faces.</summary>
public sealed record SetSceneForm(int Seat, string FaceId)
    : CoreSceneOperation
{
    /// <inheritdoc />
    public override string Name => "set-form";
}

/// <summary>Sets whether an in-play card is ready.</summary>
public sealed record SetSceneReady(SceneCard Card, bool Ready)
    : CoreSceneOperation
{
    /// <inheritdoc />
    public override string Name => "set-ready";
}

/// <summary>Creates one rules-provided status card on an in-play character.</summary>
public sealed record GiveSceneStatus(SceneCard Host, string Status)
    : CoreSceneOperation
{
    /// <inheritdoc />
    public override string Name => "give-status";
}

/// <summary>
/// Deals a complete legal Core Set game, then applies a small invariant-checked state vocabulary.
/// </summary>
/// <remarks>
/// This is specification infrastructure, not a second rules engine. The setup dataset and
/// <see cref="WorldSetup"/> decide which physical cards exist and where a legal game begins;
/// operations can only rearrange those cards. They cannot allocate a card, transfer ownership,
/// replace an identity's signature set, or silently manufacture a boundary state.
/// </remarks>
public sealed class CanonicalCoreScene
{
    private readonly Dictionary<int, int> accountedOwners;
    private readonly List<CoreSceneOperation> operations = [];

    private CanonicalCoreScene(CoreSceneRequest request, World world)
    {
        Request = request;
        World = world;
        accountedOwners = world.Cards.ToDictionary(card => card.ObjectId, card => card.Owner);
        ValidateWorld();
    }

    /// <summary>The authority obligation this scene is meant to distinguish.</summary>
    public CoreSceneRequest Request { get; }

    /// <summary>The arranged engine state.</summary>
    public World World { get; }

    /// <summary>The arrangements already applied, in transcript order.</summary>
    public IReadOnlyList<CoreSceneOperation> Operations => operations;

    /// <summary>Deals one scene exclusively from the supplied Core setup and card catalogs.</summary>
    public static CanonicalCoreScene Deal(
        CoreSceneRequest request,
        Setup.SetupCatalog setup,
        ICardFacts facts,
        ICardAbilities abilities)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(setup);
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(abilities);
        ArgumentNullException.ThrowIfNull(request.Heroes);
        if (string.IsNullOrWhiteSpace(request.Authority))
        {
            throw new ArgumentException("a scene must name its authority obligation", nameof(request));
        }

        if (request.Heroes.Count == 0)
        {
            throw new ArgumentException("a scene must contain at least one hero", nameof(request));
        }

        var order = Setup.Dealer.DealOrder(
            setup, request.Campaign, request.Heroes, request.ModularSets, facts);
        var world = WorldSetup.Deal(
            facts,
            Setup.Blueprints.From(order, facts),
            request.Heroes.Select(hero => setup.Hero(hero).Name).ToList(),
            request.Seed,
            abilities,
            expert: setup.Campaign(request.Campaign).Expert);
        return new CanonicalCoreScene(request, world);
    }

    /// <summary>Applies one arrangement and validates the complete world immediately.</summary>
    public CanonicalCoreScene Apply(CoreSceneOperation operation)
    {
        ArgumentNullException.ThrowIfNull(operation);
        try
        {
            switch (operation)
            {
                case MoveSceneCard move:
                    Move(Find(move.Card), move.Destination);
                    break;
                case StackPlayerDeck stack:
                    StackPlayer(stack);
                    break;
                case StackEncounterDeck stack:
                    StackEncounter(stack);
                    break;
                case SetSceneDamage damage:
                    Damage(Find(damage.Card), damage.Damage);
                    break;
                case SetSceneCounters counters:
                    Counters(Find(counters.Card), counters.Type, counters.Count);
                    break;
                case SetSceneForm form:
                    Form(form);
                    break;
                case SetSceneReady ready:
                    Ready(Find(ready.Card), ready.Ready);
                    break;
                case GiveSceneStatus status:
                    Status(Find(status.Host), status.Status);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(operation));
            }

            ValidateWorld();
            operations.Add(operation);
            return this;
        }
        catch (CoreSceneConstructionException)
        {
            throw;
        }
        catch (Exception error)
        {
            throw new CoreSceneConstructionException(
                Request.Authority, operation.Name, error.Message, error);
        }
    }

    /// <summary>Resolves a physical card deterministically without depending on its current zone.</summary>
    public Card Find(SceneCard reference)
    {
        ArgumentNullException.ThrowIfNull(reference);
        if (reference.Copy < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(reference), "copy must not be negative");
        }

        var matches = World.Cards
            .Where(card => card.Faces.Contains(reference.FaceId, StringComparer.Ordinal))
            .OrderBy(card => card.ObjectId)
            .ToList();
        return reference.Copy < matches.Count
            ? matches[reference.Copy]
            : throw new KeyNotFoundException(
                $"no copy {reference.Copy} of printed face '{reference.FaceId}' exists in this deal");
    }

    private void StackPlayer(StackPlayerDeck operation)
    {
        Seat seat = Player(operation.Seat);
        if (operation.TopFirst.Count == 0)
        {
            throw new ArgumentException(
                "a canonical player-deck boundary must leave at least one card to draw");
        }

        var selected = Distinct(operation.TopFirst);
        foreach (var card in selected)
        {
            RequireOwner(card, operation.Seat);
            RequirePlayerDeckCard(card);
            RequireHostCanMove(card, PlayArea.Of(operation.Seat), destinationInPlay: false);
        }

        if (operation.DiscardOthers)
        {
            Area discard = World.AreaOf(
                DeckType.DiscardPile, PlayArea.Of(operation.Seat), cardOwner: operation.Seat);
            foreach (var card in seat.Deck.Cards.Where(card => !selected.Contains(card)).ToList())
            {
                RequireHostCanMove(card, PlayArea.Of(operation.Seat), destinationInPlay: false);
                World.MoveToTop(card, discard);
            }
        }

        foreach (var card in selected.AsEnumerable().Reverse())
        {
            World.MoveToTop(card, seat.Deck);
        }
    }

    private void StackEncounter(StackEncounterDeck operation)
    {
        var selected = Distinct(operation.TopFirst);
        foreach (var card in selected)
        {
            RequireEncounterCard(card);
            RequireHostCanMove(card, PlayArea.Villains, destinationInPlay: false);
        }

        Area deck = World.AreaOf(DeckType.EncounterDeck);
        foreach (var card in selected.AsEnumerable().Reverse())
        {
            World.MoveToTop(card, deck);
        }
    }

    private List<Card> Distinct(IReadOnlyList<SceneCard> references)
    {
        ArgumentNullException.ThrowIfNull(references);
        var selected = references.Select(Find).ToList();
        if (selected.Select(card => card.ObjectId).Distinct().Count() != selected.Count)
        {
            throw new ArgumentException("a stack names the same physical card more than once");
        }

        return selected;
    }

    private void Move(Card card, SceneDestination destination)
    {
        ArgumentNullException.ThrowIfNull(destination);
        var (playArea, inPlay) = ProjectedDestination(card, destination);
        RequireHostCanMove(card, playArea, inPlay);
        RequireEntryLimits(card, playArea, inPlay);
        if (inPlay
            && !DeckTypes.IsInPlay(card.Area.Type)
            && Uniqueness.IsBlocked(World, World.Facts, card, playArea))
        {
            throw new InvalidOperationException(
                $"matching unique '{World.Facts.Title(card.FaceId)}' is already in play");
        }

        Area area = Destination(card, destination);
        bool entersPlay = inPlay && !DeckTypes.IsInPlay(card.Area.Type);
        World.MoveToTop(card, area);
        if (entersPlay)
        {
            Reveal.EnterPlay(World, World.Facts, card, [], abilities: World.Abilities);
            AccountCreatedCards();
        }
    }

    private (PlayArea PlayArea, bool InPlay) ProjectedDestination(
        Card card, SceneDestination destination)
    {
        int seat = destination.Seat;
        return destination.Zone switch
        {
            SceneZone.PlayerDeck or SceneZone.PlayerHand or SceneZone.PlayerDiscard =>
                (PlayArea.Of(Player(seat).Index), false),
            SceneZone.Ally or SceneZone.Support or SceneZone.Obligation or
                SceneZone.EngagedMinion => (PlayArea.Of(Player(seat).Index), true),
            SceneZone.Upgrade =>
                (UpgradeHost(card, seat, destination.Host).Area.PlayArea, true),
            SceneZone.Attachment =>
                (AttachmentHost(card, destination.Host).Area.PlayArea, true),
            SceneZone.EncounterDeck or SceneZone.EncounterDiscard or SceneZone.SetAside =>
                (seat == World.Scenario ? PlayArea.Villains : PlayArea.Of(Player(seat).Index), false),
            SceneZone.SideScheme or SceneZone.Environment => (PlayArea.Villains, true),
            _ => throw new ArgumentOutOfRangeException(nameof(destination)),
        };
    }

    private Area Destination(Card card, SceneDestination destination)
    {
        int seat = destination.Seat;
        CardKind kind = World.Facts.Kind(card.FaceId);
        switch (destination.Zone)
        {
            case SceneZone.PlayerDeck:
            case SceneZone.PlayerHand:
            case SceneZone.PlayerDiscard:
                Player(seat);
                RequireOwner(card, seat);
                RequirePlayerDeckCard(card);
                return destination.Zone switch
                {
                    SceneZone.PlayerDeck => World.Seats[seat].Deck,
                    SceneZone.PlayerHand => World.Seats[seat].Hand,
                    _ => World.AreaOf(DeckType.DiscardPile, PlayArea.Of(seat), cardOwner: seat),
                };
            case SceneZone.Ally:
                RequirePlayerKind(card, seat, CardKind.Ally);
                return World.AreaOf(DeckType.AlliesArea, PlayArea.Of(seat), cardOwner: seat);
            case SceneZone.Support:
                RequirePlayerKind(card, seat, CardKind.Support);
                return World.AreaOf(DeckType.SupportsArea, PlayArea.Of(seat), cardOwner: seat);
            case SceneZone.Upgrade:
                RequirePlayerKind(card, seat, CardKind.Upgrade);
                Card upgradeHost = UpgradeHost(card, seat, destination.Host);
                return World.AreaOf(
                    DeckType.UpgradesArea,
                    upgradeHost.Area.PlayArea,
                    upgradeHost.ObjectId,
                    cardOwner: seat);
            case SceneZone.Attachment:
                RequireScenarioKind(card, kind, CardKind.Attachment);
                Card attachmentHost = AttachmentHost(card, destination.Host);
                return World.AreaOf(
                    DeckType.UpgradesArea,
                    attachmentHost.Area.PlayArea,
                    destination.Host,
                    cardOwner: World.Scenario);
            case SceneZone.EngagedMinion:
                Player(seat);
                RequireScenarioKind(card, kind, CardKind.Minion);
                return World.AreaOf(
                    DeckType.EngagedEnemiesArea,
                    PlayArea.Of(seat),
                    cardOwner: World.Scenario);
            case SceneZone.Obligation:
                Player(seat);
                RequireScenarioKind(card, kind, CardKind.Obligation);
                return World.AreaOf(
                    DeckType.ObligationsArea,
                    PlayArea.Of(seat),
                    cardOwner: World.Scenario);
            case SceneZone.EncounterDeck:
            case SceneZone.EncounterDiscard:
                RequireEncounterCard(card);
                return World.AreaOf(destination.Zone == SceneZone.EncounterDeck
                    ? DeckType.EncounterDeck
                    : DeckType.EncounterDiscardPile);
            case SceneZone.SideScheme:
                RequireScenarioKind(card, kind, CardKind.EncounterSideScheme);
                return World.AreaOf(DeckType.SideSchemesArea);
            case SceneZone.Environment:
                RequireScenarioKind(card, kind, CardKind.Environment);
                return World.AreaOf(DeckType.EnvironmentArea);
            case SceneZone.SetAside:
                if (seat == World.Scenario)
                {
                    RequireOwner(card, World.Scenario);
                    return World.AreaOf(DeckType.AsideDeck);
                }

                Player(seat);
                RequireOwner(card, seat);
                return World.Seats[seat].SetAside;
            default:
                throw new ArgumentOutOfRangeException(nameof(destination));
        }
    }

    private void Damage(Card card, long damage)
    {
        if (damage < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(damage), "damage must not be negative");
        }

        if (!DeckTypes.IsInPlay(card.Area.Type)
            || !CardKinds.IsCharacter(World.Facts.Kind(card.FaceId)))
        {
            throw new InvalidOperationException("damage can be arranged only on an in-play character");
        }

        long health = Rules.Play.Damage.Health(World, World.Facts, card);
        if (damage >= health)
        {
            throw new InvalidOperationException(
                $"{damage} damage would defeat '{card.FaceId}' with {health} health");
        }

        card.TakeDamage(damage - card.Damage);
    }

    private void Counters(Card card, string type, long count)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(type);
        string normalized = type.ToLowerInvariant();

        if (count < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(count), "token count must not be negative");
        }

        string key;
        CardKind cardKind = World.Facts.Kind(card.FaceId);
        if (normalized == "threat"
            && cardKind is CardKind.MainScheme or CardKind.EncounterSideScheme
                or CardKind.PlayerSideScheme)
        {
            key = "k_threat";
        }
        else if (World.Facts.CounterTypes(card.FaceId).Contains(
                     normalized, StringComparer.Ordinal))
        {
            key = "c_" + normalized;
        }
        else
        {
            throw new InvalidOperationException(
                $"'{card.FaceId}' does not print {normalized} counters");
        }

        long held = card.Tokens.GetValueOrDefault(key, 0);
        card.PlaceTokens(key, count - held);
    }

    private void Form(SetSceneForm operation)
    {
        Seat seat = Player(operation.Seat);
        if (!seat.IdentityCard.Faces.Contains(operation.FaceId, StringComparer.Ordinal)
            || World.Facts.Kind(operation.FaceId) is not (CardKind.Hero or CardKind.AlterEgo))
        {
            throw new ArgumentException(
                $"'{operation.FaceId}' is not a printed identity face for seat {operation.Seat}");
        }

        seat.IdentityCard.TurnTo(operation.FaceId);
    }

    private static void Ready(Card card, bool ready)
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

    private void Status(Card host, string status)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(status);
        if (status is not (Statuses.Tough or Statuses.Stunned or Statuses.Confused))
        {
            throw new ArgumentException(
                $"'{status}' is not a rules-provided status card", nameof(status));
        }

        if (!DeckTypes.IsInPlay(host.Area.Type)
            || !CardKinds.IsCharacter(World.Facts.Kind(host.FaceId)))
        {
            throw new InvalidOperationException(
                "a status card can be arranged only on an in-play character");
        }

        Card created = Statuses.Inflict(World, World.Facts, host, status)
            ?? throw new InvalidOperationException(
                $"'{host.FaceId}' cannot receive another {status} status card");
        accountedOwners.Add(created.ObjectId, created.Owner);
    }

    private void AccountCreatedCards()
    {
        foreach (var created in World.Cards.Where(card => !accountedOwners.ContainsKey(card.ObjectId)))
        {
            accountedOwners.Add(created.ObjectId, created.Owner);
        }
    }

    private void ValidateWorld()
    {
        if (World.Cards.Count != accountedOwners.Count)
        {
            throw new InvalidOperationException(
                $"the scene accounts for {accountedOwners.Count} cards and the world contains {World.Cards.Count}");
        }

        var membership = new int[World.Cards.Count];
        foreach (var area in World.Areas.OrderBy(area => area.Id))
        {
            if (area.Host >= 0 && (area.Cards.Count > 0 || area.Removed.Count > 0))
            {
                if (area.Host >= World.Cards.Count)
                {
                    throw new InvalidOperationException(
                        $"hosted area {area.Id} names missing card {area.Host}");
                }

                Card host = World.Cards[area.Host];
                if (!DeckTypes.IsInPlay(host.Area.Type) || host.Area.PlayArea != area.PlayArea)
                {
                    throw new InvalidOperationException(
                        $"hosted area {area.Id} does not share an in-play host's play area");
                }
            }

            foreach (var card in area.Cards.Concat(area.Removed))
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

        for (int id = 0; id < World.Cards.Count; id++)
        {
            Card card = World.Cards[id];
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

            if (card.Owner != accountedOwners[id])
            {
                throw new InvalidOperationException(
                    $"card {id} changed ownership from {accountedOwners[id]} to {card.Owner}");
            }
        }

        var inPlay = World.Cards.Where(card => DeckTypes.IsInPlay(card.Area.Type)).ToList();
        for (int left = 0; left < inPlay.Count; left++)
        {
            for (int right = left + 1; right < inPlay.Count; right++)
            {
                if (Uniqueness.Matches(World.Facts, inPlay[left], inPlay[right]))
                {
                    throw new InvalidOperationException(
                        $"matching unique cards {inPlay[left].ObjectId} and {inPlay[right].ObjectId} are both in play");
                }
            }
        }
    }

    private Seat Player(int seat) => seat >= 0 && seat < World.Seats.Count
        ? World.Seats[seat]
        : throw new ArgumentOutOfRangeException(nameof(seat), $"there is no player seat {seat}");

    private Card Host(int host)
    {
        if (host < 0 || host >= World.Cards.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(host), $"there is no card {host}");
        }

        Card card = World.Cards[host];
        if (!DeckTypes.IsInPlay(card.Area.Type))
        {
            throw new InvalidOperationException($"host card {host} is not in play");
        }

        return card;
    }

    private Card UpgradeHost(Card card, int player, int requested)
    {
        Seat seat = Player(player);
        var eligible = CardPlay.LegalAttachmentTargets(
            World, World.Facts, seat, card, World.Abilities);
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

        return Host(requested);
    }

    private Card AttachmentHost(Card card, int requested)
    {
        Card host = Host(requested);
        int? required = World.Abilities.AttachesTo(World, card);
        if (required is null || required != host.ObjectId)
        {
            throw new InvalidOperationException(
                $"card {requested} is not the printed attach-to host for '{card.FaceId}'");
        }

        return host;
    }

    private void RequireHostCanMove(
        Card card, PlayArea destination, bool destinationInPlay)
    {
        var hosted = World.Areas
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

    private void RequireEntryLimits(Card card, PlayArea destination, bool destinationInPlay)
    {
        if (!destinationInPlay || DeckTypes.IsInPlay(card.Area.Type))
        {
            return;
        }

        if (World.Facts.Kind(card.FaceId) == CardKind.Ally && destination.IsPlayers)
        {
            int player = destination.Player;
            long limit = StateFields.Modified(
                World,
                World.Seats[player].IdentityCard,
                "ally_limit",
                World.Facts,
                World.Players);
            int held = World.Areas
                .Where(area => area.Type == DeckType.AlliesArea
                    && area.PlayArea == destination)
                .Sum(area => area.Cards.Count);
            if (held >= limit)
            {
                throw new InvalidOperationException(
                    $"seat {player} already controls its ally limit of {limit}");
            }
        }

        if (StateFields.Modified(
                World, card, "restricted", World.Facts, World.Players) > 0)
        {
            int held = World.Cards.Count(candidate =>
                DeckTypes.IsInPlay(candidate.Area.Type)
                && candidate.Owner == card.Owner
                && StateFields.Modified(
                    World, candidate, "restricted", World.Facts, World.Players) > 0);
            if (held >= StateFields.RestrictedLimit)
            {
                throw new InvalidOperationException(
                    $"seat {card.Owner} already controls {held} restricted cards");
            }
        }
    }

    private static void RequireOwner(Card card, int owner)
    {
        if (card.Owner != owner)
        {
            throw new InvalidOperationException(
                $"card {card.ObjectId} ('{card.FaceId}') is owned by {card.Owner}, not seat {owner}");
        }
    }

    private void RequirePlayerKind(Card card, int seat, CardKind expected)
    {
        Player(seat);
        RequireOwner(card, seat);
        CardKind actual = World.Facts.Kind(card.FaceId);
        if (actual != expected)
        {
            throw new InvalidOperationException(
                $"'{card.FaceId}' is {actual}, not {expected}");
        }
    }

    private void RequirePlayerDeckCard(Card card)
    {
        if (World.Facts.Kind(card.FaceId) is not (
            CardKind.Ally or CardKind.Event or CardKind.Resource or CardKind.Support or CardKind.Upgrade))
        {
            throw new InvalidOperationException(
                $"'{card.FaceId}' is not a player deck card");
        }
    }

    private void RequireEncounterCard(Card card)
    {
        RequireOwner(card, World.Scenario);
        if (World.Facts.Kind(card.FaceId) is not (
            CardKind.Obligation or CardKind.Treachery or CardKind.Minion or CardKind.Attachment
            or CardKind.EncounterSideScheme or CardKind.Environment))
        {
            throw new InvalidOperationException(
                $"'{card.FaceId}' is not an encounter card");
        }
    }

    private static void RequireScenarioKind(Card card, CardKind actual, CardKind expected)
    {
        RequireOwner(card, World.Scenario);
        if (actual != expected)
        {
            throw new InvalidOperationException(
                $"'{card.FaceId}' is {actual}, not {expected}");
        }
    }
}

/// <summary>A construction failure tied to the exact authority and operation.</summary>
public sealed class CoreSceneConstructionException : InvalidOperationException
{
    /// <summary>Creates a named construction failure.</summary>
    public CoreSceneConstructionException(
        string authority, string operation, string reason, Exception? inner = null)
        : base($"{authority}; {operation}: {reason}", inner)
    {
        Authority = authority;
        Operation = operation;
    }

    /// <summary>The authority obligation whose legal scene was being built.</summary>
    public string Authority { get; }

    /// <summary>The operation rejected by the first invariant it violated.</summary>
    public string Operation { get; }
}
