using System.Text;
using Marvel.Rules.Events;
using Marvel.Rules.State;

namespace Marvel.Rules.Play;

/// <summary>Validates and commits one resource payment, distinguishing generators from hand discards.</summary>
internal sealed class CardResourcePayment(
    World world, ICardFacts facts, IResourceCardAbilities abilities, CardResourcePaymentRequest request)
{
    internal void Spend(List<GameEvent> events)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(abilities);
        ArgumentNullException.ThrowIfNull(request.Hands);
        ArgumentNullException.ThrowIfNull(request.Paying);
        ArgumentNullException.ThrowIfNull(events);
        var spent = new List<Card>();
        var generated = new StringBuilder();
        IReadOnlyDictionary<int, int> abilityPayers = AbilityPayers();
        foreach (int id in request.Paying)
            generated.Append(Contribution(id, abilityPayers, spent, events));
        if (!Resources.Pays(generated.ToString(), request.Cost, request.Required))
            throw new RulesNotImplementedException(
                $"the cost is {request.Cost}"
                + (request.Required.Length > 0 ? $" requiring '{request.Required}'" : string.Empty)
                + $" and the payment generates '{generated}'; "
                + "rr:initiating-abilities.step.5 aborts without paying");
        // rr:cost.3: resources are spent "by discarding cards from their hand".
        // The receipt groups this explicit payment; generators retain their own committed events.
        int start = events.Count;
        foreach (Card card in spent) Discard.Card(world, card, CardPlay.Verb, events);
        ActivityBatches.Complete(events, start, "Discard", -1,
            included: spent.Select(card => card.ObjectId).ToArray());
    }

    private IReadOnlyDictionary<int, int> AbilityPayers() => request.ResourcePayers
        ?? abilities.ResourceAbilities(world, request.Payer).GroupBy(source => source.Effect)
            .ToDictionary(group => group.Key, _ => request.Payer);

    private string Contribution(int id, IReadOnlyDictionary<int, int> abilityPayers,
        List<Card> spent, List<GameEvent> events)
    {
        Card card = world.Cards[id];
        // rr:resource-ability: using a generator in play is distinct from discarding a hand card.
        if (abilityPayers.TryGetValue(id, out int payer) && !request.Hands.Contains(card.Area))
            return abilities.UseResource(world, payer, id, events);
        if (!request.Hands.Contains(card.Area)) throw OutsideHand(id);
        if (card.ObjectId == request.Itself)
            throw new RulesNotImplementedException($"card {id} is being played and cannot also pay for itself");
        spent.Add(card);
        return abilities.ResourcesGeneratedBy(world, card, request.PayingFor);
    }

    private RulesNotImplementedException OutsideHand(int id) => new(
        request.Hands.Count == 1 && request.Hands[0].CardOwner >= 0
            ? $"card {id} is not in {world.Seats[request.Hands[0].CardOwner].Name}'s hand "
              + "and cannot be spent from it"
            : $"card {id} is in no player's hand and cannot be spent from one");
}
