using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.View;

/// <summary>Display text and object references derived only from a visible prompt.</summary>
/// <param name="Heading">The one-line prompt title.</param>
/// <param name="Frame">Who answers and what the decision concerns; the client words it.</param>
/// <param name="Diagnostic">Wire identifiers for diagnostics, never shown as game copy.</param>
/// <param name="Affordances">The offered answers.</param>
public sealed record PromptPresentation(
    string Heading,
    DecisionFrame Frame,
    string Diagnostic,
    IReadOnlyList<AffordancePresentation> Affordances)
{
    /// <summary>Engine-authored commitment when declining is legal.</summary>
    public string DeclineLabel { get; init; } = "Pass this opportunity";

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
            BuildFrame(prompt, world, contextCards),
            $"Player {prompt.Player + 1} · {Words(prompt.Asking.ToString())}"
                + $" · {Words(prompt.When.ToString())} · {Words(prompt.Trigger)}"
                + $"\nWire label: {prompt.Label.Trim()}",
            prompt.Affordances.Select(option => AffordancePresenter.Present(option, world)).ToArray())
        {
            ContextCards = contextCards,
            DeclineLabel = prompt.DeclineLabel,
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
        foreach (int id in prompt.ContextCardIds.Concat(prompt.CauseCardIds).Distinct())
        {
            if (locations.TryGetValue(id, out var entry))
            {
                presented.Add(PresentContext(entry.Card, entry.Zone));
            }
        }
        return presented;
    }

    private static string[] Causes(Prompt prompt, WorldDescriptor world) => [.. prompt.CauseCardIds
        .Where(id => world.Areas.SelectMany(area => area.Cards.Concat(area.Removed))
            .Any(card => card.Id == id && card.Face is not null))
        .Select(id => Describe(id, world)).Distinct()];

    private static BoardCardPresentation PresentContext(CardDescriptor card, string zone) =>
        BoardCardPresentationFactory.Present([card], zone)[0];

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
                $"Interrupt {title}?",
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
        Question.Defender => "Choose a defender",
        Question.Element => "Choose a game element",
        _ => "Choose what happens next",
    };

    private static DecisionFrame BuildFrame(
        Prompt prompt, WorldDescriptor world,
        IReadOnlyList<BoardCardPresentation> contextCards)
    {
        string player = world.Players.FirstOrDefault(candidate => candidate.Seat == prompt.Player)
            ?.Name ?? $"Player {prompt.Player + 1}";
        DecisionSubject subject = prompt.PublicKind == PublicDecisionKind.MinionActivationOrder
            ? DecisionSubject.MinionActivation
            : contextCards.Count == 0 ? DecisionSubject.None
            : prompt.PublicKind == PublicDecisionKind.CardLook ? DecisionSubject.Inspecting
            : DecisionSubject.Resolving;
        return new DecisionFrame(player, subject,
            contextCards.Count > 0 ? contextCards[0].Title : null, prompt.Cancellable,
            Causes(prompt, world), prompt.Description?.Trim() ?? string.Empty);
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
