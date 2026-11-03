using Marvel.Client;
using Marvel.Rules.Prompts;
using Marvel.Rules.Timing;
using Xunit;

namespace Marvel.Godot.Tests;

public sealed class EffectFactsClientValidationTests
{
    [Theory]
    [InlineData(PromptEffectKind.Damage, 8, true)]
    [InlineData(PromptEffectKind.Stun, 0, true)]
    [InlineData(PromptEffectKind.Heal, 2, true)]
    [InlineData(PromptEffectKind.MoveThreat, 1, true)]
    [InlineData(PromptEffectKind.Heal, -1, false)]
    [InlineData(PromptEffectKind.Damage, -1, false)]
    [InlineData((PromptEffectKind)99, 1, false)]
    public void APromptEffectIsCompleteWithAKnownKindAndNoNegativeAmount(PromptEffectKind kind, long amount, bool accepted)
    {
        var effect = new PromptEffect(kind, amount);
        Assert.Equal(accepted, ClientResponseValidation.HasCompletePrompt(Choice() with { Effect = effect }));
        Assert.Equal(accepted, ClientResponseValidation.HasCompletePrompt(Window(effect)));
    }

    private static Prompt Choice() =>
        new(0, Question.Element, TimingPriority.Untimed, "card_revealed", "Choose", false,
            [new Affordance(4, "Choose", 3, 0, "Rhino")]);

    private static Prompt Window(PromptEffect effect) =>
        new(0, Question.Opportunity, TimingPriority.Untimed, "entered_play", "Respond", true,
            [new Affordance(4, "Response", 3, 0, "Mockingbird")
            {
                WindowChoices = [new WindowChoice(7, "Stun Rhino", null) { RecipientIds = [7], Effect = effect }],
            }]);
}
