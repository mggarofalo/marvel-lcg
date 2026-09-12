using Marvel.Content.Behavior;
using Marvel.Content.Setup;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Behavior;
public sealed class CanonicalCoreSceneSameSeedAndArrangementsProduceTheSameDigestTests : CanonicalCoreSceneTestBase
{
    [Fact]
    public void SameSeedAndArrangementsProduceTheSameDigest()
    {
        var first = OneCardDeck();
        var second = OneCardDeck();
        Assert.Equal(first.World.Digest().Canonical(), second.World.Digest().Canonical());
        Assert.Equal(first.World.Digest().Fingerprint(), second.World.Digest().Fingerprint());
    }

    [Fact]
    public void APublishedCustomizationBlockCanBuildAnotherHerosLegalDeck()
    {
        // The source name is this engine's compact recipe. DeckConstruction
        // remains the authority-backed gate for the resulting 40-card deck.
        var scene = CanonicalCoreScene.Deal(new CoreSceneRequest("behavior:card:01082:after-your-hero-defends-discard-indomitable-ready", "rhino", ["spider_man"], 302, PlayerDecks: ["black_panther"]), Setup, Cards, AuthoredCards.Runner());
        Assert.Equal("01001b", scene.World.Seats[0].IdentityCard.FaceId);
        Assert.Equal(0, scene.Find(new SceneCard("01082")).Owner);
        Assert.Equal(40, scene.World.Seats[0].Deck.Cards.Count + scene.World.Seats[0].Hand.Cards.Count);
    }

    [Fact]
    public void OneCardBoundaryAccountsForEveryOtherDeckCardInALegalZone()
    {
        var scene = OneCardDeck();
        Seat player = scene.World.Seats[0];
        Assert.Single(player.Deck.Cards);
        Assert.Equal("01006", player.Deck.Cards[^1].FaceId);
        Assert.Equal(33, scene.World.AreaOf(DeckType.DiscardPile, PlayArea.Of(0), cardOwner: 0).Cards.Count);
        Assert.Equal(6, player.Hand.Cards.Count);
        Assert.Equal(40, player.Deck.Cards.Count + player.Hand.Cards.Count + scene.World.AreaOf(DeckType.DiscardPile, PlayArea.Of(0), cardOwner: 0).Cards.Count);
        Assert.Equal(scene.World.Cards.Count, scene.World.Areas.Sum(area => area.Cards.Count + area.Removed.Count));
    }

    [Fact]
    public void OneCardBoundaryCanKeepTheDiscardPileEmpty()
    {
        var scene = Deal("behavior:rr:player-deck.4:published-result", "rhino", ["spider_man"]).Apply(new StackPlayerDeck(0, [new SceneCard("01006")], PlayerDeckRemainder.Hand));
        Assert.Single(scene.World.Seats[0].Deck.Cards);
        Assert.Equal(39, scene.World.Seats[0].Hand.Cards.Count);
        Assert.Empty(scene.World.AreaOf(DeckType.DiscardPile, PlayArea.Of(0), cardOwner: 0).Cards);
        Assert.Equal(40, scene.World.Seats[0].Deck.Cards.Count + scene.World.Seats[0].Hand.Cards.Count);
    }

    [Fact]
    public void AnExactHandUsesOnlyOwnedPlayerDeckCardsAndAccountsForTheRemainder()
    {
        var scene = Deal("behavior:rr:hand-size:below-limit", "rhino", ["spider_man"]).Apply(new SetPlayerHand(0, [new SceneCard("01002"), new SceneCard("01003")]));
        Assert.Equal(["01002", "01003"], scene.World.Seats[0].Hand.Cards.Select(card => card.FaceId));
        Assert.Equal(scene.World.Cards.Count, scene.World.Areas.Sum(area => area.Cards.Count + area.Removed.Count));
        var wrongOwner = Assert.Throws<CoreSceneConstructionException>(() => scene.Apply(new SetPlayerHand(0, [new SceneCard("01012")])));
        Assert.Contains("no copy 0", wrongOwner.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void InvasiveAiCanBeStackedOnlyFromTheLegalUltronEncounterDeck()
    {
        var scene = Deal("behavior:card:01149:each-player-discards-top-3-cards-their-one-player", "ultron", ["spider_man"]);
        scene.Apply(new StackEncounterDeck([new SceneCard("01149")]));
        Assert.Equal("01149", scene.World.AreaOf(DeckType.EncounterDeck).Cards[^1].FaceId);
        Assert.Equal(World.Scenario, scene.Find(new SceneCard("01149")).Owner);
        Assert.Equal(DeckType.EnvironmentArea, scene.Find(new SceneCard("01140")).Area.Type);
        Assert.Contains(scene.World.Cards, FacedownDrones.Is);
    }

    [Theory]
    [InlineData(EncounterDeckRemainder.Discard, DeckType.EncounterDiscardPile)]
    [InlineData(EncounterDeckRemainder.Dealt, DeckType.DealtEncounterCardsDeck)]
    public void EncounterDeckBoundaryAccountsForEveryOtherCardInALegalZone(EncounterDeckRemainder remainder, DeckType destination)
    {
        var scene = Deal("behavior:rr:encounter-deck.1:empty-with-discard", "rhino", ["spider_man"]).Apply(new StackEncounterDeck([new SceneCard("01186")], remainder));
        var deck = scene.World.AreaOf(DeckType.EncounterDeck);
        var moved = destination is DeckType.DealtEncounterCardsDeck ? scene.World.AreaOf(destination, PlayArea.Of(0)) : scene.World.AreaOf(destination);
        Assert.Single(deck.Cards);
        Assert.Equal("01186", deck.Cards[^1].FaceId);
        Assert.NotEmpty(moved.Cards);
        Assert.Equal(scene.World.Cards.Count, scene.World.Areas.Sum(area => area.Cards.Count + area.Removed.Count));
    }

    [Fact]
    public void ACompleteAbilityInterpreterIsRequiredForSetup()
    {
        var request = new CoreSceneRequest("behavior:setup:campaign:ultron:scenario-setup", "ultron", ["spider_man"], Seed: 302);
        Assert.Throws<ArgumentNullException>(() => CanonicalCoreScene.Deal(request, Setup, Cards, null!));
    }

    [Fact]
    public void ASignatureCardCannotCrossFromSpiderManToIronMan()
    {
        var scene = Deal("behavior:setup:hero:spider_man:hero-deck", "rhino", ["spider_man", "iron_man"]);
        var operation = new MoveSceneCard(new SceneCard("01006"), new SceneDestination(SceneZone.PlayerHand, Seat: 1));
        var thrown = Assert.Throws<CoreSceneConstructionException>(() => scene.Apply(operation));
        Assert.Equal("move-card", thrown.Operation);
        Assert.Contains("owned by 0, not seat 1", thrown.Message, StringComparison.Ordinal);
        Assert.Contains(scene.Request.Authority, thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AuntMayDoesNotExistInAnIronManDeal()
    {
        var scene = Deal("behavior:setup:hero:iron_man:hero-deck", "rhino", ["iron_man"]);
        var operation = new MoveSceneCard(new SceneCard("01006"), new SceneDestination(SceneZone.PlayerDiscard, Seat: 0));
        var thrown = Assert.Throws<CoreSceneConstructionException>(() => scene.Apply(operation));
        Assert.Contains("no copy 0", thrown.Message, StringComparison.Ordinal);
        Assert.Contains("01006", thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void OnlyRegisteredTokenPoolsCanBeArranged()
    {
        var scene = Deal("behavior:rr:threat.1:scheme-only", "rhino", ["spider_man"]);
        var thrown = Assert.Throws<CoreSceneConstructionException>(() => scene.Apply(new SetSceneCounters(new SceneCard("01094"), "threat", 1)));
        Assert.Equal("set-counters", thrown.Operation);
        Assert.Contains("does not print threat counters", thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void PrintedAllPurposeCounterTypesCanBeArranged()
    {
        var scene = Deal("behavior:card:01018:energy-counter-below-cap", "rhino", ["captain_marvel"]);
        scene.Apply(new MoveSceneCard(new SceneCard("01018"), new SceneDestination(SceneZone.Upgrade, Seat: 0)));
        scene.Apply(new SetSceneCounters(new SceneCard("01018"), "energy", 4));
        Assert.Equal(4, scene.Find(new SceneCard("01018")).Tokens["c_energy"]);
    }

    [Rule("rr:acceleration-token")]
    [Fact]
    public void RulesProvidedAccelerationTokensCanBeArrangedOnTheMainScheme()
    {
        // "They are placed next to the main scheme as a reminder to add X
        // additional threat to the main scheme during step one."
        var scene = Deal("behavior:rr:main-scheme-main-scheme-deck.5:published-result", "klaw", ["spider_man"]);
        scene.Apply(new SetSceneAccelerationTokens(2));
        Card scheme = scene.World.TheCardIn(DeckType.MainSchemesArea)!;
        Assert.Equal(2, scheme.Tokens[EncounterDeck.AccelerationToken]);
        var thrown = Assert.Throws<CoreSceneConstructionException>(() => scene.Apply(new SetSceneAccelerationTokens(-1)));
        Assert.Equal("set-acceleration-tokens", thrown.Operation);
        Assert.Equal(2, scheme.Tokens[EncounterDeck.AccelerationToken]);
    }

    [Fact]
    public void AUsesCardCannotRemainInPlayAtZeroCounters()
    {
        var scene = Deal("behavior:rr:uses-x-type.1:discard-at-zero", "rhino", ["spider_man"]);
        scene.Apply(new MoveSceneCard(new SceneCard("01008"), new SceneDestination(SceneZone.Upgrade, Seat: 0)));
        var thrown = Assert.Throws<CoreSceneConstructionException>(() => scene.Apply(new SetSceneCounters(new SceneCard("01008"), "web", 0)));
        Assert.Contains("would be discarded", thrown.Message, StringComparison.Ordinal);
        Assert.Equal(3, scene.Find(new SceneCard("01008")).Tokens["c_web"]);
    }

    [Fact]
    public void AFixedUsesPoolCannotExceedItsPrintedEntryCount()
    {
        var scene = Deal("behavior:rr:uses-x-type.1:printed-count", "rhino", ["spider_man"]);
        scene.Apply(new MoveSceneCard(new SceneCard("01008"), new SceneDestination(SceneZone.Upgrade, Seat: 0)));
        var thrown = Assert.Throws<CoreSceneConstructionException>(() => scene.Apply(new SetSceneCounters(new SceneCard("01008"), "web", 4)));
        Assert.Contains("printed 3", thrown.Message, StringComparison.Ordinal);
        Assert.Equal(3, scene.Find(new SceneCard("01008")).Tokens["c_web"]);
    }

    [Fact]
    public void APrintedNonUsesEntryPoolCannotExceedItsReachableMaximum()
    {
        var scene = Deal("behavior:card:01066:four-arrow-counters", "rhino", ["captain_marvel"]);
        scene.Apply(new MoveSceneCard(new SceneCard("01066"), new SceneDestination(SceneZone.Ally, Seat: 0)));
        var thrown = Assert.Throws<CoreSceneConstructionException>(() => scene.Apply(new SetSceneCounters(new SceneCard("01066"), "arrow", 5)));
        Assert.Contains("printed 4 arrow", thrown.Message, StringComparison.Ordinal);
        Assert.Equal(4, scene.Find(new SceneCard("01066")).Tokens["c_arrow"]);
    }

    [Fact]
    public void SchemeThreatCannotBypassDefeatOrAdvance()
    {
        var scene = Deal("behavior:rr:side-scheme.2:zero-threat", "klaw", ["spider_man"]);
        Card defenseNetwork = scene.Find(new SceneCard("01125"));
        Card mainScheme = scene.World.TheCardIn(DeckType.MainSchemesArea)!;
        var defeated = Assert.Throws<CoreSceneConstructionException>(() => scene.Apply(new SetSceneCounters(new SceneCard("01125"), "threat", 0)));
        var advanced = Assert.Throws<CoreSceneConstructionException>(() => scene.Apply(new SetSceneCounters(new SceneCard(mainScheme.FaceId), "threat", Cards.PrintedValue(mainScheme.FaceId, "TargetThreat", 1))));
        Assert.Contains("would be defeated", defeated.Message, StringComparison.Ordinal);
        Assert.Contains("would advance", advanced.Message, StringComparison.Ordinal);
        Assert.Equal(3, defenseNetwork.Tokens["k_threat"]);
    }

    [Fact]
    public void FormArrangementUsesOnlyTheSelectedIdentitysPrintedFaces()
    {
        var scene = Deal("behavior:rr:form-change-form.1:published-result", "rhino", ["spider_man"]);
        scene.Apply(new SetSceneForm(0, "01001a"));
        Assert.Equal("01001a", scene.World.Seats[0].IdentityCard.FaceId);
        var thrown = Assert.Throws<CoreSceneConstructionException>(() => scene.Apply(new SetSceneForm(0, "01029a")));
        Assert.Contains("not a printed identity face", thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void PlayerUpgradesCanBeAttachedToAHostWithoutChangingOwnership()
    {
        var scene = Deal("behavior:card:01009:attach-to-an-enemy", "rhino", ["spider_man"]);
        Card rhino = scene.Find(new SceneCard("01094"));
        scene.Apply(new MoveSceneCard(new SceneCard("01009"), new SceneDestination(SceneZone.Upgrade, Seat: 0, Host: rhino.ObjectId)));
        Card webbedUp = scene.Find(new SceneCard("01009"));
        Assert.Equal(0, webbedUp.Owner);
        Assert.Equal(rhino.ObjectId, webbedUp.Area.Host);
        Assert.Equal(rhino.Area.PlayArea, webbedUp.Area.PlayArea);
    }
}
