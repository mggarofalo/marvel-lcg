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
/// _operations can only rearrange those cards. They cannot allocate a card, transfer ownership,
/// replace an identity's signature set, or silently manufacture a boundary state.
/// </remarks>
public sealed class CanonicalCoreScene
{
    private static readonly Dictionary<Type, Action<CanonicalCoreScene, CoreSceneOperation>>
        OperationHandlers = new Dictionary<Type, Action<CanonicalCoreScene, CoreSceneOperation>>
        {
            [typeof(MoveSceneCard)] = (scene, value) =>
                scene.Move(scene.Find(((MoveSceneCard)value).Card), ((MoveSceneCard)value).Destination),
            [typeof(SetSceneVillain)] = (scene, value) =>
                scene.Villain(scene.Find(((SetSceneVillain)value).Card)),
            [typeof(StackPlayerDeck)] = (scene, value) => scene.StackPlayer((StackPlayerDeck)value),
            [typeof(SetPlayerHand)] = (scene, value) => scene.PlayerHand((SetPlayerHand)value),
            [typeof(StackEncounterDeck)] = (scene, value) => scene.StackEncounter((StackEncounterDeck)value),
            [typeof(SetSceneDamage)] = (scene, value) => scene.Damage(
                scene.Find(((SetSceneDamage)value).Card), ((SetSceneDamage)value).Damage),
            [typeof(SetSceneCounters)] = (scene, value) => scene.Counters(
                scene.Find(((SetSceneCounters)value).Card),
                ((SetSceneCounters)value).Type,
                ((SetSceneCounters)value).Count),
            [typeof(SetSceneAccelerationTokens)] = (scene, value) =>
                scene.AccelerationTokens(((SetSceneAccelerationTokens)value).Count),
            [typeof(SetSceneForm)] = (scene, value) => scene.Form((SetSceneForm)value),
            [typeof(SetSceneReady)] = (scene, value) => Ready(
                scene.Find(((SetSceneReady)value).Card), ((SetSceneReady)value).Ready),
            [typeof(GiveSceneStatus)] = (scene, value) => scene.Status(
                scene.Find(((GiveSceneStatus)value).Host), ((GiveSceneStatus)value).Status),
        };

    internal readonly Dictionary<int, int> _accountedOwners;
    internal readonly List<CoreSceneOperation> _operations = [];

    private CanonicalCoreScene(CoreSceneRequest request, World world)
    {
        Request = request;
        World = world;
        _accountedOwners = world.Cards.ToDictionary(card => card.ObjectId, card => card.Owner);
        this.ValidateWorld();
    }

    /// <summary>The authority obligation this scene is meant to distinguish.</summary>
    public CoreSceneRequest Request { get; }

    /// <summary>The arranged engine state.</summary>
    public World World { get; }

    /// <summary>The arrangements already applied, in transcript order.</summary>
    public IReadOnlyList<CoreSceneOperation> Operations => _operations;

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
            setup, request.Campaign, request.Heroes, request.ModularSets, facts,
            request.PlayerDecks);
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
            if (!OperationHandlers.TryGetValue(operation.GetType(), out var apply))
            {
                throw new ArgumentOutOfRangeException(nameof(operation));
            }
            apply(this, operation);
            this.ValidateWorld();
            _operations.Add(operation);
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
        Seat seat = this.Player(operation.Seat);
        if (operation.TopFirst.Count == 0)
        {
            throw new ArgumentException(
                "a canonical player-deck boundary must leave at least one card to draw");
        }

        var selected = Distinct(operation.TopFirst);
        foreach (var card in selected)
        {
            RequireOwner(card, operation.Seat);
            this.RequirePlayerDeckCard(card);
            this.RequireHostCanMove(card, PlayArea.Of(operation.Seat), destinationInPlay: false);
        }

        if (operation.Remainder is not PlayerDeckRemainder.Leave)
        {
            Area remainder = operation.Remainder switch
            {
                PlayerDeckRemainder.Discard => World.AreaOf(
                    DeckType.DiscardPile,
                    PlayArea.Of(operation.Seat),
                    cardOwner: operation.Seat),
                PlayerDeckRemainder.Hand => seat.Hand,
                _ => throw new ArgumentOutOfRangeException(nameof(operation)),
            };
            foreach (var card in seat.Deck.Cards.Where(card => !selected.Contains(card)).ToList())
            {
                this.RequireHostCanMove(card, PlayArea.Of(operation.Seat), destinationInPlay: false);
                World.MoveToTop(card, remainder);
            }
        }

        foreach (var card in selected.AsEnumerable().Reverse())
        {
            World.MoveToTop(card, seat.Deck);
        }
    }

    private void PlayerHand(SetPlayerHand operation)
    {
        Seat seat = this.Player(operation.Seat);
        var selected = Distinct(operation.Cards);
        foreach (var card in selected)
        {
            RequireOwner(card, operation.Seat);
            this.RequirePlayerDeckCard(card);
            this.RequireHostCanMove(card, PlayArea.Of(operation.Seat), destinationInPlay: false);
        }

        var discard = World.AreaOf(
            DeckType.DiscardPile,
            PlayArea.Of(operation.Seat),
            cardOwner: operation.Seat);
        foreach (var card in seat.Hand.Cards.Where(card => !selected.Contains(card)).ToList())
        {
            World.MoveToTop(card, discard);
        }

        foreach (var card in selected)
        {
            World.MoveToTop(card, seat.Hand);
        }
    }

    private void StackEncounter(StackEncounterDeck operation)
    {
        var selected = Distinct(operation.TopFirst);
        foreach (var card in selected)
        {
            this.RequireEncounterCard(card);
            this.RequireHostCanMove(card, PlayArea.Villains, destinationInPlay: false);
        }

        Area deck = World.AreaOf(DeckType.EncounterDeck);
        if (operation.Remainder is not EncounterDeckRemainder.Leave)
        {
            Area remainder = operation.Remainder switch
            {
                EncounterDeckRemainder.Discard =>
                    World.AreaOf(DeckType.EncounterDiscardPile),
                EncounterDeckRemainder.Dealt => World.AreaOf(
                    DeckType.DealtEncounterCardsDeck,
                    PlayArea.Of(this.Player(operation.Seat).Index)),
                _ => throw new ArgumentOutOfRangeException(nameof(operation)),
            };
            foreach (var card in deck.Cards.Where(card => !selected.Contains(card)).ToList())
            {
                this.RequireHostCanMove(card, PlayArea.Villains, destinationInPlay: false);
                World.MoveToTop(card, remainder);
            }
        }

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
        this.RequireStructuralRoleCanMove(card);
        var (playArea, inPlay) = ProjectedDestination(card, destination);
        this.RequireHostCanMove(card, playArea, inPlay);
        this.RequireEntryLimits(card, playArea, inPlay, destination.Host);
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
            this.AccountCreatedCards();
        }
    }

    private void Villain(Card card)
    {
        if (!CardKinds.IsVillain(World.Facts.Kind(card.FaceId))
            || card.Owner != World.Scenario)
        {
            throw new InvalidOperationException(
                $"'{card.FaceId}' is not a scenario-owned villain stage");
        }

        Card? current = World.TheCardIn(DeckType.VillainArea);
        if (current == card)
        {
            return;
        }

        if (current is not null)
        {
            World.MoveToTop(current, World.AreaOf(DeckType.RemovedArea));
        }

        World.MoveToTop(card, World.AreaOf(DeckType.VillainArea));
        card.TurnFaceUp();
    }

    private (PlayArea PlayArea, bool InPlay) ProjectedDestination(
        Card card, SceneDestination destination)
    {
        int seat = destination.Seat;
        return destination.Zone switch
        {
            SceneZone.PlayerDeck or SceneZone.PlayerHand or SceneZone.PlayerDiscard =>
                (PlayArea.Of(this.Player(seat).Index), false),
            SceneZone.Ally or SceneZone.Support or SceneZone.Obligation or
                SceneZone.EngagedMinion => (PlayArea.Of(this.Player(seat).Index), true),
            SceneZone.Upgrade =>
                (this.UpgradeHost(card, seat, destination.Host).Area.PlayArea, true),
            SceneZone.Attachment =>
                (this.AttachmentHost(card, destination.Host).Area.PlayArea, true),
            SceneZone.EncounterDeck or SceneZone.EncounterDiscard or SceneZone.SetAside =>
                (seat == World.Scenario ? PlayArea.Villains : PlayArea.Of(this.Player(seat).Index), false),
            SceneZone.SideScheme or SceneZone.Environment => (PlayArea.Villains, true),
            _ => throw new ArgumentOutOfRangeException(nameof(destination)),
        };
    }

    private Area Destination(Card card, SceneDestination destination)
    {
        return IsPlayerDestination(destination.Zone)
            ? PlayerDestination(card, destination)
            : ScenarioDestination(card, destination);
    }

    private static bool IsPlayerDestination(SceneZone zone) =>
        zone is SceneZone.PlayerDeck or SceneZone.PlayerHand or SceneZone.PlayerDiscard
            or SceneZone.Ally or SceneZone.Support or SceneZone.Upgrade
            or SceneZone.EngagedMinion or SceneZone.Obligation;

    private Area PlayerDestination(Card card, SceneDestination destination)
    {
        int seat = destination.Seat;
        return destination.Zone switch
        {
            SceneZone.PlayerDeck => PlayerDeckDestination(card, seat, World.Seats[seat].Deck),
            SceneZone.PlayerHand => PlayerDeckDestination(card, seat, World.Seats[seat].Hand),
            SceneZone.PlayerDiscard => PlayerDeckDestination(
                card, seat, World.AreaOf(DeckType.DiscardPile, PlayArea.Of(seat), cardOwner: seat)),
            SceneZone.Ally => PlayerCardDestination(card, seat, CardKind.Ally, DeckType.AlliesArea),
            SceneZone.Support => PlayerCardDestination(
                card, seat, CardKind.Support, DeckType.SupportsArea),
            SceneZone.Upgrade => UpgradeDestination(card, seat, destination.Host),
            SceneZone.EngagedMinion => EngagedMinionDestination(card, seat),
            SceneZone.Obligation => ObligationDestination(card, seat),
            _ => throw new ArgumentOutOfRangeException(nameof(destination)),
        };
    }

    private Area ScenarioDestination(Card card, SceneDestination destination) =>
        destination.Zone switch
        {
            SceneZone.Attachment => AttachmentDestination(card, destination.Host),
            SceneZone.EncounterDeck => EncounterDestination(card, DeckType.EncounterDeck),
            SceneZone.EncounterDiscard => EncounterDestination(card, DeckType.EncounterDiscardPile),
            SceneZone.SideScheme => ScenarioCardDestination(
                card, CardKind.EncounterSideScheme, DeckType.SideSchemesArea),
            SceneZone.Environment => ScenarioCardDestination(
                card, CardKind.Environment, DeckType.EnvironmentArea),
            SceneZone.SetAside => SetAsideDestination(card, destination.Seat),
            _ => throw new ArgumentOutOfRangeException(nameof(destination)),
        };

    private Area PlayerDeckDestination(Card card, int seat, Area area)
    {
        this.Player(seat);
        RequireOwner(card, seat);
        this.RequirePlayerDeckCard(card);
        return area;
    }

    private Area PlayerCardDestination(Card card, int seat, CardKind kind, DeckType area)
    {
        this.RequirePlayerKind(card, seat, kind);
        return World.AreaOf(area, PlayArea.Of(seat), cardOwner: seat);
    }

    private Area UpgradeDestination(Card card, int seat, int requestedHost)
    {
        this.RequirePlayerKind(card, seat, CardKind.Upgrade);
        Card host = this.UpgradeHost(card, seat, requestedHost);
        return World.AreaOf(DeckType.UpgradesArea, host.Area.PlayArea, host.ObjectId, cardOwner: seat);
    }

    private Area AttachmentDestination(Card card, int requestedHost)
    {
        RequireScenarioKind(card, World.Facts.Kind(card.FaceId), CardKind.Attachment);
        Card host = this.AttachmentHost(card, requestedHost);
        return World.AreaOf(
            DeckType.UpgradesArea, host.Area.PlayArea, requestedHost, cardOwner: World.Scenario);
    }

    private Area EngagedMinionDestination(Card card, int seat)
    {
        this.Player(seat);
        RequireScenarioKind(card, World.Facts.Kind(card.FaceId), CardKind.Minion);
        return World.AreaOf(
            DeckType.EngagedEnemiesArea, PlayArea.Of(seat), cardOwner: World.Scenario);
    }

    private Area ObligationDestination(Card card, int seat)
    {
        this.Player(seat);
        RequireScenarioKind(card, World.Facts.Kind(card.FaceId), CardKind.Obligation);
        int? named = RevealKeywords.Names(World, World.Facts, card);
        if (named is not null && named != seat)
        {
            throw new InvalidOperationException(named < 0
                ? $"'{card.FaceId}' names an identity absent from this game"
                : $"'{card.FaceId}' must be given to seat {named}, not seat {seat}");
        }
        return World.AreaOf(
            DeckType.ObligationsArea, PlayArea.Of(seat), cardOwner: World.Scenario);
    }

    private Area EncounterDestination(Card card, DeckType area)
    {
        this.RequireEncounterCard(card);
        return World.AreaOf(area);
    }

    private Area ScenarioCardDestination(Card card, CardKind kind, DeckType area)
    {
        RequireScenarioKind(card, World.Facts.Kind(card.FaceId), kind);
        return World.AreaOf(area);
    }

    private Area SetAsideDestination(Card card, int seat)
    {
        if (seat == World.Scenario)
        {
            RequireOwner(card, World.Scenario);
            return World.AreaOf(DeckType.AsideDeck);
        }
        this.Player(seat);
        RequireOwner(card, seat);
        return World.Seats[seat].SetAside;
    }

}
