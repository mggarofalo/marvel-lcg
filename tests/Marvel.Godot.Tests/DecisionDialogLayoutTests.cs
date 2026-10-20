using Godot;
using Marvel.Rules.Prompts;
using Marvel.Rules.Timing;
using Xunit;
namespace Marvel.Godot.Tests;

public sealed class DecisionDialogLayoutTests
{
    [Theory]
    [InlineData(1920, 1080, 80)]
    [InlineData(1920, 1080, 100)]
    [InlineData(1920, 1080, 150)]
    [InlineData(1280, 720, 150)]
    public void OrderingDialogHasRoomForBothChoicesAndASeparateCommitment(int width, int height, int scale)
    {
        var viewport = new Vector2(width, height);
        Rect2 bounds = DecisionDialogLayout.Frame(viewport, (InterfaceScale)scale, 2);
        Assert.True(new Rect2(Vector2.Zero, viewport).Encloses(bounds));
        Assert.True(bounds.Size.X <= 1000);
        Assert.True(bounds.Size.Y >= 340);
        Assert.Equal(MathF.Round((width - bounds.Size.X) / 2), bounds.Position.X);
        Assert.True(bounds.Size.Y <= height - 96);
    }

    [Fact]
    public void AutomaticDialogRoutesOrderAndNonSpatialChoicesWithoutTakingTableTargets()
    {
        var choice = new Prompt(0, Question.Order, TimingPriority.Occurrence, "reveal", "next effect", false,
            [new(1, "Resolve", 4, 0, "Incite"), new(2, "Resolve", 4, 0, "When Revealed")]);
        Assert.True(DecisionDialogLayout.Automatic(choice));
        Assert.Equal("Resume current choice", ChoiceDisclosure.Label(choice));
        Assert.True(DecisionDialogLayout.Automatic(choice with { PublicKind = PublicDecisionKind.Choice }));
        Assert.False(DecisionDialogLayout.Automatic(choice with { PublicKind = PublicDecisionKind.PlayerAction }));
        Assert.False(DecisionDialogLayout.Automatic(choice with { PublicKind = PublicDecisionKind.Choice,
            Affordances = [choice.Affordances[0] with { SelectionSourceId = 9 }] }));
        Assert.False(DecisionDialogLayout.Automatic(choice with { PublicKind = PublicDecisionKind.Choice,
            Affordances = [choice.Affordances[0] with { Targets = new([8, 9], 1, 1) }] }));
    }
}
