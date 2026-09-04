using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.View;

/// <summary>Display text and object references derived only from a visible prompt.</summary>
public sealed record PromptPresentation(
    string Heading,
    string Context,
    string Requirement,
    string Diagnostic,
    IReadOnlyList<AffordancePresentation> Affordances)
{
    /// <summary>Builds one prompt view from its response's authorized snapshot.</summary>
    public static PromptPresentation From(Prompt prompt, WorldDescriptor world)
    {
        ArgumentNullException.ThrowIfNull(prompt);
        ArgumentNullException.ThrowIfNull(world);
        string player = world.Players.FirstOrDefault(candidate => candidate.Seat == prompt.Player)
            ?.Name ?? $"Player {prompt.Player + 1}";
        string? source = Source(prompt.Label, world);
        return new PromptPresentation(
            PlayerQuestion(prompt, world, player, source),
            (source is null ? $"Decision for {player}" : $"From {source}")
                + (string.IsNullOrWhiteSpace(prompt.Description)
                    ? string.Empty
                    : $"\n{prompt.Description}"),
            prompt.Cancellable ? "You may pass." : "Choose to continue.",
            $"Player {prompt.Player + 1} · {Words(prompt.Asking.ToString())}"
                + $" · {Words(prompt.When.ToString())} · {Words(prompt.Trigger)}"
                + $"\nWire label: {prompt.Label.Trim()}",
            prompt.Affordances.Select(option => new AffordancePresentation(
                option.Id,
                DisplayLabel(option.Label, option.AnchorId, world),
                option.Description,
                Words(option.Verb),
                Describe(option.AnchorId, world),
                option.AnchorId,
                option.AnchorPlayer,
                option.Illegal,
                option.Targets is null
                    ? "No selection"
                    : Describe(option.Targets),
                option.CostOptions.Select(Describe).ToArray(),
                CostConsequence(option, world))).ToArray());
    }

    private static string PlayerQuestion(
        Prompt prompt,
        WorldDescriptor world,
        string player,
        string? source)
    {
        if (prompt.Asking == Question.TurnOption)
        {
            if (prompt.Label.Contains("mulligan", StringComparison.OrdinalIgnoreCase))
            {
                return "Choose your opening hand";
            }

            if (prompt.Label.Contains("Forced Actions", StringComparison.OrdinalIgnoreCase))
            {
                return "Choose a Forced Action";
            }

            if (string.Equals(prompt.Trigger, "End Turn", StringComparison.Ordinal))
            {
                return "Choose end-of-phase discards";
            }

            return $"{player}'s turn";
        }

        string action = prompt.Asking switch
        {
            Question.Option => "Choose an option",
            Question.Order => "Choose the order",
            Question.Opportunity when prompt.When == TimingPriority.Interrupt =>
                "Choose an interrupt",
            Question.Opportunity when prompt.When == TimingPriority.Response =>
                "Choose a response",
            Question.Opportunity => "Choose an ability",
            Question.Element when string.Equals(
                prompt.Trigger, "ChooseAttachmentTarget", StringComparison.Ordinal) =>
                source is null
                    ? "Choose where to attach the revealed card"
                    : $"Choose where to attach {source}",
            Question.Element when ChoosesPlayer(prompt, world) => "Choose a player",
            Question.Element => Instruction(prompt.Label),
            _ => Instruction(prompt.Label),
        };
        return source is null || action.Contains(source, StringComparison.Ordinal)
            ? action
            : $"{action} for {source}";
    }

    private static bool ChoosesPlayer(Prompt prompt, WorldDescriptor world)
    {
        if (prompt.Affordances.Count == 0)
        {
            return false;
        }

        var faces = world.Areas
            .SelectMany(area => area.Cards.Concat(area.Removed))
            .Where(card => card.Id is not null && card.Face is not null)
            .ToDictionary(card => card.Id!.Value, card => card.Face!);
        return prompt.Affordances.All(option =>
            faces.TryGetValue(option.AnchorId, out CardFaceDescriptor? face)
            && face.Kind is CardKind.Hero or CardKind.AlterEgo);
    }

    private static string Instruction(string label)
    {
        string instruction = label.Contains(':', StringComparison.Ordinal)
            ? label[(label.IndexOf(':', StringComparison.Ordinal) + 1)..]
            : label;
        instruction = instruction.Trim().Trim('-').Trim();
        if (instruction.Length == 0)
        {
            return "Choose what happens next";
        }

        string readable = Words(instruction);
        return char.ToUpperInvariant(readable[0]) + readable[1..];
    }

    private static string? Source(string label, WorldDescriptor world)
    {
        int separator = label.IndexOf(':', StringComparison.Ordinal);
        string? prefixedId = separator > 0 ? label[..separator].Trim() : null;
        CardFaceDescriptor[] faces = [.. world.Areas
            .SelectMany(area => area.Cards.Concat(area.Removed))
            .Select(card => card.Face)
            .OfType<CardFaceDescriptor>()];
        CardFaceDescriptor? prefixed = faces.FirstOrDefault(face =>
            string.Equals(face.Id, prefixedId, StringComparison.Ordinal));
        if (prefixed is not null)
        {
            return prefixed.Title;
        }

        return faces
            .OrderByDescending(face => face.Id.Length)
            .FirstOrDefault(face => ContainsToken(label, face.Id))
            ?.Title;
    }

    private static bool ContainsToken(string text, string token)
    {
        int start = text.IndexOf(token, StringComparison.Ordinal);
        while (start >= 0)
        {
            int end = start + token.Length;
            bool beginsAtBoundary = start == 0 || !char.IsLetterOrDigit(text[start - 1]);
            bool endsAtBoundary = end == text.Length || !char.IsLetterOrDigit(text[end]);
            if (beginsAtBoundary && endsAtBoundary)
            {
                return true;
            }

            start = text.IndexOf(token, start + 1, StringComparison.Ordinal);
        }

        return false;
    }

    private static string DisplayLabel(string label, int anchorId, WorldDescriptor world)
    {
        CardDescriptor? anchor = world.Areas
            .SelectMany(area => area.Cards.Concat(area.Removed))
            .FirstOrDefault(card => card.Id == anchorId);
        return string.Equals(label, anchor?.Face?.Id, StringComparison.Ordinal)
            ? "Choose"
            : label;
    }

    private static string? CostConsequence(Affordance option, WorldDescriptor world)
    {
        if (!string.Equals(option.Verb, "Play", StringComparison.OrdinalIgnoreCase)
            || option.CostOptions.Count != 1)
        {
            return null;
        }

        CardDescriptor? card = world.Areas
            .SelectMany(area => area.Cards.Concat(area.Removed))
            .FirstOrDefault(candidate => candidate.Id == option.AnchorId);
        string? printed = card?.Face?.Cost;
        string current = option.CostOptions[0].Cost;
        if (string.IsNullOrWhiteSpace(printed)
            || string.Equals(printed, current, StringComparison.Ordinal))
        {
            return null;
        }

        return $"Current cost {current}; printed cost {printed}.";
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

/// <summary>One visible affordance row.</summary>
public sealed record AffordancePresentation(
    int Id,
    string Label,
    string? Description,
    string Verb,
    string Anchor,
    int AnchorId,
    int AnchorPlayer,
    string? Illegal,
    string Targets,
    IReadOnlyList<string> Costs,
    string? Consequence = null);
