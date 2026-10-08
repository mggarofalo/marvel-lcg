using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class AbilityReadinessDescriptionTests
{
    [Fact]
    public void CompleteCheckedSequenceNamesTheSourceDiscardAndIdentityReadying()
    {
        var effect = new AbilityEffect.Sequence([Discard(), Ready()]);
        Assert.Equal("Discard this card, then ready your identity.", AbilityEffectDescription.Summary(effect));
    }

    [Fact]
    public void AConditionalSequenceDoesNotBecomeAnUnqualifiedPromise()
    {
        var effect = new AbilityEffect.Conditional(
            new AbilityCondition.Exists(new AbilityCardSelection.Bound(AbilityCardBinding.TriggerActor)),
            new AbilityEffect.Sequence([Discard(), Ready()]), null);

        Assert.Null(AbilityEffectDescription.Summary(effect));
    }

    [Theory]
    [InlineData("different-discard")]
    [InlineData("different-source")]
    [InlineData("different-action")]
    [InlineData("different-identity")]
    [InlineData("missing-discard")]
    [InlineData("missing-ready")]
    [InlineData("extra-effect")]
    [InlineData("different-order")]
    public void AnotherProgramCannotAcquireTheDiscardAndReadyPromise(string shape)
    {
        // Synthetic checked instructions hold the explanation boundary to its
        // complete operation and bindings; they do not add playable content.
        AbilityEffect.CardAction discard = Discard();
        AbilityEffect.CardAction ready = Ready();
        AbilityEffect.Sequence effect = shape switch
        {
            "different-discard" => new([discard with { Instruction = AbilityCardInstruction.Exhaust }, ready]),
            "different-source" => new([discard with
                { Selection = new AbilityCardSelection.Bound(AbilityCardBinding.TriggerActor) }, ready]),
            "different-action" => new([discard, ready with { Instruction = AbilityCardInstruction.Exhaust }]),
            "different-identity" => new([discard, ready with
                { Selection = new AbilityCardSelection.Bound(AbilityCardBinding.TriggerTarget) }]),
            "missing-discard" => new([ready]),
            "missing-ready" => new([discard]),
            "extra-effect" => new([discard, ready,
                new AbilityEffect.Fixed(AbilityFixedInstruction.PlaceAccelerationToken)]),
            "different-order" => new([ready, discard]),
            _ => throw new ArgumentOutOfRangeException(nameof(shape)),
        };

        Assert.Null(AbilityEffectDescription.Summary(effect));
    }

    private static AbilityEffect.CardAction Discard() => new(AbilityCardInstruction.Discard,
        new AbilityCardSelection.Bound(AbilityCardBinding.This));

    private static AbilityEffect.CardAction Ready() => new(AbilityCardInstruction.Ready,
        new AbilityCardSelection.Bound(AbilityCardBinding.You));
}
