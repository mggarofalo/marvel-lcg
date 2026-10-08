using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.View;

/// <summary>Discloses evaluated quantities after face visibility has been decided.</summary>
internal static class CardValueProjection
{
    private static readonly IReadOnlyDictionary<string, string> Fields = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["ATK"] = "attack", ["THW"] = "thwart", ["DEF"] = "defense", ["REC"] = "recover",
        ["SCH"] = "scheme", ["HS"] = "hand_size",
    };

    internal static WorldDescriptor WithValues(World world, WorldDescriptor visible, ViewScope scope)
    {
        var readable = visible.Areas.SelectMany(area => area.Cards.Concat(area.Removed))
            .Where(card => card.Id is not null && card.Face is not null)
            .ToDictionary(card => card.Id!.Value);
        return visible with
        {
            Areas = visible.Areas.Select(area => area with
            {
                Cards = area.Cards.Select(card => Describe(world, card, readable, scope)).ToArray(),
                Removed = area.Removed.Select(card => Describe(world, card, readable, scope)).ToArray(),
            }).ToArray(),
        };
    }

    private static CardDescriptor Describe(
        World world, CardDescriptor descriptor, IReadOnlyDictionary<int, CardDescriptor> readable, ViewScope scope)
    {
        if (descriptor.Id is not int id || descriptor.Face is not { } face) return descriptor;
        Card card = world.Cards[id];
        if (!DeckTypes.IsInPlay(card.Area.Type)) return descriptor;
        var values = new Dictionary<string, CardEffectiveValue>(StringComparer.Ordinal);
        foreach ((string attribute, string field) in Fields)
        {
            if (face.PrintedStats.ContainsKey(attribute))
                values.Add(attribute, Value(world,
                    CardValues.Evaluate(world, card, field, world.Facts, world.Players), readable, scope));
        }
        if (face.PrintedStats.ContainsKey("HP"))
            values.Add("HP", Value(world, CardValues.MaximumHealth(world, card, world.Facts), readable, scope));
        return descriptor with { Face = face with { EffectiveValues = values } };
    }

    private static CardEffectiveValue Value(
        World world, CardValueEvaluation value, IReadOnlyDictionary<int, CardDescriptor> readable, ViewScope scope)
    {
        var calculation = new List<CardValueCalculation>();
        foreach (CardValueStep step in value.Steps)
        {
            CardValueSourceDescriptor? source = step.Source is null ? null : CardSourceProjection.Describe(world, step.Source, readable, scope);
            if (source is not null)
                calculation.Add(new(step.Kind.ToString(), step.Amount, source, Duration(step.Duration)));
            else if (step.Kind == CardValueStepKind.Unmodifiable)
                calculation.Add(new(step.Kind.ToString(), 0, null, null));
        }
        return new(value.BaseValue, value.CurrentValue, value.BaseKind.ToString(),
            value.CurrentValue != value.BaseValue, calculation.ToArray());
    }

    private static CardValueDuration? Duration(Duration? duration) => duration is null
        ? null : new(duration.Until, duration.OnCondition, duration.Uses, duration.IsWhileInPlay);
}
