namespace Marvel.Cards.Dsl;

/// <summary>Counters of one type on one card, spent one for one by an effect.</summary>
public sealed record AbilityCounterPayment(AbilityCardSelection Holder, string Counter);
