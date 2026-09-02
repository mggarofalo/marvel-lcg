using Marvel.Rules.Events;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;

namespace Marvel.View;

/// <summary>Builds and filters the client snapshot, prompt, and event stream.</summary>
public static class WorldProjection
{
    /// <summary>Projects one engine result for an already-authorized scope.</summary>
    public static VisibleResult For(
        World world,
        Prompt? prompt,
        IReadOnlyList<GameEvent> events,
        ViewScope scope)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(events);
        ArgumentNullException.ThrowIfNull(scope);

        var promptVisible = prompt is not null && scope.Includes(prompt.Player);
        var searchVisible = promptVisible ? SearchResults(prompt!) : [];
        WorldDescriptor complete = Describe(world, prompt, searchVisible);
        WorldDescriptor visible = Filter(complete, scope);
        var addressableIds = visible.Areas
            .SelectMany(area => area.Cards.Concat(area.Removed))
            .Where(card => card.Id.HasValue)
            .Select(card => card.Id!.Value)
            .ToHashSet();
        var readableIds = visible.Areas
            .SelectMany(area => area.Cards.Concat(area.Removed))
            .Where(card => card.Id.HasValue && card.Face is not null)
            .Select(card => card.Id!.Value)
            .ToHashSet();

        return new VisibleResult(
            visible,
            promptVisible ? prompt : null,
            FilterEvents(events, addressableIds, readableIds));
    }

    private static WorldDescriptor Describe(
        World world, Prompt? prompt, IReadOnlySet<int> searchVisible)
    {
        var cards = new Dictionary<int, CardDescriptor>();
        foreach (Card card in world.Cards)
        {
            CardAudience audience = Audience(card, prompt, searchVisible);
            bool inPlay = DeckTypes.IsInPlay(card.Area.Type);
            CardKind kind = FacedownDrones.Kind(card, world.Facts);
            CardKind printedKind = world.Facts.Kind(card.FaceId);
            IReadOnlyDictionary<string, string> attributes = world.Facts.Attributes(card.FaceId);
            var face = new CardFaceDescriptor(
                card.FaceId,
                world.Facts.Title(card.FaceId),
                world.Facts.Subtitle(card.FaceId),
                kind,
                StateFields.For(
                    card, world.Facts, world.Players, inPlay,
                    card.HasRegisteredTokens,
                    card.Owner == world.FirstPlayer && card.Area.Type == DeckType.HeroArea,
                    world))
            {
                Traits = [.. Traits.Of(world, card, world.Facts)],
                Cost = attributes.TryGetValue("Cost", out string? cost) ? cost : null,
                PrintedStats = PrintedStats(attributes),
                Keywords = [.. world.Facts.Keywords(card.FaceId)],
                RulesText = world.Facts.Text(card.FaceId),
                Damage = card.Damage,
                Counters = card.Tokens
                    .Where(token => token.Key.StartsWith("k_", StringComparison.Ordinal))
                    .ToDictionary(
                        token => token.Key[2..],
                        token => token.Value,
                        StringComparer.Ordinal),
            };
            cards.Add(
                card.ObjectId,
                new CardDescriptor(
                    card.ObjectId,
                    Back(printedKind),
                    card.FaceUp,
                    card.Ready,
                    card.Area.Host,
                    face)
                {
                    Audience = audience,
                    Addressable = !DeckTypes.FaceDownOnEntry(card.Area.Type),
                });
        }

        var areas = world.Areas.Select(area => new AreaDescriptor(
            area.Id,
            area.Type.ToString(),
            area.PlayArea.Player,
            area.Host,
            area.Cards.Select(card => cards[card.ObjectId]).ToList(),
            area.Removed.Select(card => cards[card.ObjectId]).ToList())).ToList();
        var players = world.Seats.Select(seat =>
            new PlayerDescriptor(seat.Index, seat.Name, seat.Eliminated)).ToList();
        var gameAreas = world.GameAreas.Select(area =>
            new GameAreaDescriptor(
                area.Id,
                area.PlayAreas.Select(playArea => playArea.Player).Order().ToList())).ToList();
        return new WorldDescriptor(players, areas, gameAreas, world.Result);
    }

    private static Dictionary<string, string> PrintedStats(
        IReadOnlyDictionary<string, string> attributes)
    {
        string[] names =
        [
            "REC", "THW", "ATK", "DEF", "SCH", "HP", "Stage",
            "StartingThreat", "TargetThreat", "Boost",
        ];
        return names
            .Where(attributes.ContainsKey)
            .ToDictionary(name => name, name => attributes[name], StringComparer.Ordinal);
    }

    private static CardBack Back(CardKind kind) => kind is
        CardKind.AlterEgo or CardKind.Hero or CardKind.Ally or CardKind.Event
        or CardKind.Resource or CardKind.Support or CardKind.Upgrade
            ? CardBack.Player
            : CardBack.Encounter;

    private static CardAudience Audience(
        Card card, Prompt? prompt, IReadOnlySet<int> searchVisible)
    {
        if (card.Area.Type == DeckType.HandsArea && card.Area.PlayArea.IsPlayers)
        {
            return CardAudience.ForSeat(card.Area.PlayArea.Player);
        }

        if (card.FaceUp)
        {
            return CardAudience.Everyone;
        }

        if (prompt is not null && searchVisible.Contains(card.ObjectId))
        {
            return CardAudience.ForSeat(prompt.Player);
        }

        return CardAudience.Nobody;
    }

    private static HashSet<int> SearchResults(Prompt prompt) => prompt.Affordances
        .Select(option => option.Targets)
        .Where(targets => targets?.IsSearch == true)
        .SelectMany(targets => targets!.Legal)
        .ToHashSet();

    private static WorldDescriptor Filter(WorldDescriptor descriptor, ViewScope scope)
    {
        var addressableIds = descriptor.Areas
            .SelectMany(area => area.Cards.Concat(area.Removed))
            .Where(card => card.Audience.IsVisible(scope) || card.Addressable)
            .Select(card => card.Id!.Value)
            .ToHashSet();
        return descriptor with
        {
            Areas = descriptor.Areas.Select(area => area with
            {
                Host = addressableIds.Contains(area.Host) ? area.Host : -1,
                Cards = area.Cards.Select(card => Filter(card, scope, addressableIds)).ToList(),
                Removed = area.Removed.Select(card => Filter(card, scope, addressableIds)).ToList(),
            }).ToList(),
        };
    }

    private static CardDescriptor Filter(
        CardDescriptor card, ViewScope scope, HashSet<int> visible) =>
        card.Audience.IsVisible(scope)
            ? card with
            {
                Host = visible.Contains(card.Host) ? card.Host : -1,
                Audience = CardAudience.Nobody,
                Addressable = false,
            }
            : card with
            {
                Id = card.Addressable ? card.Id : null,
                FaceUp = false,
                Ready = card.Addressable ? card.Ready : true,
                Host = card.Addressable && visible.Contains(card.Host) ? card.Host : -1,
                Face = null,
                Audience = CardAudience.Nobody,
                Addressable = false,
            };

    private static List<GameEvent> FilterEvents(
        IReadOnlyList<GameEvent> events,
        HashSet<int> addressable,
        HashSet<int> readable)
    {
        var filtered = new List<GameEvent>(events.Count);
        foreach (GameEvent happened in events)
        {
            GameEvent? safe = happened switch
            {
                CardsCreated created => KeepCreated(created, readable),
                CardsMoved moved => KeepMoved(moved, addressable),
                AreaReordered reordered =>
                    reordered.Order.All(addressable.Contains) ? reordered : null,
                CardFormChanged changed => readable.Contains(changed.Card) ? changed : null,
                CardsFlipped flipped => KeepFlipped(flipped, addressable),
                CardAttached attached =>
                    addressable.Contains(attached.Card) && addressable.Contains(attached.Host)
                        ? attached
                        : null,
                CardDetached detached =>
                    addressable.Contains(detached.Card) && addressable.Contains(detached.Host)
                        ? detached
                        : null,
                ControlChanged changed => addressable.Contains(changed.Card) ? changed : null,
                FieldSet set => readable.Contains(set.Card) ? set : null,
                PlayAreaJoined joined => joined,
                PlayAreaDetached detached => detached,
                _ => throw new InvalidOperationException(
                    $"event kind {happened.GetType().Name} has no visibility decision"),
            };
            if (safe is not null)
            {
                filtered.Add(safe);
            }
        }

        return filtered;
    }

    private static CardsCreated? KeepCreated(CardsCreated created, HashSet<int> visible)
    {
        var cards = created.Cards.Where(card => visible.Contains(card.Id)).ToList();
        return cards.Count == 0 ? null : created with { Cards = cards };
    }

    private static CardsMoved? KeepMoved(CardsMoved moved, HashSet<int> visible)
    {
        var cards = moved.Cards.Where(card => visible.Contains(card.Card)).ToList();
        return cards.Count == 0 ? null : moved with { Cards = cards };
    }

    private static CardsFlipped? KeepFlipped(CardsFlipped flipped, HashSet<int> visible)
    {
        var cards = flipped.Cards.Where(visible.Contains).ToList();
        return cards.Count == 0 ? null : flipped with { Cards = cards };
    }
}

/// <summary>The complete response payload after visibility enforcement.</summary>
public sealed record VisibleResult(
    WorldDescriptor World,
    Prompt? Prompt,
    IReadOnlyList<GameEvent> Events);
