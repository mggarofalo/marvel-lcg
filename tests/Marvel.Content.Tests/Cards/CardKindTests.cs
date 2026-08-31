using System.Text.Json;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Cards;

/// <summary>The printed face kinds carried by the complete card catalog.</summary>
public sealed class CardKindTests
{
    private static readonly string CardText =
        File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json"));

    private static readonly CardCatalog Cards = CardCatalog.Parse(CardText);

    [Fact]
    public void EveryExpansionOnlyPrintedTypeHasItsOwnKind()
    {
        var expected = new Dictionary<string, (CardKind Kind, int Count)>(StringComparer.Ordinal)
        {
            ["Leader"] = (CardKind.Leader, 24),
            ["Evidence"] = (CardKind.Evidence, 9),
            ["PlayerSideScheme"] = (CardKind.PlayerSideScheme, 39),
            ["Challenge"] = (CardKind.Challenge, 26),
        };
        var counts = expected.Keys.ToDictionary(type => type, _ => 0, StringComparer.Ordinal);
        using var document = JsonDocument.Parse(CardText);

        foreach (var printed in document.RootElement.GetProperty("cards").EnumerateArray())
        {
            string type = printed.GetProperty("type").GetString()!;
            if (!expected.TryGetValue(type, out var kind))
            {
                continue;
            }

            string faceId = printed.GetProperty("card_id").GetString()!;
            Assert.Equal(kind.Kind, Cards.Kind(faceId));
            counts[type]++;
        }

        Assert.Equal(
            expected.ToDictionary(pair => pair.Key, pair => pair.Value.Count),
            counts);
    }

    [Fact]
    public void LeaderIsADistinctKindWithVillainSemantics()
    {
        // `pack:mc56:leaders`: Leader is a "new card type," and leaders
        // "function exactly the same as villains." The first sentence keeps
        // the printed kind distinct; the second controls the shared predicate.
        foreach (var kind in Enum.GetValues<CardKind>())
        {
            Assert.Equal(
                kind is CardKind.EncounterVillain or CardKind.Leader,
                CardKinds.IsVillain(kind));
            Assert.Equal(
                kind is CardKind.Minion or CardKind.EncounterVillain or CardKind.Leader,
                CardKinds.IsEnemy(kind));
            Assert.Equal(
                kind is CardKind.Hero or CardKind.AlterEgo or CardKind.Ally or CardKind.Minion
                    or CardKind.EncounterVillain or CardKind.Leader,
                CardKinds.IsCharacter(kind));
        }
    }
}
