using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
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
public sealed class ActionAbilityPowerTraceUnrelatedTraitChangeTests
{
    [Rule("rr:ability.step.1")]
    [Rule("rr:villain-defeat.4")]
    [Rule("rr:labeled-ability.4")]
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void UnrelatedTraitChangeDoesNotActivateSupportGrant(bool repeated)
    {
        // Giving an identity Aerial does not give this support Brute. Its
        // conditional villain health grant stays inactive in direct and
        // repeated traces, so an unrelated trait cannot block the action.
        var runner = UnrelatedTraitVillainGrantRunner(repeated);
        Card? source = null;
        var(game, _) = Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            board.CreateCard("01092", board.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        }, hero: true, heroes: ["spider_man", "captain_marvel"], abilities: runner, scenario: "klaw");
        Assert.Contains(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        Assert.True(source!.Ready);
    }

    [Rule("rr:ability.step.1")]
    [Rule("rr:villain-defeat.4")]
    [Rule("rr:labeled-ability.4")]
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AerialGainDoesNotInvalidateBrutePredicate(bool repeated)
    {
        // Spider-Man gains Aerial, but his Brute predicate remains false.
        // Trait invalidation is keyed by both card and trait, so its inactive
        // villain grant cannot hide either shape of the legal action.
        var runner = UnrelatedTraitVillainGrantRunner(repeated, sameCardDifferentTrait: true);
        Card? source = null;
        var(game, _) = Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            board.CreateCard("01092", board.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        }, hero: true, heroes: ["spider_man", "captain_marvel"], abilities: runner, scenario: "klaw");
        Assert.Contains(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        Assert.True(source!.Ready);
    }

    [Rule("rr:attach-to.1")]
    [Rule("rr:villain-defeat.3.2")]
    [Rule("rr:labeled-ability.4")]
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void LeavingHostedCardInvalidatesItsTraitPredicate(bool repeated)
    {
        // A different-title villain stage discards the old stage's hosted
        // attachment. Enhanced Ivory Horn therefore stops being an in-play
        // Weapon and activates the new-stage grant before either traced cost.
        var runner = DiscardedTraitVillainGrantRunner(repeated);
        Card? source = null;
        Card? horn = null;
        Card? villain = null;
        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            villain = board.TheCardIn(DeckType.VillainArea)!;
            horn = board.CreateCard("01100", board.AreaOf(DeckType.UpgradesArea, villain.Area.PlayArea, villain.ObjectId));
            board.CreateCard("01136", board.AreaOf(DeckType.VillainDeck));
            board.CreateCard("01092", board.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        }, hero: true, heroes: ["spider_man", "captain_marvel"], abilities: runner));
        Assert.Contains("retargeting constant", thrown.Message);
        Assert.True(source!.Ready);
        Assert.Equal(0, villain!.Damage);
        Assert.Equal(villain.ObjectId, horn!.Area.Host);
    }

    [Rule("rr:attach-to.1")]
    [Rule("rr:villain-defeat.3.2")]
    [Rule("rr:labeled-ability.4")]
    [Theory]
    [InlineData(false, "kind")]
    [InlineData(true, "kind")]
    [InlineData(false, "title")]
    [InlineData(true, "title")]
    public void LeavingHostedCardInvalidatesItsIdentityPredicate(bool repeated, string predicate)
    {
        // Once Rocket Boots leaves with the old villain stage, it is neither
        // an in-play upgrade nor the in-play card of that title.
        // Both exact predicates therefore activate the new-stage grant.
        var runner = DiscardedTraitVillainGrantRunner(repeated, predicate);
        Card? source = null;
        Card? boots = null;
        Card? villain = null;
        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            villain = board.TheCardIn(DeckType.VillainArea)!;
            boots = board.CreateCard("01039", board.AreaOf(DeckType.UpgradesArea, villain.Area.PlayArea, villain.ObjectId));
            board.CreateCard("01136", board.AreaOf(DeckType.VillainDeck));
            board.CreateCard("01092", board.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        }, hero: true, heroes: ["spider_man", "captain_marvel"], abilities: runner));
        Assert.Contains("retargeting constant", thrown.Message);
        Assert.True(source!.Ready);
        Assert.Equal(0, villain!.Damage);
        Assert.Equal(villain.ObjectId, boots!.Area.Host);
    }

    [Rule("rr:ability.step.1")]
    [Rule("rr:status-cards.1")]
    [Rule("rr:villain-defeat.4")]
    [Rule("rr:labeled-ability.4")]
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CappedStatusGrantDoesNotChangeStatusPredicate(bool repeated)
    {
        // A character cannot receive a second status of the same type. Giving
        // Spider-Man Stunned while he already carries it is a no-op, so the
        // inverse predicate and its villain grant remain inactive.
        var runner = UnrelatedStatusVillainGrantRunner(repeated, sameCardDifferentStatus: true, giveStunned: true, grantWhenStatusAbsent: true);
        Card? source = null;
        var(game, _) = Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            board.CreateCard("01092", board.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
            Statuses.Give(board, board.Seats[0].IdentityCard, Statuses.Stunned);
        }, hero: true, heroes: ["spider_man", "captain_marvel"], abilities: runner, scenario: "klaw");
        Assert.Contains(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        Assert.True(source!.Ready);
    }

    [Rule("rr:status-cards.1")]
    [Rule("rr:villain-defeat.4")]
    [Rule("rr:labeled-ability.4")]
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CappedToughGrantDoesNotChangeStatusPredicate(bool repeated)
    {
        // Spider-Man already has Tough, so another grant is capped and leaves
        // the inverse predicate false. Neither direct nor repeated preflight
        // may explore its inactive villain-health branch.
        var runner = CappedToughVillainGrantRunner(repeated);
        Card? source = null;
        var(game, _) = Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            Statuses.Give(board, board.Seats[0].IdentityCard, Statuses.Tough);
            board.CreateCard("01092", board.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        }, hero: true, heroes: ["spider_man", "captain_marvel"], abilities: runner, scenario: "klaw");
        Assert.Contains(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        Assert.True(source!.Ready);
    }

    [Rule("rr:damage.2")]
    [Rule("rr:villain-defeat.4")]
    [Rule("rr:labeled-ability.4")]
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void VillainDamageDoesNotInvalidateHeroDamagePredicate(bool repeated)
    {
        // Damage on the villain does not put damage on Spider-Man. His numeric
        // predicate remains false, so the inactive villain-health grant cannot
        // reject either otherwise legal trace shape.
        var runner = UnrelatedDamageVillainGrantRunner(repeated);
        Card? source = null;
        var(game, _) = Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            board.CreateCard("01092", board.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        }, hero: true, heroes: ["spider_man", "captain_marvel"], abilities: runner, scenario: "klaw");
        Assert.Contains(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        Assert.True(source!.Ready);
    }

    [Rule("rr:discard.1")]
    [Rule("rr:villain-defeat.4")]
    [Rule("rr:labeled-ability.4")]
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void MinionDepartureDoesNotInvalidateAllyCount(bool repeated)
    {
        // Discarding an engaged minion does not change the number of allies
        // this player controls. The ally-count condition remains false, so its
        // inactive villain grant cannot reject the legal action.
        var runner = UnrelatedMinionCountVillainGrantRunner(repeated);
        Card? source = null;
        var(game, _) = Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            board.CreateCard("01167", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            board.CreateCard("01092", board.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        }, hero: true, heroes: ["spider_man", "captain_marvel"], abilities: runner, scenario: "klaw");
        Assert.Contains(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        Assert.True(source!.Ready);
    }

    [Rule("rr:play-put-into-play")]
    [Rule("rr:engage.1")]
    [Rule("rr:engage.2")]
    [Rule("rr:villain-defeat.4")]
    [Rule("rr:labeled-ability.4")]
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void EnteredMinionInvalidatesEngagementCount(bool repeated)
    {
        // A minion an ability instructs a player to engage "is also considered
        // to have engaged that player." Putting Hydra Mercenary into play this
        // way changes the engaged-minion count from zero to one, which must be
        // recognized before paying the cost.
        var runner = EnteredEngagementCountVillainGrantRunner(repeated);
        Card? source = null;
        Card? mercenary = null;
        Card? villain = null;
        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            mercenary = board.CreateCard("01101", board.AreaOf(DeckType.EncounterDiscardPile));
            board.CreateCard("01092", board.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
            villain = board.TheCardIn(DeckType.VillainArea)!;
        }, hero: true, heroes: ["spider_man", "captain_marvel"], abilities: runner, scenario: "klaw"));
        Assert.Contains("retargeting constant", thrown.Message);
        Assert.True(source!.Ready);
        Assert.Equal(DeckType.EncounterDiscardPile, mercenary!.Area.Type);
        Assert.Equal(0, villain!.Damage);
    }
}
