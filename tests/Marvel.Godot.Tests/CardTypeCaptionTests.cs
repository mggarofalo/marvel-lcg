using Xunit;

namespace Marvel.Godot.Tests;

public sealed class CardTypeCaptionTests
{
    [Theory]
    [InlineData("ENCOUNTER VILLAIN", "VILLAIN")]
    [InlineData("ENCOUNTER SIDE SCHEME", "SIDE SCHEME")]
    [InlineData("PLAYER SIDE SCHEME", "SIDE SCHEME")]
    [InlineData("HERO", "HERO")]
    [InlineData("ALTER EGO", "ALTER EGO")]
    [InlineData("MAIN SCHEME", "MAIN SCHEME")]
    [InlineData("MINION", "MINION")]
    [InlineData("ENCOUNTER DECK", "ENCOUNTER DECK")]
    public void TypeCaptionsRemoveOnlyKnownClassificationPrefixes(string kind, string caption)
    {
        Assert.Equal(caption, CardTypeCaption.From(kind));
    }
}
