using Marvel.Decisions;
using Marvel.Rules.Prompts;
using Marvel.Rules.Timing;
using Marvel.View;
using Xunit;

namespace Marvel.Godot.Tests;

public sealed class CardPaymentDraftTests
{
    [Fact]
    public void VariableTypedCostRetainsItsResourceMeaningInAccessiblePresentation()
    {
        var cost = new CostOption(0, "X", Components:
            [new ResourceCost("X") { RepeatedResource = 'Y' }]);
        Assert.Equal("Pay X Energy", DecisionCostLabel.Accessible(cost));
    }

    [Fact]
    public void CardPlayKeepsEveryPaymentChoiceInItsWorkspaceInsteadOfDrawingBoardConnections()
    {
        var composer = Composer();
        composer.SelectAffordance(3);
        var visible = new AffordancePresentation(3, "Card", null, "Play", "Card", 19, 0, null, "", [])
        {
            AnchorKind = AffordanceAnchorKind.Card,
            Relationships = [new(RelationshipKind.OfferedGenerator, 19, 41)],
        };
        var prompt = new PromptPresentation("", "", "", "", "", [visible]);

        Assert.True(CardPaymentPresentation.UsesModal(composer));
        Assert.False(CardPaymentPresentation.UsesModal(composer, submitting: true));
        Assert.Empty(BoardInteractionControlProjection.From(composer, prompt));
        Assert.Empty(BoardInteractionRelationshipProjection.From(composer, prompt));
        Assert.Empty(BoardInteractionCueProjection.From(composer, prompt));
        Assert.False(composer.Progress().IsReady);
    }

    [Fact]
    public void MixedAbilityAndDiscardSelectionsAreReversibleAndRequireTheWholeCost()
    {
        // Synthetic offered sources prove composition without inventing card rules.
        var composer = Composer();
        composer.SelectAffordance(3);
        var binding = new TableDraftBinding(composer, 1, 4, (generation, revision) => generation == 1 && revision == 4);

        Assert.True(binding.TryToggleGenerator(41));
        Assert.False(composer.TryBuild(out _, out _));
        Assert.True(binding.TryToggleGenerator(42));
        Assert.True(composer.TryBuild(out EngineDecision? decision, out _));
        Assert.Equal([41, 42], decision!.Resources);
        Assert.True(binding.TryToggleGenerator(41));
        Assert.False(composer.Progress().Payment.IsSatisfied);
        Assert.DoesNotContain(41, composer.Resources);

        var stale = new TableDraftBinding(composer, 0, 4, (generation, _) => generation == 1);
        Assert.False(stale.TryToggleGenerator(41));
        Assert.Equal([42], composer.Resources);
    }

    private static DecisionComposer Composer() => new(new Prompt(0, Question.TurnOption,
        TimingPriority.Untimed, "synthetic", "Choose", false,
        [new Affordance(3, "Play", 19, 0, "Card", Costs:
            [new CostOption(19, "2", Sources: [new(41, "G"), new(42, "Y")])])]));
}
