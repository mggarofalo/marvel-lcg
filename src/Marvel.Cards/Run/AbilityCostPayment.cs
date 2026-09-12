using System.Collections.Immutable;
using Marvel.Cards.Dsl;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;

namespace Marvel.Cards.Run;

/// <summary>A validated, ordered arrow-cost payment, consumed immediately by initiation.</summary>
/// <remarks>
/// Preparation snapshots the answer and source incarnation. Commitment executes
/// payment instructions, not the authored cost tree, and owns no effect frame.
/// This is an engine-local plan, not a saveable continuation.
/// </remarks>
internal sealed class AbilityCostPayment
{
    private readonly World world;
    private readonly Card source;
    private readonly int player;
    private readonly IResourceCardAbilities resourceAbilities;
    private readonly ImmutableArray<int> paying;
    private readonly ImmutableArray<Step> steps;

    private AbilityCostPayment(
        World world, Card source, int player, IResourceCardAbilities resourceAbilities,
        ImmutableArray<int> paying,
        ImmutableArray<Step> steps)
    {
        this.world = world;
        this.source = source;
        this.player = player;
        this.resourceAbilities = resourceAbilities;
        this.paying = paying;
        this.steps = steps;
    }

    internal static AbilityCostPayment Prepare(
        World world, Card source, int player, AbilityCost? cost,
        IReadOnlyList<int> paying, IReadOnlyList<int> chosen,
        AbilityProgram program, IResourceCardAbilities resourceAbilities,
        IReadOnlyDictionary<string, long>? values = null,
        bool resourcesPaidByEvent = false)
        => new Preparation(
            world, source, player, paying, chosen, program, resourceAbilities,
            values, resourcesPaidByEvent).Build(cost);

    internal AbilityPaymentResult Commit(ICardCounterPools pools, string trigger, List<GameEvent> events)
    {
        var outcome = new CommitOutcome();
        foreach (var step in steps)
        {
            if (CommitImmediate(step, outcome, trigger, events)) continue;

            var healthBefore = world.Effects.CaptureCharacterHealth();
            CommitBound(step, outcome, pools, trigger, events);

            Statuses.RemoveAfflictionsIfStalwart(world, world.Facts, "stalwart", events);
            outcome.Suspended |= world.Effects.SettleLostHealth(healthBefore, trigger, events);
            Attack.RefreshDefender(world, world.Facts);
        }
        return new(outcome.Healed, outcome.Energy, outcome.Suspended);
    }

    private bool CommitImmediate(
        Step step, CommitOutcome outcome, string trigger, List<GameEvent> events)
    {
        switch (step)
        {
            case Spend spend:
                CardPlay.Spend(
                    world, world.Facts, resourceAbilities, [world.Seats[player].Hand],
                    paying, spend.Required.Length, spend.Required, -1, player, events);
                if (spend.Energy is { } x) outcome.Energy = x;
                return true;
            case DiscardHand discard:
                CommitDiscardHand(discard, events);
                return true;
            case ExhaustSelected exhaust:
                foreach (var card in exhaust.Cards)
                    AbilityCardOperations.Exhaust(card, trigger, events);
                return true;
            case TakeDamage damage:
                CommitTakeDamage(damage, outcome, trigger, events);
                return true;
            default:
                return false;
        }
    }

    private void CommitDiscardHand(DiscardHand discard, List<GameEvent> events)
    {
        foreach (var card in discard.Cards)
        {
            if (card.Area != discard.Hand)
            {
                throw new RulesNotImplementedException(
                    $"card {card.ObjectId} is no longer in the hand paying this cost");
            }
            Discard.Card(world, card, CardPlay.Verb, events);
        }
    }

    private void CommitTakeDamage(
        TakeDamage damage, CommitOutcome result, string trigger, List<GameEvent> events)
    {
        long before = damage.Target.Damage;
        var outcome = DamagePlacement.DealOutcome(
            world, world.Facts, source, damage.Target, damage.Amount,
            trigger, CardPlay.Verb, events);
        long taken = damage.Target.Damage - before;
        if (taken != damage.Amount)
        {
            // rr:cost.12: "If any of the damage is prevented, then the cost has
            // not been paid." Remaining costs and the post-arrow effect do not run.
            throw new RulesNotImplementedException(
                $"'{source.FaceId}' requires {damage.Amount} damage to be taken as a "
                + $"cost, but only {taken} was taken; rr:cost.12 leaves it unpaid");
        }
        result.Suspended |= outcome == Damage.Outcome.Suspended;
    }

    private void CommitBound(
        Step step, CommitOutcome outcome, ICardCounterPools pools,
        string trigger, List<GameEvent> events)
    {
        switch (step)
        {
            case Exhaust exhaust:
                CommitExhaust(exhaust, trigger, events);
                break;
            case DiscardBound discard:
                CommitDiscardBound(discard, trigger, events);
                break;
            case RemoveCounters removal:
                CommitRemoveCounters(removal, pools, trigger, events);
                break;
            case Heal heal:
                outcome.Healed = CommitHeal(heal, trigger, events);
                break;
            case DealDamage damage:
                outcome.Suspended |= CommitDamage(damage, trigger, events);
                break;
            default:
                throw new InvalidOperationException("Unknown prepared payment instruction");
        }
    }

    private static void CommitExhaust(
        Exhaust exhaust, string trigger, List<GameEvent> events)
    {
        if (exhaust.Target.Current is { } target)
            AbilityCardOperations.Exhaust(target, trigger, events);
    }

    private void CommitDiscardBound(
        DiscardBound discard, string trigger, List<GameEvent> events)
    {
        if (discard.Target.Current is not { } target) return;
        bool removableArea = DeckTypes.IsInPlay(target.Area.Type)
            || target.Area.Type is DeckType.BoostingArea
                or DeckType.ProcessingArea or DeckType.RevealingArea;
        if (removableArea && Discard.EffectCanRemove(world, world.Facts, source, target))
            Discard.CardFromEffect(world, world.Facts, source, target, trigger, events);
    }

    private void CommitRemoveCounters(
        RemoveCounters removal, ICardCounterPools pools,
        string trigger, List<GameEvent> events)
    {
        var holder = removal.Target.Current
            ?? throw new RulesNotImplementedException(
                $"'{source.FaceId}' cannot find the card paying its counter cost");
        AbilityCardOperations.RemoveCounters(
            world, pools, holder, removal.Counter, removal.Count, trigger, events);
    }

    private long CommitHeal(Heal heal, string trigger, List<GameEvent> events) =>
        heal.Target.Current is { } target
            ? DamageRecovery.Heal(
                world, world.Facts, target, heal.Amount, trigger, "Heal", events)
            : 0;

    private bool CommitDamage(
        DealDamage damage, string trigger, List<GameEvent> events)
    {
        // Costs are not attacks. Event modifiers remain live because an earlier
        // payment can remove the card granting a modifier.
        long amount = AbilityAmounts.SaturatingSum(damage.Amount,
            [AbilityEventModifiers.Amount(world, source, "eventDamage")]);
        return damage.Target.Current is { } target
            && DamagePlacement.DealOutcome(
                world, world.Facts, source, target, amount, trigger,
                "Deal_Damage", events) == Damage.Outcome.Suspended;
    }

    private sealed class CommitOutcome
    {
        internal long? Healed { get; set; }
        internal long? Energy { get; set; }
        internal bool Suspended { get; set; }
    }

    private sealed class Preparation
    {
        private readonly World world;
        private readonly Card source;
        private readonly int player;
        private readonly ImmutableArray<int> paying;
        private readonly ImmutableArray<int> selected;
        private readonly AbilityProgram program;
        private readonly IResourceCardAbilities resourceAbilities;
        private readonly ImmutableDictionary<string, long>? variables;
        private readonly bool resourcesPaidByEvent;
        private readonly int incarnation;
        private readonly ImmutableArray<Step>.Builder ordered =
            ImmutableArray.CreateBuilder<Step>();

        internal Preparation(
            World world, Card source, int player, IReadOnlyList<int> paying,
            IReadOnlyList<int> chosen, AbilityProgram program,
            IResourceCardAbilities resourceAbilities,
            IReadOnlyDictionary<string, long>? values, bool resourcesPaidByEvent)
        {
            this.world = world;
            this.source = source;
            this.player = player;
            this.paying = paying.ToImmutableArray();
            selected = chosen.ToImmutableArray();
            this.program = program;
            this.resourceAbilities = resourceAbilities;
            variables = values?.ToImmutableDictionary(StringComparer.Ordinal);
            this.resourcesPaidByEvent = resourcesPaidByEvent;
            incarnation = source.Incarnation;
        }

        internal AbilityCostPayment Build(AbilityCost? cost)
        {
            PrepareSteps(cost);
            return new AbilityCostPayment(
                world, source, player, resourceAbilities, paying, ordered.ToImmutable());
        }

        private BoundCard Bind(AbilityCostCard binding) => binding switch
        {
            AbilityCostCard.Source => new(source, incarnation),
            AbilityCostCard.Identity when player >= 0 =>
                new(world.Seats[player].IdentityCard, null),
            _ => throw new RulesNotImplementedException(
                $"'{source.FaceId}' has no resolving player for its cost target"),
        };

        private void PrepareSteps(AbilityCost? component)
        {
            AbilityPaymentPricing.ValidatePayment(
                component, paying, selected, variables, world, source, player,
                program, resourceAbilities);
            if (component is null) return;
            if (component is AbilityCost.Sequence sequence)
            {
                PrepareSequence(sequence);
                return;
            }
            if (component is AbilityCost.Spend or AbilityCost.SpendEnergy)
            {
                PrepareResource(component);
                return;
            }
            PreparePhysical(component);
        }

        private void PrepareSequence(AbilityCost.Sequence sequence)
        {
            // rr:cost.12: uncertain damage resolves before irreversible payment.
            foreach (var taking in sequence.Costs.OfType<AbilityCost.Damage>()
                         .Where(damage => damage.MustTakeAll))
                PrepareSteps(taking);
            var spends = sequence.Costs.OfType<AbilityCost.Spend>().ToList();
            if (!resourcesPaidByEvent && spends.Count > 0)
                ordered.Add(new Spend(string.Concat(spends.Select(step => step.Resources))));
            foreach (var remaining in sequence.Costs.Where(step =>
                         step is not (AbilityCost.Spend
                             or AbilityCost.Damage { MustTakeAll: true })))
                PrepareSteps(remaining);
        }

        private void PrepareResource(AbilityCost component)
        {
            if (resourcesPaidByEvent) return;
            if (component is AbilityCost.Spend spend)
            {
                ordered.Add(new Spend(spend.Resources));
                return;
            }
            long x = AbilityPaymentPricing.DefinedVariable(variables, "X", source);
            ordered.Add(new Spend(
                new string(Resources.Energy, checked((int)x)), Energy: x));
        }

        private void PreparePhysical(AbilityCost component)
        {
            switch (component)
            {
                case AbilityCost.DiscardFromHand:
                    ordered.Add(new DiscardHand(world.Seats[player].Hand,
                        [.. selected.Select(id => world.Cards[id])]));
                    break;
                case AbilityCost.ExhaustChosen:
                    ordered.Add(new ExhaustSelected(
                        [.. selected.Select(id => world.Cards[id])]));
                    break;
                case AbilityCost.Exhaust exhaust:
                    ordered.Add(new Exhaust(Bind(exhaust.Card)));
                    break;
                case AbilityCost.Discard discard:
                    ordered.Add(new DiscardBound(Bind(discard.Card)));
                    break;
                case AbilityCost.RemoveCounters counters:
                    ordered.Add(new RemoveCounters(
                        Bind(counters.Card), counters.Counter, counters.Count));
                    break;
                case AbilityCost.Heal heal:
                    ordered.Add(new Heal(Bind(heal.Card), heal.Amount));
                    break;
                case AbilityCost.Damage damage:
                    ordered.Add(damage.MustTakeAll
                        ? new TakeDamage(Bind(damage.Card).Card, damage.Amount)
                        : new DealDamage(Bind(damage.Card), damage.Amount));
                    break;
                default:
                    throw new InvalidOperationException(
                        "Unknown compiled cost in payment preparation");
            }
        }
    }

    private sealed record BoundCard(Card Card, int? Incarnation)
    {
        internal Card? Current => Incarnation is null || Card.Incarnation == Incarnation ? Card : null;
    }

    private abstract record Step;
    private sealed record Spend(string Required, long? Energy = null) : Step;
    private sealed record DiscardHand(Area Hand, ImmutableArray<Card> Cards) : Step;
    private sealed record ExhaustSelected(ImmutableArray<Card> Cards) : Step;
    private sealed record TakeDamage(Card Target, long Amount) : Step;
    private sealed record Exhaust(BoundCard Target) : Step;
    private sealed record DiscardBound(BoundCard Target) : Step;
    private sealed record RemoveCounters(BoundCard Target, string Counter, long Count) : Step;
    private sealed record Heal(BoundCard Target, long Amount) : Step;
    private sealed record DealDamage(BoundCard Target, long Amount) : Step;
}

/// <summary>Only payment outcomes needed by post-arrow resolution.</summary>
internal readonly record struct AbilityPaymentResult(long? Healed, long? Energy, bool Suspended);
