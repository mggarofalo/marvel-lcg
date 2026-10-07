using Marvel.View;
using Xunit;

namespace Marvel.Godot.Tests;

public sealed class PrintedCardFactsTests
{
    [Fact]
    public void EffectiveAllyPowerPreservesPrintedConsequentialDamage()
    {
        Dictionary<string, string> printed = new() { ["ATK"] = "2*", ["THW"] = "1**" };
        BoardCardPresentation card = Card("ALLY", printed) with
        {
            Fields = [new("ATTACK", "4"), new("THWART", "0")],
            PrintedMarks = BoardPrintedValueMarks.From(new Dictionary<string, CardPrintedValue>
            {
                ["ATK"] = new("2", false, false, 1),
                ["THW"] = new("1", false, false, 2),
            }),
        };

        CardStatValue attack = Assert.Single(CardStatValues.From(card), value => value.Name == "ATK");
        Assert.Equal("4", attack.Value);
        Assert.Equal("2", attack.Printed);
        Assert.True(attack.Modified);
        Assert.Equal(1, attack.ConsequentialDamage);
        Assert.False(attack.PerPlayer);
        Assert.Equal(2, Assert.Single(CardStatValues.From(card), value => value.Name == "THW").ConsequentialDamage);
    }

    [Fact]
    public void PrintedPlayerScalingDoesNotScaleOrReplaceLiveValuesInTheRenderer()
    {
        Dictionary<string, string> printed = new() { ["EscalationThreat"] = "1*", ["StartingThreat"] = "0", ["HS"] = "6" };
        BoardCardPresentation card = Card("MAIN SCHEME", printed) with
        {
            PrintedMarks = BoardPrintedValueMarks.From(new Dictionary<string, CardPrintedValue>
            {
                ["EscalationThreat"] = new("1", false, true, 0),
                ["StartingThreat"] = new("0", false, false, 0),
                ["HS"] = new("6", false, false, 0),
            }),
        };
        CardStatValue baseline = Assert.Single(CardStatValues.From(card), value => value.Name == "EscalationThreat");
        Assert.True(baseline.PerPlayer);
        Assert.Equal("1", baseline.Value);
        Assert.Contains(CardStatValues.From(card), value => value.Name == "StartingThreat" && value.Value == "0");
        Assert.Contains(CardStatValues.From(card), value => value.Name == "HS" && value.Value == "6");

        CardStatValue live = Assert.Single(CardStatValues.From(card with
        {
            Fields = [new("ESCALATION_THREAT", "3")],
        }), value => value.Name == "EscalationThreat");
        Assert.Equal("3", live.Value);
        Assert.Equal("1", live.Printed);
        Assert.True(live.Modified);
        Assert.False(live.PerPlayer);
    }

    [Fact]
    public void ConcealedDescriptorProducesNoStatsEvenIfMisconstructedWithFaceFacts()
    {
        BoardCardPresentation card = Card("ALLY", new() { ["ATK"] = "2*" }) with { Concealed = true };
        Assert.Empty(CardStatValues.From(card));
    }

    [Theory]
    [InlineData("ATK+", "3", "+3")]
    [InlineData("SCH+", "1", "+1")]
    [InlineData("DEF+", "-1", "-1")]
    public void PrintedAttachmentModifiersKeepTheirSign(string attribute, string printed, string shown)
    {
        CardStatValue value = Assert.Single(CardStatValues.From(Card("ATTACHMENT", new() { [attribute] = printed })));
        Assert.Equal(attribute, value.Name);
        Assert.Equal(shown, value.Value);
        Assert.Equal(shown, value.Printed);
        Assert.False(value.Modified);
    }

    [Fact]
    public void FaceTokensPreserveSuppliedOrientationStatesWithoutAddingReadyClutter()
    {
        BoardCardPresentation card = Card("MINION", new()) with { Status = "EXHAUSTED  ·  FACE DOWN" };
        var tokens = CardStatusTokens.Entries(card);
        Assert.Contains(tokens, token => token.Name == "EXHAUSTED" && token.Text == "↷");
        Assert.Contains(tokens, token => token.Name == "FACE DOWN" && token.Text == "▧");
        Assert.Empty(CardStatusTokens.Entries(card with { Status = "READY" }));
    }

    [Fact]
    public void InlineSymbolsFollowBodyTypeAndKeepDifferentMeaningsDistinct()
    {
        string result = CardRulesMarkup.ToBbCode("[physical] [per_hero] [boost] [unique]", "", symbolSize: 12);
        Assert.Contains("[font_size=12]P", result);
        Assert.Contains("[font_size=12]G", result);
        Assert.Contains("[font_size=12]B", result);
        Assert.Contains("[font_size=12]U", result);
        Assert.DoesNotContain("◆", result);
        Assert.DoesNotContain("font_size=22", result);
    }

    private static BoardCardPresentation Card(string kind, Dictionary<string, string> printed) =>
        new(1, 1, false, "Visible card", "", kind, "", [])
        {
            PrintedStats = [.. printed.Select(value => new BoardFieldPresentation(value.Key, value.Value))],
        };
}
