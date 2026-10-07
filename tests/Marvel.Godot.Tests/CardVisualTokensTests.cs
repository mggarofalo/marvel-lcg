using Xunit;

namespace Marvel.Godot.Tests;

public sealed class CardVisualTokensTests
{
    [Fact]
    public void CardTextRetainsReadableContrastOnBothApprovedFields()
    {
        Assert.True(VisualSystem.ContrastRatio(CardVisualTokens.Ink, CardVisualTokens.Paper) >= 12);
        Assert.True(VisualSystem.ContrastRatio(CardVisualTokens.Secondary, CardVisualTokens.Paper) >= 4.5);
        Assert.True(VisualSystem.ContrastRatio(CardVisualTokens.Modified, CardVisualTokens.Ink) >= 7);
    }

    [Theory]
    [InlineData("physical", "P", "C13547")]
    [InlineData("energy", "E", "D8AC1A")]
    [InlineData("mental", "M", "2675B8")]
    [InlineData("wild", "W", "278554")]
    public void InlineResourcesKeepTheirOwnColorAndCanonicalShape(string name, string glyph, string color)
    {
        string markup = CardRulesMarkup.ToBbCode($"Spend [{name}].", "unused");
        Assert.Contains($"[color=#{color}]", markup);
        Assert.Contains($"]{glyph}[/font_size][/font][/color]", markup);
        Assert.EndsWith(".", markup);
    }

    [Theory]
    [InlineData("cost", "D")]
    [InlineData("star", "S")]
    [InlineData("unique", "U")]
    public void PrintedMarksUseSeparatePinnedGlyphs(string name, string glyph)
    {
        Assert.True(CardSymbols.TryGet(name, out string? actual));
        Assert.Equal(glyph, actual);
        Assert.DoesNotContain("[color=", CardSymbols.Markup(glyph, 16));
        Assert.Contains($"]{glyph}[/font_size]", CardRulesMarkup.ToBbCode($"[{name}]", "unused"));
    }

    [Theory]
    [InlineData(CardFrameFamily.Enemy)]
    [InlineData(CardFrameFamily.Scheme)]
    [InlineData(CardFrameFamily.Environment)]
    public void EncounterFamiliesRetainEncounterAccentWhenClassificationIsNotAnAspect(CardFrameFamily family)
    {
        Assert.Equal(CardVisualTokens.Aspect("ENCOUNTER"), CardVisualTokens.Aspect("", family));
        Assert.Equal(CardVisualTokens.Aspect("ENCOUNTER"), CardVisualTokens.Aspect("Villain", family));
        Assert.NotEqual(CardVisualTokens.Aspect("HERO"), CardVisualTokens.Aspect("Villain", family));
    }

    [Fact]
    public void RequiredFontsAndOriginalFallbackArePackagedWithoutImportOrNetwork()
    {
        var assembly = typeof(CardControl).Assembly;
        foreach (string font in new[] { "Barlow-Regular", "Barlow-Bold", "Barlow-Italic", "BarlowCondensed-Bold", "ChampionsIcons" })
        {
            using Stream? bytes = assembly.GetManifestResourceStream($"Marvel.Godot.Assets.{font}.ttf");
            Assert.NotNull(bytes);
            Assert.True(bytes.Length > 1000);
        }
        using Stream? fallback = assembly.GetManifestResourceStream("Marvel.Godot.Assets.Art.fallback.svg");
        Assert.NotNull(fallback);
        using var reader = new StreamReader(fallback);
        string svg = reader.ReadToEnd();
        Assert.Contains("<path", svg);
        Assert.DoesNotContain("<image", svg);
    }
}
