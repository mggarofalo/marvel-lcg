using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content.Setup;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;
public sealed class TargetReferenceAValidatedCrisisExceptionSurvivesAChoiceContinuaTests : TargetReferenceTestBase
{
    [Rule("rr:cannot.3")]
    [Rule("rr:target.3.3")]
    [Fact]
    public void AValidatedCrisisExceptionSurvivesAChoiceContinuation()
    {
        // The target decision belongs to initiation. A preceding choice
        // suspends and reconstructs the cast, so its continuation metadata
        // carries the validated power address rather than rechecking Crisis
        // after the chosen option has already changed the board.
        var runner = Runner("01006", """
            { "seq": [
              { "choose": { "options": [
                { "draw": { "player": "you", "count": 1 } },
                { "draw": { "player": "you", "count": 2 } }
              ] } },
              { "thwart": {
                "target": { "query": "mainScheme" },
                "effect": { "removeThreat": {
                  "scheme": { "query": "mainScheme" },
                  "amount": 1,
                  "ignoresCrisis": "true"
                } }
              } }
            ] }
            """);
        Card? source = null;
        var(game, world) = Playing(board =>
        {
            source = InPlay(board, "01006", DeckType.SupportsArea);
            var crisis = board.CreateCard("01108", board.AreaOf(DeckType.SideSchemesArea));
            crisis.PlaceTokens("k_threat", 1);
            board.TheCardIn(DeckType.MainSchemesArea)!.PlaceTokens("k_threat", 2);
        }, runner);
        var main = world.TheCardIn(DeckType.MainSchemesArea)!;
        int held = world.Seats[0].Hand.Cards.Count;
        ResolveAction(game, source!);
        Assert.Equal(Question.Option, game.Pending!.Asking);
        game.Resolve(Decision.Take(game.Pending.Affordances[0].Id));
        Assert.Equal(1, main.Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(held + 1, world.Seats[0].Hand.Cards.Count);
    }

    [Rule("rr:cannot.3")]
    [Rule("rr:crisis-icon.1")]
    [Fact]
    public void StructurallyIdenticalThwartsKeepSeparateCrisisEvidence()
    {
        // Both authored powers carry the same printed exception to Crisis.
        // The second wrapper must retain evidence for its exact compiled
        // location rather than losing it through structural record equality.
        const string thwart = """
            { "thwart": {
              "target": { "query": "mainScheme" },
              "effect": { "removeThreat": {
                "scheme": { "query": "mainScheme" },
                "amount": 1,
                "ignoresCrisis": "true"
              } }
            } }
            """;
        var runner = Runner("01006", $$"""
            { "seq": [ {{thwart}}, {{thwart}} ] }
            """);
        Card? source = null;
        var(game, world) = Playing(board =>
        {
            source = InPlay(board, "01006", DeckType.SupportsArea);
            var crisis = board.CreateCard("01108", board.AreaOf(DeckType.SideSchemesArea));
            crisis.PlaceTokens("k_threat", 1);
            board.TheCardIn(DeckType.MainSchemesArea)!.PlaceTokens("k_threat", 3);
        }, runner);
        var main = world.TheCardIn(DeckType.MainSchemesArea)!;
        ResolveAction(game, source!);
        Assert.Equal(1, main.Tokens.GetValueOrDefault("k_threat"));
    }

    [Rule("rr:cannot.3")]
    [Rule("rr:target.3.3")]
    [Fact]
    public void AValidatedCrisisExceptionSurvivesAnActivationContinuation()
    {
        // Enemy activation suspends this sequence and resumes it in a fresh
        // cast. The continuation carries the previously validated thwart
        // address, so completing the activation cannot expose a late Crisis
        // refusal before the following power is scheduled.
        var runner = Runner("01006", """
            { "seq": [
              { "enemyAttacks": { "enemies": { "query": "villain" } } },
              { "thwart": {
                "target": { "query": "mainScheme" },
                "effect": { "removeThreat": {
                  "scheme": { "query": "mainScheme" },
                  "amount": 1,
                  "ignoresCrisis": "true"
                } }
              } }
            ] }
            """);
        Card? source = null;
        var(_, world) = Playing(board =>
        {
            board.Seats[0].IdentityCard.TurnTo("01001a");
            source = InPlay(board, "01006", DeckType.SupportsArea);
            var crisis = board.CreateCard("01108", board.AreaOf(DeckType.SideSchemesArea));
            crisis.PlaceTokens("k_threat", 1);
            board.TheCardIn(DeckType.MainSchemesArea)!.PlaceTokens("k_threat", 2);
        }, runner);
        var action = Assert.Single(runner.Actions(world, 0), pending => pending.Card == source!.ObjectId);
        runner.Act(world, action, [], []);
        var activation = Assert.Single(world.Agenda.Outstanding, step => step.What == Steps.Attack);
        runner.ActivationCompleted(world, new EnemyActivation(activation.Subject, activation.Seat, Attacking: true, activation.ActivationId, Made: false));
        Assert.NotNull(world.CharacterThwart);
        Assert.Equal(world.TheCardIn(DeckType.MainSchemesArea)!.ObjectId, world.CharacterThwart!.Scheme);
    }

    [Rule("rr:target.3.3")]
    [Rule("rr:target.3.9")]
    [Fact]
    public void AReachableBranchWithAnIllegalThwartTargetRefusesBeforeMutation()
    {
        // The form change can select either branch after initiation. Only the
        // currently active branch ignores Crisis, so the ordinary alternate
        // branch makes the whole sequence unsafe to begin. Refusal occurs
        // while the identity is still in hero form.
        var runner = Runner("01006", """
            { "seq": [
              { "changeForm": { "player": "you", "to": "alter-ego" } },
              { "if": {
                "test": { "inForm": { "player": "you", "form": "hero" } },
                "then": { "thwart": {
                  "target": { "query": "mainScheme" },
                  "effect": { "removeThreat": {
                    "scheme": { "query": "mainScheme" },
                    "amount": 1,
                    "ignoresCrisis": "true"
                  } }
                } },
                "else": { "thwart": {
                  "target": { "query": "mainScheme" },
                  "effect": { "removeThreat": {
                    "scheme": { "query": "mainScheme" }, "amount": 1
                  } }
                } }
              } }
            ] }
            """);
        Card? source = null;
        var(game, world) = Playing(board =>
        {
            board.Seats[0].IdentityCard.TurnTo("01001a");
            source = InPlay(board, "01006", DeckType.SupportsArea);
            var crisis = board.CreateCard("01108", board.AreaOf(DeckType.SideSchemesArea));
            crisis.PlaceTokens("k_threat", 1);
            board.TheCardIn(DeckType.MainSchemesArea)!.PlaceTokens("k_threat", 2);
        }, runner);
        Assert.DoesNotContain(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        Assert.Equal("01001a", world.Seats[0].IdentityCard.FaceId);
    }

    [Rule("rr:cannot.3")]
    [Rule("rr:target.3.3")]
    [Fact]
    public void AStepThatCannotChangeFormDoesNotOpenTheOtherFormBranch()
    {
        // Drawing changes the board but cannot change identity form. The
        // alter-ego branch is therefore unreachable, so its ordinary thwart
        // target under Crisis cannot hide the valid hero branch.
        var runner = Runner("01006", """
            { "seq": [
              { "draw": { "player": "you", "count": 1 } },
              { "if": {
                "test": { "inForm": { "player": "you", "form": "hero" } },
                "then": { "thwart": {
                  "target": { "query": "mainScheme" },
                  "effect": { "removeThreat": {
                    "scheme": { "query": "mainScheme" },
                    "amount": 1,
                    "ignoresCrisis": "true"
                  } }
                } },
                "else": { "thwart": {
                  "target": { "query": "mainScheme" },
                  "effect": { "removeThreat": {
                    "scheme": { "query": "mainScheme" }, "amount": 1
                  } }
                } }
              } }
            ] }
            """);
        Card? source = null;
        var(game, world) = Playing(board =>
        {
            board.Seats[0].IdentityCard.TurnTo("01001a");
            source = InPlay(board, "01006", DeckType.SupportsArea);
            var crisis = board.CreateCard("01108", board.AreaOf(DeckType.SideSchemesArea));
            crisis.PlaceTokens("k_threat", 1);
            board.TheCardIn(DeckType.MainSchemesArea)!.PlaceTokens("k_threat", 2);
        }, runner);
        ResolveAction(game, source!);
        Assert.Equal("01001a", world.Seats[0].IdentityCard.FaceId);
        Assert.Equal(1, world.TheCardIn(DeckType.MainSchemesArea)!.Tokens.GetValueOrDefault("k_threat"));
    }

    [Rule("rr:target.3.3")]
    [Rule("rr:target.3.9")]
    [Fact]
    public void AChoiceWrappedFormChangeOpensTheReachableFormBranch()
    {
        // Either option may be selected after initiation. Because one can
        // change to alter-ego form, the later ordinary thwart branch is
        // reachable and its Crisis-prohibited target must be refused before
        // the choice can suspend or mutate the identity.
        var runner = Runner("01006", """
            { "seq": [
              { "choose": { "options": [
                { "changeForm": { "player": "you", "to": "alter-ego" } },
                { "seq": [] }
              ] } },
              { "if": {
                "test": { "inForm": { "player": "you", "form": "hero" } },
                "then": { "thwart": {
                  "target": { "query": "mainScheme" },
                  "effect": { "removeThreat": {
                    "scheme": { "query": "mainScheme" },
                    "amount": 1,
                    "ignoresCrisis": "true"
                  } }
                } },
                "else": { "thwart": {
                  "target": { "query": "mainScheme" },
                  "effect": { "removeThreat": {
                    "scheme": { "query": "mainScheme" }, "amount": 1
                  } }
                } }
              } }
            ] }
            """);
        Card? source = null;
        var(game, world) = Playing(board =>
        {
            board.Seats[0].IdentityCard.TurnTo("01001a");
            source = InPlay(board, "01006", DeckType.SupportsArea);
            var crisis = board.CreateCard("01108", board.AreaOf(DeckType.SideSchemesArea));
            crisis.PlaceTokens("k_threat", 1);
            board.TheCardIn(DeckType.MainSchemesArea)!.PlaceTokens("k_threat", 2);
        }, runner);
        Assert.DoesNotContain(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        Assert.Equal("01001a", world.Seats[0].IdentityCard.FaceId);
    }

    [Rule("rr:cannot.3")]
    [Rule("rr:target.3.3")]
    [Fact]
    public void AnInactiveFormChangeDoesNotOpenTheLaterFormBranch()
    {
        // The first form predicate is stable and selects its draw branch. Its
        // inactive change-form branch cannot make the later alter-ego branch
        // reachable, so only the Crisis-ignoring hero thwart is required.
        var runner = Runner("01006", """
            { "seq": [
              { "if": {
                "test": { "inForm": { "player": "you", "form": "hero" } },
                "then": { "draw": { "player": "you", "count": 1 } },
                "else": { "changeForm": {
                  "player": "you", "to": "hero"
                } }
              } },
              { "if": {
                "test": { "inForm": { "player": "you", "form": "hero" } },
                "then": { "thwart": {
                  "target": { "query": "mainScheme" },
                  "effect": { "removeThreat": {
                    "scheme": { "query": "mainScheme" },
                    "amount": 1,
                    "ignoresCrisis": "true"
                  } }
                } },
                "else": { "thwart": {
                  "target": { "query": "mainScheme" },
                  "effect": { "removeThreat": {
                    "scheme": { "query": "mainScheme" }, "amount": 1
                  } }
                } }
              } }
            ] }
            """);
        Card? source = null;
        var(game, world) = Playing(board =>
        {
            board.Seats[0].IdentityCard.TurnTo("01001a");
            source = InPlay(board, "01006", DeckType.SupportsArea);
            var crisis = board.CreateCard("01108", board.AreaOf(DeckType.SideSchemesArea));
            crisis.PlaceTokens("k_threat", 1);
            board.TheCardIn(DeckType.MainSchemesArea)!.PlaceTokens("k_threat", 2);
        }, runner);
        ResolveAction(game, source!);
        Assert.Equal("01001a", world.Seats[0].IdentityCard.FaceId);
        Assert.Equal(1, world.TheCardIn(DeckType.MainSchemesArea)!.Tokens.GetValueOrDefault("k_threat"));
    }
}
