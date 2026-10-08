using Marvel.Rules.State;

namespace Marvel.View;

/// <summary>Filters passive persistent meaning after face and effective-value authorization.</summary>
internal static class CardPersistentProjection
{
    internal static WorldDescriptor WithFacts(World world, WorldDescriptor visible, ViewScope scope)
    {
        var readable = visible.Areas.SelectMany(area => area.Cards.Concat(area.Removed))
            .Where(card => card.Id is not null && card.Face is not null)
            .ToDictionary(card => card.Id!.Value);
        var contributions = Contributions(readable);
        return visible with
        {
            Areas = visible.Areas.Select(area => area with
            {
                Cards = area.Cards.Select(card => Describe(world, card, readable, contributions, scope)).ToArray(),
                Removed = area.Removed.Select(card => Describe(world, card, readable, contributions, scope)).ToArray(),
            }).ToArray(),
        };
    }

    private static Dictionary<int, List<CardContributionDescriptor>> Contributions(
        Dictionary<int, CardDescriptor> readable)
    {
        var found = new Dictionary<int, List<CardContributionDescriptor>>();
        foreach ((int target, CardDescriptor card) in readable)
        foreach ((string attribute, CardEffectiveValue value) in card.Face!.EffectiveValues.OrderBy(pair => pair.Key, StringComparer.Ordinal))
        foreach (CardValueCalculation step in value.Calculation)
        {
            if (step.Source is not { CardId: int source, Historical: false } || !readable.ContainsKey(source)) continue;
            if (!found.TryGetValue(source, out var list)) found[source] = list = [];
            list.Add(new(target, attribute, step.Operation, step.Amount, step.Duration));
        }
        return found;
    }

    private static CardDescriptor Describe(
        World world, CardDescriptor card, Dictionary<int, CardDescriptor> readable,
        IReadOnlyDictionary<int, List<CardContributionDescriptor>> contributions, ViewScope scope)
    {
        if (card.Id is not int id || card.Face is null) return card;
        PersistentCardDescription? facts = PersistentCardFacts.Describe(world, world.Cards[id]);
        if (facts is null) return card;
        CardValueSourceDescriptor? source = CardSourceProjection.Describe(world, facts.Source, readable, scope);
        if (source is not { Historical: false, CardId: not null }) return card;
        List<CardContributionDescriptor> applied = contributions.GetValueOrDefault(id) ?? [];
        if (!HasMeaning(facts, applied, card.Face.Kind)) return card;
        return card with
        {
            Persistent = new(source, Relation(facts.Relation, readable), applied,
                facts.Abilities.Select(Ability).ToArray(), facts.HasUnresolvedAbilities),
        };
    }

    private static bool HasMeaning(PersistentCardDescription facts, List<CardContributionDescriptor> applied, CardKind kind) =>
        applied.Count > 0 || facts.Abilities.Count > 0 || facts.HasUnresolvedAbilities || IsPersistentKind(kind);

    private static bool IsPersistentKind(CardKind kind) =>
        kind is CardKind.Upgrade or CardKind.Support or CardKind.Attachment or CardKind.Environment;

    private static CardRelationDescriptor Relation(PersistentCardRelation relation, Dictionary<int, CardDescriptor> readable) =>
        new(relation.Kind, relation.HostId is int host && readable.ContainsKey(host) ? host : null, relation.Controller);

    private static CardPersistentAbilityDescriptor Ability(PersistentAbility ability)
    {
        PersistentTrigger trigger = ability.Trigger;
        return new(new(trigger.Timing, trigger.Event, trigger.Subject, trigger.Actor,
                trigger.Target, trigger.Form, trigger.Player, trigger.AnyPlayer),
            ability.Costs.Select(cost => new CardPersistentCostDescriptor(cost.Operation,
                cost.Target, cost.Amount, cost.Resources, cost.Counter, cost.PrintedOnly)).ToArray(),
            ability.Effects.Select(effect => new CardPersistentEffectDescriptor(effect.Operation,
                effect.Target, effect.Amount, effect.Field, effect.Until, effect.After,
                effect.Condition is { } condition
                    ? new(condition.Target, condition.Counter, condition.Minimum) : null)).ToArray());
    }
}
