using Marvel.View;
using Xunit;

namespace Marvel.Godot.Tests;

public sealed class CardProgressValueTests
{
    [Theory]
    [InlineData("0/4")]
    [InlineData("1/4")]
    [InlineData("4/4")]
    [InlineData("11/11")]
    public void HealthRetainsTheSuppliedCurrentAndMaximumWithoutRepeatingPlayerScaling(string health)
    {
        BoardCardPresentation card = Card() with
        {
            Fields = [new("HEALTH", health)],
            PrintedStats = [new("HP", "4*")],
            PrintedMarks = [new("HP", true, 0) { Value = "4" }],
        };

        CardProgressValue value = Assert.IsType<CardProgressValue>(CardProgressValue.From(card));
        Assert.Equal(health, value.Value);
        Assert.False(value.IsThreat);
        Assert.False(value.PerPlayer);
    }

    [Theory]
    [InlineData("0", "0/14")]
    [InlineData("8", "8/14")]
    public void ThreatUsesTheSuppliedEffectiveThreshold(string threat, string expected)
    {
        BoardCardPresentation card = Card() with
        {
            Fields = [new("THREAT", threat), new("TARGET_THREAT", "14")],
            PrintedStats = [new("TargetThreat", "7*")],
            PrintedMarks = [new("TargetThreat", true, 0) { Value = "7" }],
        };

        CardProgressValue value = Assert.IsType<CardProgressValue>(CardProgressValue.From(card));
        Assert.Equal(expected, value.Value);
        Assert.True(value.IsThreat);
        Assert.False(value.PerPlayer);
    }

    [Fact]
    public void PrintedThresholdFallbackRetainsItsPerPlayerMark()
    {
        BoardCardPresentation card = Card() with
        {
            Fields = [new("THREAT", "2")],
            PrintedStats = [new("TargetThreat", "7*")],
            PrintedMarks = [new("TargetThreat", true, 0) { Value = "7" }],
        };

        CardProgressValue value = Assert.IsType<CardProgressValue>(CardProgressValue.From(card));
        Assert.Equal("2/7", value.Value);
        Assert.True(value.PerPlayer);
    }

    [Fact]
    public void SideSchemeHasNoInventedThresholdAndConcealmentHasNoProgress()
    {
        BoardCardPresentation card = Card() with
        {
            Fields = [new("THREAT", "0")],
            PrintedStats = [new("StartingThreat", "3*")],
        };
        Assert.Equal("0", CardProgressValue.From(card)?.Value);
        Assert.Null(CardProgressValue.From(card with { Concealed = true }));
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(2, false)]
    [InlineData(2, true)]
    public void MaximumModificationIsSuppliedIndependentlyOfDamage(long damage, bool modified)
    {
        var card = Card() with { Damage = damage, Fields = [new("HEALTH", "5/7")],
            EffectiveValues = new Dictionary<string, CardEffectiveValue>
            { ["HP"] = new(7, 7, "Printed", modified, []) } };
        Assert.Equal(modified, CardProgressValue.From(card)!.MaximumModified);
    }

    private static BoardCardPresentation Card() => new(1, 1, false, "Sample", "", "", "", []);
}
