using Marvel.Rules.State;

namespace Marvel.Rules.Play;

/// <summary>One pending player ally entry and its original physical identity.</summary>
public sealed record PlayerAllyEntryOperation : AgendaOperation
{
    internal PlayerAllyEntryOperation(Card card, int player, CardEntryChoices? choices, AllyEntryRequest? put)
    {
        Card = card;
        Player = player;
        Incarnation = card.Incarnation;
        Area = card.Area.Id;
        Choices = choices;
        Put = put;
    }

    internal Card Card { get; }
    internal int Player { get; }
    internal int Incarnation { get; }
    internal int Area { get; }
    internal CardEntryChoices? Choices { get; }
    internal AllyEntryRequest? Put { get; }
    internal Dictionary<string, long> Counters { get; } = [];
    /// <summary>Record a counter placed by the pending card's own entry interrupt.</summary>
    public void StageCounter(Card card, string key, long count)
    {
        if (!MatchesPending(card) || !key.StartsWith("c_", StringComparison.Ordinal) || count <= 0)
            throw new InvalidOperationException("counter staging requires the exact pending ally");
        Counters[key] = Counters.GetValueOrDefault(key) + count;
    }
    /// <summary>The original entry receipt, if a cost was paid.</summary>
    public EntryPayment? Payment => Choices?.EntryPayment ?? Put?.Payment;
    /// <summary>The card-play receipt, separate from entry payment.</summary>
    public PlayedCardPayment? PlayedPayment => Choices is null ? null :
        new PlayedCardPayment(Card.ObjectId, Incarnation + 1, Player, Choices.Paid);
    /// <summary>Whether this is still the exact pending physical card.</summary>
    public bool MatchesPending(Card card) => ReferenceEquals(Card, card)
        && card.Incarnation == Incarnation && card.Area.Id == Area;
    /// <inheritdoc/>
    public override string What => Choices is null ? Steps.CardEntersPlay : Steps.CardPlayed;
    /// <inheritdoc/>
    private protected override AgendaProcedureKind ProcedureOwner => AgendaProcedureKind.Lifecycle;
}
