using Marvel.Cards.Dsl;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Cards.Run;

// Commits one already-authored resource ability. Offer eligibility and resource
// generation remain in AbilityOfferQueries; this owner performs only payment
// and post-payment use recording through narrow capabilities.
internal sealed class AbilityResourceExecution
{
    private readonly AbilityProgram program;
    private readonly AbilityOfferQueries offers;

    internal AbilityResourceExecution(AbilityProgram program, AbilityOfferQueries offers)
    {
        ArgumentNullException.ThrowIfNull(program);
        ArgumentNullException.ThrowIfNull(offers);
        this.program = program;
        this.offers = offers;
    }

    internal ResourceExecutionResult UseResource(
        World world, AbilityExecution entry, int player, int card,
        IResourceCardAbilities resourceAbilities, ICardCounterPools pools)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(pools);
        world.Agenda.Executions.RequireBatch(entry);

        var holder = world.Cards[card];
        var ability = offers.ResourceAbility(world, player, holder)
            ?? throw new RulesNotImplementedException(
                $"card {card} has no resource ability left to use this round");

        var payment = AbilityCostPayment.Prepare(
            world, holder, player, ability.Cost, program, resourceAbilities,
            AbilityPaymentSelection.Capture([], []));
        int ordinal = AbilityProgramQueries.On(program, holder)
            .Where(candidate => candidate.Trigger.Timing == AbilityType.Resource)
            .TakeWhile(candidate => candidate != ability).Count();
        var invocation = world.Agenda.Executions.Begin(entry.Occurrence,
            new PendingAbility(card, AbilityType.Resource, player, ordinal));
        invocation.Occurrence.Begin(invocation.Ability);
        var receipt = new ResourceGenerationReceipt(world, invocation);
        var state = new ResourceAbilityContinuation(holder, player, ability, payment, receipt)
            { Execution = invocation };
        var events = new List<GameEvent>();
        Commit(world, state, pools, events);
        return new(entry.Finish(events), receipt);
    }

    internal IReadOnlyList<GameEvent> Resume(World world, PhaseStep step, ICardCounterPools pools)
    {
        world.Agenda.Executions.RequireContinuation(step);
        var state = step.Require<AbilityContinuationAgendaOperation>().Continuation
            as ResourceAbilityContinuation
            ?? throw new InvalidOperationException("resource resumption requires its accepted payment");
        world.Agenda.Executions.Resume(state.Execution!);
        var events = new List<GameEvent>();
        Commit(world, state, pools, events);
        return events;
    }

    private void Commit(World world, ResourceAbilityContinuation state,
        ICardCounterPools pools, List<GameEvent> events)
    {
        var invocation = state.Execution!;
        var owner = world.Agenda.Current;
        var occurrence = world.Agenda.Occurrence;
        var result = state.Payment.Commit(invocation, pools, Steps.TurnAction, events);
        if (result.Suspended)
        {
            var step = new PhaseStep(new AbilityContinuationAgendaOperation(AbilityContinuationStage.Resume)
                { Continuation = state }, owner?.Round ?? 0, 0, Subject: state.Source.ObjectId,
                Seat: state.Player, Plan: true);
            if (owner is null) world.Agenda.Then(step);
            else world.Agenda.ContinueBeforeOwner(occurrence!, owner.Value, step);
            invocation.Yield(events);
            return;
        }
        AbilityUseRecording.Record(world, program, state.Source, state.Ability);
        string generated = AbilityOfferQueries.Generated(state.Ability.Effect, world, state.Player);
        state.Receipt.Complete(world, invocation, generated);
        if (generated.Length > 0) invocation.Occurrence.Resolve(invocation.Ability);
        invocation.Finish(events);
    }
}
