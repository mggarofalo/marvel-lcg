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
public sealed class ActionAbilityRepeatedTraceFirstPlayerRebindActivatesVillainGrantTests
{
    [Rule("rr:ability.step.1")]
    [Rule("rr:player-elimination.5")]
    [Rule("rr:villain-defeat.4")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void FirstPlayerRebindActivatesVillainGrantBeforeAdvancement()
    {
        // Eliminating alter-ego Spider-Man passes the first-player token to
        // Captain Marvel in hero form. The first-player hero condition then
        // activates and its villain health grant must retarget to Klaw II.
        var runner = FirstPlayerVillainGrantRunner();
        Card? source = null;
        Card? conditional = null;
        Card? villain = null;
        World? world = null;
        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(board =>
        {
            world = board;
            board.Seats[1].IdentityCard.TurnTo("01010a");
            board.Seats[0].IdentityCard.TakeDamage(9);
            source = InPlay(board, AuthoredCards.AuntMay);
            conditional = board.CreateCard("01092", board.AreaOf(DeckType.SupportsArea, PlayArea.Of(1), cardOwner: 1));
            villain = board.TheCardIn(DeckType.VillainArea)!;
        }, heroes: ["spider_man", "captain_marvel"], abilities: runner, scenario: "klaw"));
        Assert.Contains("retargeting constant", thrown.Message);
        Assert.True(source!.Ready);
        Assert.Equal(PlayArea.Of(1), conditional!.Area.PlayArea);
        Assert.Equal("01113", villain!.FaceId);
        Assert.Equal(0, villain.Damage);
        Assert.Equal(9, world!.Seats[0].IdentityCard.Damage);
    }

    [Rule("rr:ability.step.1")]
    [Rule("rr:player-elimination.5")]
    [Rule("rr:villain-defeat.4")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void FirstPlayerRebindEndsVillainGrantBeforeAdvancement()
    {
        // Eliminating hero Spider-Man passes the first-player token to Carol
        // Danvers in alter-ego form. The hero-only villain health grant ends
        // before Klaw II enters and therefore does not retarget.
        var runner = FirstPlayerVillainGrantRunner();
        Card? source = null;
        Card? conditional = null;
        var(game, world) = Playing(board =>
        {
            board.Seats[0].IdentityCard.TakeDamage(9);
            source = InPlay(board, AuthoredCards.AuntMay);
            conditional = board.CreateCard("01092", board.AreaOf(DeckType.SupportsArea, PlayArea.Of(1), cardOwner: 1));
        }, hero: true, heroes: ["spider_man", "captain_marvel"], abilities: runner, scenario: "klaw");
        Assert.Contains(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        Assert.True(source!.Ready);
        Assert.Equal(PlayArea.Of(1), conditional!.Area.PlayArea);
        Assert.Equal(9, world.Seats[0].IdentityCard.Damage);
    }

    [Rule("rr:player-elimination.5")]
    [Rule("rr:modifiers.1")]
    [Rule("rr:villain-defeat.4")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void FirstPlayerRebindEndsConditionalHealthGrantBeforePayment()
    {
        // Hero Spider-Man initially makes the first-player condition grant
        // Carol +1 health. His elimination rebinds first player to alter-ego
        // Carol, ending that grant and activating the villain-health branch.
        var runner = FirstPlayerConditionalHealthDependencyRunner();
        Card? source = null;
        Card? villain = null;
        World? world = null;
        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(board =>
        {
            world = board;
            board.Seats[0].IdentityCard.TakeDamage(9);
            source = InPlay(board, AuthoredCards.AuntMay);
            board.CreateCard("01091", board.AreaOf(DeckType.SupportsArea, PlayArea.Of(1), cardOwner: 1));
            board.CreateCard("01092", board.AreaOf(DeckType.SupportsArea, PlayArea.Of(1), cardOwner: 1));
            villain = board.TheCardIn(DeckType.VillainArea)!;
        }, hero: true, heroes: ["spider_man", "captain_marvel"], abilities: runner, scenario: "klaw"));
        Assert.Contains("retargeting constant", thrown.Message);
        Assert.True(source!.Ready);
        Assert.Equal(0, villain!.Damage);
        Assert.Equal(9, world!.Seats[0].IdentityCard.Damage);
    }

    [Rule("rr:ability.step.1")]
    [Rule("rr:each-player.1")]
    [Rule("rr:player-elimination.5")]
    [Rule("rr:villain-defeat.4")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void EachPlayerOrderingChecksEveryVillainGrantPath()
    {
        // The first player chooses the each-player order. Resolving Spider-Man
        // first eliminates him and ends the hero-first-player grant; resolving
        // Carol Danvers first leaves Spider-Man and the grant active when Klaw
        // advances. Eligibility must include that legal ordering.
        var runner = OrderedFirstPlayerVillainGrantRunner();
        Card? source = null;
        Card? villain = null;
        World? world = null;
        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(board =>
        {
            world = board;
            board.Seats[0].IdentityCard.TakeDamage(9);
            source = InPlay(board, AuthoredCards.AuntMay);
            board.CreateCard("01092", board.AreaOf(DeckType.SupportsArea, PlayArea.Of(1), cardOwner: 1));
            villain = board.TheCardIn(DeckType.VillainArea)!;
        }, hero: true, heroes: ["spider_man", "captain_marvel"], abilities: runner, scenario: "klaw"));
        Assert.Contains("retargeting constant", thrown.Message);
        Assert.True(source!.Ready);
        Assert.Equal("01113", villain!.FaceId);
        Assert.Equal(0, villain.Damage);
        Assert.Equal(9, world!.Seats[0].IdentityCard.Damage);
    }

    [Rule("rr:ability.step.1")]
    [Rule("rr:villain-defeat.3")]
    [Rule("rr:villain-defeat.4")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void RepeatedAdvanceDiscardsOldVillainConstantAttachment()
    {
        // Rhino's hosted attachment leaves when different-title Ultron III
        // enters play. Its continuous villain health grant is therefore gone
        // before the repeated continuation reads the new stage.
        var runner = DepartingVillainAttachmentRunner();
        Card? source = null;
        Card? attachment = null;
        var(game, _) = Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            var villain = board.TheCardIn(DeckType.VillainArea)!;
            attachment = board.CreateCard(AuthoredCards.Charge, board.AreaOf(DeckType.UpgradesArea, villain.Area.PlayArea, villain.ObjectId));
            board.CreateCard("01136", board.AreaOf(DeckType.VillainDeck));
        }, hero: true, heroes: ["spider_man", "captain_marvel"], abilities: runner);
        Assert.Contains(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        Assert.True(source!.Ready);
        Assert.NotNull(attachment);
    }

    [Rule("rr:ability.step.1")]
    [Rule("rr:villain-defeat.4")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void EnteredCardActivatingVillainGrantRaisesBeforePowerMutates()
    {
        // Putting Hydra Mercenary into play makes the conditional constant
        // active before Klaw I is defeated. Its +10 hit points follows Klaw II,
        // so the trace must not test that branch against the unchanged discard.
        var runner = ConditionalVillainGrantRunner(repeated: false);
        Card? source = null;
        Card? conditional = null;
        Card? mercenary = null;
        Card? villain = null;
        World? world = null;
        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(board =>
        {
            world = board;
            source = InPlay(board, AuthoredCards.AuntMay);
            conditional = board.CreateCard("01092", board.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
            mercenary = board.CreateCard("01101", board.AreaOf(DeckType.EncounterDiscardPile));
            villain = board.TheCardIn(DeckType.VillainArea)!;
            board.Seats[0].IdentityCard.TakeDamage(9);
        }, hero: true, heroes: ["spider_man", "captain_marvel"], abilities: runner, scenario: "klaw"));
        Assert.Contains("retargeting constant", thrown.Message);
        Assert.True(source!.Ready);
        Assert.Equal(DeckType.SupportsArea, conditional!.Area.Type);
        Assert.Equal(DeckType.EncounterDiscardPile, mercenary!.Area.Type);
        Assert.Equal("01113", villain!.FaceId);
        Assert.Equal(0, villain.Damage);
        Assert.Equal(9, world!.Seats[0].IdentityCard.Damage);
    }

    [Rule("rr:ability.step.1")]
    [Rule("rr:villain-defeat.4")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void FinalVillainAdvanceNeedNotModelEnteringConstants()
    {
        // Ultron III's constants become active when it enters play, but this
        // labelled effect ends at that point. No continuation reads them, so
        // the action is legal and can be advertised without projecting them.
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
            { "attack": {
              "target": { "query": "villain" },
              "effect": { "seq": [
                { "dealDamage": {
                  "cards": { "query": "villain" }, "amount": 100
                } }
              ] }
            } }
            """, cost: """{ "exhaust": "this" }""", includeAuthored: true);
        Card? source = null;
        var(game, _) = Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            board.CreateCard("01136", board.AreaOf(DeckType.VillainDeck));
        }, hero: true, abilities: runner);
        Assert.Contains(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

    [Rule("rr:ability.step.1")]
    [Rule("rr:villain-defeat.4")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void FinalVillainAdvanceAfterAnotherStepNeedNotModelEnteringConstants()
    {
        // The same boundary holds when advancement is the last of several
        // effects: only a later sibling would observe the entering constant.
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
            { "attack": {
              "target": { "query": "villain" },
              "effect": { "seq": [
                { "heal": { "card": "you", "amount": 1 } },
                { "dealDamage": {
                  "cards": { "query": "villain" }, "amount": 100
                } }
              ] }
            } }
            """, cost: """{ "exhaust": "this" }""", includeAuthored: true);
        Card? source = null;
        var(game, _) = Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            board.CreateCard("01136", board.AreaOf(DeckType.VillainDeck));
            board.Seats[0].IdentityCard.TakeDamage(1);
        }, hero: true, abilities: runner);
        Assert.Contains(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }
}
