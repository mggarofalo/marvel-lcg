using Marvel.Client;
using Marvel.Rules.Events;
using Xunit;

namespace Marvel.Godot.Tests;

public sealed class TargetlessCauseClientValidationTests
{
    [Theory]
    [InlineData(EventCauseKind.Thwart, -1, true)]
    [InlineData(EventCauseKind.Thwart, -2, false)]
    [InlineData(EventCauseKind.Scheme, -1, false)]
    [InlineData(EventCauseKind.Scheme, 0, true)]
    public void OnlyAThwartCauseMayOmitItsTarget(EventCauseKind kind, int target, bool accepted)
    {
        // Synthetic wire shape: the engine emits a thwart cause with no scheme when
        // Covert Ops finds nothing to thwart; a scheme cause always names a player.
        var result = new FieldSet(3, "health", 1, 2)
        {
            Cause = new EventCause(17, kind, 3, target) { SourceTitle = "Black Widow" },
        };

        Assert.Equal(accepted, ClientResponseValidation.HasCompleteEvents([result]));
    }
}
