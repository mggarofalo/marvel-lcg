using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class LabelledPowerDefeatContinuationTests : CoreCardTestBase
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SyntheticMultiDamagePowerRetainsModifierUntilDefeatAndFollowingDamageFinish(bool soldierFirst)
    {
        string soldierDamage = """{"dealAttackDamage":{"cards":{"titled":"Hydra Soldier"},"amount":3}}""";
        string villainDamage = """{"dealAttackDamage":{"cards":{"query":"villain"},"amount":1}}""";
        string sequence = soldierFirst ? soldierDamage + "," + villainDamage : villainDamage + "," + soldierDamage;
        var runner = Runner("04043", "Action", """
            {"chooseCard":{"from":{"query":"identities"},"effect":{"seq":[
              {"attack":{"target":{"query":"villain"},"effect":{"seq":[SEQUENCE]}}},
              {"heal":{"card":"chosen","amount":1}}
            ]}}}
            """.Replace("SEQUENCE", sequence, StringComparison.Ordinal), includeAuthored: true);
        var world = Hero("01001a", 2);
        world.Abilities = runner;
        world.Seats[1].IdentityCard.TakeDamage(2);
        var villain = world.CreateCard("01094", world.AreaOf(DeckType.VillainArea));
        var soldier = world.CreateCard("01182", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1)));
        world.CreateCard("01186", world.AreaOf(DeckType.EncounterDeck));
        world.CreateCard("01187", world.AreaOf(DeckType.EncounterDeck));
        var card = world.CreateCard("04043", world.Seats[0].Hand);
        var payment = world.CreateCard("01088", world.Seats[0].Hand);
        // Keep payment from exhausting the player deck and dealing an extra encounter card.
        world.CreateCard("01089", world.Seats[0].Deck);
        var modifier = new ContinuousEffect(EffectSource.LastingEffect, Kind: "attackDamage", Amount: 1,
            Affects: card.ObjectId, Lasts: new Duration(Uses: 1));
        world.Effects.Register(modifier);
        world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect, Kind: "retaliate",
            Amount: 1, Affects: villain.ObjectId));
        var action = Assert.Single(runner.Actions(world, 0), option => option.Card == card.ObjectId && option.Ordinal == 1);
        var events = runner.ExecuteAction(world, new PlayerAction(action, [payment.ObjectId], [])).Events.ToList();
        var choice = Sequence.Work(world, Cards, runner, events)!;
        Sequence.Answer(world, Cards, runner, choice, Decision.Take(world.Seats[1].IdentityCard.ObjectId), events);
        Sequence.Finish(world, Cards, runner, events);
        Assert.Equal(Outcome.Unfinished, world.Result);
        Assert.Equal(DeckType.EncounterDiscardPile, soldier.Area.Type);
        Assert.Single(world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(1)).Cards);
        Assert.Equal(2, villain.Damage);
        Assert.Equal(1, world.Seats[0].IdentityCard.Damage);
        Assert.Equal(1, world.Seats[1].IdentityCard.Damage);
        Assert.DoesNotContain(modifier, world.Effects.Active());
        Assert.Equal(DeckType.DiscardPile, card.Area.Type);
    }

    [Fact]
    public void EncounterPileDependentFollowupStillRaisesBeforeCostInsteadOfGuessing()
    {
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
            {"attack":{"target":{"query":"villain"},"effect":{"seq":[
              {"dealAttackDamage":{"cards":{"titled":"Hydra Soldier"},"amount":4}},
              {"if":{"test":{"exists":{"cardsIn":{"area":"encounterDeck"}}},
                "then":{"draw":{"player":"you","count":1}}}}
            ]}}}
            """, cost: """{"exhaust":"this"}""", includeAuthored: true);
        Card? source = null;
        Card? soldier = null;
        var error = Assert.Throws<RulesNotImplementedException>(() => Playing(world =>
        {
            source = InPlay(world, AuthoredCards.AuntMay);
            soldier = world.CreateCard("01182", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1)));
        }, hero: true, heroes: ["spider_man", "captain_marvel"], abilities: runner));
        Assert.Contains("unprojected read", error.Message);
        Assert.True(source!.Ready);
        Assert.Equal(0, soldier!.Damage);
    }

    [Rule("rr:damage.step.7")]
    [Rule("rr:when-defeated-abilities.2.1")]
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void PressTheAdvantageFinishesItsDefeatWindowBeforeCheckingRemainingStatus(bool lethal)
    {
        // A When Defeated ability resolves before its card leaves play. Once the card leaves,
        // its status cannot satisfy the event's following "is stunned or confused" condition.
        var world = Hero("01001a", 2);
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        world.CreateCard("01094", world.AreaOf(DeckType.VillainArea));
        var soldier = world.CreateCard("01182", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1)));
        if (lethal) soldier.TakeDamage(2);
        Statuses.Give(world, soldier, Statuses.Confused);
        world.CreateCard("01186", world.AreaOf(DeckType.EncounterDeck));
        world.CreateCard("01187", world.AreaOf(DeckType.EncounterDeck));
        var card = world.CreateCard("04043", world.Seats[0].Hand);
        var payment = world.CreateCard("01088", world.Seats[0].Hand);
        world.CreateCard("01089", world.Seats[0].Deck);
        world.CreateCard("01090", world.Seats[0].Deck);
        var action = Assert.Single(runner.Actions(world, 0), option => option.Card == card.ObjectId);
        var events = runner.ExecuteAction(world, new PlayerAction(action, [payment.ObjectId], [])).Events.ToList();
        var choice = Sequence.Work(world, Cards, runner, events)!;
        Sequence.Answer(world, Cards, runner, choice, Decision.Take(soldier.ObjectId), events);
        Sequence.Finish(world, Cards, runner, events);
        Assert.Equal(lethal ? DeckType.EncounterDiscardPile : DeckType.EngagedEnemiesArea, soldier.Area.Type);
        Assert.Equal(lethal ? 1 : 0, world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(1)).Cards.Count);
        Assert.Empty(world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(0)).Cards);
        Assert.Equal(lethal ? 0 : 1, world.Seats[0].Hand.Cards.Count);
        Assert.Equal(DeckType.DiscardPile, card.Area.Type);
        Assert.Equal(DeckType.DiscardPile, payment.Area.Type);
        Assert.Null(Sequence.Work(world, Cards, runner, events));
    }
}
