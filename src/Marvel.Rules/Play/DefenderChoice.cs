using static Marvel.Rules.Play.AttackBoost;
using static Marvel.Rules.Play.AttackDamage;
using static Marvel.Rules.Play.AttackCompletion;
using Marvel.Rules.Events;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>
/// The legal defenders for one attack and whether a defender is mandatory.
/// </summary>
/// <param name="Candidates">The characters that may defend.</param>
/// <param name="Required">Whether declining to defend is illegal.</param>
public sealed record DefenderChoice(IReadOnlyList<Card> Candidates, bool Required);
