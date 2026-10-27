using System.Text.Json;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.Setup;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class AntManReprintTests : CoreCardTestBase
{
    [Fact]
    public void FourActualFacesAreAdmittedWithoutAdmittingThePack()
    {
        using var book = JsonDocument.Parse(AuthoredCards.Text);
        var rows = book.RootElement.GetProperty("cards").EnumerateArray().ToArray();
        string[] faces = ["12019", "12021", "12022", "12023"];
        Assert.Equal(AuthoredCards.ExpectedFaceCount, rows.Length);
        Assert.Equal(faces, rows.Skip(723).Take(4).Select(row => row.GetProperty("card").GetString()));
        string[] allFaces = ["12012", "12013", "12015", "12018", "12019", "12021", "12022", "12023", "12033"];
        Assert.Equal(allFaces, AuthoredCards.Book.Authored.Where(face => face.StartsWith("12", StringComparison.Ordinal)).Order());
        Assert.DoesNotContain("12020", AuthoredCards.Book.Authored);
        Assert.DoesNotContain("12001a", AuthoredCards.Book.Authored);
        Assert.All(rows.Skip(724).Take(3), row => Assert.Empty(row.GetProperty("abilities").EnumerateArray()));
    }

    [Theory]
    [InlineData(false, 1)]
    [InlineData(true, 3)]
    [Rule("rr:target.3")]
    public void FirstAidHealsChosenCharacterOnly(bool villainTarget, int damage)
    {
        // "A target is valid ... if any part of that ability can affect that target."
        var world = ThorActualFixture.Board("06001a", 2);
        var runner = AuthoredCards.Runner();
        var hero = world.Seats[1].IdentityCard;
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        var target = villainTarget ? villain : hero;
        target.TakeDamage(damage);
        var source = world.CreateCard("12019", world.Seats[0].Hand);
        var payment = world.CreateCard("12021", world.Seats[0].Hand);
        var action = Assert.Single(runner.Actions(world, 0), item => item.Card == source.ObjectId);
        runner.ExecuteAction(world, new PlayerAction(action, [payment.ObjectId], []));
        var prompt = Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, runner, []));
        Assert.Contains(prompt.Affordances, item => item.AnchorId == target.ObjectId);
        Assert.DoesNotContain(prompt.Affordances, item => item.AnchorId == world.Seats[0].IdentityCard.ObjectId);
        Assert.DoesNotContain(prompt.Affordances, item => item.AnchorId == source.ObjectId);
        Sequence.Answer(world, world.Facts, runner, prompt, Decision.Take(target.ObjectId), []);
        Sequence.Finish(world, world.Facts, runner, []);
        Assert.Equal(Math.Max(0, damage - 2), target.Damage);
        Assert.Equal(DeckType.DiscardPile, source.Area.Type);
        Assert.Equal(DeckType.DiscardPile, payment.Area.Type);
    }

    [Fact]
    [Rule("rr:target.2")]
    public void FirstAidCannotBePlayedWithoutADamagedCharacter()
    {
        // An ability requiring targets "can only be initiated if it has at least one valid target."
        var world = ThorActualFixture.Board("06001a");
        var source = world.CreateCard("12019", world.Seats[0].Hand);
        var payment = world.CreateCard("12021", world.Seats[0].Hand);
        var runner = AuthoredCards.Runner();
        string before = world.Digest().Canonical();
        Assert.DoesNotContain(runner.Actions(world, 0), item => item.Card == source.ObjectId);
        Assert.Equal(before, world.Digest().Canonical());
        Assert.Same(world.Seats[0].Hand, payment.Area);
    }

    [Theory]
    [InlineData("12021", "01088", "YY")]
    [InlineData("12022", "01089", "BB")]
    [InlineData("12023", "01090", "RR")]
    [Rule("rr:resource.1")]
    [Rule("rr:max-maximum.2")]
    public void ResourceReprintsGenerateTwoAndShareTitleLimit(string face, string core, string icons)
    {
        // A discarded card generates "the resource or resources indicated".
        // "Max X per deck" restricts copies in each deck; maximum applies across copies by title.
        var world = ThorActualFixture.Board("06001a");
        var payment = world.CreateCard(face, world.Seats[0].Hand);
        var source = world.CreateCard("01032", world.Seats[0].Hand);
        var runner = AuthoredCards.Runner();
        Assert.Equal(icons, runner.ResourcesGeneratedBy(world, payment, source));
        Assert.Equal("1", world.Facts.Attributes(face)["MaxPerDeck"]);
        Assert.Single(Blueprints.From([new Creation(face, CreationSource.PlayerDeck, 0)], Cards));
        var refused = Assert.Throws<ArgumentException>(() => Blueprints.From(
            [new Creation(face, CreationSource.PlayerDeck, 0), new Creation(core, CreationSource.PlayerDeck, 0)], Cards));
        Assert.Contains(Cards.Title(face), refused.Message, StringComparison.Ordinal);
        Assert.Contains("maximum is 1", refused.Message, StringComparison.Ordinal);
        var action = Assert.Single(runner.Actions(world, 0), item => item.Card == source.ObjectId);
        runner.ExecuteAction(world, new PlayerAction(action, [payment.ObjectId], [world.TheCardIn(DeckType.VillainArea)!.ObjectId]));
        Sequence.Finish(world, world.Facts, runner, []);
        Assert.Equal(4, world.TheCardIn(DeckType.VillainArea)!.Damage);
        Assert.Equal(DeckType.DiscardPile, payment.Area.Type);
    }
}
