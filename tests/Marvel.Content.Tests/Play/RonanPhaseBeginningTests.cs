using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>Test-local 90001 phase body on the real scheduler; no modular admission.</summary>
public sealed class RonanPhaseBeginningTests : CoreCardTestBase
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [Rule("rr:interrupt.3")]
    [Rule("rr:engage.3")]
    public void RonanMovesBeforeStepOneAndBeforeTheFirstActivation(int firstPlayer)
    {
        // 90001: "When the villain phase begins ... engages the hero with the
        // fewest remaining hit points." Interrupts resolve before their condition.
        var world = Hero("01001a,01001b", 2);
        world.FirstPlayer = firstPlayer;
        world.CreateCard("01094", world.AreaOf(DeckType.VillainArea));
        var scheme = world.CreateCard("01097b", world.AreaOf(DeckType.MainSchemesArea));
        for (int i = 0; i < 12; i++) world.CreateCard("01120", world.AreaOf(DeckType.EncounterDeck));
        var ronan = world.CreateCard("90001", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        world.Seats[1].IdentityCard.TakeDamage(3);
        ronan.TakeDamage(2);
        var state = ronan.InstanceState;
        int incarnation = ronan.Incarnation;
        var runner = Runner();
        var events = new List<GameEvent>();
        VillainPhase.Schedule(world.Agenda, 1);
        var defense = Assert.IsType<Prompt>(Sequence.Work(world, Cards, runner, events));
        Assert.Equal(Question.Defender, defense.Asking);
        Assert.Equal(PlayArea.Of(1), ronan.Area.PlayArea);
        Assert.Same(state, ronan.InstanceState);
        Assert.Equal(incarnation, ronan.Incarnation);
        Assert.Equal(2, ronan.Damage);
        int moved = events.FindIndex(e => e is CardsMoved cards && cards.Cards.Any(c => c.Card == ronan.ObjectId));
        int threat = events.FindIndex(e => e is FieldSet { Field: "k_threat" } field && field.Card == scheme.ObjectId);
        Assert.True(moved >= 0);
        Assert.True(threat > moved);
        Assert.True(scheme.Tokens["k_threat"] > 0);
        Assert.Single(events.OfType<PhaseStarted>());
        for (int count = 0; world.Agenda.Current!.Value.Subject != ronan.ObjectId; count++)
        {
            Assert.True(count < 4);
            Assert.Equal(Question.Defender, defense.Asking);
            Sequence.Answer(world, Cards, runner, defense, Decision.Decline, events);
            defense = Assert.IsType<Prompt>(Sequence.Work(world, Cards, runner, events));
        }
        Assert.Equal(1, world.Activation!.Player);
        Assert.Equal(Question.Defender, defense.Asking);

    }

    private static AbilityRunner Runner() => new(AbilityCatalog.Parse("""
        {"cards":[{"card":"90001","abilities":[{
          "trigger":{"event":"WhenVillainPhaseBegins","timing":"ForcedInterrupt","subject":"game"},
          "effect": {"chooseCard":{"chooser":"firstPlayer",
          "from":{"minBy":{"of":{"query":"heroes"},"by":"remainingHealth"}},
          "effect":{"eachPlayer":{"order":"playerOrder","effect":{"if":{
            "test":{"and":[{"isYourIdentity":"chosen"},{"isInPlay":"chosen"},
              {"inForm":{"player":"you","form":"hero"}}]},
            "then":{"putIntoPlay":{"card":"this","where":"engagedWithYou"}}
          }}}}
        }}
        }]}]}
        """));
}
