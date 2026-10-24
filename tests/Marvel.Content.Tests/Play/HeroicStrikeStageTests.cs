using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class HeroicStrikeStageTests : CoreCardTestBase
{
    [Theory]
    [InlineData("01094", "01095", "01107", false)]
    [InlineData("01094", "01095", "01107", true)]
    [InlineData("01113", "01114", "01127", false)]
    [InlineData("01113", "01114", "01127", true)]
    [Rule("rr:villain-defeat.3.1")]
    [Rule("rr:when-revealed-abilities")]
    public void HeroicStrikeStunsTheSameCharacterAfterItsNewStageReveals(
        string firstFace, string nextFace, string schemeFace, bool physical)
    {
        // The new stage is "the same character"; its "When Revealed" abilities
        // resolve before Heroic Strike continues with the payment-dependent stun.
        var world = Hero("01001a");
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        world.CreateCard("01088", world.Seats[0].Deck);
        var first = world.CreateCard(firstFace, world.AreaOf(DeckType.VillainArea));
        var next = world.CreateCard(nextFace, world.AreaOf(DeckType.VillainDeck));
        var scheme = world.CreateCard(schemeFace, world.AreaOf(DeckType.EncounterDeck));
        world.CreateCard("01186", world.AreaOf(DeckType.EncounterDeck));
        first.TakeDamage(Damage.Health(world, Cards, first) - 6);
        var source = world.CreateCard("03004", world.Seats[0].Hand);
        var doubleResource = world.CreateCard(physical ? "01090" : "01089", world.Seats[0].Hand);
        var singleResource = world.CreateCard("01093", world.Seats[0].Hand);
        var action = Assert.Single(runner.Actions(world, 0), option => option.Card == source.ObjectId);
        var events = runner.ExecuteAction(world, new PlayerAction(action,
            [doubleResource.ObjectId, singleResource.ObjectId], [])).Events.ToList();
        var prompt = Sequence.Work(world, Cards, runner, events)!;
        Sequence.Answer(world, Cards, runner, prompt, Decision.Take(first.ObjectId), events);
        Sequence.Finish(world, Cards, runner, events);
        Assert.Same(next, world.TheCardIn(DeckType.VillainArea));
        Assert.Equal(DeckType.SideSchemesArea, scheme.Area.Type);
        Assert.Equal(0, next.Damage);
        Assert.Equal(physical, Statuses.Has(world, next, Statuses.Stunned));
        Assert.Equal(DeckType.DiscardPile, source.Area.Type);
        Assert.All(new[] { doubleResource, singleResource },
            card => Assert.Equal(DeckType.DiscardPile, card.Area.Type));
    }
}
