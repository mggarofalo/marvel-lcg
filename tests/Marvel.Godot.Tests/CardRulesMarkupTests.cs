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
            "[b]Hero Action[/b] [i](attack)[/i]: Spend a [b]◉[/b] resource while [i]Aerial[/i].",
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
    public void PlainTextIsTheFallbackAndPrintedResourcesKeepTheirOrder()
    {
        Assert.Equal("Guard.", CardRulesMarkup.ToBbCode(string.Empty, "Guard."));
        Assert.Equal("● ⚡ ◉ ★", CardRulesMarkup.ResourceIcons("RYBG"));
    }
}
