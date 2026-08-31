using Marvel.Content.Behavior;
using Marvel.Content.Setup;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Behavior;

/// <summary>Legal, deterministic construction for authority-derived behavioral transcripts.</summary>
public sealed class CanonicalCoreSceneTests
{
    private const string PlayerDeckAuthority =
        "behavior:rr:player-deck.2:published-result";

    private static readonly SetupCatalog Setup = SetupCatalog.Parse(
        File.ReadAllText(RepositoryPaths.Dataset("setup", "setup.json")));

    private static readonly CardCatalog Cards = CardCatalog.Parse(
        File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));

    [Fact]
    public void SameSeedAndArrangementsProduceTheSameDigest()
    {
        var first = OneCardDeck();
        var second = OneCardDeck();

        Assert.Equal(first.World.Digest().Canonical(), second.World.Digest().Canonical());
        Assert.Equal(first.World.Digest().Fingerprint(), second.World.Digest().Fingerprint());
    }

    [Fact]
    public void OneCardBoundaryAccountsForEveryOtherDeckCardInALegalZone()
    {
        var scene = OneCardDeck();
        Seat player = scene.World.Seats[0];

        Assert.Single(player.Deck.Cards);
        Assert.Equal("01006", player.Deck.Cards[^1].FaceId);
        Assert.Equal(33, scene.World.AreaOf(
            DeckType.DiscardPile,
            PlayArea.Of(0),
            cardOwner: 0).Cards.Count);
        Assert.Equal(6, player.Hand.Cards.Count);
        Assert.Equal(
            40,
            player.Deck.Cards.Count
            + player.Hand.Cards.Count
            + scene.World.AreaOf(
                DeckType.DiscardPile,
                PlayArea.Of(0),
                cardOwner: 0).Cards.Count);
        Assert.Equal(
            scene.World.Cards.Count,
            scene.World.Areas.Sum(area => area.Cards.Count + area.Removed.Count));
    }

    [Fact]
    public void InvasiveAiCanBeStackedOnlyFromTheLegalUltronEncounterDeck()
    {
        var scene = Deal(
            "behavior:card:01149:each-player-discards-top-3-cards-their-one-player",
            "ultron",
            ["spider_man"]);

        scene.Apply(new StackEncounterDeck([new SceneCard("01149")]));

        Assert.Equal("01149", scene.World.AreaOf(DeckType.EncounterDeck).Cards[^1].FaceId);
        Assert.Equal(World.Scenario, scene.Find(new SceneCard("01149")).Owner);
        Assert.Equal(DeckType.EnvironmentArea, scene.Find(new SceneCard("01140")).Area.Type);
        Assert.Contains(scene.World.Cards, FacedownDrones.Is);
    }

    [Fact]
    public void ACompleteAbilityInterpreterIsRequiredForSetup()
    {
        var request = new CoreSceneRequest(
            "behavior:setup:campaign:ultron:scenario-setup",
            "ultron",
            ["spider_man"],
            Seed: 302);

        Assert.Throws<ArgumentNullException>(() =>
            CanonicalCoreScene.Deal(request, Setup, Cards, null!));
    }

    [Fact]
    public void ASignatureCardCannotCrossFromSpiderManToIronMan()
    {
        var scene = Deal(
            "behavior:setup:hero:spider_man:hero-deck",
            "rhino",
            ["spider_man", "iron_man"]);
        var operation = new MoveSceneCard(
            new SceneCard("01006"),
            new SceneDestination(SceneZone.PlayerHand, Seat: 1));

        var thrown = Assert.Throws<CoreSceneConstructionException>(() => scene.Apply(operation));

        Assert.Equal("move-card", thrown.Operation);
        Assert.Contains("owned by 0, not seat 1", thrown.Message, StringComparison.Ordinal);
        Assert.Contains(scene.Request.Authority, thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AuntMayDoesNotExistInAnIronManDeal()
    {
        var scene = Deal(
            "behavior:setup:hero:iron_man:hero-deck",
            "rhino",
            ["iron_man"]);
        var operation = new MoveSceneCard(
            new SceneCard("01006"),
            new SceneDestination(SceneZone.PlayerDiscard, Seat: 0));

        var thrown = Assert.Throws<CoreSceneConstructionException>(() => scene.Apply(operation));

        Assert.Contains("no copy 0", thrown.Message, StringComparison.Ordinal);
        Assert.Contains("01006", thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void OnlyRegisteredTokenPoolsCanBeArranged()
    {
        var scene = Deal(
            "behavior:rr:threat.1:scheme-only",
            "rhino",
            ["spider_man"]);

        var thrown = Assert.Throws<CoreSceneConstructionException>(() => scene.Apply(
            new SetSceneCounters(new SceneCard("01094"), "threat", 1)));

        Assert.Equal("set-counters", thrown.Operation);
        Assert.Contains("does not print threat counters", thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void PrintedAllPurposeCounterTypesCanBeArranged()
    {
        var scene = Deal(
            "behavior:card:01018:energy-counter-below-cap",
            "rhino",
            ["captain_marvel"]);
        scene.Apply(new MoveSceneCard(
            new SceneCard("01018"),
            new SceneDestination(SceneZone.Upgrade, Seat: 0)));

        scene.Apply(new SetSceneCounters(new SceneCard("01018"), "energy", 4));

        Assert.Equal(4, scene.Find(new SceneCard("01018")).Tokens["c_energy"]);
    }

    [Fact]
    public void FormArrangementUsesOnlyTheSelectedIdentitysPrintedFaces()
    {
        var scene = Deal(
            "behavior:rr:form-change-form.1:published-result",
            "rhino",
            ["spider_man"]);

        scene.Apply(new SetSceneForm(0, "01001a"));

        Assert.Equal("01001a", scene.World.Seats[0].IdentityCard.FaceId);
        var thrown = Assert.Throws<CoreSceneConstructionException>(() =>
            scene.Apply(new SetSceneForm(0, "01029a")));
        Assert.Contains("not a printed identity face", thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void PlayerUpgradesCanBeAttachedToAHostWithoutChangingOwnership()
    {
        var scene = Deal(
            "behavior:card:01009:attach-to-an-enemy",
            "rhino",
            ["spider_man"]);
        Card rhino = scene.Find(new SceneCard("01094"));

        scene.Apply(new MoveSceneCard(
            new SceneCard("01009"),
            new SceneDestination(SceneZone.Upgrade, Seat: 0, Host: rhino.ObjectId)));

        Card webbedUp = scene.Find(new SceneCard("01009"));
        Assert.Equal(0, webbedUp.Owner);
        Assert.Equal(rhino.ObjectId, webbedUp.Area.Host);
        Assert.Equal(rhino.Area.PlayArea, webbedUp.Area.PlayArea);
    }

    [Rule("rr:status-cards.1")]
    [Fact]
    public void RulesProvidedStatusCardsAreExplicitlyCreatedAndAccountedFor()
    {
        // "A character cannot have more than one status card of each type at a time."
        var scene = Deal(
            "behavior:rr:status-cards.1:one-of-each-type",
            "rhino",
            ["spider_man"]);
        int dealt = scene.World.Cards.Count;

        scene.Apply(new GiveSceneStatus(new SceneCard("01094"), Statuses.Tough));

        Card rhino = scene.Find(new SceneCard("01094"));
        Assert.Equal(1, Statuses.Count(scene.World, rhino, Statuses.Tough));
        Assert.Equal(dealt + 1, scene.World.Cards.Count);
        Assert.Equal(
            scene.World.Cards.Count,
            scene.World.Areas.Sum(area => area.Cards.Count + area.Removed.Count));
        var thrown = Assert.Throws<CoreSceneConstructionException>(() =>
            scene.Apply(new GiveSceneStatus(new SceneCard("01094"), Statuses.Tough)));
        Assert.Equal("give-status", thrown.Operation);
    }

    [Rule("rr:unique-icon.1")]
    [Fact]
    public void RejectedUniquePlacementDoesNotPartiallyMoveTheCard()
    {
        // "A player cannot bring into play a unique card if a copy of that card
        // is already in play in their game area."
        var scene = Deal(
            "behavior:rr:unique-icon.1:matching-unique-in-play",
            "rhino",
            ["spider_man", "iron_man"]);
        Card first = scene.Find(new SceneCard("01084", Copy: 0));
        Card second = scene.Find(new SceneCard("01084", Copy: 1));
        scene.Apply(new MoveSceneCard(
            new SceneCard("01084", Copy: 0),
            new SceneDestination(SceneZone.Ally, Seat: first.Owner)));
        Area before = second.Area;
        int areaCount = scene.World.Areas.Count;

        var thrown = Assert.Throws<CoreSceneConstructionException>(() => scene.Apply(
            new MoveSceneCard(
                new SceneCard("01084", Copy: 1),
                new SceneDestination(SceneZone.Ally, Seat: second.Owner))));

        Assert.Contains("already in play", thrown.Message, StringComparison.Ordinal);
        Assert.Same(before, second.Area);
        Assert.Contains(second, before.Cards);
        Assert.Equal(areaCount, scene.World.Areas.Count);
    }

    [Fact]
    public void InPlayEntryUsesTheRulesLifecycleForStartingState()
    {
        var scene = Deal(
            "behavior:card:01149:printed-starting-threat",
            "ultron",
            ["spider_man"]);

        scene.Apply(new MoveSceneCard(
            new SceneCard("01149"),
            new SceneDestination(SceneZone.SideScheme)));
        scene.Apply(new MoveSceneCard(
            new SceneCard("01008"),
            new SceneDestination(SceneZone.Upgrade, Seat: 0)));

        Assert.Equal(3, scene.Find(new SceneCard("01149")).Tokens["k_threat"]);
        Assert.Equal(3, scene.Find(new SceneCard("01008")).Tokens["c_web"]);
        Assert.Equal(
            scene.World.Seats[0].IdentityCard.ObjectId,
            scene.Find(new SceneCard("01008")).Area.Host);
    }

    [Fact]
    public void PrintedAttachmentTargetIsRequired()
    {
        var scene = Deal(
            "behavior:card:01009:attach-to-an-enemy",
            "rhino",
            ["spider_man"]);
        scene.Apply(new MoveSceneCard(
            new SceneCard("01006"),
            new SceneDestination(SceneZone.Support, Seat: 0)));
        Card auntMay = scene.Find(new SceneCard("01006"));
        Area before = scene.Find(new SceneCard("01009")).Area;

        var thrown = Assert.Throws<CoreSceneConstructionException>(() => scene.Apply(
            new MoveSceneCard(
                new SceneCard("01009"),
                new SceneDestination(SceneZone.Upgrade, Seat: 0, Host: auntMay.ObjectId))));

        Assert.Contains("not a legal printed host", thrown.Message, StringComparison.Ordinal);
        Assert.Same(before, scene.Find(new SceneCard("01009")).Area);
    }

    [Fact]
    public void PrintedPerHostAttachmentMaximumIsRequired()
    {
        var scene = Deal(
            "behavior:card:01009:max-one-per-enemy",
            "rhino",
            ["spider_man"]);
        Card rhino = scene.Find(new SceneCard("01094"));
        scene.Apply(new MoveSceneCard(
            new SceneCard("01009", Copy: 0),
            new SceneDestination(SceneZone.Upgrade, Seat: 0, Host: rhino.ObjectId)));
        Area before = scene.Find(new SceneCard("01009", Copy: 1)).Area;

        var thrown = Assert.Throws<CoreSceneConstructionException>(() => scene.Apply(
            new MoveSceneCard(
                new SceneCard("01009", Copy: 1),
                new SceneDestination(SceneZone.Upgrade, Seat: 0, Host: rhino.ObjectId))));

        Assert.Contains("not a legal printed host", thrown.Message, StringComparison.Ordinal);
        Assert.Same(before, scene.Find(new SceneCard("01009", Copy: 1)).Area);
    }

    [Fact]
    public void AHostCannotLeavePlayWhileItsHostedCardsRemain()
    {
        var scene = Deal(
            "behavior:rr:leaves-play.1:hosted-cards-leave-play",
            "rhino",
            ["spider_man"]);
        Card nick = scene.Find(new SceneCard("01084"));
        scene.Apply(new MoveSceneCard(
            new SceneCard("01084"),
            new SceneDestination(SceneZone.Ally, Seat: 0)));
        scene.Apply(new GiveSceneStatus(new SceneCard("01084"), Statuses.Tough));
        Area before = nick.Area;

        var thrown = Assert.Throws<CoreSceneConstructionException>(() => scene.Apply(
            new StackPlayerDeck(0, [new SceneCard("01084")])));

        Assert.Contains("still holds a hosted card", thrown.Message, StringComparison.Ordinal);
        Assert.Same(before, nick.Area);
        Assert.Equal(1, Statuses.Count(scene.World, nick, Statuses.Tough));
    }

    [Fact]
    public void SetAsideCannotMoveAnOwnedCardAcrossSeats()
    {
        var scene = Deal(
            "behavior:setup:hero:spider_man:hero-deck",
            "rhino",
            ["spider_man", "iron_man"]);

        var thrown = Assert.Throws<CoreSceneConstructionException>(() => scene.Apply(
            new MoveSceneCard(
                new SceneCard("01006"),
                new SceneDestination(SceneZone.SetAside, Seat: 1))));

        Assert.Contains("owned by 0, not seat 1", thrown.Message, StringComparison.Ordinal);
    }

    [Rule("rr:player-deck.1")]
    [Fact]
    public void ConstructorCannotStopAtAnEmptyPlayerDeckWithADiscardPile()
    {
        // "immediately shuffle the discard pile to create a new player deck";
        // a transcript reaches that transition by drawing the last card.
        var scene = Deal(
            "behavior:rr:player-deck.1:empty-with-discard",
            "rhino",
            ["spider_man"]);

        var thrown = Assert.Throws<CoreSceneConstructionException>(() => scene.Apply(
            new StackPlayerDeck(0, [], DiscardOthers: true)));

        Assert.Contains("leave at least one card", thrown.Message, StringComparison.Ordinal);
        Assert.NotEmpty(scene.World.Seats[0].Deck.Cards);
    }

    private static CanonicalCoreScene OneCardDeck() => Deal(
            PlayerDeckAuthority,
            "rhino",
            ["spider_man"])
        .Apply(new StackPlayerDeck(
            Seat: 0,
            TopFirst: [new SceneCard("01006")],
            DiscardOthers: true));

    private static CanonicalCoreScene Deal(
        string authority, string campaign, IReadOnlyList<string> heroes) =>
        CanonicalCoreScene.Deal(
            new CoreSceneRequest(authority, campaign, heroes, Seed: 302),
            Setup,
            Cards,
            AuthoredCards.Runner());
}
