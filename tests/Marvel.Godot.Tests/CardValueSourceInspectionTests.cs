using Marvel.View;
using Xunit;

namespace Marvel.Godot.Tests;

public sealed class CardValueSourceInspectionTests
{
    [Fact]
    public void HistoricalSourceDoesNotAcquireALiveLinkEvenIfACallerSuppliesAnId()
    {
        var source = new CardValueSourceDescriptor("authorized-face", "Source", 90, true)
        { RulesText = "Authorized text", RulesMarkup = "<b>Authorized text</b>" };
        BoardCardPresentation card = CardValueSourceInspection.DescribeSource(source);
        Assert.Null(card.TargetId);
        Assert.Equal("Earlier source", card.Subtitle);
        Assert.Equal(source.RulesMarkup, card.RulesMarkup);
        Assert.Empty(card.EffectiveValues);
        Assert.Empty(card.PrintedStats);
        Assert.Equal(90, CardValueSourceInspection.DescribeSource(source with { Historical = false }).TargetId);
    }

    [Theory]
    [InlineData("DefineBase", 6, "Base 6")]
    [InlineData("Add", 6, "+6")]
    [InlineData("Add", -2, "-2")]
    public void IntrinsicDefinitionsRemainDistinctFromAdditions(string operation, long amount, string expected)
    {
        Assert.Equal(expected, CardValueSourceInspection.Operation(new(operation, amount, null, null)));
    }
}
