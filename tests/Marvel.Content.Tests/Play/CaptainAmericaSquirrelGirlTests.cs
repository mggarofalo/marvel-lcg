using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class CaptainAmericaSquirrelGirlTests
{
    [Fact]
    [Rule("rr:damage.step.7")]
    [Rule("rr:damage.step.8")]
    [Rule("rr:forced.5")]
    public void LethalEntryDamageDefeatsEachPlayersSoldierAndDealsEachOwnerOneEncounterCard()
    {
        // rr:damage.step.7 resolves "When Defeated" abilities before step.8, "Discarding of a defeated character."
        Card? squirrel = null;
        Card[] soldiers = [];
        int[] payment = [];
        var (game, world) = CaptainAmericaFixture.PlayingCaptain(board =>
        {
            squirrel = board.CreateCard("03013", board.Seats[0].Hand);
            payment = [board.CreateCard("03022", board.Seats[0].Hand).ObjectId];
            soldiers = [.. Enumerable.Range(0, 2).Select(player => board.CreateCard("01182",
                board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(player))))];
            foreach (var soldier in soldiers) soldier.TakeDamage(3);
        }, twoPlayers: true);
        var play = Assert.Single(game.Pending!.Affordances, item => item.AnchorId == squirrel!.ObjectId && item.PlaysCard);
        game.Resolve(new Decision(play.Id, [world.Seats[0].IdentityCard.ObjectId], payment,
            Allocations: ResourcePayment.Allocate(play.CostOptions[0], payment)));
        game.Resolve(Decision.Take(Assert.Single(game.Pending!.Affordances,
            item => item.AnchorId == squirrel!.ObjectId).Id));
        // rr:forced.5: "the first player determines the order" of simultaneous
        // forced abilities. Both printed When Defeated abilities precede departure.
        Assert.Equal(Question.Order, game.Pending!.Asking);
        Assert.Equal(world.FirstPlayer, game.Pending.Player);
        Assert.Equal(2, game.Pending.Affordances.Count);
        Assert.All(soldiers, soldier => Assert.Equal(DeckType.EngagedEnemiesArea, soldier.Area.Type));
        game.Resolve(Decision.Take(soldiers[1].ObjectId));
        Assert.All(soldiers, soldier => Assert.Equal(DeckType.EncounterDiscardPile, soldier.Area.Type));
        Assert.All(Enumerable.Range(0, 2), player => Assert.Single(
            world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(player)).Cards));
        Assert.Equal(1, world.TheCardIn(DeckType.VillainArea)!.Damage);
        Assert.Equal(Question.TurnOption, game.Pending!.Asking);
        Assert.Equal(0, squirrel!.Damage);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [Rule("rr:ability.11")]
    public void ActualEntryOptionDamagesEveryEnemyWithoutAttacking(bool decline, bool tough)
    {
        // rr:ability.11: "Unless prefaced by the word “Forced”, all interrupt and response abilities are optional."
        Card? squirrel = null;
        Card? own = null;
        Card? foreign = null;
        Card? villain = null;
        int[] payment = [];
        var (game, world) = CaptainAmericaFixture.PlayingCaptain(board =>
        {
            squirrel = board.CreateCard("03013", board.Seats[0].Hand);
            payment = [board.CreateCard("03022", board.Seats[0].Hand).ObjectId];
            own = board.CreateCard("01182", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            foreign = board.CreateCard("01182", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1)));
            villain = board.TheCardIn(DeckType.VillainArea)!;
            // Synthetic keyword isolates the printed response's nonattack boundary.
            board.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect,
                "retaliate", 1, Affects: villain.ObjectId));
            if (tough) Statuses.Give(board, foreign, Statuses.Tough);
            Statuses.Give(board, board.Seats[0].IdentityCard, Statuses.Stunned);
        }, twoPlayers: true);
        var play = Assert.Single(game.Pending!.Affordances, item => item.AnchorId == squirrel!.ObjectId && item.PlaysCard);
        game.Resolve(new Decision(play.Id, [world.Seats[0].IdentityCard.ObjectId], payment,
            Allocations: ResourcePayment.Allocate(play.CostOptions[0], payment)));
        Assert.Equal(DeckType.AlliesArea, squirrel!.Area.Type);
        Assert.Equal(Question.Opportunity, game.Pending!.Asking);
        if (decline) game.Resolve(Decision.Decline);
        else game.Resolve(Decision.Take(Assert.Single(game.Pending.Affordances,
            item => item.AnchorId == squirrel.ObjectId).Id));
        Assert.Equal(decline ? 0 : 1, villain!.Damage);
        Assert.Equal(decline ? 0 : 1, own!.Damage);
        AssertEnemyOutcome(world, foreign!, decline, tough);
        Assert.True(Statuses.Has(world, world.Seats[0].IdentityCard, Statuses.Stunned));
        Assert.Equal(0, squirrel.Damage);
        Assert.Equal(0, world.Seats[0].IdentityCard.Damage);
        Assert.Equal(0, world.Seats[1].IdentityCard.Damage);
        Assert.Equal(Question.TurnOption, game.Pending!.Asking);
    }

    private static void AssertEnemyOutcome(World world, Card enemy, bool decline, bool tough)
    {
        Assert.Equal(decline || tough ? 0 : 1, enemy.Damage);
        Assert.Equal(tough && decline, Statuses.Has(world, enemy, Statuses.Tough));
    }
}
