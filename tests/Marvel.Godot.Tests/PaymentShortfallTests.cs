using Marvel.Decisions;
using Marvel.Rules.Prompts;
using Marvel.Rules.Timing;
using Xunit;

namespace Marvel.Godot.Tests;

public sealed class PaymentShortfallTests
{
    [Fact]
    public void StagingAndRemovingSourcesUpdatesTheVisibleShortfallWithoutCommitting()
    {
        var composer = new DecisionComposer(new Prompt(0, Question.TurnOption,
            TimingPriority.Untimed, "synthetic", "Pay", false,
            [new Affordance(3, "Play", 19, 0, "Card", Costs:
                [new CostOption(19, "3", Sources: [new(41, "GG"), new(42, "Y")])])]));
        composer.SelectAffordance(3);
        Assert.Contains("3 more resources needed", CardPaymentPresentation.Progress(composer.Progress().Payment));
        composer.ToggleResource(41);
        Assert.Contains("1 more resource needed", CardPaymentPresentation.Progress(composer.Progress().Payment));
        Assert.False(composer.TryBuild(out _, out _));
        composer.ToggleResource(42);
        Assert.Contains("Payment ready", CardPaymentPresentation.Progress(composer.Progress().Payment));
        Assert.True(composer.TryBuild(out _, out _));
        composer.ToggleResource(41);
        Assert.Contains("2 more resources needed", CardPaymentPresentation.Progress(composer.Progress().Payment));
        Assert.False(composer.TryBuild(out _, out _));
    }

    [Fact]
    public void UnknownOrUnallocatedPaymentNeverClaimsReadinessFromZeroShortfall()
    {
        var payment = new PaymentProgress(CostSelectionState.Selected, 0, 1, 1, 3, 0, 0, 0, false);
        Assert.Contains("Choose resources", CardPaymentPresentation.Progress(payment));
        Assert.DoesNotContain("Payment ready", CardPaymentPresentation.Progress(payment with { RemainingRequired = 0 }));
        Assert.Contains("Assign selected resources", CardPaymentPresentation.Progress(payment with { CanCoverCost = true }));
    }
}
