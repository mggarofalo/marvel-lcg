using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>Synthetic attribution controls; no additional card admission.</summary>
public sealed class ActivationBoostIndependentReviewTests
{
    [Fact]
    public void BoostKeepsItsCardAsDamageSourceWhileLegacyAttackDamageRemainsTwo()
    {
        var runner = Runner("""
            {"enemyAttacks":{"enemies":{"query":"villain"}}},
            {"if":{"test":{"atLeast":{"value":{"result":"activationDamagePlaced"},"count":1}},
              "then":{"draw":{"player":"you","count":1}}}},
            {"if":{"test":{"atLeast":{"value":{"result":"activationDamagePlaced"},"count":2}},
              "then":{"draw":{"player":"you","count":3}}}},
            {"if":{"test":{"atLeast":{"value":{"result":"activationDamage"},"count":2}},
              "then":{"draw":{"player":"you","count":1}}}}
            """, Blast + """
            ,{"card":"01092","abilities":[{
              "trigger":{"event":"WhenDamageWouldBeDealt","timing":"ForcedInterrupt","actor":"villain","target":"you"},
              "effect":{"preventDamage":"trigger.target"}
            }]}
            """);
        var world = Board(runner);
        InPlay(world, "01092");
        int held = world.Seats[0].Hand.Cards.Count;
        var events = Reveal(world, runner);
        Finish(world, runner, events);
        Assert.Equal(1, world.Seats[0].IdentityCard.Damage);
        Assert.Equal(held + 2, world.Seats[0].Hand.Cards.Count);
        Assert.Equal(2, Assert.Single(events.OfType<AttackCompleted>()).DamageDealt);
    }

    [Fact]
    public void PaidInterruptEventInsideAttackDoesNotInheritBoostOwnership()
    {
        var runner = Runner("""
            {"enemyAttacks":{"enemies":{"query":"villain"}}},
            {"if":{"test":{"atLeast":{"value":{"result":"activationDamagePlaced"},"count":3}},
              "then":{"draw":{"player":"you","count":1}}}},
            {"if":{"test":{"atLeast":{"value":{"result":"activationDamagePlaced"},"count":4}},
              "then":{"draw":{"player":"you","count":3}}}}
            """, Blast + """
            ,{"card":"01077","abilities":[{
              "trigger":{"event":"WhenDamageWouldBeDealt","timing":"Interrupt","actor":"villain","target":"you"},
              "effect":{"dealDamage":{"cards":"you","amount":1}}
            }]}
            """);
        var world = Board(runner);
        var played = world.CreateCard("01077", world.Seats[0].Hand);
        int held = world.Seats[0].Hand.Cards.Count;
        var events = Reveal(world, runner);
        var defense = Sequence.Work(world, world.Facts, runner, events)!;
        Assert.Equal(Question.Defender, defense.Asking);
        Sequence.Answer(world, world.Facts, runner, defense, Decision.Decline, events);
        var interrupt = Sequence.Work(world, world.Facts, runner, events)!;
        var offer = Assert.Single(interrupt.Affordances, item => item.AnchorId == played.ObjectId);
        Sequence.Answer(world, world.Facts, runner, interrupt, Decision.Take(offer.Id), events);
        Finish(world, runner, events);
        Assert.Equal(DeckType.DiscardPile, played.Area.Type);
        Assert.Equal(4, world.Seats[0].IdentityCard.Damage);
        Assert.Equal(held, world.Seats[0].Hand.Cards.Count);
        Assert.Equal(2, Assert.Single(events.OfType<AttackCompleted>()).DamageDealt);
    }

    [Theory]
    [InlineData(false, 1)]
    [InlineData(true, 0)]
    public void BoostIndirectAssignmentRetainsOwnershipAndCountsOnlyUnpreventedShares(bool toughAlly, int drawn)
    {
        var runner = Runner("""
            {"enemyAttacks":{"enemies":{"query":"villain"}}},
            {"if":{"test":{"atLeast":{"value":{"result":"activationDamagePlaced"},"count":4}},
              "then":{"draw":{"player":"you","count":1}}}}
            """, """
            ,{"card":"01154","abilities":[{
              "trigger":{"event":"WhenCardRevealed","timing":"Boost","subject":"this"},
              "effect":{"indirectDamage":{"among":{"query":"heroesAndAllies"},"amount":3}}
            }]}
            """);
        var world = Board(runner);
        var hero = world.Seats[0].IdentityCard;
        var ally = world.CreateCard("01020", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        if (toughAlly) Statuses.Give(world, ally, Statuses.Tough);
        int held = world.Seats[0].Hand.Cards.Count;
        var events = Reveal(world, runner);
        var defense = Sequence.Work(world, world.Facts, runner, events)!;
        Assert.Equal(Question.Defender, defense.Asking);
        Sequence.Answer(world, world.Facts, runner, defense, Decision.Decline, events);
        var assignment = Sequence.Work(world, world.Facts, runner, events)!;
        var offer = Assert.Single(assignment.Affordances);
        Assert.Equal(3, offer.Targets!.Min);
        Sequence.Answer(world, world.Facts, runner, assignment,
            Decision.Take(offer.Id, [hero.ObjectId, ally.ObjectId, ally.ObjectId], []), events);
        Finish(world, runner, events);
        Assert.Equal(3, hero.Damage);
        Assert.Equal(DeckType.AlliesArea, ally.Area.Type);
        Assert.Equal(toughAlly ? 0 : 2, ally.Damage);
        Assert.Equal(held + drawn, world.Seats[0].Hand.Cards.Count);
        Assert.Equal(2, Assert.Single(events.OfType<AttackCompleted>()).DamageDealt);
    }

    private const string Blast = """
        ,{"card":"01154","abilities":[{
          "trigger":{"event":"WhenCardRevealed","timing":"Boost","subject":"this"},
          "effect":{"dealDamage":{"cards":"you","amount":1}}
        }]}
        """;

    private static AbilityRunner Runner(string effect, string extra) => new(AbilityCatalog.Parse(
        "{\"cards\":[{\"card\":\"02031\",\"abilities\":[{\"trigger\":{\"event\":\"WhenCardRevealed\",\"timing\":\"WhenRevealed\",\"subject\":\"this\"},\"effect\":{\"seq\":["
        + effect + "]}}]}" + extra + "]}"));

    private static World Board(AbilityRunner runner, bool hero = true, string boost = "01154")
    {
        var (_, world) = Playing(_ => { }, hero: hero, abilities: runner);
        for (int i = 0; i < 12; i++) world.CreateCard("01087", world.AreaOf(DeckType.EncounterDeck));
        world.CreateCard(boost, world.AreaOf(DeckType.EncounterDeck));
        return world;
    }

    private static List<GameEvent> Reveal(World world, AbilityRunner runner)
    {
        runner.ExecuteReveal(world, world.CreateCard("02031", world.AreaOf(DeckType.RevealingArea)), 0);
        return [];
    }

    private static void Finish(World world, AbilityRunner runner, List<GameEvent> events)
    {
        for (int i = 0; i < 30; i++)
        {
            var prompt = Sequence.Work(world, world.Facts, runner, events);
            if (prompt is null) break;
            Sequence.Answer(world, world.Facts, runner, prompt, Decision.Decline, events);
        }
        Assert.False(world.IsOver);
        Assert.Empty(world.Agenda.Outstanding);
        Assert.Null(world.Activation);
    }
}
