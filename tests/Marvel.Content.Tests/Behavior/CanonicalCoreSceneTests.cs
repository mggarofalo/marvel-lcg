using Marvel.Content.Behavior;
using Marvel.Content.Setup;
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

    [Rule("rr:player-deck.2")]
    [Fact]
    public void OneCardBoundaryAccountsForEveryOtherDeckCardInALegalZone()
    {
        // "the player continues to draw cards up to the number specified";
        // the boundary is a one-card deck, not an invented one-card game.
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
            "behavior:card:01007:deal-8-damage-to-an-enemy",
            "rhino",
            ["spider_man"]);

        var thrown = Assert.Throws<CoreSceneConstructionException>(() => scene.Apply(
            new SetSceneTokens(new SceneCard("01007"), "k_threat", 1)));

        Assert.Equal("set-tokens", thrown.Operation);
        Assert.Contains("does not register", thrown.Message, StringComparison.Ordinal);
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
            Cards);
}
