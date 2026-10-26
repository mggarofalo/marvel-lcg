using Marvel.Decisions;
using Marvel.Rules.Play;
using Marvel.View;
using Xunit;

namespace Marvel.Godot.Tests;

public sealed class WidowChoiceMeaningTests
{
    [Fact]
    public void SafeHouseSelectedDraftRetainsExhaustionAndOwnDiscardChoice()
    {
        Game game = WidowNativeSceneTests.Create("widow-safe-house");
        VisibleResult visible = Project(game);
        PromptPresentation prompt = PromptPresentation.From(visible.Prompt!, visible.World);
        AffordancePresentation offer = Assert.Single(prompt.Affordances, item => item.Anchor == "Safe House #29");
        var draft = new DecisionComposer(visible.Prompt!);
        draft.SelectAffordance(offer.Id);
        string before = Assert.IsType<string>(TableDraftSummary.From(draft, prompt, visible.World));
        Assert.Contains("Exhaust Safe House #29", before);
        Assert.Contains("PREPARATION", before);
        Assert.Contains("discard pile", before);
        int target = offer.TargetRequest!.Legal[1];
        draft.SelectTargets([target]);
        string staged = Assert.IsType<string>(TableDraftSummary.From(draft, prompt, visible.World));
        Assert.Contains("Exhaust Safe House #29", staged);
        Assert.Contains("1 selected", staged);
        Assert.Contains("Widow's Bite", staged);
    }

    [Fact]
    public void CompletionButtonsRetainSuppliedDamageInstructionAndExhaustionCost()
    {
        Game game = WidowNativeSceneTests.Create("widow-preparation");
        game.Resolve(Decision.Take(Assert.Single(game.Pending!.Affordances).Id));
        VisibleResult visible = Project(game);
        PromptPresentation prompt = PromptPresentation.From(visible.Prompt!, visible.World);
        AffordancePresentation widow = Assert.Single(prompt.Affordances, item => item.Anchor == "Black Widow");
        var choice = Assert.Single(widow.WindowChoices, item => item.Label.Contains("Rhino"));
        string label = DecisionCopy.AbilityLabel(widow, choice.Label, choice.Description);
        Assert.Contains("Current damage: 1", label);
        Assert.Contains("14/14 → 13/14 HP", label);
        Assert.Contains("Later effects may change it", label);
        Assert.DoesNotContain("Printed damage", label);
        AffordancePresentation suit = Assert.Single(prompt.Affordances, item => item.Anchor == "Synth-Suit");
        Assert.Contains("Exhaust Synth-Suit", DecisionCopy.AbilityLabel(suit, suit.Label));
    }

    private static VisibleResult Project(Game game) => WorldProjection.For(game.State, game.Pending, [],
        new PermissiveVisibilityPolicy().Authorize(null, game.State.Seats.Count));
}
