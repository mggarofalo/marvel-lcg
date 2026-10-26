using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class SoldierDefeatIndependentReviewTests : CoreCardTestBase
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:when-defeated-abilities.2.1")]
    public void PressTheAdvantageChecksBoundStatusAfterSoldierSettlement(bool lethal)
    {
        // "A defeated card leaves play after its 'When Defeated' ability is resolved."
        // Printed 04043 draws only if that enemy is stunned or confused.
        var world = Hero("01001a", 2);
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        world.CreateCard("01094", world.AreaOf(DeckType.VillainArea));
        var soldier = world.CreateCard("02023", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1)));
        if (lethal) soldier.TakeDamage(3);
        Statuses.Give(world, soldier, Statuses.Confused);
        var source = world.CreateCard("04043", world.Seats[0].Hand);
        var resource = world.CreateCard("01088", world.Seats[0].Hand);
        world.CreateCard("01089", world.Seats[0].Deck);
        world.CreateCard("01090", world.Seats[0].Deck);
        var drawn = world.Seats[0].Deck.Cards[^1];
        var action = Assert.Single(runner.Actions(world, 0), option => option.Card == source.ObjectId);
        var events = runner.ExecuteAction(world, new PlayerAction(action, [resource.ObjectId], [])).Events.ToList();
        var prompt = Sequence.Work(world, Cards, runner, events)!;
        Sequence.Answer(world, Cards, runner, prompt, Decision.Take(soldier.ObjectId), events);
        Sequence.Finish(world, Cards, runner, events);
        Assert.Equal(lethal ? DeckType.EncounterDiscardPile : DeckType.EngagedEnemiesArea, soldier.Area.Type);
        Assert.Equal(lethal ? 1 : 0, world.Seats[1].IdentityCard.Damage);
        Assert.Equal(0, world.Seats[0].IdentityCard.Damage);
        Assert.Equal(lethal ? 0 : 1, world.Seats[0].Hand.Cards.Count);
        Assert.Same(lethal ? world.Seats[0].Deck : world.Seats[0].Hand, drawn.Area);
        Assert.Equal(DeckType.DiscardPile, source.Area.Type);
        Assert.Equal(DeckType.DiscardPile, resource.Area.Type);
        Assert.Null(Sequence.Work(world, Cards, runner, events));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:when-defeated-abilities.2.1")]
    [Rule("rr:tough.1")]
    public void PhotonicBlastSettlesSoldierDamageProtectionBeforeItsEnergyDraw(bool tough)
    {
        // "A defeated card leaves play after its 'When Defeated' ability is resolved."
        // "When this character would take damage, remove a tough status card from it instead."
        // Printed 01013's energy-paid draw still resolves.
        var world = Hero("01010a", 2);
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        world.CreateCard("01094", world.AreaOf(DeckType.VillainArea));
        var soldier = world.CreateCard("02023", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1)));
        if (tough) Statuses.Give(world, world.Seats[1].IdentityCard, Statuses.Tough);
        var source = world.CreateCard("01013", world.Seats[0].Hand);
        var energy = world.CreateCard("01014", world.Seats[0].Hand);
        world.CreateCard("01088", world.Seats[0].Deck);
        world.CreateCard("01089", world.Seats[0].Deck);
        var drawn = world.Seats[0].Deck.Cards[^1];
        var action = Assert.Single(runner.Actions(world, 0), option => option.Card == source.ObjectId);
        var events = runner.ExecuteAction(world, new PlayerAction(action, [energy.ObjectId], [])).Events.ToList();
        var prompt = Sequence.Work(world, Cards, runner, events)!;
        Sequence.Answer(world, Cards, runner, prompt, Decision.Take(soldier.ObjectId), events);
        Sequence.Finish(world, Cards, runner, events);
        Assert.Equal(DeckType.EncounterDiscardPile, soldier.Area.Type);
        Assert.Equal(tough ? 0 : 1, world.Seats[1].IdentityCard.Damage);
        Assert.False(Statuses.Has(world, world.Seats[1].IdentityCard, Statuses.Tough));
        Assert.Equal(0, world.Seats[0].IdentityCard.Damage);
        Assert.Same(drawn, Assert.Single(world.Seats[0].Hand.Cards));
        Assert.Equal(DeckType.DiscardPile, source.Area.Type);
        Assert.Equal(DeckType.DiscardPile, energy.Area.Type);
        Assert.Null(Sequence.Work(world, Cards, runner, events));
    }

    [Fact]
    public void BoardConditionAfterOwnDefeatRaisesEvenWhenBothBranchesAreFixedDraws()
    {
        // Synthetic power: live board membership cannot be inferred through an opaque defeat.
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
            {"attack":{"target":{"query":"villain"},"effect":{"seq":[
              {"dealDamage":{"cards":{"titled":"Goblin Soldier"},"amount":100}},
              {"if":{"test":{"exists":{"query":"minions"}},
                "then":{"draw":{"player":"you","count":1}},
                "else":{"draw":{"player":"you","count":2}}}}
            ]}}}
            """, cost: """{"exhaust":"this"}""", includeAuthored: true);
        Card? source = null;
        Card? soldier = null;
        var error = Assert.Throws<RulesNotImplementedException>(() => Playing(world =>
        {
            source = InPlay(world, AuthoredCards.AuntMay);
            soldier = world.CreateCard("02023", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        }, hero: true, abilities: runner));
        Assert.Contains("unprojected defeat window", error.Message);
        Assert.True(source!.Ready);
        Assert.Equal(0, soldier!.Damage);
        Assert.Equal(DeckType.EngagedEnemiesArea, soldier.Area.Type);
    }
}

