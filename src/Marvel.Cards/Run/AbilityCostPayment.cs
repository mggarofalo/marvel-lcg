using static Marvel.Cards.Run.AbilityPaymentInstructions;
using System.Collections.Immutable;
using Marvel.Cards.Dsl;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;

namespace Marvel.Cards.Run;

/// <summary>A validated arrow-cost payment with an explicit resumable instruction cursor.</summary>
/// <remarks>
/// Preparation snapshots the answer and source incarnation. Commitment executes
/// payment instructions, not the authored cost tree, and owns no effect frame.
/// The owning invocation retains this plan across any cost procedure that suspends.
/// </remarks>
internal sealed class AbilityCostPayment
{
    private readonly World world;
    private readonly Card source;
    private readonly int player;
    private readonly IResourceCardAbilities resourceAbilities;
    private readonly ImmutableArray<int> paying;
    private readonly ImmutableArray<Step> steps;
    private readonly CommitOutcome outcome = new();
    private int nextStep;
    private ResourcePaymentReceipt? resourcePayment;
    private DamageCostReceipt? damagePayment;
    private readonly List<DamageCostReceipt> damageCosts = [];
    private bool settled;
    private AbilityMinionEntryCost? minionEntry;
    internal bool HasMinionEntryCost => minionEntry is not null;
    internal bool Unpaid => minionEntry is { IsComplete: true, Paid: false };
    private long? requiredTakenDamage;

    internal AbilityCostPayment(
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
        AbilityProgram program, IResourceCardAbilities resourceAbilities,
        AbilityPaymentSelection selection)
        => new AbilityCostPreparation(world, source, player, program, resourceAbilities,
            selection).Build(cost);

    internal AbilityPaymentResult Commit(AbilityExecution owner, ICardCounterPools pools, string trigger, List<GameEvent> events)
    {
        world.Agenda.Executions.RequireActive(owner);
        outcome.Suspended = false;
        outcome.Discarded.Clear();
        outcome.Exhausted.Clear();
        if (resourcePayment is { IsComplete: false })
            throw new InvalidOperationException("resource payment resumed before its owned work completed");
        resourcePayment = null;
        CompleteDamagePayment();
        if (minionEntry is { IsComplete: false })
            throw new InvalidOperationException("minion entry cost resumed before entry completed");
        if (Unpaid) nextStep = steps.Length;
        while (nextStep < steps.Length)
        {
            var step = steps[nextStep++];
            if (CommitImmediate(step, outcome, owner, trigger, events))
            {
                if (outcome.Suspended) break;
                continue;
            }

            var healthBefore = world.Effects.CaptureCharacterHealth();
            CommitBound(step, outcome, owner, pools, trigger, events);

            Statuses.RemoveAfflictionsIfStalwart(world, world.Facts, "stalwart", events);
            outcome.Suspended |= world.Effects.SettleLostHealth(healthBefore, trigger, events);
            Attack.RefreshDefender(world, world.Facts);
            if (outcome.Suspended) break;
        }
        if (!outcome.Suspended && !settled)
        {
            // rr:cost.5: multiple costs "must be paid simultaneously."
            // Damage placement fixes payment before its defeat work can remove
            // a card that must also exhaust or pay another physical component.
            settled = true;
            DamageCostSettlement.Settle(world, owner, damageCosts, events);
        }
        outcome.Suspended |= damageCosts.Any(damage => !damage.IsComplete);
        return new(outcome.Healed, outcome.Energy, outcome.Suspended) { Discarded = [.. outcome.Discarded], Exhausted = [.. outcome.Exhausted] };
    }

    private bool CommitImmediate(
        Step step, CommitOutcome outcome, AbilityExecution owner, string trigger, List<GameEvent> events)
    {
        switch (step)
        {
            case DeckCost { Cost: AbilityCost.DiscardUntilMinionEngaged }:
                minionEntry = AbilityMinionEntryCost.Begin(world, player, trigger, events);
                outcome.Suspended = true;
                return true;
            case DeckCost deck:
                outcome.Discarded.AddRange(AbilityDeckCosts.Pay(world, player, deck.Cost, trigger, events));
                return true;
            case Spend spend:
                resourcePayment = AbilityResourcePayment.Spend(world, resourceAbilities,
                    new(source, player, paying, spend.Required.Length + spend.Generic, spend.Required)
                    { Price = spend.Price, Allocations = spend.Allocations }, events);
                outcome.Suspended = !resourcePayment.IsComplete;
                if (spend.Energy is { } x) outcome.Energy = x;
                return true;
            case RandomDiscardHand random:
                outcome.Discarded.AddRange(AbilityRandomHandCost.Pay(world, random, player, trigger, events));
                return true;
            case DiscardHand discard:
                CommitDiscardHand(discard, events);
                return true;
            case ExhaustSelected exhaust:
                var selected = exhaust.Cards.Select(card => card.RequireCurrent()).ToArray();
                foreach (var card in selected) AbilityPhysicalPayment.RequireReady(card);
                foreach (var card in selected) AbilityCardOperations.Exhaust(card, trigger, events);
                outcome.Exhausted.AddRange(selected.Select(card => card.ObjectId));
                return true;
            case TakeDamage damage:
                CommitTakeDamage(damage, owner, trigger, events);
                return true;
            default:
                return false;
        }
    }

    private void CommitDiscardHand(DiscardHand discard, List<GameEvent> events)
    {
        var cards = discard.Cards.Select(card => card.RequireCurrent()).ToArray();
        foreach (var card in cards)
        {
            if (card.Area != discard.Hand)
            {
                throw new RulesNotImplementedException(
                    $"card {card.ObjectId} is no longer in the hand paying this cost");
            }
        }
        int start = events.Count;
        foreach (var card in cards) Discard.Card(world, card, CardPlay.Verb, events);
        outcome.Discarded.AddRange(cards);
        ActivityBatches.Complete(events, start, "Discard", player,
            included: cards.Select(card => card.ObjectId).ToArray());
    }

    private void CommitTakeDamage(
        TakeDamage damage, AbilityExecution owner, string trigger, List<GameEvent> events)
    {
        ObserveDamagePayment(DamagePlacement.BeginCost(world, world.Facts,
            new DamageRequest(source, damage.Target.RequireCurrent(), damage.Amount, trigger, CardPlay.Verb), owner, events),
            damage.Amount);
    }

    private void ObserveDamagePayment(DamageCostReceipt? result, long? required = null)
    {
        damagePayment = result;
        if (result is not null) damageCosts.Add(result);
        requiredTakenDamage = required;
        if (result is { IsPlaced: false }) outcome.Suspended = true;
        else CompleteDamagePayment();
    }

    private void CompleteDamagePayment()
    {
        if (damagePayment is null) return;
        if (!damagePayment.IsPlaced)
            throw new InvalidOperationException("damage cost resumed before its placement completed");
        if (requiredTakenDamage is { } required && damagePayment.Taken != required)
        {
            // rr:cost.12: "If any of the damage is prevented, then the cost has
            // not been paid." Remaining costs and the post-arrow effect do not run.
            throw new RulesNotImplementedException(
                $"'{source.FaceId}' requires {required} damage to be taken as a "
                + $"cost, but only {damagePayment.Taken} was taken; rr:cost.12 leaves it unpaid");
        }
        damagePayment = null;
        requiredTakenDamage = null;
    }

    private void CommitBound(
        Step step, CommitOutcome outcome, AbilityExecution owner, ICardCounterPools pools,
        string trigger, List<GameEvent> events)
    {
        var physical = new AbilityPhysicalPayment(world, source);
        switch (step)
        {
            case Exhaust exhaust:
                CommitExhaust(exhaust, trigger, events);
                break;
            case ConsumeCampaignBound consume:
                physical.ConsumeCampaign(consume.Target.RequireCurrent(), trigger, events);
                break;
            case DiscardBound discard:
                physical.Discard(discard.Target.RequireCurrent(), trigger, events);
                break;
            case ReturnBound returned:
                physical.ReturnToHand(returned.Target.RequireCurrent(), trigger, events);
                break;
            case RemoveCounters removal:
                physical.RemoveCounters(removal.Target.RequireCurrent(), removal.Counter, removal.Count, pools, trigger, events);
                break;
            case Heal heal:
                outcome.Healed = physical.Heal(heal.Target.RequireCurrent(), heal.Amount, trigger, events);
                break;
            case DealDamage damage:
                ObserveDamagePayment(physical.Damage(damage.Target.RequireCurrent(), damage.Amount, owner, trigger, events));
                break;
            default:
                throw new InvalidOperationException("Unknown prepared payment instruction");
        }
    }

    private static void CommitExhaust(
        Exhaust exhaust, string trigger, List<GameEvent> events)
    {
        var target = exhaust.Target.RequireCurrent();
        AbilityPhysicalPayment.RequireReady(target);
        AbilityCardOperations.Exhaust(target, trigger, events);
    }

    private sealed class CommitOutcome
    {
        internal List<int> Exhausted { get; } = [];
        internal long? Healed { get; set; }
        internal long? Energy { get; set; }
        internal bool Suspended { get; set; }
        internal List<Card> Discarded { get; } = [];
    }

}
