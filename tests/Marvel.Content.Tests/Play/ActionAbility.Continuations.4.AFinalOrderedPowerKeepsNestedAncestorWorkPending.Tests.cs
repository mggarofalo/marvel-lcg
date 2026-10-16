using Marvel.Cards.Run;
using Marvel.Cards.Dsl;
using Marvel.Content.Setup;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;
public sealed class ActionAbilityContinuationsAFinalOrderedPowerKeepsNestedAncestorWorkPendingTests
{
    [Rule("rr:and.1")]
    [Fact]
    public void AFinalOrderedPowerKeepsNestedAncestorWorkPending()
    {
        var runner = Runner(AuthoredCards.AuntMay, "Action", """{ "if": { "test": { "inForm": { "player": "you", "form": "hero" } }, "then": { "seq": [ { "and": [ { "draw": { "player": "you", "count": 1 } }, { "attack": { "target": { "query": "villain" }, "effect": { "dealAttackDamage": { "cards": { "query": "villain" }, "amount": 1 } } } } ] }, { "draw": { "player": "you", "count": 1 } } ] } } }""");
        Card? source = null;
        var(game, world) = Playing(board => source = InPlay(board, AuthoredCards.AuntMay), hero: true, abilities: runner);
        int held = world.Seats[0].Hand.Cards.Count;
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        var action = Assert.Single(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        game.Resolve(Decision.Take(action.Id));
        var order = Assert.Single(game.Pending!.Affordances);
        game.Resolve(new Decision(order.Id, [0, 1]));
        Assert.Equal(1, villain.Damage);
        Assert.Equal(held + 2, world.Seats[0].Hand.Cards.Count);
    }

    [Fact]
    public void InvalidOrderedContinuationIsRejectedBeforeRunningAnySibling()
    {
        var runner = Runner(AuthoredCards.AuntMay, "Action", """{ "and": [ { "exhaust": "this" }, { "draw": { "player": "you", "count": 1 } } ] }""");
        Card? source = null;
        var(_, world) = Playing(board => source = InPlay(board, AuthoredCards.AuntMay), abilities: runner);
        int held = world.Seats[0].Hand.Cards.Count;
        var forged = AbilityContinuationFixtures.Current(world, source!, new SimultaneousFrame(0, [0, 1], []));
        Assert.Throws<RulesNotImplementedException>(() => runner.ResumeAbility(world, forged));
        Assert.True(source!.Ready);
        Assert.Equal(held, world.Seats[0].Hand.Cards.Count);
    }

    [Fact]
    public void MalformedStructuralContinuationIsRejectedBeforeRunningAnySibling()
    {
        // An invalid typed cursor must fail before a sibling can mutate the board.
        var runner = Runner(AuthoredCards.AuntMay, "Action", """{ "seq": [ { "exhaust": "this" }, { "draw": { "player": "you", "count": 1 } } ] }""");
        Card? source = null;
        var(_, world) = Playing(board => source = InPlay(board, AuthoredCards.AuntMay), abilities: runner);
        int held = world.Seats[0].Hand.Cards.Count;
        var forged = AbilityContinuationFixtures.Current(world, source!, new SequenceFrame(-1, 2));
        Assert.Throws<RulesNotImplementedException>(() => runner.ResumeAbility(world, forged));
        Assert.True(source!.Ready);
        Assert.Equal(held, world.Seats[0].Hand.Cards.Count);
    }

    [Fact]
    public void AnAbsentInterpreterFrameCannotResumeAnAbility()
    {
        // A rules operation cannot invent an interpreter execution frame.
        var runner = Runner(AuthoredCards.AuntMay, "Action", """{ "seq": [ { "draw": { "player": "you", "count": 1 } }, { "discard": "this" } ] }""");
        Card? source = null;
        var(_, world) = Playing(board => source = InPlay(board, AuthoredCards.AuntMay), abilities: runner);
        var absent = new PhaseStep(new AbilityContinuationAgendaOperation(AbilityContinuationStage.Resume), 1, 2, Subject: source!.ObjectId, Seat: 0);
        world.Agenda.Now(absent);
        var thrown = Assert.Throws<InvalidOperationException>(() => runner.ResumeAbility(world, world.Agenda.Current!.Value));
        Assert.Contains("typed ability continuation", thrown.Message, StringComparison.Ordinal);
        Assert.Equal(DeckType.SupportsArea, source.Area.Type);
    }

    [Fact]
    public void ADirectLastingEffectCannotBeginOutsideItsPeriod()
    {
        var runner = Runner(AuthoredCards.AuntMay, "WhenRevealed", """{ "grantUntil": { "card": "this", "trait": "AERIAL", "until": "EndOfAttack" } }""", eventName: Steps.CardRevealed);
        Card? source = null;
        var(_, world) = Playing(board => source = InPlay(board, AuthoredCards.AuntMay), abilities: runner);
        var thrown = Assert.Throws<RulesNotImplementedException>(() => runner.ExecuteReveal(world, source!, 0));
        Assert.Contains("outside its named period", thrown.Message, StringComparison.Ordinal);
        Assert.Empty(world.Effects.Active());
    }

    [Fact]
    public void PaymentCannotSwitchIntoALastingEffectOutsideItsPeriod()
    {
        var runner = Runner(AuthoredCards.AuntMay, "Action", """{ "if": { "test": { "titleInPlay": "Aunt May" }, "then": { "draw": { "player": "you", "count": 1 } }, "else": { "grantUntil": { "card": "you", "trait": "AERIAL", "until": "EndOfAttack" } } } }""", cost: """{ "discard": "this" }""");
        Card? source = null;
        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(board => source = InPlay(board, AuthoredCards.AuntMay), abilities: runner));
        Assert.Contains("outside its named period", thrown.Message, StringComparison.Ordinal);
        Assert.Equal(DeckType.SupportsArea, source!.Area.Type);
    }

    [Fact]
    public void AStableFormBranchIgnoresAnUnreachableLastingConstraint()
    {
        var runner = Runner(AuthoredCards.AuntMay, "Action", """{ "if": { "test": { "inForm": { "player": "you", "form": "hero" } }, "then": { "draw": { "player": "you", "count": 1 } }, "else": { "grantUntil": { "card": "this", "trait": "AERIAL", "until": "EndOfAttack" } } } }""", cost: """{ "exhaust": "this" }""");
        Card? source = null;
        var(game, _) = Playing(board => source = InPlay(board, AuthoredCards.AuntMay), hero: true, abilities: runner);
        Assert.Contains(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

    [Fact]
    public void PaymentCannotSwitchIntoALastingEffectWithNoTarget()
    {
        var runner = Runner(AuthoredCards.AuntMay, "Action", """{ "if": { "test": { "titleInPlay": "Aunt May" }, "then": { "draw": { "player": "you", "count": 1 } }, "else": { "grantUntil": { "card": "attachedTo", "trait": "AERIAL", "until": "EndOfRound" } } } }""", cost: """{ "discard": "this" }""");
        Card? source = null;
        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(board => source = InPlay(board, AuthoredCards.AuntMay), abilities: runner));
        Assert.Contains("no target after payment", thrown.Message, StringComparison.Ordinal);
        Assert.Equal(DeckType.SupportsArea, source!.Area.Type);
    }

    [Fact]
    public void AFirstActivationResumesTheRestOfASequence()
    {
        var runner = Runner(AuthoredCards.AuntMay, "WhenRevealed", """{ "seq": [ { "enemyAttacks": { "enemies": { "query": "villain" }, "first": "true" } }, { "draw": { "player": "you", "count": 1 } } ] }""", eventName: Steps.CardRevealed);
        Card? source = null;
        var(_, world) = Playing(board => source = InPlay(board, AuthoredCards.AuntMay), abilities: runner);
        int held = world.Seats[0].Hand.Cards.Count;
        runner.ExecuteReveal(world, source!, 0);
        var attack = Assert.Single(world.Agenda.Outstanding, pending => pending.What == Steps.Attack);
        Assert.Equal(held, world.Seats[0].Hand.Cards.Count);
        Statuses.Give(world, world.Cards[attack.Subject], Statuses.Stunned);
        Sequence.Work(world, world.Facts, runner, []);
        Assert.Equal(held + 1, world.Seats[0].Hand.Cards.Count);
    }

    [Fact]
    public void AFinalNestedSequenceEndpointDoesNotInventAThreatContinuation()
    {
        var runner = Runner(AuthoredCards.AuntMay, "WhenRevealed", """{ "seq": [ { "seq": [ { "enemyAttacks": { "enemies": { "query": "villain" } } }, { "placeThreat": { "scheme": { "query": "mainScheme" }, "amount": 1 } } ] } ] }""", eventName: Steps.CardRevealed);
        Card? source = null;
        var(_, world) = Playing(board => source = InPlay(board, AuthoredCards.AuntMay), abilities: runner);
        runner.ExecuteReveal(world, source!, 0);
        var attack = Assert.Single(world.Agenda.Outstanding, pending => pending.What == Steps.Attack);
        var scheme = world.TheCardIn(DeckType.MainSchemesArea)!;
        long before = scheme.Tokens["k_threat"];
        Statuses.Give(world, world.Cards[attack.Subject], Statuses.Stunned);
        Sequence.Work(world, world.Facts, runner, []);
        Assert.Equal(before + 1, scheme.Tokens["k_threat"]);
        Assert.DoesNotContain(world.Agenda.Outstanding, pending => pending.What == Steps.PlaceThreatEffect);
    }

    [Fact]
    public void NonFinalEachPlayerFrameDoesNotClaimTheOuterSuffix()
    {
        var runner = Runner(AuthoredCards.AuntMay, "WhenRevealed", """{ "seq": [ { "eachPlayer": { "effect": { "placeThreat": { "scheme": { "query": "mainScheme" }, "amount": 1 } } } }, { "draw": { "player": "you", "count": 1 } } ] }""", eventName: Steps.CardRevealed);
        Card? source = null;
        var(_, world) = Playing(board => source = InPlay(board, AuthoredCards.AuntMay),
            heroes: ["spider_man", "captain_marvel"], abilities: runner);
        int held = world.Seats[0].Hand.Cards.Count;
        runner.ExecuteReveal(world, source!, 0);
        var ordering = Sequence.Work(world, world.Facts, runner, [])!;
        Sequence.Answer(world, world.Facts, runner, ordering,
            new Decision(ordering.Affordances.Single().Id,
                world.Seats.Select(seat => seat.IdentityCard.ObjectId).ToArray()), []);
        var first = world.Agenda.Outstanding.First(step => step.What == Steps.ResolveEachPlayer);
        runner.ResolveEachPlayer(world, first);
        Assert.Contains(world.Agenda.Outstanding, pending => pending.What == Steps.PlaceThreatEffect);
        Assert.Equal(held, world.Seats[0].Hand.Cards.Count);
    }

    [Fact]
    public void AChoiceCannotOfferALastingEffectOutsideItsPeriod()
    {
        var runner = Runner(AuthoredCards.AuntMay, "Action", """{ "choose": { "options": [ { "grantUntil": { "card": "this", "keyword": "attack", "amount": 1, "until": "EndOfAttack" } }, { "draw": { "player": "you", "count": 1 } } ] } }""");
        Card? source = null;
        var(game, _) = Playing(board => source = InPlay(board, AuthoredCards.AuntMay), abilities: runner);
        var action = Assert.Single(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        game.Resolve(Decision.Take(action.Id));
        var option = Assert.Single(game.Pending!.Affordances);
        Assert.Equal(1, option.Id);
    }

    [Rule("rr:initiating-abilities.step.5")]
    [Fact]
    public void AResourceCostInsideASequenceIsAdvertised()
    {
        // Tenacity pays one physical resource and discards itself. Discarding
        // the upgrade is automatic, but the resource is a player choice and
        // therefore has to survive the surrounding cost sequence onto the
        // affordance.
        Card? tenacity = null;
        var(game, _) = Playing(board =>
        {
            tenacity = InPlay(board, "01093");
            board.Seats[0].IdentityCard.Exhaust();
            Physical(board, 1);
        }, hero: true);
        var action = Assert.Single(game.Pending!.Affordances, option => option.Verb == Game.ActionVerb && option.AnchorId == tenacity!.ObjectId);
        var price = Assert.Single(action.CostOptions);
        Assert.Equal("1", price.Cost);
        Assert.Equal(["R"], price.Rule);
    }
}
