using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>The card-DSL action that declares a defender during an attack.</summary>
public sealed class CardDeclaredDefenderTests
{
    private static readonly CardCatalog Cards = CardCatalog.Parse(
        File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));

    [Rule("rr:defend-defense.2.1")]
    [Rule("rr:defend-defense.2.2")]
    [Fact]
    public void AnInitiationInterruptCanDeclareAnExhaustedHeroAsBasicDefender()
    {
        // The defense label first establishes the hero as the defender; the
        // printed declaration then makes it a basic defense. "Without
        // exhausting" permits an already-exhausted hero and does not ready it.
        var (world, villain) = Board();
        var hero = world.Seats[0].IdentityCard;
        hero.Exhaust();
        var runner = Runner(
            "01001a",
            """
            { "defense": { "effect": {
              "declareDefender": { "card": "you" }
            } } }
            """);

        ResolveInitiationInterrupt(world, runner, villain);

        Assert.Equal(hero.ObjectId, world.FinishedAttack!.Defender);
        Assert.True(world.FinishedAttack.BasicDefense);
        Assert.False(hero.Ready);
        Assert.Equal(0, hero.Damage);
    }

    [Rule("rr:defend-defense.3.2")]
    [Rule("rr:defend-defense.3.3")]
    [Fact]
    public void AnInitiationInterruptCanDeclareAnExhaustedAllyAsDefender()
    {
        // A card ability that declares an ally makes it the defender, and an
        // instruction that does so without exhausting can name an exhausted
        // ally. The ally remains exhausted and does not contribute DEF.
        var (world, villain) = Board();
        var ally = world.CreateCard(
            "01076", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        ally.Exhaust();
        var runner = Runner(
            "01076",
            """{ "declareDefender": { "card": "this" } }""");

        ResolveInitiationInterrupt(world, runner, villain);

        Assert.Equal(ally.ObjectId, world.FinishedAttack!.Defender);
        Assert.False(world.FinishedAttack.BasicDefense);
        Assert.False(ally.Ready);
        Assert.Equal(2, ally.Damage);
    }

    [Rule("rr:attack-enemy-activation.3.2")]
    [Fact]
    public void LaterTextInTheSameAbilitySeesThatTheDefendingAllyLeftPlay()
    {
        // The ally leaves before attack damage, so the identity immediately
        // becomes the target and the attack is undefended. The second node in
        // this same ability proves that the transition is not deferred until
        // the later damage-calculation step.
        var (world, villain) = Board();
        world.CreateCard("01087", world.Seats[0].Deck);
        var ally = world.CreateCard(
            "01076", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        world.Attack = new EnemyAttack(
            villain.ObjectId, 0, ally.ObjectId, Defender: ally.ObjectId);
        world.Activation = new EnemyActivation(villain.ObjectId, 0, Attacking: true);
        var runner = Runner(
            "01076",
            """
            { "seq": [
              { "discard": "this" },
              { "if": {
                "test": { "undefendedAttack": "you" },
                "then": { "giveStatus": { "card": "you", "status": "tough" } }
              } }
            ] }
            """,
            eventName: "WhenBoostCardGiven");
        var occurrence = Occurrence.ForAttack(
            1, ["WhenBoostCardGiven"], world, Cards,
            villain.ObjectId, ally.ObjectId, 0);

        var pending = Assert.Single(
            runner.Waiting(world, occurrence, WindowKind.Interrupt));
        runner.Resolve(world, occurrence, pending, [], []);

        Assert.Equal(DeckType.DiscardPile, ally.Area.Type);
        Assert.False(world.Attack!.IsDefended);
        Assert.Equal(world.Seats[0].IdentityCard.ObjectId, world.Attack.Target);
        Assert.True(Statuses.Has(world, world.Seats[0].IdentityCard, Statuses.Tough));
    }

    private static void ResolveInitiationInterrupt(
        World world, AbilityRunner runner, Card villain)
    {
        world.Abilities = runner;
        world.Agenda.Add(new PhaseStep(
            Steps.Attack, Round: 1, Number: 2, Index: 0,
            Subject: villain.ObjectId, Seat: 0));
        var events = new List<Marvel.Rules.Events.GameEvent>();

        var asked = Sequence.Work(world, Cards, runner, events);
        Assert.NotNull(asked);
        Assert.Equal(Marvel.Rules.Prompts.Question.Opportunity, asked.Asking);
        Sequence.Answer(
            world, Cards, runner, asked,
            Decision.Take(Assert.Single(asked.Affordances).Id), events);

        // Finish the initiation window and apply the attack root. Step 2 must
        // not ask for another defender after the card already declared one.
        Assert.Null(Sequence.Work(world, Cards, runner, events));
    }

    private static AbilityRunner Runner(
        string card, string effect, string eventName = "WhenAttackInitiated") => new(
        AbilityCatalog.Parse(
            $$"""
            { "cards": [ { "card": "{{card}}", "abilities": [ {
              "trigger": {
                "event": "{{eventName}}",
                "timing": "Interrupt",
                "subject": "game"
              },
              "effect": {{effect}}
            } ] } ] }
            """));

    private static (World World, Card Villain) Board()
    {
        var world = new World(Cards, players: 1);
        var seat = world.CreateSeat("p0");
        seat.IdentityCard = world.CreateCard("01001a", seat.Hero);
        var villain = world.CreateCard("01094", world.AreaOf(DeckType.VillainArea));
        world.CreateCard("01104", world.AreaOf(DeckType.EncounterDeck));
        world.CreateCard("01105", world.AreaOf(DeckType.EncounterDeck));
        return (world, villain);
    }
}
