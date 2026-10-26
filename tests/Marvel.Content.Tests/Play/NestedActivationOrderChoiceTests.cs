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
    public void FirstPlayerOrdersAttackEndResponseActivationsAfterActiveStateClears()
    {
        // "If multiple activations are initiated this way, the first player
        // decides the order in which they are resolved." The enclosing attack
        // has ended, but its response window remains inside its activation.
        var (world, groups, _) = WreckingEncounterCardFixture.Create(
            "07024", 1, players: 2);
        var runner = new AbilityRunner(AbilityCatalog.Parse("""
            {"cards":[{"card":"07017","abilities":[{
              "trigger":{"event":"WhenAttackEnds","timing":"ForcedResponse",
                "actor":"this","player":"trigger.player"},
              "effect":{"enemyAttacks":{"enemies":{"query":"minionsEngagedWithYou"},
                "against":"you"}}
            }]}]}
            """));
        world.Abilities = runner;
        var engaged = world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1));
        var hydra = world.CreateCard("01101", engaged);
        var sandman = world.CreateCard("01102", engaged);
        world.Agenda.Add(new PhaseStep(new EnemyAttackOperation(), 1, 1,
            Subject: groups[1].Villain.ObjectId, Seat: 1));

        var prompt = AdvanceToOrder(world, runner);
        AssertEndResponseOrder(world, runner, prompt,
            groups[1].Villain, hydra, sandman);
    }

    private static void AssertEndResponseOrder(World world, AbilityRunner runner,
        Prompt prompt, Card villain, Card hydra, Card sandman)
    {
        Assert.Null(world.Activation);
        Assert.True(world.Agenda.Current?.AttackOrigin?.ActivationId >= 0);
        var ending = Assert.Single(world.Agenda.Outstanding,
            step => step.What == Steps.EndAttack);
        Assert.Equal(world.Agenda.Current?.AttackOrigin?.ActivationId,
            ending.ActivationId);
        Assert.Equal(0, world.FirstPlayer);
        Assert.Equal(0, prompt.Player);
        var offer = Assert.Single(prompt.Affordances);
        Assert.Equal([hydra.ObjectId, sandman.ObjectId], offer.Targets!.Legal);
        var reversed = Decision.Take(offer.Id, [sandman.ObjectId, hydra.ObjectId], []);
        Assert.Equal(DecisionRejection.UnauthorizedActor,
            DecisionAssessment.Assess(prompt, reversed, actor: 1).Rejection);
        Assert.True(DecisionAssessment.Assess(prompt, reversed, actor: 0).IsAccepted);
        Sequence.Answer(world, world.Facts, runner, prompt, reversed, []);
        var outstanding = world.Agenda.Outstanding.ToList();
        int outerEnd = outstanding.IndexOf(ending);
        Assert.Equal(villain.ObjectId, ending.Subject);
        int firstMinion = outstanding.FindIndex(step => step.What == Steps.Attack
            && step.Subject == sandman.ObjectId);
        Assert.True(outerEnd >= 0 && firstMinion > outerEnd);
        Assert.Equal([sandman.ObjectId, hydra.ObjectId], outstanding
            .Where(step => step.What == Steps.Attack
                && (step.Subject == sandman.ObjectId || step.Subject == hydra.ObjectId))
            .Select(step => step.Subject));
    }

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

        var prompt = AdvanceToOrder(world, runner);
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

    private static Prompt AdvanceToOrder(World world, AbilityRunner runner)
    {
        for (int answered = 0; answered < 20; answered++)
        {
            var prompt = Sequence.Work(world, world.Facts, runner, []);
            Assert.NotNull(prompt);
            if (prompt.Asking == Question.Order) return prompt;
            Sequence.Answer(world, world.Facts, runner, prompt, Decision.Decline, []);
        }

        throw new Xunit.Sdk.XunitException("No activation-order choice was reached");
    }
}
