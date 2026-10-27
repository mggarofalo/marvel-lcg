extern alias extractor;

using System.Text.Json;
using Supplement = extractor::Marvel.Cards.Extract.Supplement;
using ExtractedCards = extractor::Marvel.Cards.Extract.Cards;
using Xunit;

namespace Marvel.Content.Tests;

public sealed class SupplementStatAnnotationTests
{
    [Fact]
    public void FullCardPreservesIndependentPrintedSymbolsThroughOfflineExtraction()
    {
        // Synthetic extraction facts distinguish special stars from numeric/per-player values.
        using var source = JsonDocument.Parse("""
            {"groups":[{"cards":[{
              "card_id":"sample","name":"Sample","type":"Villain",
              "attributes":{"ATK":"2","HP":"10*","Boost":"0"},
              "stat_annotations":{
                "ATK":{"special_star":true},
                "SCH":{"value":"★","special_star":true},
                "HP":{"per_player":true},
                "Boost":{"value":"0","special_star":true,"per_player":false}
              }
            }]}]}
            """);
        var supplement = Supplement.Read(source.RootElement);
        var card = Assert.Single(supplement.Only);
        Assert.Equal("2", card.Attributes["ATK"]);
        Assert.Equal("10*", card.Attributes["HP"]);
        Assert.False(card.Attributes.ContainsKey("SCH"));
        Assert.Empty(card.LinkedTo);

        using var generated = JsonDocument.Parse(ExtractedCards.Write(supplement.Only, supplement));
        var output = Assert.Single(generated.RootElement.GetProperty("cards").EnumerateArray());
        var annotations = output.GetProperty("stat_annotations");
        Assert.True(annotations.GetProperty("ATK").GetProperty("special_star").GetBoolean());
        Assert.False(annotations.GetProperty("ATK").TryGetProperty("value", out _));
        Assert.Equal("★", annotations.GetProperty("SCH").GetProperty("value").GetString());
        Assert.True(annotations.GetProperty("HP").GetProperty("per_player").GetBoolean());
        Assert.Equal("0", annotations.GetProperty("Boost").GetProperty("value").GetString());
        Assert.True(annotations.GetProperty("Boost").GetProperty("special_star").GetBoolean());
        Assert.False(annotations.GetProperty("Boost").GetProperty("per_player").GetBoolean());
        Assert.Equal("0", output.GetProperty("attributes").GetProperty("Boost").GetString());
    }

    [Fact]
    public void FullCardWithoutAnnotationsDoesNotInventPrintedStars()
    {
        using var source = JsonDocument.Parse("""
            {"groups":[{"cards":[{"card_id":"sample","type":"Villain",
              "attributes":{"HP":"10*","ATK":"2"}}]}]}
            """);
        var supplement = Supplement.Read(source.RootElement);
        Assert.Empty(Assert.Single(supplement.Only).StatAnnotations);
        using var generated = JsonDocument.Parse(ExtractedCards.Write(supplement.Only, supplement));
        var output = Assert.Single(generated.RootElement.GetProperty("cards").EnumerateArray());
        Assert.False(output.TryGetProperty("stat_annotations", out _));
        Assert.Equal("10*", output.GetProperty("attributes").GetProperty("HP").GetString());
    }
}
