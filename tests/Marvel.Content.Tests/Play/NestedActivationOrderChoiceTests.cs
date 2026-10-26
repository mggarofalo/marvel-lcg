using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class NestedActivationOrderChoiceTests
{
    [Rule("rr:activation.8.1")]
    [Fact]
    public void FirstPlayerOrdersMultipleActivationsInitiatedInsideAnotherActivation()
    {
        // "If multiple activations are initiated this way, the first player
        // decides the order in which they are resolved." The boost is revealed
        // during an attack on seat one; both requested minions attack that seat.
        var (world, groups, boost) = WreckingEncounterCardFixture.Create(
            "07024", 1, players: 2, boostFace: "07037");
        var runner = new AbilityRunner(AbilityCatalog.Parse("""
            {"cards":[{"card":"07024","abilities":[{
              "trigger":{"event":"WhenCardRevealed","timing":"Boost","subject":"this"},
              "effect":{"enemyAttacks":{"enemies":{"query":"minions"},"against":"you"}}
            }]}]}
            """));
        world.Abilities = runner;
        var engaged = world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1));
        var hydra = world.CreateCard("01101", engaged);
        var sandman = world.CreateCard("01102", engaged);
        World.MoveToTop(boost, groups[1].Deck);
        world.Agenda.Add(new PhaseStep(new EnemyAttackOperation(), 1, 1,
            Subject: groups[1].Villain.ObjectId, Seat: 1));

        Prompt? prompt = null;
        for (int answered = 0; answered < 20; answered++)
        {
            prompt = Sequence.Work(world, world.Facts, runner, []);
            Assert.NotNull(prompt);
            if (prompt.Asking == Question.Order) break;
            Sequence.Answer(world, world.Facts, runner, prompt, Decision.Decline, []);
        }

        Assert.NotNull(prompt);
        Assert.Equal(Question.Order, prompt.Asking);
        Assert.NotNull(world.Activation);
        Assert.Equal(0, world.FirstPlayer);
        Assert.Equal(0, prompt.Player);
        var offer = Assert.Single(prompt.Affordances);
        Assert.Equal(0, offer.AnchorPlayer);
        Assert.Equal([hydra.ObjectId, sandman.ObjectId], offer.Targets!.Legal);
        var reversed = Decision.Take(offer.Id, [sandman.ObjectId, hydra.ObjectId], []);
        Assert.Equal(DecisionRejection.UnauthorizedActor,
            DecisionAssessment.Assess(prompt, reversed, actor: 1).Rejection);
        Assert.True(DecisionAssessment.Assess(prompt, reversed, actor: 0).IsAccepted);

        Sequence.Answer(world, world.Facts, runner, prompt, reversed, []);
        Assert.Equal([sandman.ObjectId, hydra.ObjectId], world.Agenda.Outstanding
            .Where(step => step.What == Steps.Attack
                && (step.Subject == sandman.ObjectId || step.Subject == hydra.ObjectId))
            .Select(step => step.Subject));
    }
}
