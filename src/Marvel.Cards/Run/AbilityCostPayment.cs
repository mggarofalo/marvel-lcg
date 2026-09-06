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
    private readonly ImmutableArray<int> paying;
    private readonly ImmutableArray<Step> steps;

    private AbilityCostPayment(
        World world, Card source, int player, ImmutableArray<int> paying,
        ImmutableArray<Step> steps)
    {
        this.world = world;
        this.source = source;
        this.player = player;
        this.paying = paying;
        this.steps = steps;
    }

    internal static AbilityCostPayment Prepare(
        World world, Card source, int player, AbilityCost? cost,
        IReadOnlyList<int> paying, IReadOnlyList<int> chosen,
        IReadOnlyDictionary<string, long>? values = null,
        bool resourcesPaidByEvent = false)
    {
        var generators = paying.ToImmutableArray();
        var selected = chosen.ToImmutableArray();
        var variables = values?.ToImmutableDictionary(StringComparer.Ordinal);
        int incarnation = source.Incarnation;
        var ordered = ImmutableArray.CreateBuilder<Step>();
        PrepareSteps(cost);
        return new(world, source, player, generators, ordered.ToImmutable());

        BoundCard Bind(AbilityCostCard binding) => binding switch
        {
            AbilityCostCard.Source => new(source, incarnation),
            AbilityCostCard.Identity when player >= 0 => new(world.Seats[player].IdentityCard, null),
            _ => throw new RulesNotImplementedException(
                $"'{source.FaceId}' has no resolving player for its cost target"),
        };

        void PrepareSteps(AbilityCost? component)
        {
            AbilityPaymentRules.ValidatePayment(
                component, generators, selected, variables, world, source, player);
            if (component is null) return;
            if (component is AbilityCost.Sequence sequence)
            {
                // rr:cost.12: "If any of the damage is prevented, then the
                // cost has not been paid." Resolve that uncertain component
                // before irreversible resources, discards or exhaustions.
                foreach (var taking in sequence.Costs.OfType<AbilityCost.Damage>()
                             .Where(damage => damage.MustTakeAll))
                    PrepareSteps(taking);
                var spends = sequence.Costs.OfType<AbilityCost.Spend>().ToList();
                if (!resourcesPaidByEvent && spends.Count > 0)
                    ordered.Add(new Spend(string.Concat(spends.Select(spend => spend.Resources))));
                foreach (var remaining in sequence.Costs.Where(step =>
                             step is not (AbilityCost.Spend or AbilityCost.Damage { MustTakeAll: true })))
                    PrepareSteps(remaining);
                return;
            }

            switch (component)
            {
                case AbilityCost.Spend spend when !resourcesPaidByEvent:
                    ordered.Add(new Spend(spend.Resources));
                    break;
                case AbilityCost.SpendEnergy when !resourcesPaidByEvent:
                    long x = AbilityPaymentRules.DefinedVariable(variables, "X", source);
                    ordered.Add(new Spend(new string(Resources.Energy, checked((int)x)), Energy: x));
                    break;
                case AbilityCost.Spend or AbilityCost.SpendEnergy:
                    break;
                case AbilityCost.DiscardFromHand:
                    ordered.Add(new DiscardHand(world.Seats[player].Hand,
                        [.. selected.Select(id => world.Cards[id])]));
                    break;
                case AbilityCost.ExhaustChosen:
                    ordered.Add(new ExhaustSelected([.. selected.Select(id => world.Cards[id])]));
                    break;
                case AbilityCost.Exhaust exhaust:
                    ordered.Add(new Exhaust(Bind(exhaust.Card)));
                    break;
                case AbilityCost.Discard discard:
                    ordered.Add(new DiscardBound(Bind(discard.Card)));
                    break;
                case AbilityCost.RemoveCounters counters:
                    ordered.Add(new RemoveCounters(Bind(counters.Card), counters.Counter, counters.Count));
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
                    throw new InvalidOperationException("Unknown compiled cost in payment preparation");
            }
        }
    }

    internal AbilityPaymentResult Commit(ICardCounterPools pools, string trigger, List<GameEvent> events)
    {
        long? healed = null;
        long? energy = null;
        bool suspended = false;
        foreach (var step in steps)
        {
            switch (step)
            {
                case Spend spend:
                    CardPlay.Spend(world, world.Facts, [world.Seats[player].Hand], paying,
                        spend.Required.Length, spend.Required, itself: -1, player, events);
                    if (spend.Energy is { } x) energy = x;
                    continue;
                case DiscardHand discard:
                    foreach (var card in discard.Cards)
                    {
                        if (card.Area != discard.Hand)
                            throw new RulesNotImplementedException(
                                $"card {card.ObjectId} is no longer in the hand paying this cost");
                        Discard.Card(world, card, CardPlay.Verb, events);
                    }
                    continue;
                case ExhaustSelected exhaust:
                    foreach (var card in exhaust.Cards)
                        AbilityCardOperations.Exhaust(card, trigger, events);
                    continue;
                case TakeDamage damage:
                    long before = damage.Target.Damage;
                    var outcome = Damage.DealOutcome(world, world.Facts, source, damage.Target,
                        damage.Amount, trigger, CardPlay.Verb, events);
                    long taken = damage.Target.Damage - before;
                    if (taken != damage.Amount)
                    {
                        // rr:cost.12: "If any of the damage is prevented, then
                        // the cost has not been paid." Prevention has occurred;
                        // the remaining costs and post-arrow effect have not.
                        throw new RulesNotImplementedException(
                            $"'{source.FaceId}' requires {damage.Amount} damage to be taken as a "
                            + $"cost, but only {taken} was taken; rr:cost.12 leaves it unpaid");
                    }
                    suspended |= outcome == Damage.Outcome.Suspended;
                    continue;
            }

            var healthBefore = world.Effects.CaptureCharacterHealth();
            switch (step)
            {
                case Exhaust exhaust:
                    if (exhaust.Target.Current is { } exhausted)
                        AbilityCardOperations.Exhaust(exhausted, trigger, events);
                    break;
                case DiscardBound discard:
                    if (discard.Target.Current is { } discarded
                        && (DeckTypes.IsInPlay(discarded.Area.Type)
                            || discarded.Area.Type is DeckType.BoostingArea or DeckType.ProcessingArea or DeckType.RevealingArea)
                        && Discard.EffectCanRemove(world, world.Facts, source, discarded))
                        Discard.CardFromEffect(world, world.Facts, source, discarded, trigger, events);
                    break;
                case RemoveCounters removal:
                    var holder = removal.Target.Current
                        ?? throw new RulesNotImplementedException(
                            $"'{source.FaceId}' cannot find the card paying its counter cost");
                    AbilityCardOperations.RemoveCounters(
                        world, pools, holder, removal.Counter, removal.Count, trigger, events);
                    break;
                case Heal heal:
                    healed = heal.Target.Current is { } target
                        ? Damage.Heal(world, world.Facts, target, heal.Amount, trigger, "Heal", events)
                        : 0;
                    break;
                case DealDamage damage:
                    // Costs are not attacks. Event modifiers remain live: an
                    // earlier payment can remove the card granting a modifier.
                    long amount = AbilityAmounts.SaturatingSum(damage.Amount,
                        [AbilityEventModifiers.Amount(world, source, "eventDamage")]);
                    if (damage.Target.Current is { } damaged)
                        suspended |= Damage.DealOutcome(world, world.Facts, source, damaged,
                            amount, trigger, "Deal_Damage", events) == Damage.Outcome.Suspended;
                    break;
                default:
                    throw new InvalidOperationException("Unknown prepared payment instruction");
            }

            Statuses.RemoveAfflictionsIfStalwart(world, world.Facts, "stalwart", events);
            suspended |= world.Effects.SettleLostHealth(healthBefore, trigger, events);
            Attack.RefreshDefender(world, world.Facts);
        }
        return new(healed, energy, suspended);
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
