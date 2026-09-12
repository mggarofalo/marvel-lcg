using Marvel.Content.Behavior;
using Marvel.Content.Setup;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Behavior;
public sealed class CanonicalCoreSceneRulesProvidedStatusCardsTests : CanonicalCoreSceneTestBase
{
    [Rule("rr:status-cards.1")]
    [Fact]
    public void RulesProvidedStatusCardsAreExplicitlyCreatedAndAccountedFor()
    {
        // "A character cannot have more than one status card of each type at a time."
        var scene = Deal("behavior:rr:status-cards.1:one-of-each-type", "rhino", ["spider_man"]);
        int dealt = scene.World.Cards.Count;
        scene.Apply(new GiveSceneStatus(new SceneCard("01094"), Statuses.Tough));
        Card rhino = scene.Find(new SceneCard("01094"));
        Assert.Equal(1, Statuses.Count(scene.World, rhino, Statuses.Tough));
        Assert.Equal(dealt + 1, scene.World.Cards.Count);
        Assert.Equal(scene.World.Cards.Count, scene.World.Areas.Sum(area => area.Cards.Count + area.Removed.Count));
        var thrown = Assert.Throws<CoreSceneConstructionException>(() => scene.Apply(new GiveSceneStatus(new SceneCard("01094"), Statuses.Tough)));
        Assert.Equal("give-status", thrown.Operation);
    }

    [Rule("rr:unique-icon.1")]
    [Fact]
    public void RejectedUniquePlacementDoesNotPartiallyMoveTheCard()
    {
        // "A player cannot bring into play a unique card if a copy of that card
        // is already in play in their game area."
        var scene = Deal("behavior:rr:unique-icon.1:matching-unique-in-play", "rhino", ["spider_man", "iron_man"]);
        Card first = scene.Find(new SceneCard("01084", Copy: 0));
        Card second = scene.Find(new SceneCard("01084", Copy: 1));
        scene.Apply(new MoveSceneCard(new SceneCard("01084", Copy: 0), new SceneDestination(SceneZone.Ally, Seat: first.Owner)));
        Area before = second.Area;
        int areaCount = scene.World.Areas.Count;
        var thrown = Assert.Throws<CoreSceneConstructionException>(() => scene.Apply(new MoveSceneCard(new SceneCard("01084", Copy: 1), new SceneDestination(SceneZone.Ally, Seat: second.Owner))));
        Assert.Contains("already in play", thrown.Message, StringComparison.Ordinal);
        Assert.Same(before, second.Area);
        Assert.Contains(second, before.Cards);
        Assert.Equal(areaCount, scene.World.Areas.Count);
    }

    [Fact]
    public void InPlayEntryUsesTheRulesLifecycleForStartingState()
    {
        var scene = Deal("behavior:card:01149:printed-starting-threat", "ultron", ["spider_man"]);
        scene.Apply(new MoveSceneCard(new SceneCard("01149"), new SceneDestination(SceneZone.SideScheme)));
        scene.Apply(new MoveSceneCard(new SceneCard("01008"), new SceneDestination(SceneZone.Upgrade, Seat: 0)));
        Assert.Equal(3, scene.Find(new SceneCard("01149")).Tokens["k_threat"]);
        Assert.Equal(3, scene.Find(new SceneCard("01008")).Tokens["c_web"]);
        Assert.Equal(scene.World.Seats[0].IdentityCard.ObjectId, scene.Find(new SceneCard("01008")).Area.Host);
    }

    [Fact]
    public void PrintedAttachmentTargetIsRequired()
    {
        var scene = Deal("behavior:card:01009:attach-to-an-enemy", "rhino", ["spider_man"]);
        scene.Apply(new MoveSceneCard(new SceneCard("01006"), new SceneDestination(SceneZone.Support, Seat: 0)));
        Card auntMay = scene.Find(new SceneCard("01006"));
        Area before = scene.Find(new SceneCard("01009")).Area;
        var thrown = Assert.Throws<CoreSceneConstructionException>(() => scene.Apply(new MoveSceneCard(new SceneCard("01009"), new SceneDestination(SceneZone.Upgrade, Seat: 0, Host: auntMay.ObjectId))));
        Assert.Contains("not a legal printed host", thrown.Message, StringComparison.Ordinal);
        Assert.Same(before, scene.Find(new SceneCard("01009")).Area);
    }

    [Fact]
    public void PrintedPerHostAttachmentMaximumIsRequired()
    {
        var scene = Deal("behavior:card:01009:max-one-per-enemy", "rhino", ["spider_man"]);
        Card rhino = scene.Find(new SceneCard("01094"));
        scene.Apply(new MoveSceneCard(new SceneCard("01009", Copy: 0), new SceneDestination(SceneZone.Upgrade, Seat: 0, Host: rhino.ObjectId)));
        Area before = scene.Find(new SceneCard("01009", Copy: 1)).Area;
        var thrown = Assert.Throws<CoreSceneConstructionException>(() => scene.Apply(new MoveSceneCard(new SceneCard("01009", Copy: 1), new SceneDestination(SceneZone.Upgrade, Seat: 0, Host: rhino.ObjectId))));
        Assert.Contains("not a legal printed host", thrown.Message, StringComparison.Ordinal);
        Assert.Same(before, scene.Find(new SceneCard("01009", Copy: 1)).Area);
    }

    [Fact]
    public void PrintedPerPlayerMaximumIsRequired()
    {
        var scene = Deal("behavior:card:01018:max-one-per-player", "rhino", ["captain_marvel"]);
        scene.Apply(new MoveSceneCard(new SceneCard("01018", Copy: 0), new SceneDestination(SceneZone.Upgrade, Seat: 0)));
        Area before = scene.Find(new SceneCard("01018", Copy: 1)).Area;
        var thrown = Assert.Throws<CoreSceneConstructionException>(() => scene.Apply(new MoveSceneCard(new SceneCard("01018", Copy: 1), new SceneDestination(SceneZone.Upgrade, Seat: 0))));
        Assert.Contains("printed maximum", thrown.Message, StringComparison.Ordinal);
        Assert.Same(before, scene.Find(new SceneCard("01018", Copy: 1)).Area);
    }

    [Fact]
    public void FirstPlayerMayChooseEitherTiedLegalEncounterAttachmentHost()
    {
        var scene = Deal("behavior:card:01163:tied-highest-printed-health", "rhino", ["she_hulk"], ["masters_of_evil"]);
        scene.Apply(new MoveSceneCard(new SceneCard("01162"), new SceneDestination(SceneZone.EngagedMinion, Seat: 0)));
        scene.Apply(new MoveSceneCard(new SceneCard("01130"), new SceneDestination(SceneZone.EngagedMinion, Seat: 0)));
        Card titania = scene.Find(new SceneCard("01162"));
        scene.Apply(new MoveSceneCard(new SceneCard("01163"), new SceneDestination(SceneZone.Attachment, Host: titania.ObjectId)));
        Assert.Equal(titania.ObjectId, scene.Find(new SceneCard("01163")).Area.Host);
    }

    [Fact]
    public void AHostCannotLeavePlayWhileItsHostedCardsRemain()
    {
        var scene = Deal("behavior:rr:leaves-play.1:hosted-cards-leave-play", "rhino", ["spider_man"]);
        Card nick = scene.Find(new SceneCard("01084"));
        scene.Apply(new MoveSceneCard(new SceneCard("01084"), new SceneDestination(SceneZone.Ally, Seat: 0)));
        scene.Apply(new GiveSceneStatus(new SceneCard("01084"), Statuses.Tough));
        Area before = nick.Area;
        var thrown = Assert.Throws<CoreSceneConstructionException>(() => scene.Apply(new StackPlayerDeck(0, [new SceneCard("01084")])));
        Assert.Contains("still holds a hosted card", thrown.Message, StringComparison.Ordinal);
        Assert.Same(before, nick.Area);
        Assert.Equal(1, Statuses.Count(scene.World, nick, Statuses.Tough));
    }

    [Fact]
    public void SetAsideCannotMoveAnOwnedCardAcrossSeats()
    {
        var scene = Deal("behavior:setup:hero:spider_man:hero-deck", "rhino", ["spider_man", "iron_man"]);
        var thrown = Assert.Throws<CoreSceneConstructionException>(() => scene.Apply(new MoveSceneCard(new SceneCard("01006"), new SceneDestination(SceneZone.SetAside, Seat: 1))));
        Assert.Contains("owned by 0, not seat 1", thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ActiveStructuralCardsCannotBeMovedOutOfTheirRoles()
    {
        var scene = Deal("behavior:rr:identity.4:identity-remains-in-play", "rhino", ["spider_man"]);
        Card identity = scene.World.Seats[0].IdentityCard;
        var thrown = Assert.Throws<CoreSceneConstructionException>(() => scene.Apply(new MoveSceneCard(new SceneCard(identity.FaceId), new SceneDestination(SceneZone.SetAside, Seat: 0))));
        Assert.Contains("cannot be rearranged", thrown.Message, StringComparison.Ordinal);
        Assert.Equal(DeckType.HeroArea, identity.Area.Type);
    }

    [Fact]
    public void FutureScenarioStagesCannotLeaveTheirRequiredDecks()
    {
        var scene = Deal("behavior:setup:campaign:klaw:stage-decks", "klaw", ["spider_man"]);
        Card futureScheme = scene.Find(new SceneCard("01117a"));
        Card futureVillain = scene.Find(new SceneCard("01114"));
        var scheme = Assert.Throws<CoreSceneConstructionException>(() => scene.Apply(new MoveSceneCard(new SceneCard("01117a"), new SceneDestination(SceneZone.SetAside))));
        var villain = Assert.Throws<CoreSceneConstructionException>(() => scene.Apply(new MoveSceneCard(new SceneCard("01114"), new SceneDestination(SceneZone.SetAside))));
        Assert.Contains("structural MainScheme", scheme.Message, StringComparison.Ordinal);
        Assert.Contains("structural EncounterVillain", villain.Message, StringComparison.Ordinal);
        Assert.Equal(DeckType.MainSchemesDeck, futureScheme.Area.Type);
        Assert.Equal(DeckType.VillainDeck, futureVillain.Area.Type);
    }

    [Fact]
    public void ASignatureObligationMustGoToItsNamedIdentity()
    {
        var scene = Deal("behavior:rr:obligation.4:named-player", "rhino", ["spider_man", "iron_man"]);
        Card evictionNotice = scene.Find(new SceneCard("01165"));
        Area before = evictionNotice.Area;
        var thrown = Assert.Throws<CoreSceneConstructionException>(() => scene.Apply(new MoveSceneCard(new SceneCard("01165"), new SceneDestination(SceneZone.Obligation, Seat: 1))));
        Assert.Contains("must be given to seat 0", thrown.Message, StringComparison.Ordinal);
        Assert.Same(before, evictionNotice.Area);
    }

    [Rule("rr:player-deck.1")]
    [Fact]
    public void ConstructorCannotStopAtAnEmptyPlayerDeckWithADiscardPile()
    {
        // "immediately shuffle the discard pile to create a new player deck";
        // a transcript reaches that transition by drawing the last card.
        var scene = Deal("behavior:rr:player-deck.1:empty-with-discard", "rhino", ["spider_man"]);
        var thrown = Assert.Throws<CoreSceneConstructionException>(() => scene.Apply(new StackPlayerDeck(0, [], PlayerDeckRemainder.Discard)));
        Assert.Contains("leave at least one card", thrown.Message, StringComparison.Ordinal);
        Assert.NotEmpty(scene.World.Seats[0].Deck.Cards);
    }
}
