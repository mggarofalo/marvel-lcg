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
    public void MarkedForDeathExplicitlySupersedesItsInPlayInterpretation()
    {
        using var graph = JsonDocument.Parse(File.ReadAllText(RepositoryPaths.Dataset("rules-graph.json")));
        var current = graph.RootElement.GetProperty("card_clarifications").GetProperty("ruling:6d6264d6620980b3");
        Assert.True(current.GetProperty("cards").TryGetProperty("04028", out _));
        Assert.True(current.GetProperty("supersedes").TryGetProperty("ruling:681d243446872e43", out _));
    }
}
