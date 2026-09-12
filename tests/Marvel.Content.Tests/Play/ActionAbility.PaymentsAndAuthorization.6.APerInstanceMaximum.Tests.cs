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
public sealed class ActionAbilityPaymentsAndAuthorizationAPerInstanceMaximumTests
{
    [Rule("rr:max-maximum.5")]
    [Fact]
    public void APerInstanceMaximumIsSharedAcrossCopiesForOneOccurrence()
    {
        var runner = Runner(AuthoredCards.AuntMay, "Response", """{ "draw": { "player": "you", "count": 1 } }""", eventName: "WhenDamageWouldBeDealt", maximum: "Instance");
        Card? first = null;
        Card? second = null;
        var(_, world) = Playing(board =>
        {
            first = InPlay(board, AuthoredCards.AuntMay);
            second = InPlay(board, AuthoredCards.AuntMay);
        }, abilities: runner);
        var occurrence = new Occurrence(91, ["WhenDamageWouldBeDealt"], Player: 0, Target: world.Seats[0].IdentityCard.ObjectId);
        var offered = runner.Waiting(world, occurrence, WindowKind.Response);
        Assert.Equal(2, offered.Count);
        runner.Resolve(world, occurrence, offered.Single(pending => pending.Card == first!.ObjectId), [], []);
        Assert.Empty(runner.Waiting(world, occurrence, WindowKind.Response));
        Assert.Equal(2, runner.Waiting(world, new Occurrence(92, ["WhenDamageWouldBeDealt"], Player: 0, Target: world.Seats[0].IdentityCard.ObjectId), WindowKind.Response).Count);
        Assert.NotNull(second);
    }

    [Rule("rr:max-maximum.6")]
    [Fact]
    public void AMaximumWithinAnAbilityCapsOnlyThatResolution()
    {
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
            { "dealDamage": {
              "cards": { "query": "villain" },
              "amount": { "min": [ 20, 10 ] }
            } }
            """);
        Card? source = null;
        var(_, world) = Playing(board => source = InPlay(board, AuthoredCards.AuntMay), abilities: runner);
        var action = Assert.Single(runner.Actions(world, 0), pending => pending.Card == source!.ObjectId);
        runner.Act(world, action, [], []);
        Assert.Equal(10, world.TheCardIn(DeckType.VillainArea)!.Damage);
    }

    [Rule("rr:labeled-ability.5")]
    [Rule("rr:labeled-ability.6")]
    [Rule("rr:labeled-ability.6.2")]
    [Fact]
    public void MultiLabeledAbilityCancelsOnceAfterCostsAndBeforeAnyEffect()
    {
        // Crosscounter's attack/defense/thwart labels are one ability. A stun
        // or confusion cancels the whole post-arrow effect, removes every
        // matching status, and leaves the already-paid exhaustion cost paid.
        var runner = Runner("01017", "Action", """{ "draw": { "player": "you", "count": 1 } }""", cost: """{ "exhaust": "this" }""", limit: 1, labels: "[ \"attack\", \"defense\", \"thwart\" ]");
        Card? source = null;
        var(game, world) = Playing(board =>
        {
            source = board.CreateCard("01017", board.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), board.Seats[0].IdentityCard.ObjectId, cardOwner: 0));
            Statuses.Give(board, board.Seats[0].IdentityCard, Statuses.Stunned);
            Statuses.Give(board, board.Seats[0].IdentityCard, Statuses.Confused);
        }, hero: true, abilities: runner);
        int held = world.Seats[0].Hand.Cards.Count;
        var action = Assert.Single(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        game.Resolve(Decision.Take(action.Id));
        Assert.False(source!.Ready);
        Assert.Equal(held, world.Seats[0].Hand.Cards.Count);
        Assert.False(Statuses.Has(world, world.Seats[0].IdentityCard, Statuses.Stunned));
        Assert.False(Statuses.Has(world, world.Seats[0].IdentityCard, Statuses.Confused));
    }

    [Rule("rr:labeled-ability.2")]
    [Rule("rr:labeled-ability.6")]
    [Rule("rr:retaliate-x.1")]
    [Fact]
    public void LabeledPowerDoesNotBeginAgainDuringItsEffect()
    {
        // A labeled ability is canceled "when the player initiates" it. The
        // stun gained after initiation therefore remains in play and cannot
        // retroactively cancel the attack child of the already-running ability.
        var runner = Runner("01017", "Action", """
            { "attack": {
              "target": { "query": "villain" },
              "effect": { "seq": [
                { "giveStatus": { "card": "you", "status": "stunned" } },
                { "dealAttackDamage": {
                  "cards": { "query": "villain" }, "amount": 1
                } }
              ] }
            } }
            """, labels: "[ \"attack\" ]");
        Card? source = null;
        var(game, world) = Playing(board =>
        {
            source = board.CreateCard("01017", board.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), board.Seats[0].IdentityCard.ObjectId, cardOwner: 0));
            var villain = board.TheCardIn(DeckType.VillainArea)!;
            board.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect, Kind: "retaliate", Amount: 1, Card: villain.ObjectId, Affects: villain.ObjectId));
        }, hero: true, abilities: runner);
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        var action = Assert.Single(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        game.Resolve(Decision.Take(action.Id));
        Assert.True(Statuses.Has(world, world.Seats[0].IdentityCard, Statuses.Stunned));
        Assert.Equal(1, villain.Damage);
        Assert.Equal(1, world.Seats[0].IdentityCard.Damage);
    }

    [Rule("rr:labeled-ability.1")]
    [Rule("rr:upgrade.4")]
    [Rule("rr:piercing.1")]
    [Fact]
    public void LabeledPerformerSurvivesAChoiceContinuation()
    {
        // An upgrade attached to "another friendly character" attributes its
        // labeled ability to that character. The chosen ally remains the
        // performer after the prompt, so its Piercing discards each Tough card.
        var runner = Runner("01017", "Action", """
            { "chooseCard": {
              "from": { "query": "attackableEnemies" },
              "effect": { "attack": {
                "target": "chosen",
                "effect": { "dealAttackDamage": {
                  "cards": "chosen", "amount": 1
                } }
              } }
            } }
            """, labels: "[ \"attack\" ]");
        Card? source = null;
        Card? ally = null;
        var(game, world) = Playing(board =>
        {
            ally = board.CreateCard(AuthoredCards.BlackCat, board.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
            source = board.CreateCard("01017", board.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), ally.ObjectId, cardOwner: 0));
            board.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect, Kind: Keywords.Piercing, Card: ally.ObjectId, Affects: ally.ObjectId));
        }, hero: true, abilities: runner);
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        Statuses.Give(world, villain, Statuses.Tough);
        var action = Assert.Single(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        game.Resolve(Decision.Take(action.Id));
        game.Resolve(Decision.Take(villain.ObjectId));
        Assert.False(Statuses.Has(world, villain, Statuses.Tough));
        Assert.Equal(1, villain.Damage);
    }

    [Rule("rr:labeled-ability.2")]
    [Fact]
    public void AttackEnvelopeWithoutAPowerLifecycleFailsBeforeCosts()
    {
        // The whole labeled ability "is considered to be an attack". A raw
        // damage effect has no saveable attack occurrence for interrupts,
        // responses, or Retaliate, so this unsupported shape is refused before
        // its exhaust cost instead of resolving as plausible non-attack damage.
        var runner = Runner("01017", "Action", """{ "dealAttackDamage": { "cards": { "query": "villain" }, "amount": 1 } }""", cost: """{ "exhaust": "this" }""", labels: "[ \"attack\" ]");
        Card? source = null;
        var(_, world) = Playing(board => source = board.CreateCard("01017", board.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), board.Seats[0].IdentityCard.ObjectId, cardOwner: 0)), hero: true);
        var forged = new PendingAbility(source!.ObjectId, AbilityType.Action, 0);
        var thrown = Assert.Throws<RulesNotImplementedException>(() => runner.Act(world, forged, [], []));
        Assert.Contains("saveable attack power", thrown.Message, StringComparison.Ordinal);
        Assert.True(source.Ready);
        Assert.Equal(0, world.TheCardIn(DeckType.VillainArea)!.Damage);
    }

    [Rule("rr:labeled-ability.2")]
    [Fact]
    public void AutomaticAttackEnvelopeCannotBypassLifecyclePreflight()
    {
        // Automatic entry points use the same envelope gate as Actions. A When
        // Revealed ability with raw attack damage therefore raises before the
        // damage instead of bypassing the attack occurrence and Retaliate.
        var runner = Runner("01017", "WhenRevealed", """{ "dealAttackDamage": { "cards": { "query": "villain" }, "amount": 1 } }""", eventName: Steps.CardRevealed, labels: "[ \"attack\" ]");
        Card? source = null;
        var(_, world) = Playing(board => source = board.CreateCard("01017", board.AreaOf(DeckType.RevealingArea)), hero: true);
        var thrown = Assert.Throws<RulesNotImplementedException>(() => runner.WhenRevealed(world, source!, 0));
        Assert.Contains("saveable attack power", thrown.Message, StringComparison.Ordinal);
        Assert.Equal(0, world.TheCardIn(DeckType.VillainArea)!.Damage);
    }

    [Rule("rr:labeled-ability.2")]
    [Fact]
    public void EveryAttackEnvelopeBranchMustEnterTheLifecycle()
    {
        // An inactive branch cannot lend its attack node to the active branch.
        // Here the hero-form path only draws, so the envelope would not be an
        // attack on that path and is rejected before the exhaust cost.
        var runner = Runner("01017", "Action", """
            { "if": {
              "test": { "inForm": { "player": "you", "form": "hero" } },
              "then": { "draw": { "player": "you", "count": 1 } },
              "else": { "attack": {
                "target": { "query": "villain" },
                "effect": { "dealAttackDamage": {
                  "cards": { "query": "villain" }, "amount": 1
                } }
              } }
            } }
            """, cost: """{ "exhaust": "this" }""", labels: "[ \"attack\" ]");
        Card? source = null;
        var(_, world) = Playing(board => source = board.CreateCard("01017", board.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), board.Seats[0].IdentityCard.ObjectId, cardOwner: 0)), hero: true);
        int held = world.Seats[0].Hand.Cards.Count;
        var forged = new PendingAbility(source!.ObjectId, AbilityType.Action, 0);
        Assert.Throws<RulesNotImplementedException>(() => runner.Act(world, forged, [], []));
        Assert.True(source.Ready);
        Assert.Equal(held, world.Seats[0].Hand.Cards.Count);
    }
}
