using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class AbilityRevealDescriptionTests
{
    [Theory]
    [InlineData("different-cancel")]
    [InlineData("different-action")]
    [InlineData("different-subject")]
    [InlineData("different-final-effect")]
    [InlineData("missing-step")]
    [InlineData("extra-step")]
    [InlineData("different-order")]
    public void AnIncompleteOrDifferentProgramDoesNotClaimToCancelAndReplaceTheRevealedCard(string shape)
    {
        // Synthetic programs verify the explanation boundary, not playable content.
        AbilityEffect cancel = new AbilityEffect.Fixed(AbilityFixedInstruction.CancelWhenRevealed);
        var discard = new AbilityEffect.CardAction(AbilityCardInstruction.Discard,
            new AbilityCardSelection.Bound(AbilityCardBinding.TriggerSubject));
        AbilityEffect reveal = new AbilityEffect.Fixed(AbilityFixedInstruction.RevealTop);
        AbilityEffect other = new AbilityEffect.Fixed(AbilityFixedInstruction.PlaceAccelerationToken);
        AbilityEffect.Sequence program = shape switch
        {
            "different-cancel" => new([other, discard, reveal]),
            "different-action" => new([cancel, discard with { Instruction = AbilityCardInstruction.Exhaust }, reveal]),
            "different-subject" => new([cancel, discard with
                { Selection = new AbilityCardSelection.Bound(AbilityCardBinding.This) }, reveal]),
            "different-final-effect" => new([cancel, discard, other]),
            "missing-step" => new([cancel, reveal]),
            "extra-step" => new([cancel, discard, reveal, other]),
            "different-order" => new([reveal, cancel, discard]),
            _ => throw new ArgumentOutOfRangeException(nameof(shape)),
        };

        Assert.Null(AbilityEffectDescription.Summary(program));
    }
}
