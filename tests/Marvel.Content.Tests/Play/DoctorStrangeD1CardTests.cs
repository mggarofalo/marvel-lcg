using System.Text.Json;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>Actual production faces on nonpublic boards; no complete-product admission.</summary>
public sealed class DoctorStrangeD1CardTests
{
    [Fact]
    public void D1AddsExactlyFivePrintedFacesWithoutStartingCounters()
    {
        using var book = JsonDocument.Parse(AuthoredCards.Text);
        var rows = book.RootElement.GetProperty("cards").EnumerateArray().ToArray();
        string[] faces = ["09001a", "09001b", "09002", "09005", "09036"];
        Assert.Equal(659, rows.Length);
        Assert.Equal(faces, rows.Skip(654).Select(row => row.GetProperty("card").GetString()));
        Assert.All(rows.Skip(654), row => Assert.False(row.TryGetProperty("startingCounters", out _)));
        Assert.DoesNotContain("09032", AuthoredCards.Book.Authored);
        using var printed = JsonDocument.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));
        var actual = printed.RootElement.GetProperty("cards").EnumerateArray()
            .Where(row => faces.Contains(row.GetProperty("card_id").GetString())).ToArray();
        Assert.Equal(5, actual.Length);
        Assert.Equal("Hero", Face(actual, "09001a").GetProperty("type").GetString());
        Assert.Equal("AlterEgo", Face(actual, "09001b").GetProperty("type").GetString());
        Assert.Equal("3", Face(actual, "09002").GetProperty("attributes").GetProperty("Cost").GetString());
        Assert.Equal("1", Face(actual, "09005").GetProperty("attributes").GetProperty("Cost").GetString());
        Assert.Equal("0", Face(actual, "09036").GetProperty("attributes").GetProperty("Cost").GetString());
    }

    [Fact]
    [Rule("rr:ability.13")]
    public void OrdinaryFormDecisionOffersNaturalTalentAndPreventsMasterInAlterEgo()
    {
        // A hero/alter-ego ability requires "the specified form".
        var (world, game, _, runner) = SpellMasteryFixture.BoardInAlterEgo();
        var hero = world.Seats[1].IdentityCard;
        var master = Assert.Single(world.Seats[1].Hand.Cards, card => card.FaceId == "09005");
        Assert.Equal("09001b", hero.FaceId);
        Assert.DoesNotContain(game.Pending!.Affordances, offer => offer.AnchorId == master.ObjectId);
        var natural = Assert.Single(runner.Actions(world, 1), action => action.Card == hero.ObjectId);
        Assert.Equal("Natural Talent", runner.Describe(world, natural).Label);
        Assert.True(hero.Ready);
        game.Resolve(Decision.Take(Assert.Single(game.Pending.Affordances,
            offer => offer.Verb == Game.ChangeForm).Id));
        Assert.Equal("09001a", hero.FaceId);
        Assert.Contains(game.Pending!.Affordances, offer => offer.AnchorId == master.ObjectId);
    }

    private static JsonElement Face(JsonElement[] rows, string face) =>
        Assert.Single(rows, row => row.GetProperty("card_id").GetString() == face);
}
