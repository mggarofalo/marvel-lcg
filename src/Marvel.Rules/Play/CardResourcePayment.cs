using System.Text;
using Marvel.Rules.Events;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>Retains the accepted contribution order while generator costs resolve.</summary>
internal sealed class CardResourcePayment(
    World world, ICardFacts facts, IResourceCardAbilities abilities, CardResourcePaymentRequest request)
{
    private readonly List<int> discarded = [];
    private int firstDiscardEvent = -1;
    private readonly StringBuilder generated = new();
    private IReadOnlyDictionary<int, int> payers = new Dictionary<int, int>();
    private ResourceContribution[] contributions = [];
    private int next;
    private ResourceExecutionResult? pending;
    internal AbilityExecution Execution { get; private set; } = null!;

    internal ResourcePaymentReceipt Spend(List<GameEvent> events)
    {
        Prepare();
        return Commit(events);
    }

    internal void Prepare()
    {
        ArgumentNullException.ThrowIfNull(facts);
        payers = request.ResourcePayers ?? abilities.ResourceAbilities(world, request.Payer)
            .GroupBy(source => source.Effect).ToDictionary(group => group.Key, _ => request.Payer);
        Validate();
        contributions = [.. request.Paying.Select(id => new ResourceContribution(
            world.Cards[id], world.Cards[id].Incarnation, IsGenerator(world.Cards[id])))];
    }

    internal ResourcePaymentReceipt Commit(List<GameEvent> events)
    {
        world.Agenda.Executions.RunBatch(
            new Occurrence(0, ["ResourcePayment"], Subject: request.Itself, Player: request.Payer),
            entry => { Execution = entry; return Continue(events); });
        return new(Execution);
    }

    private void Validate()
    {
        if (request.Paying.Distinct().Count() != request.Paying.Count)
            throw new RulesNotImplementedException("one resource contributor cannot pay twice");
        var proposed = new StringBuilder();
        foreach (int id in request.Paying)
        {
            Card card = world.Cards[id];
            if (IsGenerator(card))
            {
                var offered = abilities.ResourceAbilities(world, payers[id]);
                proposed.Append(offered.Single(source => source.Effect == id).Generates);
            }
            else
            {
                RequireHand(card);
                proposed.Append(abilities.ResourcesGeneratedBy(world, card, request.PayingFor));
            }
        }
        RequireEnough(proposed.ToString());
    }

    internal void Resume(List<GameEvent> events)
    {
        world.Agenda.Executions.Resume(Execution);
        _ = Continue(events);
    }

    private AbilityExecutionResult Continue(List<GameEvent> events)
    {
        if (pending is { } previous)
        {
            if (!previous.Execution.IsComplete || !previous.Generation.IsComplete)
                throw new InvalidOperationException("payment resumed before its resource generator completed");
            generated.Append(previous.Generation.Resources);
            pending = null;
        }
        while (next < contributions.Length)
        {
            var contribution = contributions[next++];
            if (!Contribute(contribution, events))
            {
                world.Agenda.ThenContinuation(
                    new PhaseStep(new ResourcePaymentOperation(this), world.Agenda.Current?.Round ?? 0, 0), Execution.Occurrence);
                return Execution.Yield(events);
            }
        }
        RequireEnough(generated.ToString());
        CompleteDiscardBatch(events);
        return Execution.Finish(events);
    }

    private bool Contribute(ResourceContribution contribution, List<GameEvent> events)
    {
        var card = contribution.Card;
        if (card.Incarnation != contribution.Incarnation
            || IsGenerator(card) != contribution.IsGenerator)
            throw new RulesNotImplementedException(
                $"resource contributor {card.ObjectId} no longer names its accepted copy and contribution");
        if (contribution.IsGenerator)
        {
            CompleteDiscardBatch(events);
            var result = abilities.ExecuteResource(world, payers[card.ObjectId], card.ObjectId);
            events.AddRange(result.Execution.Events);
            if (!result.Execution.IsComplete) { pending = result; return false; }
            generated.Append(result.Generation.Resources);
        }
        else
        {
            RequireHand(card);
            generated.Append(abilities.ResourcesGeneratedBy(world, card, request.PayingFor));
            // rr:resource.1: generate resources "by discarding cards from their hand".
            // Generation commits this discard before a later generator can suspend.
            if (firstDiscardEvent < 0) firstDiscardEvent = events.Count;
            discarded.Add(card.ObjectId);
            Discard.Card(world, card, CardPlay.Verb, events);
        }
        return true;
    }

    private void CompleteDiscardBatch(List<GameEvent> events)
    {
        if (firstDiscardEvent < 0) return;
        ActivityBatches.Complete(events, firstDiscardEvent, "Discard", -1, included: discarded.ToArray());
        discarded.Clear();
        firstDiscardEvent = -1;
    }

    private bool IsGenerator(Card card) => payers.ContainsKey(card.ObjectId) && !request.Hands.Contains(card.Area);

    private void RequireHand(Card card)
    {
        if (!request.Hands.Contains(card.Area) || card.ObjectId == request.Itself)
            throw new RulesNotImplementedException($"card {card.ObjectId} cannot be discarded to pay this cost");
    }

    private void RequireEnough(string resources)
    {
        if (!Resources.Pays(resources, request.Cost, request.Required))
            throw new RulesNotImplementedException(
                $"the cost is {request.Cost} requiring '{request.Required}' and payment generates '{resources}'; "
                + "rr:initiating-abilities.step.5 aborts without paying");
    }
}
