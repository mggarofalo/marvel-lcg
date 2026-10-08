using Marvel.Rules.Play;
using Marvel.Rules.Timing;
using static Marvel.Rules.State.StateFieldCatalog;

namespace Marvel.Rules.State;

/// <summary>Evaluates live quantities together with their actual contributions.</summary>
public static class CardValues
{
    /// <summary>Evaluates a modifiable engine field, including signed adjustment fields.</summary>
    public static CardValueEvaluation Evaluate(
        World world, Card card, string field, ICardFacts facts, int players)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(card);
        ArgumentNullException.ThrowIfNull(facts);
        (long value, CardValueBaseKind kind) = Base(card, field, facts, players);
        return Evaluate(world, card, field, facts, players, value, kind);
    }

    /// <summary>The character's maximum HP, including active HP modifiers.</summary>
    public static CardValueEvaluation MaximumHealth(World world, Card card, ICardFacts facts)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(card);
        ArgumentNullException.ThrowIfNull(facts);
        // `health` is a signed adjustment in the field contract. Its HP base
        // belongs here so consumers do not reconstruct maximum HP themselves.
        CardValueEvaluation adjustment = Evaluate(world, card, "health", facts, world.Players);
        long value = EffectiveCards.BaseValue(card, facts, "HP", world.Players);
        return adjustment with
        {
            BaseValue = value,
            BaseKind = IdentityBase(card),
            CurrentValue = value + adjustment.CurrentValue,
            Steps = adjustment.Steps.Select(step => step with { Result = value + step.Result }).ToArray(),
        };
    }

    /// <summary>Remaining HP changes with maximum HP while damage stays on the card.</summary>
    public static long RemainingHealth(World world, Card card, ICardFacts facts) =>
        Math.Max(0, MaximumHealth(world, card, facts).CurrentValue - card.Damage);

    private static CardValueEvaluation Evaluate(
        World world, Card card, string field, ICardFacts facts, int players,
        long value, CardValueBaseKind kind)
    {
        IReadOnlyList<ContinuousEffect> active = world.Effects.Active();
        var steps = new List<CardValueStep>();
        if (DefinedBase(world, card, field, active) is { } definition)
        {
            // rr:non-numerical-variable: "treat that variable as the defined value."
            value = definition.Amount;
            kind = CardValueBaseKind.Defined;
            steps.Add(new(CardValueStepKind.DefineBase, value, value, Source(world, definition), definition.Lasts));
        }
        ContinuousEffect? loss = active.FirstOrDefault(effect =>
            effect.Kind == Characteristics.LossOf(field) && effect.AppliesTo(world, card));
        if (loss is not null)
            return new(field, value, kind, 0,
                [.. steps, new(CardValueStepKind.Lost, 0, 0, Source(world, loss), loss.Lasts)]);

        if (IsUnmodifiable(card, field, facts))
        {
            // `rr:dash-value.3`: a referenced dash is an unmodifiable zero.
            return new(field, value, kind, 0, [new(CardValueStepKind.Unmodifiable, 0, 0)]);
        }

        long current = value;
        foreach ((long amount, CardSourceSnapshot source) in Attachments(world, card, field, facts, players))
        {
            current += amount;
            steps.Add(new(CardValueStepKind.Add, amount, current, source, Duration.WhileInPlay));
        }

        foreach (ContinuousEffect effect in Modifiers(world, card, field, active))
        {
            current += effect.Amount;
            steps.Add(new(CardValueStepKind.Add, effect.Amount, current, Source(world, effect), effect.Lasts));
        }

        // `rr:modifiers.4`: clamp complete values, not adjustment-only fields.
        if (kind != CardValueBaseKind.Adjustment && current < 0)
        {
            current = 0;
            steps.Add(new(CardValueStepKind.MinimumZero, 0, current));
        }
        return new(field, value, kind, current, steps.ToArray());
    }

    private static IEnumerable<ContinuousEffect> Modifiers(
        World world, Card card, string field, IReadOnlyList<ContinuousEffect> active) =>
        active.Where(effect => effect.ValueRole == ContinuousValueRole.Modifier
            && effect.Kind == field && effect.AppliesTo(world, card));

    private static ContinuousEffect? DefinedBase(
        World world, Card card, string field, IReadOnlyList<ContinuousEffect> active)
    {
        ContinuousEffect[] definitions = active.Where(effect =>
            effect.ValueRole == ContinuousValueRole.BaseDefinition
            && effect.Kind == field && effect.AppliesTo(world, card)).ToArray();
        if (definitions.Length == 0) return null;
        if (definitions.Length != 1 || !IsBasicPowerField(field)
            || definitions[0].Source != EffectSource.ConstantAbility || definitions[0].Card != card.ObjectId)
            throw new RulesNotImplementedException($"ambiguous or unsupported base definition for '{field}'");
        return definitions[0];
    }

    private static bool IsUnmodifiable(Card card, string field, ICardFacts facts) =>
        PrintedFrom.TryGetValue(field, out string? attribute)
        && PowerAttributes.Contains(attribute)
        && !EffectiveCards.HasProfile(card)
        && !HasUsablePrintedPower(facts, card.FaceId, attribute);

    private static IEnumerable<(long Amount, CardSourceSnapshot Source)> Attachments(
        World world, Card card, string field, ICardFacts facts, int players)
    {
        if (!ModifiedBy.TryGetValue(field, out string? plus)) yield break;
        foreach (Area area in world.Areas)
        {
            if (area.Host != card.ObjectId || !DeckTypes.IsInPlay(area.Type)) continue;
            foreach (Card attached in area.Cards)
            {
                long amount = facts.PrintedValue(attached.FaceId, plus, players);
                if (amount != 0) yield return (amount, CardSourceSnapshot.Capture(attached, facts));
            }
        }
    }

    private static (long Value, CardValueBaseKind Kind) Base(
        Card card, string field, ICardFacts facts, int players)
    {
        if (field == "ally_limit" && EffectiveCards.Kind(card, facts) is CardKind.Hero or CardKind.AlterEgo)
            return (AllyLimit, CardValueBaseKind.Rule);
        return PrintedFrom.TryGetValue(field, out string? attribute)
            ? (EffectiveCards.BaseValue(card, facts, attribute, players), IdentityBase(card))
            : (0, CardValueBaseKind.Adjustment);
    }

    private static CardValueBaseKind IdentityBase(Card card) =>
        EffectiveCards.HasProfile(card) ? CardValueBaseKind.Replacement : CardValueBaseKind.Printed;

    private static CardSourceSnapshot? Source(World world, ContinuousEffect effect) =>
        CardEffectSources.For(world, effect);
}
