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
        Assert.Equal(accepted, ClientResponseValidation.HasCompletePrompt(Defense(attacker, target, prevented, null, null)));
        Assert.Equal(accepted, ClientResponseValidation.HasCompletePrompt(Defense(attacker, target, prevented, 0, 0)));
        Assert.False(ClientResponseValidation.HasCompletePrompt(Defense(attacker, target, prevented, -1, 0)));
    }

    [Theory]
    [InlineData(2L, 1L, true)]
    [InlineData(2L, 0L, true)]
    [InlineData(2L, -1L, false)]
    [InlineData(2L, null, false)]
    [InlineData(null, 1L, false)]
    public void FacedownBoostCardsAccompanyARevealedBoostCard(long? icons, long? facedown, bool accepted) =>
        Assert.Equal(accepted, ClientResponseValidation.HasCompletePrompt(Defense(12, 3, false, icons, facedown)));

    private static Prompt Defense(int attacker, int target, bool prevented, long? boostIcons, long? facedown)
    {
        var prompt = new Prompt(0, Question.Defender, TimingPriority.Untimed, "attack_initiated", "Defend", true,
            [new Affordance(4, "Defend", 3, 0, "Defend")])
        {
            Attack = new PromptAttack(attacker, target, 2, Calculated: false, BoostUnresolved: true, prevented)
            {
                BoostIcons = boostIcons,
                BoostFacedown = facedown,
            },
        };
        return prompt;
    }
}
