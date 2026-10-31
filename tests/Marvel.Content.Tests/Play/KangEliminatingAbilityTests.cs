using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

// Actual-card nonpublic board; Kang's public admission stays closed.
public sealed class KangEliminatingAbilityTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:player-elimination.5")]
    public void AnAbilityThatEliminatesTheLastLocalPlayerFinishesBeforeItsStageCompletes(bool expert)
    {
        // 11031: "When Chitauri Soldier attacks you, discard the top card of the encounter deck
        // → take indirect damage equal to the number of boost icons on that card."
        // rr:player-elimination.5: "If a player is eliminated partway through the resolution of
        // an ability, resolve the entire ability"; .5.1 ends the attack. 11009b-11012b: "If all
        // the players at this stage are defeated, this stage is complete[d]."
        var events = new List<GameEvent>();
        var world = KangStageFlowFixture.Split(2, expert, events);
        var hero = world.Seats[0].IdentityCard;
        hero.TurnTo("01001a");
        hero.TakeDamage(9);
        var scheme = KangStageFlowFixture.LocalScheme(world, 0);
        var soldier = world.CreateCard("11031", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        var top = world.CreateCard("11032", world.AreaOf(DeckType.EncounterDeck));
        World.MoveToTop(top, world.AreaOf(DeckType.EncounterDeck));
        world.Agenda.Add(new PhaseStep(new EnemyAttackOperation(), 1, 2, Index: 0, Subject: soldier.ObjectId, Seat: 0));

        var prompt = Sequence.Work(world, world.Facts, world.Abilities, events);
        for (int step = 0; prompt is not null; step++)
        {
            Assert.True(step < 40, prompt.Description);
            var decision = prompt.Cancellable ? Decision.Decline : Decision.Take(prompt.Affordances[0].Id);
            Sequence.Answer(world, world.Facts, world.Abilities, prompt, decision, events);
            prompt = Sequence.Work(world, world.Facts, world.Abilities, events);
        }

        Assert.True(world.Seats[0].Eliminated);
        Assert.Equal(DeckType.EncounterDiscardPile, top.Area.Type);
        Assert.Equal(1, scheme.Tokens.GetValueOrDefault("is_completed"));
        var host = Assert.Single(world.Cards, card => card.FaceId == "11013a");
        Assert.Single(BeneathCards.Of(world, host));
        Assert.Single(world.Agenda.KangTransitions.Pending);
        Assert.Equal(Outcome.Unfinished, world.Result);
        Assert.False(world.Agenda.IsBusy);
    }
}
