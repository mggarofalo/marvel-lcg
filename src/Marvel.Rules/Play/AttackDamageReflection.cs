using Marvel.Rules.State;

namespace Marvel.Rules.Play;

/// <summary>A replacement retained by the player attack that established it.</summary>
public sealed record AttackDamageReflection(Card Source, Card Target, int TargetIncarnation, Card Recipient, string Trigger);
