using System.Text.Json;
using Marvel.Cards.Run;
using Marvel.Content.Setup;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.Setup;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.View.Tests;

public sealed class DecisionQueryPurityTests
{
    [Fact]
    public void RepeatedProjectionAndAssessmentPreserveRandomnessSignalsAndFutureDecisions()
    {
        // Engineering contract exercised on a legal Core opening. A renderer may
        // inspect a decision repeatedly; only an accepted answer changes the game.
        var setup = SetupCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("setup", "setup.json")));
        var runner = new AbilityRunner(PersistentFixture.Abilities);
        (Game game, World world) = Opening(setup, runner);
        (Game control, World untouched) = Opening(setup, runner);
        Assert.Equal(Snapshot(control, untouched), Snapshot(game, world));
        Assert.Equal(new Resolution(untouched, control.Pending, []).Information,
            new Resolution(world, game.Pending, []).Information);

        for (int boundary = 0; boundary < 2; boundary++)
        {
            string before = Snapshot(game, world);
            string expectedOffers = JsonSerializer.Serialize(runner.Actions(world, 0));
            for (int repeat = 0; repeat < 4; repeat++)
            {
                foreach (ViewScope scope in new[] { ViewScope.None, new RestrictedVisibilityPolicy(0).Authorize(null, 1) })
                {
                    VisibleResult visible = WorldProjection.For(world, game.Pending, [], scope);
                    _ = BoardPresentation.From(visible.World);
                    if (visible.Prompt is { } prompt)
                    {
                        _ = PromptPresentation.From(prompt, visible.World);
                        foreach (Affordance offer in prompt.Affordances)
                            _ = DecisionAssessment.Assess(prompt, Decision.Take(offer.Id), actor: prompt.Player);
                    }
                }
                Assert.Equal(expectedOffers, JsonSerializer.Serialize(runner.Actions(world, 0)));
                Assert.Equal(before, Snapshot(game, world));
                Assert.Empty(new Resolution(world, game.Pending, []).Information);
            }

            Decision answer = Answer(game.Pending!, boundary);
            Decision controlAnswer = Answer(control.Pending!, boundary);
            Resolution actual = game.Resolve(answer);
            Resolution expected = control.Resolve(controlAnswer);
            Assert.Equal(expected.Events.Select(EventJson.Write), actual.Events.Select(EventJson.Write));
            Assert.Equal(expected.Information, actual.Information);
            Assert.Equal(Snapshot(control, untouched), Snapshot(game, world));
        }
        Assert.Equal(GamePhase.PlayerTurn, game.Phase);
        Assert.Equal("01001a", world.Seats[0].IdentityCard.FaceId);
    }

    private static Decision Answer(Prompt prompt, int boundary)
    {
        if (boundary == 0)
        {
            Assert.Equal(PublicDecisionKind.OpeningHand, prompt.PublicKind);
            Affordance mulligan = Assert.Single(prompt.Affordances);
            return new Decision(mulligan.Id, [mulligan.Targets!.Legal[0]]);
        }
        Assert.Equal(Question.TurnOption, prompt.Asking);
        return Decision.Take(Assert.Single(prompt.Affordances, offer => offer.Verb == Game.ChangeForm).Id);
    }

    private static (Game, World) Opening(SetupCatalog setup, AbilityRunner runner)
    {
        World world = WorldSetup.Deal(PersistentFixture.Cards,
            Blueprints.From(Dealer.DealOrder(setup, "rhino", ["spider_man"], facts: PersistentFixture.Cards), PersistentFixture.Cards),
            ["Spider-Man"], 7, runner);
        return (Game.Begin(world, PersistentFixture.Cards, runner), world);
    }

    private static string Snapshot(Game game, World world) => JsonSerializer.Serialize(new
    {
        game.Pending, game.Phase, game.Round,
        Digest = world.Digest().Canonical(),
        Random = world.Random.Generator.GetState(), world.Random.Generator.WordsConsumed,
        world.Agenda.Stage,
        Agenda = world.Agenda.Outstanding.Select(step => new
        {
            step.What, step.Round, step.Number, step.Index, step.Subject, step.Seat,
            step.Plan, step.ActivationId, step.OccurrenceId,
        }),
    });
}
