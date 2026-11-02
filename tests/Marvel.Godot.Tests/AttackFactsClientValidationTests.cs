using Marvel.Client;
using Marvel.Rules.Prompts;
using Marvel.Rules.Timing;
using Xunit;

namespace Marvel.Godot.Tests;

public sealed class AttackFactsClientValidationTests
{
    [Theory]
    [InlineData(12, 3, true, true)]
    [InlineData(12, 3, false, true)]
    [InlineData(-1, 3, true, false)]
    [InlineData(12, -1, false, false)]
    public void AnAttackIsCompleteWhenItNamesBothCardsPreventedOrNot(int attacker, int target, bool prevented, bool accepted)
    {
        Assert.Equal(accepted, ClientResponseValidation.HasCompletePrompt(Defense(attacker, target, prevented, null)));
        Assert.Equal(accepted, ClientResponseValidation.HasCompletePrompt(Defense(attacker, target, prevented, 0)));
        Assert.False(ClientResponseValidation.HasCompletePrompt(Defense(attacker, target, prevented, -1)));
    }

    private static Prompt Defense(int attacker, int target, bool prevented, long? boostIcons)
    {
        var prompt = new Prompt(0, Question.Defender, TimingPriority.Untimed, "attack_initiated", "Defend", true,
            [new Affordance(4, "Defend", 3, 0, "Defend")])
        {
            Attack = new PromptAttack(attacker, target, 2, Calculated: false, BoostUnresolved: true, prevented)
            {
                BoostIcons = boostIcons,
            },
        };
        return prompt;
    }
}
