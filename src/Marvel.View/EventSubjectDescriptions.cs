using System.Globalization;
using System.Text;
using Marvel.Rules.Events;

namespace Marvel.View;

/// <summary>Names only subjects and locations retained in an authorized event or snapshot.</summary>
internal static class EventSubjectDescriptions
{
    internal static string CardList(
        IEnumerable<int> ids, WorldDescriptor world, GameEvent? happened = null)
    {
        string[] names = ids.Select(id => Card(id, world, happened)).ToArray();
        return names.Length switch
        {
            0 => "no cards",
            1 => names[0],
            2 => $"{names[0]} and {names[1]}",
            _ => $"{string.Join(", ", names[..^1])}, and {names[^1]}",
        };
    }

    internal static string Possessive(int seat, WorldDescriptor world) =>
        $"{Player(seat, world)}'s";

    internal static string Card(
        int id, WorldDescriptor world, GameEvent? happened = null)
    {
        if (happened?.Subjects?.TryGetValue(id, out string? subject) == true)
        {
            return subject;
        }

        CardDescriptor? card = world.Areas
            .SelectMany(area => area.Cards.Concat(area.Removed))
            .FirstOrDefault(candidate => candidate.Id == id);
        if (card?.Face is { } face)
        {
            return face.Title;
        }

        if (card is not null)
        {
            return $"face-down {card.Back.ToString().ToLowerInvariant()} card";
        }

        // An authorized event may outlive the object's presence in the resulting
        // snapshot. Its response-scoped object id is safe; a printed face is not.
        return $"card {id.ToString(CultureInfo.InvariantCulture)}";
    }

    internal static string Area(AreaRef area, WorldDescriptor world)
    {
        string name = Words(area.Zone, trimArea: true).ToLowerInvariant();
        string owner = area.Owner < 0 ? "the scenario" : Player(area.Owner, world);
        if (area.Host >= 0)
        {
            return $"{owner}'s {name} on {Card(area.Host, world)}";
        }

        return $"{owner}'s {name}";
    }

    internal static string Player(int seat, WorldDescriptor world) =>
        seat < 0
            ? "the scenario"
            : world.Players.FirstOrDefault(player => player.Seat == seat)?.Name
                ?? $"player {seat + 1}";

    internal static string PlayArea(int seat, WorldDescriptor world) =>
        seat < 0 ? "The villain play area" : $"{Player(seat, world)}'s play area";

    internal static string Cause(GameEvent happened)
    {
        string verb = Words(happened.Verb);
        string trigger = Words(happened.Trigger);
        return (verb.Length, trigger.Length) switch
        {
            (0, 0) => "Engine resolution",
            (> 0, 0) => verb,
            (0, > 0) => trigger,
            _ => $"{verb} · {trigger}",
        };
    }

    internal static string Words(string value, bool trimArea = false)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        string normalized = value.StartsWith("k_", StringComparison.Ordinal)
            ? value[2..]
            : value;
        string text = trimArea && normalized.EndsWith("Area", StringComparison.Ordinal)
            ? normalized[..^"Area".Length]
            : normalized;
        var result = new StringBuilder(text.Length + 8);
        for (int index = 0; index < text.Length; index++)
        {
            char current = text[index];
            if (StartsWord(text, index, current))
            {
                result.Append(' ');
            }

            if (current != '_')
            {
                result.Append(current);
            }
        }

        return result.ToString();
    }

    internal static bool StartsWord(string text, int index, char current) =>
        index > 0
        && (current == '_' || char.IsUpper(current) && char.IsLower(text[index - 1]));
}
