using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.View;

/// <summary>Display text and object references derived only from a visible prompt.</summary>
public sealed record PromptPresentation(
    string Heading,
    string Context,
    string Resolution,
    string Requirement,
    string Diagnostic,
    IReadOnlyList<AffordancePresentation> Affordances)
{
    /// <summary>Readable cards whose occurrence caused the pending decision.</summary>
    public IReadOnlyList<BoardCardPresentation> ContextCards { get; init; } = [];

    /// <summary>Readable timing tier for resolution-stage treatment.</summary>
    public string ResolutionKind { get; init; } = string.Empty;

    /// <summary>Builds one prompt view from its response's authorized snapshot.</summary>
    public static PromptPresentation From(Prompt prompt, WorldDescriptor world)
    {
        ArgumentNullException.ThrowIfNull(prompt);
        ArgumentNullException.ThrowIfNull(world);
        IReadOnlyList<BoardCardPresentation> contextCards = PresentContextCards(prompt, world);
        return new PromptPresentation(
            BuildHeading(prompt, contextCards),
            BuildContext(prompt, world, contextCards),
            prompt.Description?.Trim() ?? string.Empty,
            prompt.Cancellable ? "You may pass." : "Choose to continue.",
            $"Player {prompt.Player + 1} · {Words(prompt.Asking.ToString())}"
                + $" · {Words(prompt.When.ToString())} · {Words(prompt.Trigger)}"
                + $"\nWire label: {prompt.Label.Trim()}",
            prompt.Affordances.Select(option => Present(option, world)).ToArray())
        {
            ContextCards = contextCards,
            ResolutionKind = Words(prompt.When.ToString()),
        };
    }

    private static List<BoardCardPresentation> PresentContextCards(
        Prompt prompt, WorldDescriptor world)
    {
        var locations = world.Areas
            .SelectMany(area => area.Cards.Concat(area.Removed)
                .Select(card => (Card: card, area.Zone)))
            .Where(entry => entry.Card.Id is not null && entry.Card.Face is not null)
            .ToDictionary(entry => entry.Card.Id!.Value);
        var presented = new List<BoardCardPresentation>();
        foreach (int id in prompt.ContextCardIds.Distinct())
        {
            if (locations.TryGetValue(id, out var entry))
            {
                presented.Add(PresentContext(entry.Card, entry.Zone));
            }
        }
        return presented;
    }

    private static BoardCardPresentation PresentContext(CardDescriptor card, string zone) =>
        BoardCardPresentationFactory.Present([card], zone)[0];

    private static AffordancePresentation Present(Affordance option, WorldDescriptor world)
    {
        AffordanceSourceDescriptor? source = Source(option, world);
        return new AffordancePresentation(
            option.Id,
            option.Label,
            option.Description,
            Words(option.Verb),
            DescribeAnchor(option, world),
            option.AnchorId,
            option.AnchorPlayer,
            option.Illegal,
            option.Targets is null ? "No selection" : Describe(option.Targets),
            option.CostOptions.Select(Describe).ToArray())
        {
            AnchorKind = option.AnchorKind,
            Source = source,
            TargetRequest = option.Targets,
            CostOptions = option.CostOptions,
            Relationships = Relationships(option, source, world),
        };
    }

    private static string BuildHeading(
        Prompt prompt, IReadOnlyList<BoardCardPresentation> contextCards) =>
        string.IsNullOrWhiteSpace(prompt.DisplayQuestion)
            ? ContextHeading(prompt, contextCards) ?? GenericHeading(prompt)
            : prompt.DisplayQuestion.Trim();

    private static string? ContextHeading(
        Prompt prompt, IReadOnlyList<BoardCardPresentation> contextCards)
    {
        string? title = contextCards.Count > 0 ? contextCards[0].Title : null;
        if (title is null)
        {
            return null;
        }
        return (prompt.Asking, prompt.When) switch
        {
            (Question.Opportunity, TimingPriority.Interrupt) =>
                $"{title} was revealed — interrupt?",
            (Question.Opportunity, TimingPriority.Response) => $"Respond to {title}",
            _ => null,
        };
    }

    private static string GenericHeading(Prompt prompt) => prompt.Asking switch
    {
        Question.TurnOption => "Choose an action",
        Question.Option => "Choose an option",
        Question.Order => "Choose an order",
        Question.Opportunity when prompt.When == TimingPriority.Interrupt =>
            "Choose an interrupt",
        Question.Opportunity when prompt.When == TimingPriority.Response =>
            "Choose a response",
        Question.Opportunity => "Choose an ability",
        Question.Element => "Choose a game element",
        _ => "Choose what happens next",
    };

    private static string BuildContext(
        Prompt prompt, WorldDescriptor world,
        IReadOnlyList<BoardCardPresentation> contextCards)
    {
        string player = world.Players.FirstOrDefault(candidate => candidate.Seat == prompt.Player)
            ?.Name ?? $"Player {prompt.Player + 1}";
        string subject = contextCards.Count > 0
            ? $" · Resolving {contextCards[0].Title}"
            : string.Empty;
        return $"Decision for {player}{subject}";
    }

    private static AffordanceSourceDescriptor? Source(Affordance option, WorldDescriptor world)
    {
        if (option.AnchorKind == AffordanceAnchorKind.Card)
        {
            CardDescriptor? card = world.Areas
                .SelectMany(area => area.Cards.Concat(area.Removed))
                .FirstOrDefault(candidate => candidate.Id == option.AnchorId);
            return card?.Id is not null && card.Location is not null
                ? new AffordanceSourceDescriptor(option.AnchorKind, card.Id,
                    card.Location.AreaId, card.Location.Controller)
                : null;
        }

        if (option.AnchorKind != AffordanceAnchorKind.Area)
        {
            return null;
        }

        AreaDescriptor? area = world.Areas.FirstOrDefault(candidate => candidate.Id == option.AnchorId);
        return area is null ? null : new AffordanceSourceDescriptor(
            option.AnchorKind, null, area.Id, area.Owner);
    }

    private static List<TableRelationshipDescriptor> Relationships(
        Affordance option,
        AffordanceSourceDescriptor? source,
        WorldDescriptor world)
    {
        if (source?.CardId is not int sourceId)
        {
            return [];
        }
        var visible = world.Areas.SelectMany(area => area.Cards.Concat(area.Removed))
            .Where(card => card.Id is not null).Select(card => card.Id!.Value).ToHashSet();
        var relationships = new List<TableRelationshipDescriptor>();
        if (option.Targets is not null)
        {
            relationships.AddRange(option.Targets.Legal.Where(visible.Contains)
                .Select(target => new TableRelationshipDescriptor(
                    RelationshipKind.OfferedTarget, sourceId, target)));
        }
        foreach (ResourceSource generator in option.CostOptions.SelectMany(cost => cost.Generators))
        {
            if (visible.Contains(generator.Effect))
            {
                relationships.Add(new TableRelationshipDescriptor(
                    RelationshipKind.OfferedGenerator, sourceId, generator.Effect));
            }
        }
        return relationships;
    }

    /// <summary>Names an authorized board object, or leaves an opaque fallback.</summary>
    public static string Describe(int id, WorldDescriptor world)
    {
        CardDescriptor? card = world.Areas
            .SelectMany(area => area.Cards.Concat(area.Removed))
            .FirstOrDefault(candidate => candidate.Id == id);
        if (card?.Face is { } face)
        {
            return string.IsNullOrWhiteSpace(face.Subtitle)
                ? face.Title
                : $"{face.Title} · {face.Subtitle}";
        }

        if (card is not null)
        {
            return $"Face-down {card.Back.ToString().ToLowerInvariant()} card";
        }

        AreaDescriptor? area = world.Areas.FirstOrDefault(candidate => candidate.Id == id);
        return area is null ? $"Object {id}" : Words(area.Zone);
    }

    private static string DescribeAnchor(Affordance option, WorldDescriptor world) =>
        option.AnchorKind switch
        {
            AffordanceAnchorKind.Card => DescribeCard(option.AnchorId, world),
            AffordanceAnchorKind.Area => DescribeArea(option.AnchorId, world),
            _ => $"Object {option.AnchorId}",
        };

    private static string DescribeCard(int id, WorldDescriptor world)
    {
        CardDescriptor? card = world.Areas.SelectMany(area => area.Cards.Concat(area.Removed))
            .FirstOrDefault(candidate => candidate.Id == id);
        return card is null ? $"Object {id}" : Describe(id, world);
    }

    private static string DescribeArea(int id, WorldDescriptor world)
    {
        AreaDescriptor? area = world.Areas.FirstOrDefault(candidate => candidate.Id == id);
        return area is null ? $"Object {id}" : Words(area.Zone);
    }

    private static string Describe(TargetRequest request)
    {
        if (request.IsGrouped)
        {
            return $"Choose one of {request.Groups!.Count} complete groups";
        }

        string count = request.Min == request.Max
            ? $"Choose {request.Min}"
            : $"Choose {request.Min}–{request.Max}";
        string mode = request.IsSearch ? " search results" : " targets";
        if (request.AllowRepeated)
        {
            mode += " with repetition";
        }

        return count + mode + $" from {request.Legal.Count}";
    }

    private static string Describe(CostOption cost)
    {
        string primary = $"Cost {cost.Cost}{CostRequirement(cost.Rule)}";
        string alternative = cost.HasAlternative
            ? $" or {cost.OrCost}{CostRequirement(cost.OrRule)}"
            : string.Empty;
        return primary + alternative + $" · {cost.Generators.Count} generators";
    }

    private static string CostRequirement(IReadOnlyList<string>? rule) =>
        rule is { Count: > 0 } ? $" [{string.Join(", ", rule)}]" : string.Empty;

    /// <summary>Converts a wire identifier into readable words.</summary>
    public static string Words(string value)
    {
        var result = new System.Text.StringBuilder(value.Length + 8);
        for (int index = 0; index < value.Length; index++)
        {
            char current = value[index];
            if (index > 0 && (current == '_'
                || char.IsUpper(current) && char.IsLower(value[index - 1])))
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
}
