using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

// Actual production row on synthetic boards; no public pack admission.
public sealed class WaspAllForOneTests : CoreCardTestBase
{
    private const string Hellcat = "01020";
    private const string Vision = "01068";
    private const string MariaHill = "01067";

    [Theory]
    [InlineData(0, 3)]
    [InlineData(1, 4)]
    [InlineData(2, 5)]
    [Rule("rr:attack-player-ability-type.1")]
    public void EachExhaustedAvengerAddsOneDamage(int exhausting, int damage)
    {
        var runner = AuthoredCards.Runner();
        var world = WaspBoards.Board("13001a");
        var wasp = world.Seats[0].IdentityCard;
        var hellcat = WaspBoards.InPlay(world, Hellcat, DeckType.AlliesArea);
        var vision = WaspBoards.InPlay(world, Vision, DeckType.AlliesArea);
        var maria = WaspBoards.InPlay(world, MariaHill, DeckType.AlliesArea);
        vision.Exhaust();
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        var card = world.CreateCard("13032", world.Seats[0].Hand);
        var energy = world.CreateCard("01088", world.Seats[0].Hand);
        var action = Assert.Single(runner.Actions(world, 0), a => a.Card == card.ObjectId);
        runner.ExecuteAction(world, new PlayerAction(action, [energy.ObjectId], []));
        var target = Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, runner, []));
        Sequence.Answer(world, world.Facts, runner, target, Decision.Take(villain.ObjectId), []);
        var prompt = Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, runner, []));
        var offer = Assert.Single(prompt.Affordances);
        // Only ready Avengers you control: Maria Hill lacks the trait, Vision is exhausted.
        Assert.Equal([wasp.ObjectId, hellcat.ObjectId], offer.Targets!.Legal.Order());
        Assert.Equal(0, offer.Targets.Min);
        Assert.Equal(2, offer.Targets.Max);
        int[] chosen = [.. new[] { hellcat, wasp }.Take(exhausting).Select(item => item.ObjectId)];
        Sequence.Answer(world, world.Facts, runner, prompt, Decision.Take(offer.Id) with { Targets = chosen }, []);
        Sequence.Finish(world, world.Facts, runner, []);
        Assert.Equal(damage, villain.Damage);
        Assert.Equal(exhausting, new[] { wasp, hellcat }.Count(item => !item.Ready));
        Assert.True(maria.Ready);
        Assert.Equal(DeckType.DiscardPile, card.Area.Type);
    }

    [Fact]
    [Rule("rr:attack-player-ability-type.1")]
    public void WithNoReadyAvengerTheAttackDealsThree()
    {
        var runner = AuthoredCards.Runner();
        var world = WaspBoards.Board("13001a");
        world.Seats[0].IdentityCard.Exhaust();
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        var card = world.CreateCard("13032", world.Seats[0].Hand);
        var energy = world.CreateCard("01088", world.Seats[0].Hand);
        var action = Assert.Single(runner.Actions(world, 0), a => a.Card == card.ObjectId);
        runner.ExecuteAction(world, new PlayerAction(action, [energy.ObjectId], []));
        var target = Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, runner, []));
        Sequence.Answer(world, world.Facts, runner, target, Decision.Take(villain.ObjectId), []);
        Assert.Null(Sequence.Work(world, world.Facts, runner, []));
        Assert.Equal(3, villain.Damage);
    }
}
