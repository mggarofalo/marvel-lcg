using Godot;
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

    [Theory]
    [InlineData(172, 240, false)]
    [InlineData(264, 369, false)]
    [InlineData(400, 560, true)]
    public void CurrentRetaliationFitsItsOwnRowWithoutMovingStatsOrCorners(int width, int height, bool full)
    {
        var features = new CardFaceFeatures(false, false, false, true, true, full, 1);
        var plain = new CardFaceRegions(new Vector2(width - 8, height - 8), features);
        var current = new CardFaceRegions(new Vector2(width - 8, height - 8), features with { HasRetaliate = true });
        Assert.Equal(plain.Stats, current.Stats);
        Assert.Equal(plain.Health, current.Health);
        Assert.Equal(plain.Resources, current.Resources);
        Assert.True(current.Retaliate.Size.Y > 0);
        Assert.True(current.Retaliate.Position.Y >= current.Tokens.End.Y);
        Assert.True(current.Rules.Position.Y >= current.Retaliate.End.Y);
        Assert.True(current.Rules.Size.Y > 0);
        Assert.False(current.Retaliate.Intersects(current.Stats));
    }
    [Theory]
    [InlineData(0, true)]
    [InlineData(2, false)]
    [InlineData(2, true)]
    public void OptionalPortraitArtYieldsItsSpaceToCurrentValues(int tokenRows, bool retaliate)
    {
        var features = new CardFaceFeatures(false, false, true, true, true, true, 0)
        { RulesHeight = 180, TitleHeight = 42 };
        var plain = new CardFaceRegions(new Vector2(392, 552), features);
        var current = new CardFaceRegions(new Vector2(392, 552), features with
        { TokenRows = tokenRows, HasRetaliate = retaliate });
        Assert.True(current.Illustration.Size.Y < plain.Illustration.Size.Y);
        Assert.True(current.Rules.Size.Y >= plain.Rules.Size.Y - 4 * current.Unit - 0.001f);
        Assert.Equal(plain.Stats.Size, current.Stats.Size);
        Assert.False(current.Stats.Intersects(current.Rules));
        Assert.Equal(plain.Health, current.Health);
    }

}
