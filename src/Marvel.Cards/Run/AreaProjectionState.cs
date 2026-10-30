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
    // A missing entry means the physical card's live face remains active.
    private readonly Dictionary<int, string> faces = [];
    public IReadOnlyDictionary<int, string> Faces => faces;
    public Dictionary<(int Card, string Field), long> Modifiers { get; } = [];
    public bool SourceReferenceCurrent { get; set; } = true;
    public int ActiveVillain { get; set; } =
        EncounterRouting.Villain(context.World)?.ObjectId ?? -1;
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
        EffectiveCards.BaseValue(
            card, current.World.Facts, "HP", current.World.Players, FaceOf(card)),
        [ModifiedOf(current, card, "health")]);

    public string FaceOf(Card card) => Faces.GetValueOrDefault(card.ObjectId, card.FaceId);

    public void SetFace(Card card, string face)
    {
        if (!card.Faces.Contains(face, StringComparer.Ordinal))
            throw new ArgumentException("Face must belong to the card.", nameof(face));
        if (face == card.FaceId) faces.Remove(card.ObjectId);
        else faces[card.ObjectId] = face;
    }

    public long ModifiedOf(AbilityAdmissionContext current, Card card, string field)
    {
        new AbilityProjectedConstantQueries(this).RequireFaceConstantsProjected(current);
        var evaluation = CardValues.EvaluateRaw(
            current.World, card, field,
            current.World.Facts, current.World.Players, FaceOf(card));
        long value = evaluation.CurrentValue;
        value += ProjectedConstantAdjustment(current, card, field);
        if (ProjectedPredicateInputsChanged(current)
            && new AbilityProjectedConstantQueries(this).MayModify(current, card, field))
        {
            throw new RulesNotImplementedException(
                $"'{current.Source.FaceId}' changes game state before reading "
                + $"a conditional constant '{field}' modifier on "
                + $"'{card.FaceId}'; projecting that predicate is not implemented");
        }
        value = AddProjectedModifier(value, Modifiers.GetValueOrDefault((card.ObjectId, field)));

        string? printedModifier = field switch
        {
            "attack" => "ATK+",
            "scheme" => "SCH+",
            "thwart" => "THW+",
            _ => null,
        };
        if (printedModifier is null)
        {
            return ApplyBounds(current, card, field, evaluation.BaseKind, value);
        }
        return ApplyBounds(current, card, field, evaluation.BaseKind, value + ProjectedAttachmentAdjustment(
            current, card, printedModifier));
    }

    private static long AddProjectedModifier(long value, long modifier)
    {
        if (modifier > 0 && value > long.MaxValue - modifier) return long.MaxValue;
        if (modifier < 0 && value < long.MinValue - modifier) return long.MinValue;
        return value + modifier;
    }

    private long ApplyBounds(AbilityAdmissionContext current, Card card, string field,
        CardValueBaseKind kind, long value)
    {
        var maximum = CardValues.Maximum(current.World, card, field, Departed);
        value = CardValues.Floor(kind, value);
        return maximum is null ? value : Math.Min(value, maximum.Amount);
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
        || Faces.Count > 0
        || Modifiers.Count > 0
        || ActiveVillain != (current.World.TheCardIn(
            DeckType.VillainArea)?.ObjectId ?? -1);

    public bool HasTrait(AbilityAdmissionContext current, Card card, string trait)
    {
        new AbilityProjectedConstantQueries(this).RequireFaceConstantsProjected(current);
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
        return EffectiveCards.InherentTraits(card, current.World.Facts, FaceOf(card))
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
        foreach (var (card, face) in Faces)
        {
            clone.faces[card] = face;
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

    public string Key() => AreaProjectionKey.Key(this);
}
