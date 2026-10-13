using Marvel.Rules.State;

namespace Marvel.Rules.Play;

/// <summary>An attack's damage roles and the activation, if any, responsible for it.</summary>
public sealed record AttackDamageRequest(
    Card Attacker, Card Source, Card Target, long Amount, string Trigger, string Verb,
    bool Retaliate = true, int? ActivationId = null);
