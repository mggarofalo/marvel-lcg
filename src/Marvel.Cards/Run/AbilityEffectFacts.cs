using Marvel.Cards.Dsl;
using Marvel.Rules.Prompts;

namespace Marvel.Cards.Run;

/// <summary>The printed effect of a checked instruction as one typed fact, for the effects a client shows as a chip.</summary>
/// <remarks>
/// Only a printed constant becomes an amount, as <see cref="AbilityEffectDescription"/> words "Printed damage";
/// a computed, bounded or per-player amount has no fact, so its description stays the only statement of it.
/// Which effects are typed is our choice: the common ones a player meets at most decisions.
/// </remarks>
internal static class AbilityEffectFacts
{
    internal static PromptEffect? Printed(AbilityEffect? effect) => effect switch
    {
        AbilityEffect.AttackDamage { Amount: AbilityNumber.Constant amount } => new(PromptEffectKind.Damage, amount.Value),
        AbilityEffect.Damage { Amount: AbilityNumber.Constant amount, Maximum: null } =>
            new(PromptEffectKind.Damage, amount.Value),
        AbilityEffect.RemoveThreat { Amount: AbilityNumber.Constant amount, Maximum: null, Assigned: false } =>
            new(PromptEffectKind.RemoveThreat, amount.Value),
        AbilityEffect.Draw draw => new(PromptEffectKind.Draw, draw.Count),
        AbilityEffect.GiveStatus { Status: "stunned" } => new(PromptEffectKind.Stun, 0),
        AbilityEffect.GiveStatus { Status: "confused" } => new(PromptEffectKind.Confuse, 0),
        AbilityEffect.GiveStatus { Status: "tough" } => new(PromptEffectKind.Tough, 0),
        AbilityEffect.Power power => Printed(power.Effect),
        AbilityEffect.Sequence { Effects.Length: 1 } sequence => Printed(sequence.Effects[0]),
        _ => null,
    };
}
