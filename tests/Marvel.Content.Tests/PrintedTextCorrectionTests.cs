extern alias extractor;

using System.Text.Json;
using PrintedTextCorrection = extractor::Marvel.Cards.Extract.PrintedTextCorrection;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests;

public sealed class PrintedTextCorrectionTests
{
    [Fact]
    public void AbsentTextPreservesUpstreamWhileExplicitEmptyTextRemovesIt()
    {
        using var absent = JsonDocument.Parse("""{"attributes":{"ATK":"2"}}""");
        using var empty = JsonDocument.Parse("""{"text":"","authority":"printed erratum"}""");
        Assert.Null(PrintedTextCorrection.Read(absent.RootElement));
        Assert.Equal("", PrintedTextCorrection.Read(empty.RootElement));
    }

    [Theory]
    [InlineData("""{"text":null,"authority":"source"}""")]
    [InlineData("""{"text":4,"authority":"source"}""")]
    [InlineData("""{"text":"replacement"}""")]
    [InlineData("""{"text":"replacement","authority":""}""")]
    [InlineData("""{"text":"replacement","authority":false}""")]
    public void UnreadableOrUnsourcedCorrectionsFailClosed(string json)
    {
        using var document = JsonDocument.Parse(json);
        Assert.Throws<InvalidDataException>(
            () => PrintedTextCorrection.Read(document.RootElement));
    }

    [Fact]
    public void SourcedCorrectionPreservesMarkupAndExactText()
    {
        using var document = JsonDocument.Parse(
            """{"text":"<b>Forced Response</b>: Do this.","authority":"printed card"}""");
        Assert.Equal("<b>Forced Response</b>: Do this.",
            PrintedTextCorrection.Read(document.RootElement));
    }

    [Fact]
    public void PileItOnNamesItsOwnVillainInBothDisplayForms()
    {
        // Official mc03 insert p.3 card illustration:
        // "This card cannot leave play while Piledriver is in play."
        var cards = CardCatalog.Parse(File.ReadAllText(
            RepositoryPaths.Dataset("cards", "cards.json")));
        Assert.Contains("while Piledriver is in play", cards.Text("07034"));
        Assert.Contains("while Piledriver is in play", cards.FormattedText("07034"));
        Assert.DoesNotContain("while Wrecker is in play", cards.Text("07034"));
        Assert.Contains("while Wrecker is in play", cards.Text("07004"));
    }

    [Fact]
    public void BurningHungerRetainsBothPrintedTraitBranches()
    {
        // Phoenix #28: "search the encounter deck, discard pile, and set-aside
        // area for Dark Phoenix and reveal her"; "this card gains surge."
        var cards = CardCatalog.Parse(File.ReadAllText(
            RepositoryPaths.Dataset("cards", "cards.json")));
        var plain = cards.Text("34028");
        var formatted = cards.FormattedText("34028");

        Assert.Contains("Give to the Jean Grey player.", plain);
        Assert.Contains("If you have the UNLEASHED trait", plain);
        Assert.Contains("area for Dark Phoenix and reveal her. Then, remove Burning Hunger from the game.", plain);
        Assert.Contains("If you have the RESTRAINED trait", plain);
        Assert.Contains("remove 1 power counter from Phoenix Force and this card gains surge. Discard this card.", plain);
        Assert.Contains("<b>When Revealed</b>", formatted);
        Assert.Contains("[[UNLEASHED]]", formatted);
        Assert.Contains("[[RESTRAINED]]", formatted);
    }
}
