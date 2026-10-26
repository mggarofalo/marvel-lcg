using Marvel.Cards.Dsl;

namespace Marvel.Cards.Run;

/// <summary>Describes compiled deck searches without consulting hidden candidates.</summary>
internal static class AbilitySearchDescription
{
    internal static string? Action(AbilityEffect effect) => SearchChoice(effect) is { } choice
        && Criterion(choice.From) is { } criterion
        && SearchExtent(choice.From) is { } extent
            ? $"Search {extent} for {criterion} and add it to your hand"
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
        AbilityCardSelection.WithTrait trait when trait.Cards is not AbilityCardSelection.InAreas { Title: not null }
            && Criterion(trait.Cards) is { } inner =>
            trait.Exclude ? $"{inner} without the {trait.Trait} trait" : WithTrait(inner, trait.Trait),
        AbilityCardSelection.InAreas area => AreaCriterion(area),
        _ => null,
    };

    private static string? AreaCriterion(AbilityCardSelection.InAreas selection)
    {
        if (selection.Set is { } set)
            return AreaCriterion(selection with { Set = null }) is { } criterion
                ? $"{criterion} from the {System.Globalization.CultureInfo.InvariantCulture.TextInfo.ToTitleCase(set.Replace('_', ' '))} set"
                : null;
        return selection switch
        {
            AbilityCardSelection.InAreas
            {
                Title: { } title, Trait: null, AspectOnly: false, CardClass: null,
            } area when SearchExtent(area) is not null =>
                area.Kind is { } kind ? $"the {title} {kind.ToString().ToLowerInvariant()}" : title,
            AbilityCardSelection.InAreas { Title: null } area when SearchExtent(area) is not null =>
                Article(string.Join(" ", new[] { area.AspectOnly ? "aspect" : null,
                    area.CardClass == "Hero" ? "hero-specific" : area.CardClass?.ToLowerInvariant(), area.Trait?.Replace('_', ' '),
                    area.Kind?.ToString().ToLowerInvariant() ?? "card" }.Where(value => value is not null))),
            _ => null,
        };
    }

    private static string? SearchExtent(AbilityCardSelection selector) => selector switch
    {
        AbilityCardSelection.WithTrait trait => SearchExtent(trait.Cards),
        AbilityCardSelection.InAreas { Areas: [AbilitySearchArea.YourDeck], Top: { } count } =>
            $"the top {count} cards of your deck",
        AbilityCardSelection.InAreas { Areas: [AbilitySearchArea.YourDeck] } => "your deck",
        AbilityCardSelection.InAreas { Top: null, Areas: [AbilitySearchArea.YourDeck, AbilitySearchArea.YourDiscardPile] }
            or AbilityCardSelection.InAreas { Top: null, Areas: [AbilitySearchArea.YourDiscardPile, AbilitySearchArea.YourDeck] } =>
            "your deck and discard pile",
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
                a.Trait == b.Trait && a.Exclude == b.Exclude && SameSelection(a.Cards, b.Cards),
            (AbilityCardSelection.InAreas a, AbilityCardSelection.InAreas b) =>
                SameAreaCriteria(a, b),
            _ => false,
        };
    private static bool SameAreaCriteria(AbilityCardSelection.InAreas left, AbilityCardSelection.InAreas right) =>
        left.Areas.SequenceEqual(right.Areas) && left.Kind == right.Kind && left.Trait == right.Trait
        && left.Title == right.Title && left.Top == right.Top && left.AspectOnly == right.AspectOnly
        && left.CardClass == right.CardClass && left.Set == right.Set;
}
