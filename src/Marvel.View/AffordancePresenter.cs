using Marvel.Rules.Prompts;
using Marvel.Rules.Timing;

namespace Marvel.View;

/// <summary>Projects source identity, targets and prices for an authorized offered action.</summary>
internal static class AffordancePresenter
{
    internal static AffordancePresentation Present(Affordance option, WorldDescriptor world)
    {
        AffordanceSourceDescriptor? source = Source(option, world);
        var sourceCopy = AffordanceSourcePresentation.From(source, world);
        return new AffordancePresentation(
            option.Id,
            option.Label,
            option.Description,
            PromptPresentation.Words(option.Verb),
            DescribeAnchor(option, world),
            option.AnchorId,
            option.AnchorPlayer,
            option.Illegal,
            option.Targets is null ? "No selection" : Describe(option.Targets),
            option.CostOptions.Select(Describe).ToArray())
        {
            DisplayLabel = option.DisplayLabel,
            CommitLabel = option.CommitLabel,
            SourceName = sourceCopy.Name,
            SourceState = sourceCopy.State,
            CostDescription = option.CostDescription,
            AnchorKind = option.AnchorKind,
            Source = source,
            TargetRequest = option.Targets,
            PlaysCard = option.PlaysCard,
            DeferredTargetSelection = option.DeferredTargetSelection,
            CostOptions = option.CostOptions,
            Relationships = Relationships(option, source, world),
        };
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
        return card is null ? $"Object {id}" : PromptPresentation.Describe(id, world);
    }

    private static string DescribeArea(int id, WorldDescriptor world)
    {
        AreaDescriptor? area = world.Areas.FirstOrDefault(candidate => candidate.Id == id);
        return area is null ? $"Object {id}" : PromptPresentation.Words(area.Zone);
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

}
