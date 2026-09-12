using Marvel.Cards.Dsl;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Cards.Run;

internal sealed class AreaProjectionState(AbilityAdmissionContext context)
{
    public Dictionary<int, long> Damage { get; } = [];
    public Dictionary<int, long> Tough { get; } = [];
    public Dictionary<int, long> Threat { get; } = [];
    public Dictionary<(int Card, string Status), long> Status { get; } = [];
    public HashSet<int> Departed { get; } = [];
    public HashSet<int> Entered { get; } = [];
    public Dictionary<int, int> Hosts { get; } = [];
    public Dictionary<int, int> EngagedWith { get; } = [];
    public Dictionary<int, HashSet<string>> Traits { get; } = [];
    public Dictionary<(int Card, string Field), long> Modifiers { get; } = [];
    public bool SourceReferenceCurrent { get; set; } = true;
    public int ActiveVillain { get; set; } =
        context.World.TheCardIn(DeckType.VillainArea)?.ObjectId ?? -1;
    public int VillainAttachmentHost { get; set; } = -1;

    public long DamageOf(Card card) =>
        Damage.GetValueOrDefault(card.ObjectId, card.Damage);

    public long ToughOf(AbilityAdmissionContext current, Card card) => Tough.GetValueOrDefault(
        card.ObjectId,
        Statuses.Count(current.World, card, Statuses.Tough));

    public long StatusOf(AbilityAdmissionContext current, Card card, string status) =>
        status == Statuses.Tough
            ? ToughOf(current, card)
            : Status.GetValueOrDefault(
                (card.ObjectId, status),
                Statuses.Count(current.World, card, status));

    public long ThreatOf(Card card) => Threat.GetValueOrDefault(
        card.ObjectId, card.Tokens.GetValueOrDefault("k_threat"));

    public long HealthOf(AbilityAdmissionContext current, Card card) => AbilityAmounts.SaturatingSum(
        FacedownDrones.BaseValue(
            card, current.World.Facts, "HP", current.World.Players),
        [ModifiedOf(current, card, "health")]);

    public long ModifiedOf(AbilityAdmissionContext current, Card card, string field)
    {
        long value = StateFields.Modified(
            current.World, card, field,
            current.World.Facts, current.World.Players);
        value += ProjectedConstantAdjustment(current, card, field);
        if (ProjectedPredicateInputsChanged(current)
            && ConditionalConstantMayModify(current, card, field))
        {
            throw new RulesNotImplementedException(
                $"'{current.Source.FaceId}' changes game state before reading "
                + $"a conditional constant '{field}' modifier on "
                + $"'{card.FaceId}'; projecting that predicate is not implemented");
        }
        value = AbilityAmounts.SaturatingSum(
            value,
            [Modifiers.GetValueOrDefault((card.ObjectId, field))]);

        string? printedModifier = field switch
        {
            "attack" => "ATK+",
            "scheme" => "SCH+",
            "thwart" => "THW+",
            _ => null,
        };
        if (printedModifier is null)
        {
            return value;
        }
        return value + ProjectedAttachmentAdjustment(
            current, card, printedModifier);
    }

    private long ProjectedConstantAdjustment(
        AbilityAdmissionContext current, Card card, string field)
    {
        long adjustment = 0;
        foreach (var effect in current.World.Effects.Active())
        {
            adjustment += HostedConstantAdjustment(current, card, field, effect);
            adjustment += DepartedConstantAdjustment(current, card, field, effect);
        }
        return adjustment;
    }

    private long HostedConstantAdjustment(
        AbilityAdmissionContext current, Card card, string field,
        ContinuousEffect effect)
    {
        if (effect.Source != EffectSource.ConstantAbility
            || effect.Card is not int source
            || Departed.Contains(source)
            || !Hosts.TryGetValue(source, out int projectedHost)
            || !string.Equals(effect.Kind, field, StringComparison.Ordinal))
        {
            return 0;
        }
        bool liveApplies = effect.AppliesTo(current.World, card);
        if (liveApplies && projectedHost != card.ObjectId) return -effect.Amount;
        return !liveApplies && projectedHost == card.ObjectId ? effect.Amount : 0;
    }

    private long DepartedConstantAdjustment(
        AbilityAdmissionContext current, Card card, string field,
        ContinuousEffect effect) =>
        effect.Source == EffectSource.ConstantAbility
        && effect.Card is int source
        && Departed.Contains(source)
        && string.Equals(effect.Kind, field, StringComparison.Ordinal)
        && effect.AppliesTo(current.World, card)
            ? -effect.Amount
            : 0;

    private long ProjectedAttachmentAdjustment(
        AbilityAdmissionContext current, Card card, string printedModifier)
    {
        long adjustment = 0;
        foreach (var attached in current.World.Cards.Where(candidate =>
                     candidate.Area.Host == card.ObjectId
                     && DeckTypes.IsInPlay(candidate.Area.Type)))
        {
            if (Departed.Contains(attached.ObjectId)
                || Hosts.TryGetValue(attached.ObjectId, out int projected)
                    && projected != card.ObjectId)
            {
                adjustment -= current.World.Facts.PrintedValue(
                    attached.FaceId, printedModifier, current.World.Players);
            }
        }
        foreach (var (attachedId, host) in Hosts)
        {
            var attached = current.World.Cards[attachedId];
            if (host == card.ObjectId
                && attached.Area.Host != card.ObjectId
                && !Departed.Contains(attachedId))
            {
                adjustment += current.World.Facts.PrintedValue(
                    attached.FaceId, printedModifier, current.World.Players);
            }
        }
        return adjustment;
    }

    private bool ProjectedPredicateInputsChanged(AbilityAdmissionContext current) =>
        ProjectedValuesChanged(current)
        || ProjectedMembershipChanged(current);

    private bool ProjectedValuesChanged(AbilityAdmissionContext current) =>
        Damage.Any(pair => pair.Value
            != current.World.Cards[pair.Key].Damage)
        || Tough.Any(pair => pair.Value != Statuses.Count(
            current.World, current.World.Cards[pair.Key], Statuses.Tough))
        || Threat.Any(pair => pair.Value != current.World.Cards[pair.Key]
            .Tokens.GetValueOrDefault("k_threat"))
        || Status.Any(pair => pair.Value != Statuses.Count(
            current.World,
            current.World.Cards[pair.Key.Card], pair.Key.Status));

    private bool ProjectedMembershipChanged(AbilityAdmissionContext current) =>
        Departed.Count > 0
        || Entered.Count > 0
        || Hosts.Count > 0
        || EngagedWith.Count > 0
        || Traits.Count > 0
        || Modifiers.Count > 0
        || ActiveVillain != (current.World.TheCardIn(
            DeckType.VillainArea)?.ObjectId ?? -1);

    private bool ConditionalConstantMayModify(
        AbilityAdmissionContext current, Card target, string field)
    {
        var sources = current.World.Areas
            .Where(area => DeckTypes.IsInPlay(area.Type))
            .SelectMany(area => area.Cards)
            .Concat(Entered.Select(id => current.World.Cards[id]))
            .Where(source => !Departed.Contains(source.ObjectId))
            .DistinctBy(source => source.ObjectId);
        return sources
            .Any(source => AbilityProgramQueries.On(current.Program, source)
                .Where(ability =>
                    ability.Trigger.Timing == AbilityType.Constant)
                .Any(ability => ConditionalGrant(
                    ability.Effect, field, ability.When is not null,
                    source, target, current)));
    }

    private bool ConditionalGrant(
        AbilityEffect effect, string field, bool conditioned,
        Card source, Card target, AbilityAdmissionContext current)
    {
        if (effect is AbilityEffect.Conditional conditional)
            return ConditionalBranchesGrant(conditional, field, source, target, current);
        if (effect is AbilityEffect.GrantField { Until: null } grant
            && string.Equals(grant.Field, field, StringComparison.Ordinal))
        {
            bool dynamicAmount = grant.Amount is not AbilityNumber.Constant;
            return (conditioned || dynamicAmount)
                && GrantCouldAffect(grant.Cards, source, target, current);
        }
        if (effect is AbilityEffect.Sequence sequence)
        {
            return sequence.Effects.Any(child =>
                ConditionalGrant(
                    child, field, conditioned, source, target, current));
        }
        if (effect is AbilityEffect.Simultaneous simultaneous)
        {
            return simultaneous.Effects.Any(child =>
                ConditionalGrant(
                    child, field, conditioned, source, target, current));
        }
        return false;
    }

    private bool ConditionalBranchesGrant(
        AbilityEffect.Conditional conditional, string field,
        Card source, Card target, AbilityAdmissionContext current) =>
        conditional.Then is { } then && ConditionalGrant(
            then, field, conditioned: true, source, target, current)
        || conditional.Else is { } otherwise && ConditionalGrant(
            otherwise, field, conditioned: true, source, target, current);

    private bool GrantCouldAffect(
        AbilityCardSelection selector, Card source, Card target, AbilityAdmissionContext current)
    {
        if (selector is AbilityCardSelection.Bound { Binding: AbilityCardBinding.This })
        {
            return source.ObjectId == target.ObjectId;
        }
        if (selector is AbilityCardSelection.Bound { Binding: AbilityCardBinding.AttachedTo })
        {
            return Hosts.GetValueOrDefault(
                source.ObjectId, source.Area.Host) == target.ObjectId;
        }
        if (selector is AbilityCardSelection.Titled titled)
        {
            return string.Equals(
                current.World.Facts.Title(target.FaceId),
                titled.Title, StringComparison.Ordinal);
        }
        if (selector is AbilityCardSelection.Query { Kind: AbilityCardQuery.Villain })
        {
            return CardKinds.IsVillain(
                current.World.Facts.Kind(target.FaceId));
        }
        // Other selectors may change membership with the projected facts.
        // Failing closed is required until that membership is projected.
        return true;
    }

    public bool HasTrait(AbilityAdmissionContext current, Card card, string trait)
    {
        var active = current.World.Effects.Active()
            .Where(effect => effect.Source != EffectSource.ConstantAbility
                || effect.Card is not int source
                || !Departed.Contains(source))
            .ToList();
        bool lost = active.Any(effect =>
            ProjectedTraitEffectApplies(current, effect, card)
            && string.Equals(
                effect.Kind,
                Characteristics.Lost + Rules.State.Traits.Granted + trait,
                StringComparison.Ordinal));
        if (lost)
        {
            return false;
        }
        return FacedownDrones.InherentTraits(card, current.World.Facts)
                .Contains(trait, StringComparer.Ordinal)
            || active.Any(effect =>
                ProjectedTraitEffectApplies(current, effect, card)
                && string.Equals(
                    effect.Kind,
                    Rules.State.Traits.Granted + trait,
                    StringComparison.Ordinal))
            || Traits.TryGetValue(card.ObjectId, out var granted)
                && granted.Contains(trait);
    }

    private bool ProjectedTraitEffectApplies(
        AbilityAdmissionContext current, ContinuousEffect effect, Card card)
    {
        if (effect.Source == EffectSource.ConstantAbility
            && effect.Card is int source
            && Hosts.TryGetValue(source, out int projectedHost)
            && current.World.Cards[source].Area.Host == effect.Affects)
        {
            return projectedHost == card.ObjectId;
        }
        return effect.AppliesTo(current.World, card);
    }

    public AreaProjectionState Clone()
    {
        var clone = new AreaProjectionState(context);
        foreach (var (card, amount) in Damage)
        {
            clone.Damage[card] = amount;
        }
        foreach (var (card, count) in Tough)
        {
            clone.Tough[card] = count;
        }
        foreach (var (card, amount) in Threat)
        {
            clone.Threat[card] = amount;
        }
        foreach (var (key, count) in Status)
        {
            clone.Status[key] = count;
        }
        clone.Departed.UnionWith(Departed);
        clone.Entered.UnionWith(Entered);
        foreach (var (card, host) in Hosts)
        {
            clone.Hosts[card] = host;
        }
        foreach (var (card, player) in EngagedWith)
        {
            clone.EngagedWith[card] = player;
        }
        foreach (var (card, traits) in Traits)
        {
            clone.Traits[card] = [.. traits];
        }
        foreach (var (key, amount) in Modifiers)
        {
            clone.Modifiers[key] = amount;
        }
        clone.ActiveVillain = ActiveVillain;
        clone.VillainAttachmentHost = VillainAttachmentHost;
        clone.SourceReferenceCurrent = SourceReferenceCurrent;
        return clone;
    }

    public static List<AreaProjectionState> Distinct(
        IEnumerable<AreaProjectionState> states) =>
        states.GroupBy(state => state.Key(), StringComparer.Ordinal)
            .Select(group => group.First()).ToList();

    public string Key() => string.Join(
        ";",
        Damage.OrderBy(pair => pair.Key)
            .Select(pair => $"d{pair.Key}:{pair.Value}")
            .Concat(Tough.OrderBy(pair => pair.Key)
                .Select(pair => $"s{pair.Key}:{pair.Value}"))
            .Concat(Threat.OrderBy(pair => pair.Key)
                .Select(pair => $"t{pair.Key}:{pair.Value}"))
            .Concat(Status.OrderBy(pair => pair.Key.Card)
                .ThenBy(pair => pair.Key.Status, StringComparer.Ordinal)
                .Select(pair =>
                    $"x{pair.Key.Card}:{pair.Key.Status}:{pair.Value}"))
            .Concat(Departed.Order().Select(card => $"o{card}"))
            .Concat(Entered.Order().Select(card => $"i{card}"))
            .Concat(Hosts.OrderBy(pair => pair.Key)
                .Select(pair => $"h{pair.Key}:{pair.Value}"))
            .Concat(EngagedWith.OrderBy(pair => pair.Key)
                .Select(pair => $"e{pair.Key}:{pair.Value}"))
            .Concat(Traits.OrderBy(pair => pair.Key)
                .SelectMany(pair => pair.Value.Order(StringComparer.Ordinal)
                    .Select(trait => $"g{pair.Key}:{trait}")))
            .Concat(Modifiers.OrderBy(pair => pair.Key.Card)
                .ThenBy(pair => pair.Key.Field, StringComparer.Ordinal)
                .Select(pair =>
                    $"m{pair.Key.Card}:{pair.Key.Field}:{pair.Value}"))
            .Append($"v{ActiveVillain}:{VillainAttachmentHost}")
            .Append($"r{SourceReferenceCurrent}"));
}
