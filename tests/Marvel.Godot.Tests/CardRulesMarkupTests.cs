using Xunit;

namespace Marvel.Godot.Tests;

public sealed class CardRulesMarkupTests
{
    [Fact]
    public void SupportedCardMarkupPreservesEmphasisTraitsAndSymbols()
    {
        string rendered = CardRulesMarkup.ToBbCode(
            "<b>Hero Action</b> <i>(attack)</i>: Spend a [mental] resource while [[Aerial]].",
            "unused");

        Assert.Equal(
            "[b]Hero Action[/b] [i](attack)[/i]: Spend a [outline_size=1][outline_color=#132532][color=#2675B8][font=res://assets/fonts/ChampionsIcons.runtime.tres][font_size=22]M[/font_size][/font][/color][/outline_color][/outline_size] resource while [i]Aerial[/i].",
            rendered);
    }

    [Fact]
    public void UnsupportedMarkupIsDisplayedLiterallyRatherThanExecuted()
    {
        string rendered = CardRulesMarkup.ToBbCode(
            "<url>unsafe</url> [color=red]text",
            "unused");

        Assert.Equal("<url>unsafe</url> [lb]color=red]text", rendered);
    }

    [Fact]
    public void PlainTextIsTheFallbackAndPrintedResourcesUseNamedDistinctGlyphs()
    {
        Assert.Equal(
            "res://assets/fonts/ChampionsIcons.ttf",
            CardRulesMarkup.ResourceFontSourcePath);
        Assert.Equal(
            "res://assets/fonts/ChampionsIcons.runtime.tres",
            CardRulesMarkup.ResourceFontPath);
        Assert.Equal("Guard.", CardRulesMarkup.ToBbCode(string.Empty, "Guard."));
        Assert.Equal("P M E W", CardRulesMarkup.ResourceIcons("RBYW"));
        Assert.Equal(["P", "M", "E", "W"], CardRulesMarkup.ResourceGlyphs("RBYW"));
        Assert.Equal(
            [("Physical", "P"), ("Mental", "M"), ("Energy", "E"), ("Wild", "W")],
            CardRulesMarkup.ResourceTokens("RBYW"));
        Assert.Equal(
            "Physical, Mental, Energy, Wild",
            CardRulesMarkup.ResourceNames("RBYW"));
        Assert.DoesNotContain('R', CardRulesMarkup.ResourceIcons("R"));
    }

    [Theory]
    [InlineData(InterfaceScale.Percent80, 18)]
    [InlineData(InterfaceScale.Percent100, 22)]
    [InlineData(InterfaceScale.Percent120, 27)]
    public void InspectorResourceMarkupUsesScaleAwareOpticalSizing(
        InterfaceScale scale,
        int expectedMentalSize)
    {
        string rendered = CardRulesMarkup.ToBbCode(
            "Spend a [mental] resource.",
            "unused",
            scale);

        Assert.Equal(
            $"Spend a [outline_size=1][outline_color=#132532][color=#2675B8][font={CardRulesMarkup.ResourceFontPath}]"
                + $"[font_size={expectedMentalSize}]M[/font_size][/font][/color][/outline_color][/outline_size] resource.",
            rendered);
        Assert.DoesNotContain(">B<", rendered);
    }
}
