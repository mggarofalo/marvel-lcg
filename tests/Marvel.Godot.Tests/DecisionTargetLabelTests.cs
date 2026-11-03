using Marvel.Decisions;
using Marvel.Godot;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Xunit;

namespace Marvel.Godot.Tests;

// A choice row is its action and name; a detail is a chip only when it tells the row from the others.
public sealed class DecisionTargetLabelTests
{
    private static TargetRequest Request(params (int Id, string? Detail)[] targets) =>
        new([.. targets.Select(target => target.Id)], 1, 1)
        {
            Details = targets.Where(target => target.Detail is not null)
                .ToDictionary(target => target.Id, target => target.Detail!),
        };

    [Fact]
    public void EffectShownInTheFixedHeaderIsNotRepeatedInTheTargetSelector() =>
        Assert.Null(ChoiceRowWording.Distinguishing(
            Request((1, "Rhino: Printed damage: 8; later effects unresolved."), (2, "Hydra Mercenary: 2 HP")), 1, "Rhino",
            "Printed damage: 8; later effects unresolved."));

    [Theory]
    [InlineData("Rhino: 14/14 → 12/14 HP", "14/14 → 12/14 HP")]
    [InlineData("3 → 1 threat", "3 → 1 threat")]
    public void ADetailThatTellsTargetsApartIsAChipWithoutTheName(string detail, string expected) =>
        Assert.Equal(expected, ChoiceRowWording.Distinguishing(
            Request((1, detail), (2, "Hydra Mercenary: 3/3 → 1/3 HP")), 1, "Rhino"));

    [Fact]
    public void ADetailEveryTargetSharesStaysOnHover()
    {
        const string shared = "Discard and replace this card when you confirm your mulligan.";
        TargetRequest request = Request((1, shared), (2, shared), (3, shared));

        Assert.Null(ChoiceRowWording.Distinguishing(request, 2, "Web-Shooter"));
        Assert.Equal($"Web-Shooter: {shared}", ChoiceRowWording.Tooltip("Web-Shooter", shared));
    }

    [Fact]
    public void ASoleTargetsDetailIsAChipBecauseNoOtherRowShowsIt() =>
        Assert.Equal("Heal 2 damage", ChoiceRowWording.Distinguishing(Request((1, "Heal 2 damage")), 1, "Spider-Man"));

    [Fact]
    public void ATargetWithoutADetailIsItsNameAlone()
    {
        Assert.Null(ChoiceRowWording.Distinguishing(Request((1, null), (2, "Other")), 1, "Rhino"));
        Assert.Equal("Rhino", ChoiceRowWording.Tooltip("Rhino", null));
    }

    [Theory]
    [InlineData(false, "◇ Replace · Aunt May")]
    [InlineData(true, "✓ Replace · Aunt May")]
    public void ARowsLabelIsItsMarkActionAndName(bool selected, string expected) =>
        Assert.Equal(expected, ChoiceRowWording.Target("Replace", "Aunt May", selected));

    [Fact]
    public void AGroupRowNamesItsMembersInOrderWithoutShoutedState()
    {
        Assert.Equal("◇ Group 2 · Rhino → Hydra Mercenary", ChoiceRowWording.Group(1, ["Rhino", "Hydra Mercenary"], false));
        Assert.Equal("✓ Group 1 · Rhino", ChoiceRowWording.Group(0, ["Rhino"], true));
    }

    [Theory]
    [InlineData(Game.ResolveMulligans, "Redraw", 6)]
    [InlineData(Game.EndPhaseVerb, "Discard", 6)]
    [InlineData("Attack", "Targets", 6)]
    public void SelectionProgressIsATrackerAgainstTheMostThatCanBeSelected(string verb, string label, int total)
    {
        var composer = new DecisionComposer(new Prompt(0, Question.TurnOption, Marvel.Rules.Timing.TimingPriority.Untimed,
            "test", "Choose", false, [new Affordance(3, verb, 19, 0, verb, new TargetRequest([1, 2, 3, 4, 5, 6], 0, 6))]));
        composer.SelectAffordance(3);
        composer.SelectTargets([2, 4]);

        DraftPart part = ChoiceRowWording.Progress(composer, composer.Progress().Targets)!;

        Assert.Equal((label, 2, total), (part.Label, part.Filled, part.Total));
        Assert.Equal(DecisionPanelCopy.TargetProgress(composer, composer.Progress().Targets), part.Text);
    }

    [Fact]
    public void AGroupedSelectionTracksOneCompleteGroup()
    {
        var request = new TargetRequest([1, 2, 3], 3, 3, Groups: [[1, 2], [1, 3]]);
        var composer = new DecisionComposer(new Prompt(0, Question.TurnOption, Marvel.Rules.Timing.TimingPriority.Untimed,
            "test", "Choose", false, [new Affordance(3, "Choose", 19, 0, "Explosive Arrow", request)]));
        composer.SelectAffordance(3);

        DraftPart part = ChoiceRowWording.Progress(composer, composer.Progress().Targets)!;

        Assert.Equal((0, 1), (part.Filled, part.Total));
    }

    [Fact]
    public void ARepeatableTargetTracksItsCountAgainstItsLimit()
    {
        DraftPart part = ChoiceRowWording.Repeated("Rhino", "Deal 1 damage", 2, 3);

        Assert.Equal(("Rhino", 2, 3), (part.Label, part.Filled, part.Total));
        Assert.True(DraftFactsRow.Tracks(part));
        Assert.Contains("Deal 1 damage", part.Text);
    }
}
