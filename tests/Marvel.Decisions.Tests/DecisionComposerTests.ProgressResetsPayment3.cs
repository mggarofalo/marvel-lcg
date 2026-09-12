using Marvel.Decisions;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Server;
using Marvel.Tests;
using Marvel.View;
using Xunit;

namespace Marvel.Decisions.Tests;
public sealed class DecisionComposerProgressResetsPaymentTests : DecisionComposerTestBase
{
    [Fact]
    public void ProgressHidesDraftPaymentStateUntilAnAffordanceIsSelected()
    {
        var composer = new DecisionComposer(Prompt(
            cancellable: false,
            new Affordance(7, "Play", 20, 0, "Paid action")));
        composer.ToggleResource(40);

        PaymentProgress payment = composer.Progress().Payment;

        Assert.Equal(CostSelectionState.Unavailable, payment.CostState);
        Assert.Null(payment.SelectedCost);
        Assert.Equal(0, payment.CostOptions);
        Assert.Equal(0, payment.SelectedGenerators);
        Assert.Equal(0, payment.GeneratedIcons);
        Assert.Equal(0, payment.AssignedIcons);
        Assert.Equal(0, payment.DefinedVariables);
        Assert.Equal(0, payment.RequestedVariables);
        Assert.False(payment.IsSatisfied);
    }

    [Fact]
    public void ProgressResetsPaymentWhenTheAffordanceOrPricedTargetChanges()
    {
        var composer = new DecisionComposer(Prompt(cancellable: false, new Affordance(7, "Play", 20, 0, "Variable price", new TargetRequest([11, 12], 1, 1), [new CostOption(11, "1", Sources: [new ResourceSource(40, "Y")]), new CostOption(12, "0"), ]), new Affordance(8, "Thwart", 21, 0, "Free action")));
        composer.SelectAffordance(7);
        composer.SelectTargets([11]);
        composer.SelectCost(0);
        composer.ToggleResource(40);
        composer.AssignResource(40, 0, 0, Resources.Energy);
        Assert.True(composer.Progress().Payment.IsSatisfied);
        composer.SelectTargets([12]);
        PaymentProgress changedTarget = composer.Progress().Payment;
        Assert.Equal(CostSelectionState.Required, changedTarget.CostState);
        Assert.Null(changedTarget.SelectedCost);
        Assert.Equal(0, changedTarget.SelectedGenerators);
        Assert.Equal(0, changedTarget.AssignedIcons);
        composer.SelectAffordance(8);
        DecisionProgressPresentation changedAffordance = composer.Progress();
        Assert.Equal(TargetSelectionMode.None, changedAffordance.Targets.Mode);
        Assert.Equal(CostSelectionState.NotRequired, changedAffordance.Payment.CostState);
        Assert.True(changedAffordance.IsReady);
    }

    [Fact]
    public void ChangingATargetClearsEvenAnAutomaticallySelectedSinglePayment()
    {
        var composer = new DecisionComposer(Prompt(cancellable: false, new Affordance(7, "Play", 20, 0, "Paid action", new TargetRequest([11, 12], 1, 1), [new CostOption(0, "1", Sources: [new ResourceSource(40, "Y")])])));
        composer.SelectAffordance(7);
        composer.SelectTargets([11]);
        composer.ToggleResource(40);
        composer.AssignResource(40, 0, 0, Resources.Energy);
        Assert.True(composer.Progress().Payment.IsSatisfied);
        composer.SelectTargets([12]);
        PaymentProgress changedTarget = composer.Progress().Payment;
        Assert.Equal(CostSelectionState.Selected, changedTarget.CostState);
        Assert.Equal(0, changedTarget.SelectedCost);
        Assert.Equal(0, changedTarget.SelectedGenerators);
        Assert.Equal(0, changedTarget.AssignedIcons);
        Assert.False(changedTarget.IsSatisfied);
    }

    [Fact]
    public void ProgressDoesNotRetainVisibleOrConcealedObjectIdentities()
    {
        const int visibleTarget = 987654;
        const int concealedGenerator = 765432;
        var composer = new DecisionComposer(Prompt(cancellable: false, new Affordance(7, "Play", 20, 0, "Paid action", new TargetRequest([visibleTarget], 1, 1), [new CostOption(visibleTarget, "1", Sources: [new ResourceSource(concealedGenerator, "Y")])])));
        composer.SelectAffordance(7);
        composer.SelectTargets([visibleTarget]);
        composer.ToggleResource(concealedGenerator);
        composer.AssignResource(concealedGenerator, 0, 0, Resources.Energy);
        string progress = composer.Progress().ToString();
        Assert.DoesNotContain(visibleTarget.ToString(), progress);
        Assert.DoesNotContain(concealedGenerator.ToString(), progress);
    }

    [Fact]
    public void IllegalAndForcedOptionsCannotCreateAuthority()
    {
        var composer = new DecisionComposer(Prompt(cancellable: false, new Affordance(3, "Play", 9, 0, "Too expensive", Illegal: "Need 3")));
        Assert.False(composer.TryDecline(out _, out _));
        composer.SelectAffordance(3);
        Assert.False(composer.TryBuild(out _, out string? error));
        Assert.Equal("Need 3", error);
    }

    [Fact]
    public void ADeclineIsBuiltOnlyFromTheCurrentCancellablePrompt()
    {
        var composer = new DecisionComposer(Prompt(cancellable: true, new Affordance(3, "Respond", 9, 0, "Optional response")));
        Assert.True(composer.TryDecline(out EngineDecision? decision, out _));
        Assert.Equal(EngineDecision.Decline, decision);
    }
}
