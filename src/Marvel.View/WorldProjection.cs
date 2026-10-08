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
        ViewScope scope,
        int? activePlayer = null,
        Prompt? publicPrompt = null)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(events);
        ArgumentNullException.ThrowIfNull(scope);

        var promptVisible = prompt is not null && scope.Includes(prompt.Player);
        var searchVisible = promptVisible ? SearchResults(prompt!) : [];
        WorldDescriptor complete = Describe(world, prompt, searchVisible);
        WorldDescriptor visible = CardValueProjection.WithValues(world, Filter(complete, scope), scope);
        visible = CardPersistentProjection.WithFacts(world, visible, scope);
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

        Prompt? authorizedPrompt = promptVisible
            ? prompt! with
            {
                ContextCardIds = [.. prompt!.ContextCardIds.Where(readableIds.Contains)],
                CauseCardIds = [.. prompt.CauseCardIds.Where(readableIds.Contains)],
            }
            : null;
        int active = activePlayer ?? world.FirstPlayer;
        visible = TableDescriptorProjection.WithContext(
            visible, authorizedPrompt, scope, active, world.FirstPlayer,
            publicPrompt ?? prompt, PublicReadableIds(complete, readableIds));
        return new VisibleResult(visible, authorizedPrompt, VisibilityEventFilter.Filter(events, addressableIds, readableIds));
    }

    private static HashSet<int> PublicReadableIds(WorldDescriptor complete, HashSet<int> readableIds) =>
        [.. complete.Areas.SelectMany(area => area.Cards.Concat(area.Removed))
            .Where(card => card.Audience.Public && card.FaceUp
                && card.Id is not null && readableIds.Contains(card.Id.Value))
            .Select(card => card.Id!.Value)];

    private static WorldDescriptor Describe(
        World world, Prompt? prompt, IReadOnlySet<int> searchVisible)
    {
        var cards = new Dictionary<int, CardDescriptor>();
        foreach (Card card in world.Cards)
        {
            CardDescriptor? descriptor = DescribeCard(world, prompt, searchVisible, card);
            if (descriptor is not null)
            {
                cards.Add(card.ObjectId, descriptor);
            }
        }

        var areas = world.Areas.Select(area => new AreaDescriptor(
            area.Id,
            area.Type.ToString(),
            area.PlayArea.Player,
            area.Host,
            area.Cards.Where(card => cards.ContainsKey(card.ObjectId))
                .Select(card => cards[card.ObjectId]).ToList(),
            area.Removed.Where(card => cards.ContainsKey(card.ObjectId))
                .Select(card => cards[card.ObjectId]).ToList())).ToList();
        var players = world.Seats.Select(seat =>
            new PlayerDescriptor(seat.Index, seat.Name, seat.Eliminated)).ToList();
        var gameAreas = world.GameAreas.Select(area =>
            new GameAreaDescriptor(
                area.Id,
                area.PlayAreas.Select(playArea => playArea.Player).Order().ToList())).ToList();
        return new WorldDescriptor(players, areas, gameAreas, world.Result);
    }

    private static CardDescriptor? DescribeCard(
        World world, Prompt? prompt, IReadOnlySet<int> searchVisible, Card card)
    {
        CardKind printedKind = world.Facts.Kind(card.FaceId);
        // Insert pseudo-cards are engine bookkeeping, not physical components.
        if (printedKind == CardKind.Insert)
        {
            return null;
        }
        bool assigned = EffectiveCards.HasProfile(card);
        CardKind kind = EffectiveCards.Kind(card, world.Facts);
        IReadOnlyDictionary<string, string> attributes = EffectiveCards.Attributes(card, world.Facts);
        CardFaceDescriptor face = DescribeFace(world, card, kind, attributes, assigned);
        return CardDescriptorProjection.WithTableState(
            world,
            card,
            Back(printedKind),
            face,
            assigned ? CardAudience.Everyone : Audience(card, prompt, searchVisible),
            addressable: !DeckTypes.FaceDownOnEntry(card.Area.Type));
    }

    private static CardFaceDescriptor DescribeFace(
        World world,
        Card card,
        CardKind kind,
        IReadOnlyDictionary<string, string> attributes,
        bool assigned)
    {
        var face = new CardFaceDescriptor(
            EffectiveCards.FaceId(card),
            EffectiveCards.Title(card, world.Facts),
            assigned ? string.Empty : world.Facts.Subtitle(card.FaceId),
            kind,
            ProjectedFields(world, card, kind))
        {
            Traits = DisplayTraits(world, card),
            Cost = attributes.TryGetValue("Cost", out string? cost) ? cost : null,
            PrintedStats = PrintedStats(attributes),
            PrintedValues = CardPrintedValues.From(assigned
                ? PrintedStatFacts.From(kind, attributes)
                : world.Facts.PrintedStats(card.FaceId)),
            Keywords = assigned ? [] : [.. world.Facts.Keywords(card.FaceId)],
            RulesText = assigned ? string.Empty : world.Facts.Text(card.FaceId),
            RulesMarkup = assigned ? string.Empty : world.Facts.FormattedText(card.FaceId),
            ArtFaceId = assigned ? null : card.FaceId,
            Damage = card.Damage,
            Counters = card.Tokens
                .Where(token => token.Key.StartsWith("c_", StringComparison.Ordinal))
                .ToDictionary(token => token.Key[2..], token => token.Value, StringComparer.Ordinal),
        };
        return face;
    }

    private static IReadOnlyDictionary<string, long> ProjectedFields(
        World world, Card card, CardKind kind)
    {
        bool inPlay = DeckTypes.IsInPlay(card.Area.Type);
        IReadOnlyDictionary<string, long> fields = StateFields.For(
            card, world.Facts, world.Players, inPlay, card.HasRegisteredTokens,
            card.Owner == world.FirstPlayer && card.Area.Type == DeckType.HeroArea, world);
        if (!inPlay || (kind is not (CardKind.Hero or CardKind.AlterEgo or CardKind.Ally or CardKind.Minion)
            && !CardKinds.IsVillain(kind)))
        {
            return fields;
        }
        return new Dictionary<string, long>(fields, StringComparer.Ordinal)
        {
            ["health"] = CardValues.RemainingHealth(world, card, world.Facts),
        };
    }

    private static IReadOnlyList<string> DisplayTraits(World world, Card card)
    {
        IReadOnlyList<string> rulesKeys = world.Facts.Traits(card.FaceId);
        IReadOnlyList<string> printed = world.Facts.PrintedTraits(card.FaceId);
        var labels = rulesKeys
            .Select((key, index) => new
            {
                Key = key,
                Label = index < printed.Count ? printed[index] : key.Replace('_', ' '),
            })
            .ToDictionary(pair => pair.Key, pair => pair.Label, StringComparer.Ordinal);
        return
        [
            .. Traits.Of(world, card, world.Facts)
                .Select(trait => labels.GetValueOrDefault(trait, trait.Replace('_', ' '))),
        ];
    }

    private static Dictionary<string, string> PrintedStats(
        IReadOnlyDictionary<string, string> attributes)
    {
        string[] names =
        [
            "REC", "THW", "ATK", "DEF", "SCH", "HP", "HS", "Stage",
            "REC+", "THW+", "ATK+", "DEF+", "SCH+", "HP+",
            "StartingThreat", "TargetThreat", "EscalationThreat", "Boost", "RES", "Class",
            "Unique", "Acceleration", "Amplify", "Crisis", "Hazard",
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

    private static HashSet<int> SearchResults(Prompt prompt)
    {
        var visible = prompt.Affordances
            .Select(option => option.Targets)
            .Where(targets => targets?.IsSearch == true)
            .SelectMany(targets => targets!.Legal)
            .ToHashSet();
        if (prompt.ExposesConcealedCandidates)
        {
            // Some choices, including Futurist, expose each looked-at card as
            // its own affordance instead of wrapping the candidates in a
            // target request. This metadata is the server's explicit signal
            // that those otherwise-concealed anchors are readable now.
            visible.UnionWith(prompt.Affordances.Select(option => option.AnchorId));
        }

        return visible;
    }

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
                Cards = area.Cards.Select(card => CardDescriptorProjection.Filter(card, scope, addressableIds)).ToList(),
                Removed = area.Removed.Select(card => CardDescriptorProjection.Filter(card, scope, addressableIds)).ToList(),
            }).ToList(),
        };
    }

}
