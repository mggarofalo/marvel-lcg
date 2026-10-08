using Marvel.View;
using Xunit;

namespace Marvel.Godot.Tests;

public sealed class CardRetaliateTokenTests
{
    [Theory]
    [InlineData(null, false, "")]
    [InlineData(0L, false, "")]
    [InlineData(1L, false, "Retaliate 1")]
    [InlineData(12L, false, "Retaliate 12")]
    [InlineData(2L, true, "")]
    public void CaptionUsesOnlyAuthorizedPositiveCurrentValues(long? value, bool concealed, string expected)
    {
        var card = new BoardCardPresentation(1, 1, concealed, "Visible character", "", "HERO", "", [])
        { Retaliate = value, RulesText = "Retaliate 99." };
        Assert.Equal(expected, CardRetaliateToken.Caption(card));
    }

}
