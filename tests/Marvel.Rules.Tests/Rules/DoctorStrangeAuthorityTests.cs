using System.Text.Json;
using Marvel.Rules.Packs.Harvest;
using Marvel.Tests;
using Xunit;
using PackHarvest = Marvel.Rules.Packs.Harvest.Harvest;

namespace Marvel.Rules.Tests.Rules;

/// <summary>Source completeness controls; these do not certify hero execution.</summary>
public sealed class DoctorStrangeAuthorityTests
{
    [Fact]
    public void InvocationSetupRetainsTheCompleteSeparateFiveCardDeck()
    {
        // Authority: pack:mc08:the-invocation-deck, PDF p.1, first two paragraphs.
        string text = Read();
        Assert.Contains("five-card “Invocation deck” in addition to his player deck", text);
        Assert.Contains(
            "shuffle all five of Doctor Strange’s Invocation cards together "
            + "(Crimson Bands of Cyttorak, Images of Ikonn, Seven Rings of Raggadorr, "
            + "Vapors of Valtorr, and Winds of Watoomb)", text);
        Assert.Contains("place the Invocation deck facedown next to your identity card", text);
    }

    [Fact]
    public void TheTopCardIsPublicDuringPlayAndSpellMasteryResolvesIt()
    {
        // Authority: pack:mc08:the-invocation-deck, PDF p.1, third paragraph.
        string text = Read();
        Assert.Contains("During the game, play with the top card of the Invocation deck faceup at all times", text);
        Assert.Contains("You may use Doctor Strange’s “Spell Mastery” action to resolve the ability on that card", text);
    }

    [Fact]
    public void ResolvedCardsUseTheirOwnDiscardAndAnyEmptyDeckRecyclesWithoutPenalty()
    {
        // Authority: pack:mc08:the-invocation-deck, PDF p.1, third paragraph.
        string text = Read();
        Assert.Contains("After that card is resolved, it is placed in a special discard pile that belongs to the Invocation deck", text);
        Assert.Contains("If the Invocation deck is ever empty, shuffle the Invocation discard pile back into the Invocation deck", text);
        Assert.Contains("There is no penalty for doing this.", text);
    }

    [Fact]
    public void TheCompletePrintedSectionHasAStableIndexedIdentityAndContentHash()
    {
        string text = Read();
        string body = text.Split("# The Invocation Deck\n\n", StringSplitOptions.None)[1].Trim();
        Assert.Equal(3, body.Split("\n\n", StringSplitOptions.None).Length);
        using var index = JsonDocument.Parse(File.ReadAllBytes(
            RepositoryPaths.Dataset("rules-packs", "index.json")));
        var record = Assert.Single(index.RootElement.GetProperty("entries").EnumerateArray(),
            entry => entry.GetProperty("id").GetString() == "pack:mc08:the-invocation-deck");

        Assert.Equal("The Invocation Deck", record.GetProperty("title").GetString());
        Assert.Equal("the-invocation-deck", PackHarvest.Slug(record.GetProperty("title").GetString()!));
        Assert.Equal("mc08_doctor_strange_rulesheet.pdf", record.GetProperty("source").GetString());
        Assert.Equal(1, record.GetProperty("page").GetInt32());
        Assert.Equal(0, record.GetProperty("rules").GetInt32());
        Assert.Equal(Emit.Hash(body), record.GetProperty("hash").GetString());
        Assert.Contains($"hash: \"{Emit.Hash(body)}\"", text);
    }

    [Fact]
    public void TheRulesheetRetainsItsReviewedPdfBytePin()
    {
        using var manifest = JsonDocument.Parse(File.ReadAllBytes(
            RepositoryPaths.Dataset("rules-packs", "sources.manifest.json")));
        var source = Assert.Single(manifest.RootElement.GetProperty("files").EnumerateArray(),
            file => file.GetProperty("path").GetString() == "mc08_doctor_strange_rulesheet.pdf");

        Assert.Equal(7181027, source.GetProperty("bytes").GetInt64());
        Assert.Equal("sha256:15dba31ffeafaedac36cfd81b4f95e635c929d87775f52152595adebcd3a1cb3",
            source.GetProperty("hash").GetString());
    }

    private static string Read() => File.ReadAllText(
        RepositoryPaths.Dataset("rules-packs", "mc08", "the-invocation-deck.md"))
        .ReplaceLineEndings("\n");
}
