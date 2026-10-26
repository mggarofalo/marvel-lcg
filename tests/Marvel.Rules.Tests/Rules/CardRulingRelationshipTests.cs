using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Marvel.Rules.Index;
using Marvel.Tests;
using Xunit;

namespace Marvel.Rules.Tests.Rules;

/// <summary>Card-scoped interpretations remain pinned to both printed text and the reviewed ruling.</summary>
public sealed class CardRulingRelationshipTests
{
    [Fact]
    public void OnlyAuditedRulingRelationshipsAreCitable()
    {
        using var graph = JsonDocument.Parse(File.ReadAllText(RepositoryPaths.Dataset("rules-graph.json")));
        using var published = JsonDocument.Parse(File.ReadAllText(RepositoryPaths.Dataset("rulings", "rulings.json")));
        var audited = graph.RootElement.GetProperty("modifications").EnumerateObject()
            .Concat(graph.RootElement.GetProperty("card_clarifications").EnumerateObject())
            .Select(relationship => relationship.Name).ToHashSet(StringComparer.Ordinal);
        Assert.NotEmpty(audited);
        Assert.All(audited, id => Assert.Contains(id, RuleCitations.Citable));
        var unrelated = published.RootElement.GetProperty("rulings").EnumerateArray()
            .Select(ruling => ruling.GetProperty("id").GetString()!)
            .Where(id => !audited.Contains(id)).ToArray();
        Assert.NotEmpty(unrelated);
        Assert.All(unrelated, id => Assert.DoesNotContain(id, RuleCitations.Citable));
        Assert.DoesNotContain("ruling:0000000000000000", RuleCitations.Citable);
        Assert.DoesNotContain("rr:missing-citation", RuleCitations.Citable);
    }

    [Fact]
    public void EveryCardClarificationNamesCurrentPrintedTextAndPinnedRulings()
    {
        using var graph = JsonDocument.Parse(File.ReadAllText(RepositoryPaths.Dataset("rules-graph.json")));
        using var printed = JsonDocument.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));
        using var published = JsonDocument.Parse(File.ReadAllText(RepositoryPaths.Dataset("rulings", "rulings.json")));
        var cards = printed.RootElement.GetProperty("cards").EnumerateArray().ToDictionary(card => card.GetProperty("card_id").GetString()!);
        var rulings = published.RootElement.GetProperty("rulings").EnumerateArray().ToDictionary(ruling => ruling.GetProperty("id").GetString()!);
        var corpus = Corpus.Read();
        var relationships = graph.RootElement.GetProperty("card_clarifications").EnumerateObject().ToArray();
        Assert.NotEmpty(relationships);
        foreach (var relationship in relationships)
        {
            Assert.Equal(rulings[relationship.Name].GetProperty("hash").GetString(), relationship.Value.GetProperty("ruling_hash").GetString());
            Assert.False(string.IsNullOrWhiteSpace(relationship.Value.GetProperty("why").GetString()));
            var linkedCards = relationship.Value.GetProperty("cards").EnumerateObject().ToArray();
            Assert.NotEmpty(linkedCards);
            foreach (var card in linkedCards)
            {
                string text = cards[card.Name].GetProperty("text_plain").GetString()!;
                Assert.Equal("sha256:" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text))).ToLowerInvariant(), card.Value.GetString());
            }
            foreach (var reference in relationship.Value.GetProperty("references").EnumerateArray())
                Assert.NotNull(corpus.Find(reference.GetString()!));
            foreach (var superseded in relationship.Value.GetProperty("supersedes").EnumerateObject())
            {
                Assert.NotEqual(relationship.Name, superseded.Name);
                Assert.Equal(rulings[superseded.Name].GetProperty("hash").GetString(), superseded.Value.GetString());
            }
        }
    }

    [Fact]
    public void RevealOriginClarificationIsScopedToMatchingPrintedTriggers()
    {
        using var graph = JsonDocument.Parse(File.ReadAllText(RepositoryPaths.Dataset("rules-graph.json")));
        var current = graph.RootElement.GetProperty("card_clarifications")
            .GetProperty("ruling:7075a85e02f2203e");
        Assert.Equal("01004,01075,09007", string.Join(",", current.GetProperty("cards")
            .EnumerateObject().Select(card => card.Name)));
        Assert.Equal("ruling:2d5b4da522e21bb6", Assert.Single(current.GetProperty("supersedes")
            .EnumerateObject()).Name);
        Assert.Contains("ruling:7075a85e02f2203e", RuleCitations.Citable);
        Assert.Contains("Return the Favor", current.GetProperty("why").GetString(), StringComparison.Ordinal);
    }

    [Fact]
    public void MarkedForDeathExplicitlySupersedesItsInPlayInterpretation()
    {
        using var graph = JsonDocument.Parse(File.ReadAllText(RepositoryPaths.Dataset("rules-graph.json")));
        var current = graph.RootElement.GetProperty("card_clarifications").GetProperty("ruling:6d6264d6620980b3");
        Assert.True(current.GetProperty("cards").TryGetProperty("04028", out _));
        Assert.True(current.GetProperty("supersedes").TryGetProperty("ruling:681d243446872e43", out _));
    }

    [Theory]
    [InlineData("ruling:b07d53e12625ab23", "09005", "ruling:ea32bb34b1e7b1b0")]
    [InlineData("ruling:8883bc6b0af1c9b8", "09035", "ruling:5bafad5aafa9093b")]
    [InlineData("ruling:8c9c9d30f4c01189", "09033,09035", "ruling:5bafad5aafa9093b")]
    public void InvocationClarificationsSupersedeConflictingAnswersWithinReviewedCardScope(
        string rulingId, string cardIds, string historicalId)
    {
        using var graph = JsonDocument.Parse(File.ReadAllText(RepositoryPaths.Dataset("rules-graph.json")));
        using var published = JsonDocument.Parse(File.ReadAllText(RepositoryPaths.Dataset("rulings", "rulings.json")));
        var current = graph.RootElement.GetProperty("card_clarifications").GetProperty(rulingId);
        Assert.Equal(cardIds.Split(','), current.GetProperty("cards").EnumerateObject().Select(card => card.Name));
        Assert.Equal(historicalId, Assert.Single(current.GetProperty("supersedes").EnumerateObject()).Name);
        Assert.Contains(rulingId, RuleCitations.Citable);
        Assert.DoesNotContain(historicalId, RuleCitations.Citable);
        Assert.Contains(published.RootElement.GetProperty("rulings").EnumerateArray(),
            ruling => ruling.GetProperty("id").GetString() == historicalId);
        Assert.False(graph.RootElement.GetProperty("modifications").TryGetProperty(rulingId, out _));
    }

    [Fact]
    public void MasterClarificationRetainsInvocationInsertAndThenSequenceAsAuthorities()
    {
        using var graph = JsonDocument.Parse(File.ReadAllText(RepositoryPaths.Dataset("rules-graph.json")));
        var references = graph.RootElement.GetProperty("card_clarifications")
            .GetProperty("ruling:b07d53e12625ab23").GetProperty("references")
            .EnumerateArray().Select(reference => reference.GetString()).ToArray();
        Assert.Contains("pack:mc08:the-invocation-deck", references);
        Assert.Contains("rr:then", references);
        Assert.Contains("rr:special", references);
    }
}
