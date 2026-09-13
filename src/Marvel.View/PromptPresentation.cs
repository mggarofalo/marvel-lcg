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
    /// <summary>Builds one prompt view from its response's authorized snapshot.</summary>
    public static PromptPresentation From(Prompt prompt, WorldDescriptor world)
    {
        ArgumentNullException.ThrowIfNull(prompt);
        ArgumentNullException.ThrowIfNull(world);
        return new PromptPresentation(
            BuildHeading(prompt),
            BuildContext(prompt, world),
            prompt.Description?.Trim() ?? string.Empty,
            prompt.Cancellable ? "You may pass." : "Choose to continue.",
            $"Player {prompt.Player + 1} · {Words(prompt.Asking.ToString())}"
                + $" · {Words(prompt.When.ToString())} · {Words(prompt.Trigger)}"
                + $"\nWire label: {prompt.Label.Trim()}",
            prompt.Affordances.Select(option => Present(option, world)).ToArray());
    }

    private static AffordancePresentation Present(Affordance option, WorldDescriptor world)
    {
        AffordanceSourceDescriptor? source = Source(option, world);
        return new AffordancePresentation(
            option.Id,
            option.Label,
            option.Description,
            Words(option.Verb),
            Describe(option.AnchorId, world),
            option.AnchorId,
            option.AnchorPlayer,
            option.Illegal,
            option.Targets is null ? "No selection" : Describe(option.Targets),
            option.CostOptions.Select(Describe).ToArray())
        {
            Source = source,
            TargetRequest = option.Targets,
            CostOptions = option.CostOptions,
            Relationships = Relationships(option, source, world),
        };
    }

    private static string BuildHeading(Prompt prompt) => prompt.Asking switch
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

    private static string BuildContext(Prompt prompt, WorldDescriptor world)
    {
        string player = world.Players.FirstOrDefault(candidate => candidate.Seat == prompt.Player)
            ?.Name ?? $"Player {prompt.Player + 1}";
        return $"Decision for {player}";
    }

    private static AffordanceSourceDescriptor? Source(Affordance option, WorldDescriptor world)
    {
        CardDescriptor? card = world.Areas
            .SelectMany(area => area.Cards.Concat(area.Removed))
            .FirstOrDefault(candidate => candidate.Id == option.AnchorId);
        if (card?.Id is not null && card.Location is not null)
        {
            return new AffordanceSourceDescriptor(card.Id, card.Location.AreaId,
                card.Location.Controller);
        }

        AreaDescriptor? area = world.Areas.FirstOrDefault(candidate => candidate.Id == option.AnchorId);
        return area is null
            ? null
            : new AffordanceSourceDescriptor(null, area.Id, area.Owner);
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
