using Marvel.Cards.Dsl;

namespace Marvel.Cards.Run;

/// <summary>Describes compiled deck searches without consulting hidden candidates.</summary>
internal static class AbilitySearchDescription
{
    internal static string? Action(AbilityEffect effect) => SearchChoice(effect) is { } choice
        && Criterion(choice.From) is { } criterion
            ? $"Search your deck for {criterion} and add it to your hand"
            : null;

    internal static string? Summary(AbilityEffect effect) => Action(effect) is { } action
        ? $"{action}. Choose a matching card if available, then shuffle your deck."
        : null;

    internal static string? Selection(AbilityEffect.ChooseCard choice) => IsAddAndShuffle(choice.Effect)
        && Criterion(choice.From) is { } criterion ? $"choose {criterion} to add to your hand" : null;

    internal static string? Commitment(AbilityEffect effect, string title) => IsAddAndShuffle(effect)
        ? $"Add {title} to your hand" : null;

    private static AbilityEffect.ChooseCard? SearchChoice(AbilityEffect effect) => effect switch
    {
        AbilityEffect.ChooseCard choice when IsAddAndShuffle(choice.Effect) => choice,
        AbilityEffect.Conditional
        {
            Test: AbilityCondition.Exists exists,
            Then: AbilityEffect.ChooseCard choice,
            Else: AbilityEffect.Shuffle { Area: AbilitySearchArea.YourDeck },
        } when SameSelection(exists.Cards, choice.From) && IsAddAndShuffle(choice.Effect) => choice,
        _ => null,
    };

    private static bool IsAddAndShuffle(AbilityEffect effect) => effect is AbilityEffect.Sequence
    {
        Effects: [AbilityEffect.CardAction
        {
            Instruction: AbilityCardInstruction.AddToHand,
            Selection: AbilityCardSelection.Bound { Binding: AbilityCardBinding.Chosen },
        }, AbilityEffect.Shuffle { Area: AbilitySearchArea.YourDeck }],
    };

    private static string? Criterion(AbilityCardSelection selector) => selector switch
    {
        AbilityCardSelection.WithTrait trait when Criterion(trait.Cards) is { } inner =>
            WithTrait(inner, trait.Trait),
        AbilityCardSelection.InAreas { Areas: [AbilitySearchArea.YourDeck], Title: null } area =>
            Article(string.Join(" ", new[] { area.Trait?.Replace('_', ' '),
                area.Kind?.ToString().ToLowerInvariant() ?? "card" }.Where(value => value is not null))),
        _ => null,
    };

    private static string WithTrait(string criterion, string trait) =>
        Article($"{trait.Replace('_', ' ')} {criterion[(criterion.IndexOf(' ') + 1)..]}");

    private static string Article(string noun) =>
        $"{("aeiou".Contains(char.ToLowerInvariant(noun[0])) ? "an" : "a")} {noun}";

    private static bool SameSelection(AbilityCardSelection left, AbilityCardSelection right) =>
        (left, right) switch
        {
            (AbilityCardSelection.WithTrait a, AbilityCardSelection.WithTrait b) =>
                a.Trait == b.Trait && SameSelection(a.Cards, b.Cards),
            (AbilityCardSelection.InAreas a, AbilityCardSelection.InAreas b) =>
                a.Areas.SequenceEqual(b.Areas) && a.Kind == b.Kind && a.Trait == b.Trait && a.Title == b.Title,
            _ => false,
        };
}
