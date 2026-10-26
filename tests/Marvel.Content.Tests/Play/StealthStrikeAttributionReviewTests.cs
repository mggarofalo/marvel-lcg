using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class StealthStrikeAttributionReviewTests
{
    [Fact]
    [Rule("rr:stun-stunned.5")]
    public void StunnedAttackPaysButCannotRetainADefeatOrOfferThreatRemoval()
    {
        // "discard the stunned card instead. Costs associated with the attack attempt ... must still be paid."
        var runner = StealthStrikeContinuationTests.Runner();
        var world = ThorActualFixture.Board("06001a");
        world.Abilities = runner;
        var enemy = world.CreateCard("01167", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        var scheme = world.TheCardIn(DeckType.MainSchemesArea)!;
        scheme.PlaceTokens("k_threat", 5);
        Statuses.Give(world, world.Seats[0].IdentityCard, Statuses.Stunned);
        var (strike, payment, events) = Start(world, runner);
        Assert.Null(Sequence.Work(world, world.Facts, runner, events));
        Assert.Equal(DeckType.DiscardPile, strike.Area.Type);
        Assert.All(payment, card => Assert.Equal(DeckType.DiscardPile, card.Area.Type));
        Assert.False(Statuses.Has(world, world.Seats[0].IdentityCard, Statuses.Stunned));
        Assert.Equal(0, enemy.Damage);
        Assert.Equal(5, scheme.Tokens["k_threat"]);
        Assert.Empty(world.Agenda.Outstanding);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void NestedDamageDefeatIsNotCreditedToTheAttack(bool defeatsSelectedEnemy)
    {
        // Printed 08013 requires "that enemy is defeated by this attack". A nested response's
        // direct damage is its own cause, whether it defeats the attacked enemy or another one.
        var runner = new AbilityRunner(AbilityCatalog.Parse("""
            {"cards":[
              {"card":"08013","abilities":[{
                "trigger":{"event":"TurnAction","timing":"Action","subject":"you","form":"hero"},
                "labels":["attack"],
                "effect":{"chooseCard":{"from":{"query":"enemies"},"effect":{"seq":[
                  {"attack":{"target":"chosen","effect":{"dealAttackDamage":{"cards":"chosen","amount":4}}}},
                  {"if":{"test":{"wasDefeatedByDamage":"chosen"},"then":
                    {"chooseCard":{"from":{"query":"schemes"},"effect":{"removeThreat":{"scheme":"chosen","amount":2}}}}}}
                ]}}}
              }]},
              {"card":"08023","abilities":[{
                "trigger":{"event":"WhenDamageDealt","timing":"Response","subject":"game"},
                "effect":{"chooseCard":{"from":{"query":"enemies"},"effect":{"dealDamage":{"cards":"chosen","amount":10}}}}
              }]}
            ]}
            """));
        var world = ThorActualFixture.Board("06001a");
        world.Abilities = runner;
        var attacked = world.CreateCard("01181", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        var other = world.CreateCard("01167", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        var observer = world.CreateCard("08023", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        var scheme = world.TheCardIn(DeckType.MainSchemesArea)!;
        scheme.PlaceTokens("k_threat", 5);
        var (_, _, events) = Start(world, runner);
        var target = Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, runner, events));
        Sequence.Answer(world, world.Facts, runner, target, Decision.Take(attacked.ObjectId), events);
        var response = Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, runner, events));
        Sequence.Answer(world, world.Facts, runner, response,
            Decision.Take(Assert.Single(response.Affordances, a => a.AnchorId == observer.ObjectId).Id), events);
        target = Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, runner, events));
        var defeated = defeatsSelectedEnemy ? attacked : other;
        Sequence.Answer(world, world.Facts, runner, target, Decision.Take(defeated.ObjectId), events);
        // The observer may answer its own nested damage once; decline that independent window.
        if (Sequence.Work(world, world.Facts, runner, events) is { } nested)
            Sequence.Answer(world, world.Facts, runner, nested, Decision.Decline, events);
        Assert.Null(Sequence.Work(world, world.Facts, runner, events));
        Assert.Equal(DeckType.EncounterDiscardPile, defeated.Area.Type);
        Assert.Equal(5, scheme.Tokens["k_threat"]);
        if (!defeatsSelectedEnemy) Assert.Equal(4, attacked.Damage);
        Assert.Empty(world.Agenda.Outstanding);
    }

    private static (Card Strike, Card[] Payment, List<GameEvent> Events) Start(World world, AbilityRunner runner)
    {
        var strike = world.CreateCard("08013", world.Seats[0].Hand);
        Card[] payment = [world.CreateCard("01088", world.Seats[0].Hand), world.CreateCard("01089", world.Seats[0].Hand)];
        var events = runner.ExecuteAction(world, new PlayerAction(
            Assert.Single(runner.Actions(world, 0), a => a.Card == strike.ObjectId), payment.Select(card => card.ObjectId).ToArray(), [])).Events.ToList();
        return (strike, payment, events);
    }
}
