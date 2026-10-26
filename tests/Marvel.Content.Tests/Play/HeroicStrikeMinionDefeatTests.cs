using Marvel.Content.Tests.Cards;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class HeroicStrikeMinionDefeatTests : CoreCardTestBase
{
    [Fact]
    public void AnotherDamageTargetAfterAnOpaqueDefeatWindowRaisesBeforeCosts()
    {
        // Synthetic attack: the first minion's defeat may change the next target.
        // No speculative board result authorizes damage to that next character.
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
            {"attack":{"target":{"query":"villain"},"effect":{"seq":[
              {"dealDamage":{"cards":{"query":"minions"},"amount":100}},
              {"draw":{"player":"you","count":1}}
            ]}}}
            """, cost: """{"exhaust":"this"}""", includeAuthored: true);
        Card? source = null;
        Card? soldier = null;
        Card? other = null;
        var error = Assert.Throws<RulesNotImplementedException>(() => Playing(world =>
        {
            source = InPlay(world, AuthoredCards.AuntMay);
            soldier = world.CreateCard("02023", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            other = world.CreateCard("01101", soldier.Area);
        }, hero: true, abilities: runner));
        Assert.Contains("damage batch", error.Message);
        Assert.True(source!.Ready);
        Assert.Equal(0, soldier!.Damage);
        Assert.Equal(0, other!.Damage);
        Assert.Equal(DeckType.EngagedEnemiesArea, soldier.Area.Type);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    [Rule("rr:when-defeated-abilities.2.1")]
    [Rule("rr:leaves-play.1")]
    public void HeroicStrikeFinishesSoldierDefeatBeforeItsBoundStun(bool physical, bool lethal)
    {
        // "A defeated card leaves play after its When Defeated ability is resolved."
        // Leaving play creates "a new copy of the card": the following stun cannot
        // rebind to a different Goblin Soldier or to the discarded incarnation.
        var world = Hero("03001a", 2);
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        foreach (var seat in world.Seats)
            world.CreateCard("01088", seat.Deck);
        world.CreateCard("01186", world.AreaOf(DeckType.EncounterDeck));
        world.CreateCard("01187", world.AreaOf(DeckType.EncounterDeck));
        world.CreateCard("01094", world.AreaOf(DeckType.VillainArea));
        var soldier = world.CreateCard("02023", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1)));
        var other = world.CreateCard("02023", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1)));
        var source = world.CreateCard("03004", world.Seats[0].Hand);
        var doubleResource = world.CreateCard(physical ? "01090" : "01089", world.Seats[0].Hand);
        var singleResource = world.CreateCard("01093", world.Seats[0].Hand);
        if (!lethal) Statuses.Give(world, soldier, Statuses.Tough);
        var action = Assert.Single(runner.Actions(world, 0), option => option.Card == source.ObjectId);
        var events = runner.ExecuteAction(world, new PlayerAction(action,
            [doubleResource.ObjectId, singleResource.ObjectId], [])).Events.ToList();
        var prompt = Sequence.Work(world, Cards, runner, events)!;
        Sequence.Answer(world, Cards, runner, prompt, Decision.Take(soldier.ObjectId), events);
        Sequence.Finish(world, Cards, runner, events);
        Assert.Equal(lethal ? DeckType.EncounterDiscardPile : DeckType.EngagedEnemiesArea, soldier.Area.Type);
        Assert.Equal(lethal ? 1 : 0, world.Seats[1].IdentityCard.Damage);
        Assert.Equal(0, world.Seats[0].IdentityCard.Damage);
        Assert.Equal(physical && !lethal, Statuses.Has(world, soldier, Statuses.Stunned));
        Assert.False(Statuses.Has(world, other, Statuses.Stunned));
        Assert.Equal(0, other.Damage);
        Assert.Equal(DeckType.DiscardPile, source.Area.Type);
        Assert.All(new[] { doubleResource, singleResource }, card => Assert.Equal(DeckType.DiscardPile, card.Area.Type));
        Assert.Null(Sequence.Work(world, Cards, runner, events));
    }
}
