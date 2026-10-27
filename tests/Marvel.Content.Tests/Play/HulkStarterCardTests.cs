using System.Text.Json;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class HulkStarterCardTests
{
    [Fact]
    public void ExactStarterSuffixAddsEightFacesWithoutOpeningRemainder()
    {
        using var book = JsonDocument.Parse(AuthoredCards.Text);
        var rows = book.RootElement.GetProperty("cards").EnumerateArray().ToArray();
        Assert.Equal(712, rows.Length);
        Assert.Equal(Enumerable.Range(10011, 9).Where(id => id != 10015).Select(id => id.ToString(System.Globalization.CultureInfo.InvariantCulture)),
            rows.Skip(700).Take(8).Select(row => row.GetProperty("card").GetString()));
        Assert.All(rows.Skip(700).Take(8), row => Assert.False(row.TryGetProperty("startingCounters", out _)));
        Assert.Equal(27, AuthoredCards.Book.Authored.Count(id => id.StartsWith("10", StringComparison.Ordinal)));
        Assert.DoesNotContain("10015", AuthoredCards.Book.Authored);
        Assert.DoesNotContain("10025", AuthoredCards.Book.Authored);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(3)]
    public void SheHulkAttackTracksLiveDamageAndHealing(int damage)
    {
        var (_, world) = HulkFirstSliceFixture.PlayingHulk(_ => { });
        world.Abilities = AuthoredCards.Runner();
        var ally = world.CreateCard("10013", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(1), cardOwner: 0));
        ally.TakeDamage(damage);
        Assert.Equal(1 + damage, StateFields.Modified(world, ally, "attack", world.Facts, world.Players));
        Damage.Heal(world, world.Facts, ally, damage, "fixture", "heal", []);
        Assert.Equal(1, StateFields.Modified(world, ally, "attack", world.Facts, world.Players));
    }

    [Theory]
    [InlineData("10014", true)]
    [InlineData("10002", true)]
    [InlineData("10003", false)]
    [InlineData("10019", false)]
    public void MartialProwessUsesActualAttackTraitRatherThanAttackLabel(string face, bool allowed)
    {
        var (_, world) = HulkFirstSliceFixture.PlayingHulk(_ => { });
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        var upgrade = world.CreateCard("10018", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(1), cardOwner: 0));
        var source = world.CreateCard(face, world.Seats[1].Hand);
        Assert.Equal(allowed, CardPayment.Generators(world, world.Facts, world.Seats[1], runner, source)
            .Any(generator => generator.Effect == upgrade.ObjectId));
        Assert.True(upgrade.Ready);
    }

    [Theory]
    [InlineData("10014", "GG")]
    [InlineData("10019", "G")]
    public void PowerAggressionDoublesOnlyForTheActualAspectCard(string face, string expected)
    {
        var (_, world) = HulkFirstSliceFixture.PlayingHulk(_ => { });
        var runner = AuthoredCards.Runner();
        var power = world.CreateCard("10017", world.Seats[1].Hand);
        var source = world.CreateCard(face, world.Seats[1].Hand);
        Assert.Equal(expected, runner.ResourcesGeneratedBy(world, power, source));
        Assert.Equal("2", world.Facts.Attributes("10017")["MaxPerDeck"]);
    }
}
