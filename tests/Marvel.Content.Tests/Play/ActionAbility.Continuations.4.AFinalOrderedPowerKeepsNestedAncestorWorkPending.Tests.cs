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
        var forged = new PhaseStep(Steps.ResumeAbility, 1, 2, Subject: source!.ObjectId, Seat: 0, Tier: AbilityType.Action, AbilityOrdinal: 0, AbilityPath: ["and:0:0,1:"], AbilityFace: source.FaceId, AbilityHasContinuation: true);
        Assert.Throws<RulesNotImplementedException>(() => runner.ResumeAbility(world, forged));
        Assert.True(source.Ready);
        Assert.Equal(held, world.Seats[0].Hand.Cards.Count);
    }

    [Fact]
    public void MalformedStructuralContinuationIsRejectedBeforeRunningAnySibling()
    {
        // The continuation path is engine wire data. A malformed cursor must
        // fail while decoding the compiled tree, before it can select a
        // plausible sibling and mutate the board.
        var runner = Runner(AuthoredCards.AuntMay, "Action", """{ "seq": [ { "exhaust": "this" }, { "draw": { "player": "you", "count": 1 } } ] }""");
        Card? source = null;
        var(_, world) = Playing(board => source = InPlay(board, AuthoredCards.AuntMay), abilities: runner);
        int held = world.Seats[0].Hand.Cards.Count;
        var forged = new PhaseStep(Steps.ResumeAbility, 1, 2, Subject: source!.ObjectId, Seat: 0, Tier: AbilityType.Action, AbilityOrdinal: 0, AbilityPath: ["seq:not-an-index"], AbilityFace: source.FaceId, AbilityHasContinuation: true);
        Assert.Throws<RulesNotImplementedException>(() => runner.ResumeAbility(world, forged));
        Assert.True(source.Ready);
        Assert.Equal(held, world.Seats[0].Hand.Cards.Count);
    }

    [Fact]
    public void LegacyContinuationWithoutSourceProvenanceRaisesInsteadOfSkipping()
    {
        // Continuation metadata from before card-incarnation bindings cannot
        // prove that `this` still means the same copy. That ambiguity raises;
        // it must not silently turn the remaining discard into a no-op.
        var runner = Runner(AuthoredCards.AuntMay, "Action", """{ "seq": [ { "draw": { "player": "you", "count": 1 } }, { "discard": "this" } ] }""");
        Card? source = null;
        var(_, world) = Playing(board => source = InPlay(board, AuthoredCards.AuntMay), abilities: runner);
        var legacy = new PhaseStep(Steps.ResumeAbility, 1, 2, Subject: source!.ObjectId, Seat: 0, Tier: AbilityType.Action, AbilityOrdinal: 0, AbilityPath: ["seq:0"], AbilityFace: source.FaceId, AbilityHasContinuation: false);
        var thrown = Assert.Throws<RulesNotImplementedException>(() => runner.ResumeAbility(world, legacy));
        Assert.Contains("source-card provenance", thrown.Message, StringComparison.Ordinal);
        Assert.Equal(DeckType.SupportsArea, source.Area.Type);
    }

    [Fact]
    public void ADirectLastingEffectCannotBeginOutsideItsPeriod()
    {
        var runner = Runner(AuthoredCards.AuntMay, "WhenRevealed", """{ "grantUntil": { "card": "this", "trait": "AERIAL", "until": "EndOfAttack" } }""", eventName: Steps.CardRevealed);
        Card? source = null;
        var(_, world) = Playing(board => source = InPlay(board, AuthoredCards.AuntMay), abilities: runner);
        var thrown = Assert.Throws<RulesNotImplementedException>(() => runner.WhenRevealed(world, source!, 0));
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
        runner.WhenRevealed(world, source!, 0);
        var attack = Assert.Single(world.Agenda.Outstanding, pending => pending.What == Steps.Attack);
        Assert.Equal(held, world.Seats[0].Hand.Cards.Count);
        runner.ActivationCompleted(world, new EnemyActivation(attack.Subject, attack.Seat, Attacking: true, attack.ActivationId, Made: false));
        Assert.Equal(held + 1, world.Seats[0].Hand.Cards.Count);
    }

    [Fact]
    public void AFinalNestedSequenceEndpointDoesNotInventAThreatContinuation()
    {
        var runner = Runner(AuthoredCards.AuntMay, "WhenRevealed", """{ "seq": [ { "seq": [ { "enemyAttacks": { "enemies": { "query": "villain" } } }, { "placeThreat": { "scheme": { "query": "mainScheme" }, "amount": 1 } } ] } ] }""", eventName: Steps.CardRevealed);
        Card? source = null;
        var(_, world) = Playing(board => source = InPlay(board, AuthoredCards.AuntMay), abilities: runner);
        runner.WhenRevealed(world, source!, 0);
        var attack = Assert.Single(world.Agenda.Outstanding, pending => pending.What == Steps.Attack);
        runner.ActivationCompleted(world, new EnemyActivation(attack.Subject, attack.Seat, Attacking: true, attack.ActivationId, Made: true));
        var placement = Assert.Single(world.Agenda.Outstanding, pending => pending.What == Steps.PlaceThreatEffect);
        Assert.Equal(world.TheCardIn(DeckType.MainSchemesArea)!.ObjectId, placement.Placement!.Scheme);
        Assert.Equal(1, placement.Placement.Assigned);
    }

    [Fact]
    public void LegacyNonFinalEachPlayerFrameDoesNotClaimTheOuterSuffix()
    {
        var runner = Runner(AuthoredCards.AuntMay, "WhenRevealed", """{ "seq": [ { "eachPlayer": { "effect": { "placeThreat": { "scheme": { "query": "mainScheme" }, "amount": 1 } } } }, { "draw": { "player": "you", "count": 1 } } ] }""", eventName: Steps.CardRevealed);
        Card? source = null;
        var(_, world) = Playing(board => source = InPlay(board, AuthoredCards.AuntMay), abilities: runner);
        int held = world.Seats[0].Hand.Cards.Count;
        runner.ResolveEachPlayer(world, source!, player: 0, stoppedAt: 1, AbilityType.WhenRevealed, finalStep: false, finalPlayer: false);
        Assert.Single(world.Agenda.Outstanding, pending => pending.What == Steps.PlaceThreatEffect);
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
