using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class GoblinGimmicksAttachmentTests : CoreCardTestBase
{
    [Theory]
    [InlineData("02033")]
    [InlineData("02019")]
    [Rule("rr:attach-to")]
    [Rule("rr:attachment.1")]
    [Rule("rr:surge.1")]
    public void GliderRanksPrintedHealthExcludesSameTitleAndSurgesWithoutAnEligibleEnemy(string face)
    {
        // 02033: "highest printed hit points and without another Goblin Glider".
        // rr:attachment.1: "modify that character's ATK".
        // rr:surge.1: "deal yourself 1 facedown encounter card".
        var f = RedSkullFixture.Create(players: 2);
        var minion = f.World.CreateCard("01162", f.World.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1)));
        f.Villain.TakeDamage(15);
        long attack = Modified(f.World, f.Villain, "attack");
        var first = f.Reveal(face, 1);
        f.Finish();
        Assert.Equal(f.Villain.ObjectId, first.Area.Host);
        Assert.Equal(attack + 1, Modified(f.World, f.Villain, "attack"));
        Assert.Empty(f.World.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(1)).Cards);
        // A same-title face from another encounter set also occupies that host.
        World.MoveToTop(first, f.World.AreaOf(DeckType.EncounterDiscardPile));
        f.World.CreateCard("02019", f.World.AreaOf(DeckType.UpgradesArea, PlayArea.Villains, f.Villain.ObjectId));
        long minionAttack = Modified(f.World, minion, "attack");
        var second = f.Reveal(face, 1);
        f.Finish();
        Assert.Equal(minion.ObjectId, second.Area.Host);
        Assert.Equal(minionAttack + 1, Modified(f.World, minion, "attack"));
        var third = f.Reveal(face, 1);
        f.Finish();
        Assert.Equal(DeckType.EncounterDiscardPile, third.Area.Type);
        Assert.Single(f.World.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(1)).Cards);
        Assert.Empty(f.World.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(0)).Cards);
    }

    [Theory]
    [InlineData("02033", "01088", "01090", "YY")]
    [InlineData("02019", "01088", "01090", "YY")]
    [InlineData("02020", "01089", "01090", "BB")]
    [InlineData("02021", "01090", "01088", "RR")]
    [InlineData("02034", "01090", "01088", "RR")]
    [Rule("rr:cost.3")]
    [Rule("rr:attachment.1")]
    public void HeroCanRemoveAttachmentOnlyWithItsPrintedResourceTypes(
        string face, string correct, string wrong, string required)
    {
        // 02033 Hero Action: "Spend [energy] [energy] resources".
        // 02034 Hero Action: "Spend [physical] [physical] resources".
        // rr:cost.3: resources generated "by discarding cards from their hand".
        // rr:attachment.1: "modify that character's ATK" while attached.
        Card attachment = null!;
        Card payment = null!;
        Card invalid = null!;
        var (game, world) = ActionAbilitySupportFixtures6.Playing(board =>
        {
            var villain = board.TheCardIn(DeckType.VillainArea)!;
            attachment = board.CreateCard(face, board.AreaOf(DeckType.UpgradesArea, PlayArea.Villains, villain.ObjectId));
            ActionAbilitySupportFixtures.Hand(board, correct, 1);
            payment = Assert.Single(board.Seats[0].Hand.Cards);
            invalid = board.CreateCard(wrong, board.Seats[0].Hand);
        }, hero: true);
        var action = Assert.Single(game.Pending!.Affordances, item => item.AnchorId == attachment.ObjectId);
        Assert.Equal(required, ResourcePayment.RequiredResources(Assert.Single(action.CostOptions[0].ResourceCosts)));
        var pending = game.Pending;
        string before = world.Digest().Canonical();
        Assert.Throws<ArgumentException>(() => game.Resolve(new Decision(action.Id, [], [invalid.ObjectId],
            Allocations: [new(invalid.ObjectId, 0, required[0].ToString())])));
        Assert.Same(pending, game.Pending);
        Assert.Equal(before, world.Digest().Canonical());
        int[] paying = [payment.ObjectId];
        game.Resolve(new Decision(action.Id, [], paying,
            Allocations: ResourcePayment.Allocate(action.CostOptions[0], paying)));
        Assert.Equal(DeckType.EncounterDiscardPile, attachment.Area.Type);
        Assert.Equal(DeckType.DiscardPile, payment.Area.Type);
        Assert.Same(world.Seats[0].Hand, invalid.Area);
        var villainAfter = world.TheCardIn(DeckType.VillainArea)!;
        Assert.Equal(2, Modified(world, villainAfter, "attack"));
    }

    [Theory]
    [InlineData("02034")]
    [InlineData("02021")]
    [Rule("rr:attach-to")]
    public void PumpkinBombsRevealAttachesToTheVillainWithoutChangingItsAttackValue(string face)
    {
        // rr:attach-to: "must be attached to ... the specified game element".
        // 02034: "Attach to the villain"; its star reminds the forced response.
        var f = RedSkullFixture.Create(players: 2);
        long attack = Modified(f.World, f.Villain, "attack");
        var bombs = f.Reveal(face, 1);
        f.Finish();
        Assert.Equal(DeckType.UpgradesArea, bombs.Area.Type);
        Assert.Equal(f.Villain.ObjectId, bombs.Area.Host);
        Assert.Equal(attack, Modified(f.World, f.Villain, "attack"));
    }

    [Theory]
    [InlineData("02033", "01088")]
    [InlineData("02019", "01088")]
    [InlineData("02020", "01089")]
    [InlineData("02021", "01090")]
    [InlineData("02034", "01090")]
    public void PrintedHeroRemovalActionIsUnavailableInAlterEgo(string face, string resource)
    {
        // Printed Hero Action restricts both removal abilities to hero form.
        var f = RedSkullFixture.Create(hero: false);
        var attachment = f.World.CreateCard(face,
            f.World.AreaOf(DeckType.UpgradesArea, PlayArea.Villains, f.Villain.ObjectId));
        f.World.CreateCard(resource, f.World.Seats[0].Hand);
        Assert.DoesNotContain(f.Runner.Actions(f.World, 0), item => item.Card == attachment.ObjectId);
    }
}

