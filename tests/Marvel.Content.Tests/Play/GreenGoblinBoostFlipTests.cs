using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class GreenGoblinBoostFlipTests : CoreCardTestBase
{
    [Rule("rr:attack-enemy-activation.step.3.b")]
    [Rule("rr:attack-enemy-activation.step.4")]
    [Fact]
    public void PayoffFlipsGoblinDuringBoostAndTheAttackUsesNormansZeroAtk()
    {
        // ruling:a3c80b14b14ac3a0: After the last madness counter is removed
        // by a Boost ability, Norman's 0 ATK plus the boost icons deals damage.
        // The attack has passed his "would attack" interrupt window.
        // 02011: "Place 1 infamy counter on Criminal Enterprise. If you cannot,
        // remove 1 madness counter from State of Madness."
        var runner = Marvel.Content.Tests.Cards.AuthoredCards.Runner();
        var world = Hero("01001a");
        world.Abilities = runner;
        var hero = world.Seats[0].IdentityCard;
        var villain = world.CreateCard("02001a,02001b", world.AreaOf(DeckType.VillainArea));
        villain.TurnTo("02001b");
        var environment = world.CreateCard("02006a,02006b", world.AreaOf(DeckType.EnvironmentArea));
        environment.TurnTo("02006b");
        environment.PlaceTokens("c_madness", 1);
        world.CreateCard("02004b", world.AreaOf(DeckType.MainSchemesArea));
        var encounterDeck = world.AreaOf(DeckType.EncounterDeck);
        var reserve = world.CreateCard("01186", encounterDeck);
        var boost = world.CreateCard("02011", encounterDeck);
        World.MoveToTop(boost, encounterDeck);
        var events = new List<GameEvent>();
        world.Agenda.Add(new PhaseStep(new EnemyAttackOperation(), 1, 2,
            Subject: villain.ObjectId, Seat: 0));

        var first = Sequence.Work(world, Cards, runner, events);
        Assert.True(first is not null,
            $"No defender prompt: face={villain.FaceId}, damage={hero.Damage}, "
            + $"boostArea={boost.Area.Type}, attack={world.Attack}, result={world.Result}, "
            + $"events={string.Join(',', events.Select(e => e.GetType().Name))}");
        var defender = Assert.IsType<Prompt>(first);
        Assert.Equal(Question.Defender, defender.Asking);
        Assert.Same(boost, Assert.Single(world.AreaOf(DeckType.BoostCardsDeck,
            villain.Area.PlayArea, host: villain.ObjectId).Cards));
        Assert.Equal("02001b", villain.FaceId);
        Sequence.Answer(world, Cards, runner, defender, Decision.Decline, events);
        Sequence.Finish(world, Cards, runner, events);

        Assert.Same(villain, world.TheCardIn(DeckType.VillainArea));
        Assert.Same(environment, world.TheCardIn(DeckType.EnvironmentArea));
        Assert.Equal("02001a", villain.FaceId);
        Assert.Equal("02006a", environment.FaceId);
        Assert.Equal(0, environment.Tokens.GetValueOrDefault("c_madness"));
        Assert.Equal(2, environment.Tokens.GetValueOrDefault("c_infamy"));
        Assert.Equal(1, hero.Damage);
        Assert.Equal(1, Assert.Single(events.OfType<AttackCompleted>()).DamageDealt);
        Assert.Same(boost, Assert.Single(world.AreaOf(DeckType.EncounterDiscardPile).Cards));
        Assert.Same(reserve, Assert.Single(encounterDeck.Cards));
        Assert.Empty(world.AreaOf(DeckType.BoostCardsDeck,
            villain.Area.PlayArea, host: villain.ObjectId).Cards);
        Assert.Null(world.Attack);
        Assert.Null(world.Activation);
        Assert.False(world.Agenda.IsBusy);
    }
}
