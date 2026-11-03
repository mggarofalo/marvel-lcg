using System.Collections.Immutable;
using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Prompts;
using Xunit;

namespace Marvel.Content.Tests.Play;

// Which printed effects become one typed fact is an engine presentation choice, not a game rule.
public sealed class AbilityEffectFactsTests
{
    private static readonly AbilityCardSelection Chosen = new AbilityCardSelection.Bound(AbilityCardBinding.Chosen);
    private static readonly AbilityNumber Eight = new AbilityNumber.Constant(8);

    public static TheoryData<AbilityEffect, PromptEffect?> Effects => new()
    {
        { new AbilityEffect.Damage(Chosen, Eight, AttackVerb: false), new PromptEffect(PromptEffectKind.Damage, 8) },
        { new AbilityEffect.AttackDamage(Chosen, Eight, Overkill: false), new PromptEffect(PromptEffectKind.Damage, 8) },
        { new AbilityEffect.RemoveThreat(Chosen, new AbilityNumber.Constant(3), false, null),
            new PromptEffect(PromptEffectKind.RemoveThreat, 3) },
        { new AbilityEffect.Draw(new AbilityPlayerSelection.OnePlayer(AbilityPlayer.TriggerPlayer), 2),
            new PromptEffect(PromptEffectKind.Draw, 2) },
        { new AbilityEffect.GiveStatus(Chosen, "stunned"), new PromptEffect(PromptEffectKind.Stun, 0) },
        { new AbilityEffect.GiveStatus(Chosen, "confused"), new PromptEffect(PromptEffectKind.Confuse, 0) },
        { new AbilityEffect.GiveStatus(Chosen, "tough"), new PromptEffect(PromptEffectKind.Tough, 0) },
        { new AbilityEffect.Power(AbilityPowerKind.Attack, Chosen, new AbilityEffect.AttackDamage(Chosen, Eight, false), false),
            new PromptEffect(PromptEffectKind.Damage, 8) },
        { new AbilityEffect.Sequence([new AbilityEffect.Damage(Chosen, Eight, false)]),
            new PromptEffect(PromptEffectKind.Damage, 8) },
        { new AbilityEffect.Heal(Chosen, new AbilityNumber.Constant(2)), new PromptEffect(PromptEffectKind.Heal, 2) },
        { new AbilityEffect.CardAction(AbilityCardInstruction.Ready, Chosen), new PromptEffect(PromptEffectKind.Ready, 0) },
        { new AbilityEffect.CardAction(AbilityCardInstruction.Exhaust, Chosen), new PromptEffect(PromptEffectKind.Exhaust, 0) },
        { new AbilityEffect.PlaceThreat(Chosen, new AbilityNumber.Constant(1)), new PromptEffect(PromptEffectKind.PlaceThreat, 1) },
        { new AbilityEffect.MoveThreat(Chosen, Chosen, new AbilityNumber.Constant(2)), new PromptEffect(PromptEffectKind.MoveThreat, 2) },
        // A computed, per-player, bounded or assigned amount is not printed, so it has no fact.
        { new AbilityEffect.Heal(Chosen, new AbilityNumber.PerPlayer(1)), null },
        { new AbilityEffect.PlaceThreat(Chosen, new AbilityNumber.PerPlayer(1)), null },
        { new AbilityEffect.MoveThreat(Chosen, Chosen, Eight) { All = true }, null },
        { new AbilityEffect.CardAction(AbilityCardInstruction.Discard, Chosen), null },
        { new AbilityEffect.Damage(Chosen, new AbilityNumber.PerPlayer(1), false), null },
        { new AbilityEffect.Damage(Chosen, Eight, false) { Maximum = 2 }, null },
        { new AbilityEffect.RemoveThreat(Chosen, Eight, false, null) { Maximum = 3 }, null },
        { new AbilityEffect.RemoveThreat(Chosen, Eight, false, null) { Assigned = true }, null },
        { new AbilityEffect.Sequence([new AbilityEffect.Damage(Chosen, Eight, false),
            new AbilityEffect.GiveStatus(Chosen, "stunned")]), null },
        { new AbilityEffect.GiveStatus(Chosen, "webbed"), null },
    };

    [Theory]
    [MemberData(nameof(Effects))]
    public void APrintedEffectIsOneTypedFact(AbilityEffect effect, PromptEffect? expected) =>
        Assert.Equal(expected, AbilityEffectFacts.Printed(effect));
}
