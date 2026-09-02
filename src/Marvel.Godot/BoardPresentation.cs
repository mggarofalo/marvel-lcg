using System.Globalization;
using System.Text;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>A display-only board derived from one visibility-safe snapshot.</summary>
public sealed record BoardPresentation(IReadOnlyList<BoardAreaPresentation> Areas)
{
    /// <summary>Scenario, player, and fallback lanes used by the tabletop renderer.</summary>
    public IReadOnlyList<BoardLanePresentation> Lanes { get; init; } = [];

    /// <summary>Builds a fresh presentation without retaining or enriching engine state.</summary>
    public static BoardPresentation From(WorldDescriptor world)
    {
        ArgumentNullException.ThrowIfNull(world);
        var players = world.Players.ToDictionary(player => player.Seat);
        BoardAreaPresentation[] areas =
            [.. world.Areas.Select(area => Present(area, players))];
        BoardPlayerPresentation[] seats =
            [.. world.Players.Select(player => new BoardPlayerPresentation(
                player.Seat, player.Name))];
        return new BoardPresentation(areas)
        {
            Lanes = BoardLayout.Arrange(areas, seats),
        };
    }

    private static BoardAreaPresentation Present(
        AreaDescriptor area,
        Dictionary<int, PlayerDescriptor> players)
    {
        string owner = players.TryGetValue(area.Owner, out PlayerDescriptor? player)
            ? player.Name
            : area.Owner < 0 ? "Scenario" : $"Seat {area.Owner}";
        string context = $"AREA {area.Id}  ·  OWNER {owner.ToUpperInvariant()}";
        if (area.Host >= 0)
        {
            context += $"  ·  HOST {area.Host}";
        }

        return new BoardAreaPresentation(
            area.Id,
            Humanize(area.Zone, trimArea: true).ToUpperInvariant(),
            context,
            Present(area.Cards),
            Present(area.Removed))
        {
            Zone = area.Zone,
            Seat = area.Owner,
            Host = area.Host,
        };
    }

    private static List<BoardCardPresentation> Present(
        IReadOnlyList<CardDescriptor> cards)
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

            presented.Add(Present(card));
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
            });
        }

        return presented;
    }

    private static BoardCardPresentation Present(CardDescriptor card)
    {
        string status = card.Ready ? "READY" : "EXHAUSTED";
        status += card.FaceUp ? "  ·  FACE UP" : "  ·  FACE DOWN";
        if (card.Host >= 0)
        {
            status += $"  ·  HOST {card.Host}";
        }

        if (card.Face is null)
        {
            return new BoardCardPresentation(
                card.Id,
                Count: 1,
                Concealed: true,
                Title: $"Face-down {card.Back.ToString().ToLowerInvariant()} card",
                Subtitle: "Identity hidden",
                Kind: "CONCEALED CARD",
                Status: status,
                Fields: [])
            {
                Back = card.Back.ToString().ToUpperInvariant(),
            };
        }

        return new BoardCardPresentation(
            card.Id,
            Count: 1,
            Concealed: false,
            card.Face.Title,
            card.Face.Subtitle,
            Humanize(card.Face.Kind.ToString(), trimArea: false).ToUpperInvariant(),
            status,
            card.Face.Fields
                .Where(field => !field.Key.StartsWith("t_", StringComparison.Ordinal))
                .OrderBy(field => field.Key, StringComparer.Ordinal)
                .Select(field => new BoardFieldPresentation(
                    Humanize(
                        field.Key.StartsWith("k_", StringComparison.Ordinal)
                            ? field.Key[2..]
                            : field.Key,
                        trimArea: false).ToUpperInvariant(),
                    field.Value.ToString(CultureInfo.InvariantCulture)))
                .ToArray())
        {
            Back = card.Back.ToString().ToUpperInvariant(),
            FaceId = card.Face.Id,
            Traits = card.Face.Traits,
            Cost = card.Face.Cost,
            PrintedStats = card.Face.PrintedStats
                .Select(field => new BoardFieldPresentation(field.Key, field.Value))
                .ToArray(),
            Keywords = card.Face.Keywords,
            RulesText = card.Face.RulesText,
            Damage = card.Face.Damage,
            Counters = card.Face.Counters
                .OrderBy(counter => counter.Key, StringComparer.Ordinal)
                .Select(counter => new BoardFieldPresentation(
                    Humanize(counter.Key, trimArea: false).ToUpperInvariant(),
                    counter.Value.ToString(CultureInfo.InvariantCulture)))
                .ToArray(),
        };
    }

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
    /// <summary>The non-identifying physical back.</summary>
    public string Back { get; init; } = string.Empty;

    /// <summary>The stable visible face id used only for optional local art.</summary>
    public string? FaceId { get; init; }

    /// <summary>Effective traits visible on the current face.</summary>
    public IReadOnlyList<string> Traits { get; init; } = [];

    /// <summary>The printed cost, or null when none is printed.</summary>
    public string? Cost { get; init; }

    /// <summary>Printed stats, kept separate from current live values.</summary>
    public IReadOnlyList<BoardFieldPresentation> PrintedStats { get; init; } = [];

    /// <summary>Printed keyword labels.</summary>
    public IReadOnlyList<string> Keywords { get; init; } = [];

    /// <summary>Printed rules text.</summary>
    public string RulesText { get; init; } = string.Empty;

    /// <summary>Damage currently on the card.</summary>
    public long Damage { get; init; }

    /// <summary>Live counters currently on the card.</summary>
    public IReadOnlyList<BoardFieldPresentation> Counters { get; init; } = [];
}

/// <summary>One live public field rendered on a readable card.</summary>
public sealed record BoardFieldPresentation(string Name, string Value);
