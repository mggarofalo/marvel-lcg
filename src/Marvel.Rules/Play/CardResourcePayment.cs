using System.Text;
using Marvel.Rules.Events;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>Retains the accepted contribution order while generator costs resolve.</summary>
internal sealed class CardResourcePayment(
    World world, ICardFacts facts, IResourceCardAbilities abilities, CardResourcePaymentRequest request)
{
    private readonly List<int> discarded = [];
    private readonly List<SpentResourceCard> spentCards = [];
    private int firstDiscardEvent = -1;
    private readonly StringBuilder generated = new();
    private IReadOnlyDictionary<int, int> payers = new Dictionary<int, int>();
    private ResourceContribution[] contributions = [];
    private readonly Dictionary<int, string> actualResources = [];
    private int next;
    private ResourceExecutionResult? pending;
    private CostOption? price;
    private IReadOnlyList<ResourceAllocation> allocations = [];
    private bool explicitAllocations;
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
        price = request.Price;
        allocations = request.Allocations;
        explicitAllocations = allocations.Count > 0;
        PreparePrice();
        Validate();
        contributions = [.. request.Paying.Select(id => new ResourceContribution(
            world.Cards[id], world.Cards[id].Incarnation, IsGenerator(world.Cards[id])))];
    }

    private void PreparePrice()
    {
        QuoteConditionalPrice();
        if (price is not null && allocations.Count == 0
            && price.Generators.Any(source => request.Paying.Contains(source.Effect) && source.ConditionalBonus is not null))
            allocations = ResourcePayment.AllocateUnambiguousGeneration(price, request.Paying)
                ?? throw new RulesNotImplementedException("conditional resource generation requires the player's allocation");
    }

    private void QuoteConditionalPrice()
    {
        if (price is null && (request.PayingFor is not null || request.AbilitySource is not null))
        {
            var sources = request.Paying.Select(id => IsGenerator(world.Cards[id])
                ? abilities.ResourceAbilities(world, payers[id]).Single(source => source.Effect == id)
                : abilities.QuoteHandResources(world, world.Cards[id], request.PayingFor ?? request.AbilitySource)).ToArray();
            if (sources.Any(source => source.ConditionalBonus is not null))
                price = new CostOption(0, request.Cost.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    Rule: [request.Required], Sources: sources);
        }
    }

    internal ResourcePaymentReceipt Commit(List<GameEvent> events)
    {
        var origin = ResourceCostOrigin.For(request.PayingFor, request.Payer);
        world.Agenda.Executions.RunBatch(
            new Occurrence(0, ["ResourcePayment"], Subject: request.Itself, Player: request.Payer),
            entry => { Execution = entry; return Continue(events); });
        return new(Execution, () => explicitAllocations
            ? string.Concat(allocations.Select(allocation => allocation.PaidAs))
            : Resources.Paid(generated.ToString(), request.Cost, request.Required),
            () => generated.ToString(), () => spentCards.ToArray(), origin);
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
                var source = offered.Single(candidate => candidate.Effect == id);
                if (!ResourceSourceEligibility.Allows(world, facts, source, request.PayingFor ?? request.AbilitySource))
                    throw new RulesNotImplementedException("the selected resource ability cannot pay this card's cost");
                proposed.Append(source.Generates);
            }
            else
            {
                RequireHand(card);
                proposed.Append(abilities.ResourcesGeneratedBy(world, card, request.PayingFor));
            }
        }
        if (price is { } acceptedPrice)
        {
            if (allocations.Count == 0 && request.Cost > 0
                && price.Generators.Any(source => request.Paying.Contains(source.Effect)
                    && source.ConditionalBonus is not null))
                throw new RulesNotImplementedException("conditional resource generation requires the player's allocation");
            foreach (int id in request.Paying)
            {
                var source = acceptedPrice.Generators.Single(candidate => candidate.Effect == id);
                string quoted = ResourcePayment.GeneratedBy(source, allocations);
                proposed.Append(quoted.AsSpan(source.Generates.Length));
            }
        }
        RequireEnough(proposed.ToString());
        RequireAllocation(price);
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
            int id = contributions[next - 1].Card.ObjectId;
            string produced = Produced(id, previous.Generation.Resources);
            generated.Append(produced);
            actualResources[id] = produced;
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
        RequireAllocation(price is { } acceptedPrice ? acceptedPrice with
        { Sources = [.. acceptedPrice.Generators.Select(source => source with
            { Generates = actualResources.GetValueOrDefault(source.Effect, source.Generates), ConditionalBonus = null })] } : null);
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
            return ContributeGenerator(card, events);
        ContributeHand(card, events);
        return true;
    }

    private bool ContributeGenerator(Card card, List<GameEvent> events)
    {
        CompleteDiscardBatch(events);
        var result = abilities.ExecuteResource(world, payers[card.ObjectId], card.ObjectId);
        events.AddRange(result.Execution.Events);
        if (!result.Execution.IsComplete) { pending = result; return false; }
        string produced = Produced(card.ObjectId, result.Generation.Resources);
        generated.Append(produced);
        actualResources[card.ObjectId] = produced;
        return true;
    }

    private void ContributeHand(Card card, List<GameEvent> events)
    {
        RequireHand(card);
        if (price?.Generators.SingleOrDefault(source => source.Effect == card.ObjectId) is { } accepted
            && (request.PayingFor ?? request.AbilitySource) is { } owner
            && abilities.QuoteHandResources(world, card, owner, accepted.ConditionalBonus?.ComponentMask ?? 1) != accepted)
            throw new RulesNotImplementedException("the conditional resource source changed before generation");
        string produced = Produced(card.ObjectId, abilities.ResourcesGeneratedBy(world, card, request.PayingFor));
        generated.Append(produced);
        actualResources[card.ObjectId] = produced;
        // rr:resource.1: generate resources "by discarding cards from their hand".
        // Generation commits this discard before a later generator can suspend.
        if (firstDiscardEvent < 0) firstDiscardEvent = events.Count;
        spentCards.Add(new(card.ObjectId, card.Incarnation, card.Area.PlayArea.Player));
        discarded.Add(card.ObjectId);
        Discard.Card(world, card, CardPlay.Verb, events);
    }

    private string Produced(int id, string actual)
    {
        ResourceSource? quoted = price?.Generators.SingleOrDefault(source => source.Effect == id);
        if (quoted is not { ConditionalBonus: not null } source) return actual;
        if (!string.Equals(source.Generates, actual, StringComparison.Ordinal))
            throw new RulesNotImplementedException("the conditional resource source changed before generation");
        return ResourcePayment.GeneratedBy(source, allocations);
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
        if (!request.Hands.Contains(card.Area) || card.ObjectId == request.Itself
            || !abilities.CanUseHandResources(world, card))
            throw new RulesNotImplementedException($"card {card.ObjectId} cannot be discarded to pay this cost");
    }

    private void RequireAllocation(Marvel.Rules.Prompts.CostOption? price)
    {
        if (price is not null && !Marvel.Rules.Prompts.ResourcePayment.Allows(
                price, request.Paying, null, allocations))
            throw new RulesNotImplementedException("the generated resources do not satisfy the accepted allocation");
    }

    private void RequireEnough(string resources)
    {
        if (!Resources.Pays(resources, request.Cost, request.Required))
            throw new RulesNotImplementedException(
                $"the cost is {request.Cost} requiring '{request.Required}' and payment generates '{resources}'; "
                + "rr:initiating-abilities.step.5 aborts without paying");
    }
}
