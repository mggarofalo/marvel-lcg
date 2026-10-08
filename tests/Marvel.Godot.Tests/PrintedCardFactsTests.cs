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
            Fields = [new("ATTACK", "99"), new("THWART", "99")],
            EffectiveValues = new Dictionary<string, CardEffectiveValue>
            { ["ATK"] = new(2, 4, "Printed", true, []), ["THW"] = new(1, 0, "Printed", true, []) },
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
        Assert.False(live.Modified);
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

    [Theory]
    [InlineData("—", false)]
    [InlineData("X", true)]
    [InlineData("★", true)]
    [InlineData("0", false)]
    public void CanonicalSymbolsDoNotDependOnLegacyAttributePresence(string value, bool special)
    {
        BoardCardPresentation card = Card("ALLY", new()) with
        {
            PrintedMarks = [new("ATK", false, 2) { Value = value, SpecialStar = special }],
            Fields = [new("ATTACK", "0")],
        };
        CardStatValue stat = Assert.Single(CardStatValues.From(card));
        Assert.Equal(value, stat.Value);
        Assert.Equal(special, stat.SpecialStar);
        Assert.Equal(value == "★", stat.IsBareStar);
        Assert.Equal(2, stat.ConsequentialDamage);
        Assert.Empty(CardStatValues.From(card with { PrintedMarks = [], Fields = [] }));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ResolvedXUsesAuthoritativeModificationFlag(bool modified)
    {
        BoardCardPresentation card = Card("ENCOUNTER MINION", new()) with
        {
            PrintedMarks = [new("ATK", false, 0) { Value = "X", SpecialStar = true }],
            EffectiveValues = new Dictionary<string, CardEffectiveValue>
            { ["ATK"] = new(6, modified ? 9 : 6, "Defined", modified, []) },
        };
        CardStatValue value = Assert.Single(CardStatValues.From(card));
        Assert.Equal("X", value.Printed);
        Assert.Equal(modified ? "9" : "6", value.Value);
        Assert.Equal(modified, value.Modified);
        Assert.True(value.SpecialStar);
    }

    [Fact]
    public void FaceTokensPreserveSuppliedOrientationStatesWithoutAddingReadyClutter()
    {
        BoardCardPresentation card = Card("MINION", new()) with { Status = "EXHAUSTED  ·  FACE DOWN" };
        var tokens = CardStatusEntries.From(card);
        Assert.Contains(tokens, token => token.Name == "EXHAUSTED" && token.Text == "Exhausted");
        Assert.Contains(tokens, token => token.Name == "FACE DOWN" && token.Text == "Face Down");
        Assert.Empty(CardStatusEntries.From(card with { Status = "READY" }));
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
            PrintedMarks = BoardPrintedValueMarks.From(printed.ToDictionary(value => value.Key,
                value => new CardPrintedValue(value.Value, false, false, 0))),
        };
}
