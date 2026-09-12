using System.Globalization;
using System.Text;
using Marvel.Rules.State;
using Marvel.View;

namespace Marvel.View;

/// <summary>A display-only board derived from one visibility-safe snapshot.</summary>
public sealed record BoardPresentation(IReadOnlyList<BoardAreaPresentation> Areas)
{
    private static readonly HashSet<string> LiveZeroFields = new(
        ["attack", "defense", "recover", "scheme", "thwart"],
        StringComparer.Ordinal);

    /// <summary>Scenario, player, and fallback lanes used by the tabletop renderer.</summary>
    public IReadOnlyList<BoardLanePresentation> Lanes { get; init; } = [];

    /// <summary>Builds a fresh presentation without retaining or enriching engine state.</summary>
    public static BoardPresentation From(WorldDescriptor world)
    {
        ArgumentNullException.ThrowIfNull(world);
        var players = world.Players.ToDictionary(player => player.Seat);
        var asideOrdinals = world.Areas
            .Where(area => area.Zone == nameof(DeckType.AsideDeck))
            .GroupBy(area => area.Owner)
            .SelectMany(group => group.Select((area, ordinal) => (area.Id, ordinal)))
            .ToDictionary(pair => pair.Id, pair => pair.ordinal);
        BoardAreaPresentation[] areas = CombineProgressiveAreas(
            [.. world.Areas.Select(area => Present(
                area,
                players,
                asideOrdinals.GetValueOrDefault(area.Id, -1)))]);
        BoardPlayerPresentation[] seats =
            [.. world.Players.Select(player => new BoardPlayerPresentation(
                player.Seat, player.Name))];
        return new BoardPresentation(areas)
        {
            Lanes = BoardLayout.Arrange(areas, seats),
        };
    }

    private static BoardAreaPresentation[] CombineProgressiveAreas(
        BoardAreaPresentation[] source)
    {
        var hidden = new HashSet<int>();
        var result = new List<BoardAreaPresentation>(source.Length);
        foreach (BoardAreaPresentation area in source)
        {
            if (hidden.Contains(area.Id))
            {
                continue;
            }
            if (ShouldHideProgressiveDeck(area, source))
            {
                continue;
            }

            BoardAreaPresentation? upcoming = UpcomingArea(area, source);
            if (upcoming is null)
            {
                result.Add(area);
                continue;
            }

            hidden.Add(upcoming.Id);
            BoardCardPresentation[] cards = [.. area.Cards, .. upcoming.Cards];
            BoardCardPresentation[] removed = [.. area.Removed, .. upcoming.Removed];
            result.Add(area with
            {
                Title = area.Zone == "VillainArea" ? "VILLAIN" : "MAIN SCHEME",
                Context = "Current and upcoming stages",
                Cards = cards,
                Removed = removed,
                Prominence = Prominence(area.Zone, cards.Length + removed.Length),
            });
        }

        return [.. result];
    }

    private static bool ShouldHideProgressiveDeck(
        BoardAreaPresentation area, BoardAreaPresentation[] source)
    {
        string? currentZone = area.Zone switch
        {
            "VillainDeck" => "VillainArea",
            "MainSchemesDeck" => "MainSchemesArea",
            _ => null,
        };
        return currentZone is not null && source.Any(candidate =>
            candidate.Seat == area.Seat
            && candidate.Host == area.Host
            && candidate.Zone == currentZone);
    }

    private static BoardAreaPresentation? UpcomingArea(
        BoardAreaPresentation area, BoardAreaPresentation[] source)
    {
        string? deckZone = area.Zone switch
        {
            "VillainArea" => "VillainDeck",
            "MainSchemesArea" => "MainSchemesDeck",
            _ => null,
        };
        return deckZone is null ? null : source.FirstOrDefault(candidate =>
            candidate.Zone == deckZone && candidate.Seat == area.Seat && candidate.Host == area.Host);
    }

    private static BoardAreaPresentation Present(
        AreaDescriptor area,
        Dictionary<int, PlayerDescriptor> players,
        int asideOrdinal)
    {
        string owner = players.TryGetValue(area.Owner, out PlayerDescriptor? player)
            ? player.Name
            : area.Owner < 0 ? "Scenario" : $"Seat {area.Owner}";
        // These labels are a client choice. The tabletop rules define the zone,
        // not the diagnostic wording that distinguishes it from set-aside cards.
        string title = area.Zone switch
        {
            "VillainDeck" => "UPCOMING VILLAIN STAGES",
            "AsideDeck" when asideOrdinal == 1 => $"{owner.ToUpperInvariant()}'S NEMESIS SET",
            "AsideDeck" => $"{owner.ToUpperInvariant()}'S SET-ASIDE AREA",
            _ => Humanize(area.Zone, trimArea: true).ToUpperInvariant(),
        };
        string context = area.Zone == "VillainDeck"
            ? "Out of play · enters after the current stage"
            : area.Owner < 0 ? "Scenario" : owner;
        if (area.Host >= 0)
        {
            context += " · hosted area";
        }

        return new BoardAreaPresentation(
            area.Id,
            title,
            context,
            Present(area.Cards, area.Zone),
            Present(area.Removed, area.Zone))
        {
            Zone = area.Zone,
            Seat = area.Owner,
            Host = area.Host,
            Prominence = Prominence(area.Zone, area.Cards.Count + area.Removed.Count),
        };
    }

    private static BoardAreaProminence Prominence(string zone, int cardCount)
    {
        if (cardCount == 0)
        {
            return BoardAreaProminence.Empty;
        }

        return IsInPlay(zone) || zone == "StatusArea"
            ? BoardAreaProminence.Live
            : BoardAreaProminence.Supporting;
    }

    private static List<BoardCardPresentation> Present(
        IReadOnlyList<CardDescriptor> cards,
        string zone)
    {
        var presented = new List<BoardCardPresentation>();
        var concealed = new Dictionary<CardBack, int>();
        foreach (CardDescriptor card in cards)
        {
            if (card.Face is null && card.Id is null)
            {
                concealed[card.Back] = concealed.GetValueOrDefault(card.Back) + 1;
                continue;
            }

            presented.Add(Present(card, zone));
        }

        foreach ((CardBack back, int count) in concealed)
        {
            string noun = count == 1 ? "card" : "cards";
            presented.Add(new BoardCardPresentation(
                TargetId: null,
                Count: count,
                Concealed: true,
                Title: $"{count} concealed {back.ToString().ToLowerInvariant()} {noun}",
                Subtitle: "Identity and order hidden",
                Kind: "CONCEALED PILE",
                Status: $"{back.ToString().ToUpperInvariant()} BACK",
                Fields: [])
            {
                Back = back.ToString().ToUpperInvariant(),
                StageRole = ProgressiveStageRole(zone),
            });
        }

        return presented;
    }

    private static BoardCardPresentation Present(CardDescriptor card, string zone)
    {
        if (card.Face is null)
        {
            return new BoardCardPresentation(
                card.Id,
                Count: 1,
                Concealed: true,
                Title: $"Face-down {card.Back.ToString().ToLowerInvariant()} card",
                Subtitle: "Identity hidden",
                Kind: "CONCEALED CARD",
                Status: Status(card, zone, kind: null),
                Fields: [])
            {
                Back = card.Back.ToString().ToUpperInvariant(),
                StageRole = ProgressiveStageRole(zone),
            };
        }

        bool inPlay = IsInPlay(zone);
        return new BoardCardPresentation(
            card.Id,
            Count: 1,
            Concealed: false,
            card.Face.Title,
            card.Face.Subtitle,
            Humanize(card.Face.Kind.ToString(), trimArea: false).ToUpperInvariant(),
            Status(card, zone, card.Face.Kind),
            card.Face.Fields
                .Where(field => VisibleField(field, inPlay, card.Face.Kind))
                .OrderBy(field => field.Key, StringComparer.Ordinal)
                .Select(field => new BoardFieldPresentation(
                    FieldName(field.Key), FieldValue(field, card.Face.Damage)))
                .ToArray())
        {
            Back = card.Back.ToString().ToUpperInvariant(),
            FaceId = card.Face.ArtFaceId,
            StageRole = ProgressiveStageRole(zone),
            Traits = card.Face.Traits,
            Cost = card.Face.Cost,
            PrintedStats = card.Face.PrintedStats
                .Where(field => field.Key != "Class")
                .Select(field => new BoardFieldPresentation(field.Key, field.Value))
                .ToArray(),
            Classification = card.Face.PrintedStats.GetValueOrDefault("Class", string.Empty),
            Keywords = card.Face.Keywords,
            RulesText = card.Face.RulesText,
            RulesMarkup = card.Face.RulesMarkup,
            Damage = card.Face.Damage,
            Counters = card.Face.Counters
                .OrderBy(counter => counter.Key, StringComparer.Ordinal)
                .Select(counter => new BoardFieldPresentation(
                    Humanize(counter.Key, trimArea: false).ToUpperInvariant(),
                    counter.Value.ToString(CultureInfo.InvariantCulture)))
                .ToArray(),
        };
    }

    private static bool VisibleField(
        KeyValuePair<string, long> field, bool inPlay, CardKind kind)
    {
        if (field.Key.StartsWith("t_", StringComparison.Ordinal) || field.Key == "is_exhaust")
        {
            return false;
        }
        bool schemeThreat = field.Key == "k_threat"
            && kind is CardKind.MainScheme or CardKind.EncounterSideScheme;
        if (field.Key == "k_threat" && !schemeThreat)
        {
            return false;
        }
        return field.Value != 0 || inPlay && ShowsZero(field.Key, schemeThreat);
    }

    private static bool ShowsZero(string key, bool schemeThreat) =>
        LiveZeroFields.Contains(key) || key == "health" || schemeThreat;

    private static string FieldName(string key) => Humanize(
        key.StartsWith("k_", StringComparison.Ordinal) ? key[2..] : key,
        trimArea: false).ToUpperInvariant();

    private static string FieldValue(KeyValuePair<string, long> field, long damage) =>
        field.Key == "health"
            ? $"{field.Value.ToString(CultureInfo.InvariantCulture)}/{(field.Value + damage).ToString(CultureInfo.InvariantCulture)}"
            : field.Value.ToString(CultureInfo.InvariantCulture);

    private static BoardStageRole ProgressiveStageRole(string zone) => zone switch
    {
        "VillainArea" or "MainSchemesArea" => BoardStageRole.Current,
        "VillainDeck" or "MainSchemesDeck" => BoardStageRole.Upcoming,
        _ => BoardStageRole.None,
    };

    private static string Status(CardDescriptor card, string zone, CardKind? kind)
    {
        bool inPlay = IsInPlay(zone);
        bool canExhaust = kind is null or CardKind.AlterEgo or CardKind.Hero
            or CardKind.Ally or CardKind.Support or CardKind.Upgrade;
        string status = inPlay && canExhaust
            ? card.Ready ? "READY" : "EXHAUSTED"
            : string.Empty;
        if (inPlay && !card.FaceUp)
        {
            status += status.Length == 0 ? "FACE DOWN" : "  ·  FACE DOWN";
        }
        if (card.Host >= 0)
        {
            status += status.Length == 0 ? $"HOST {card.Host}" : $"  ·  HOST {card.Host}";
        }
        return status;
    }

    private static bool IsInPlay(string zone) =>
        Enum.TryParse(zone, out DeckType deckType) && DeckTypes.IsInPlay(deckType);

    private static string Humanize(string value, bool trimArea)
    {
        string text = trimArea && value.EndsWith("Area", StringComparison.Ordinal)
            ? value[..^"Area".Length]
            : value;
        var result = new StringBuilder(text.Length + 8);
        for (int index = 0; index < text.Length; index++)
        {
            char current = text[index];
            if (index > 0 && char.IsUpper(current)
                && (char.IsLower(text[index - 1])
                    || index + 1 < text.Length && char.IsLower(text[index + 1])))
            {
                result.Append(' ');
            }

            result.Append(current);
        }

        return result.ToString();
    }
}

/// <summary>One engine-provided area and its two card containers.</summary>
public sealed record BoardAreaPresentation(
    int Id,
    string Title,
    string Context,
    IReadOnlyList<BoardCardPresentation> Cards,
    IReadOnlyList<BoardCardPresentation> Removed)
{
    /// <summary>The descriptor zone name, retained for diagnostics and generic rendering.</summary>
    public string Zone { get; init; } = string.Empty;

    /// <summary>The scenario or player table coordinate, not card ownership.</summary>
    public int Seat { get; init; } = -1;

    /// <summary>The visible host card id, or -1.</summary>
    public int Host { get; init; } = -1;

    /// <summary>Nesting depth beneath a visible host area.</summary>
    public int Depth { get; init; }

    /// <summary>The visible host title, or empty when unhosted.</summary>
    public string HostedBy { get; init; } = string.Empty;

    /// <summary>How prominently the desktop table should present this area.</summary>
    public BoardAreaProminence Prominence { get; init; } = BoardAreaProminence.Empty;
}

/// <summary>Presentation-only table priority; it does not change area visibility.</summary>
public enum BoardAreaProminence
{
    /// <summary>No card currently occupies the area.</summary>
    Empty,

    /// <summary>The area contains cards but is not part of the round-to-round tableau.</summary>
    Supporting,

    /// <summary>The area contains live state players commonly monitor each round.</summary>
    Live,
}

/// <summary>
/// The authoritative progressive-stage role retained from the engine area's
/// zone before current and upcoming stages are combined for presentation.
/// </summary>
public enum BoardStageRole
{
    /// <summary>The card is not part of a progressive villain or main-scheme stack.</summary>
    None,

    /// <summary>The card occupies the engine's active villain or main-scheme area.</summary>
    Current,

    /// <summary>The card occupies the engine's out-of-play upcoming-stage deck.</summary>
    Upcoming,
}

/// <summary>One readable card, face-down object, or concealed pile summary.</summary>
public sealed record BoardCardPresentation(
    int? TargetId,
    int Count,
    bool Concealed,
    string Title,
    string Subtitle,
    string Kind,
    string Status,
    IReadOnlyList<BoardFieldPresentation> Fields)
{
    /// <summary>The card's engine-zone-derived role in a progressive stage stack.</summary>
    public BoardStageRole StageRole { get; init; }

    /// <summary>The non-identifying physical back.</summary>
    public string Back { get; init; } = string.Empty;

    /// <summary>The stable visible face id used only for optional local art.</summary>
    public string? FaceId { get; init; }

    /// <summary>Effective traits visible on the current face.</summary>
    public IReadOnlyList<string> Traits { get; init; } = [];

    /// <summary>The printed aspect or Basic classification.</summary>
    public string Classification { get; init; } = string.Empty;

    /// <summary>The printed cost, or null when none is printed.</summary>
    public string? Cost { get; init; }

    /// <summary>Printed stats, kept separate from current live values.</summary>
    public IReadOnlyList<BoardFieldPresentation> PrintedStats { get; init; } = [];

    /// <summary>Printed keyword labels.</summary>
    public IReadOnlyList<string> Keywords { get; init; } = [];

    /// <summary>Printed rules text.</summary>
    public string RulesText { get; init; } = string.Empty;

    /// <summary>Printed rules text with display-only emphasis and symbol tokens.</summary>
    public string RulesMarkup { get; init; } = string.Empty;

    /// <summary>Damage currently on the card.</summary>
    public long Damage { get; init; }

    /// <summary>Live counters currently on the card.</summary>
    public IReadOnlyList<BoardFieldPresentation> Counters { get; init; } = [];
}

/// <summary>One live public field rendered on a readable card.</summary>
public sealed record BoardFieldPresentation(string Name, string Value);
