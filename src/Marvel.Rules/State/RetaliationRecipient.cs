namespace Marvel.Rules.State;

/// <summary>One attacked physical character and its original public play area.</summary>
public sealed record RetaliationRecipient(Attacked Attack, int Area);
