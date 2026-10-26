using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

// Synthetic boards use actual printed Mjolnir and the complete authored book.
public sealed class ThorMeanSwingCardTests : CoreCardTestBase
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:attack-player-ability-type.1")]
    public void ActualBasicAttackPaysWeaponAndAddsThreeOnlyForThisAttack(bool invalidFirst)
    {
        // A basic attack deals damage "equal to the character's ATK value".
        var world = ThorActualFixture.Board("06001a");
        var control = ThorActualFixture.Board("06001a");
        foreach (var board in new[] { world, control })
        {
            var runner = AuthoredCards.Runner();
            var hero = board.Seats[0].IdentityCard;
            var weapon = board.CreateCard("06009", board.AreaOf(DeckType.UpgradesArea,
                PlayArea.Of(0), hero.ObjectId, cardOwner: 0));
            var card = board.CreateCard("06015", board.Seats[0].Hand);
            var enemy = board.TheCardIn(DeckType.VillainArea)!;
            BasicPowerInitiation.BasicAttack(board, board.Facts, 0, enemy, []);
            var prompt = Sequence.Work(board, board.Facts, runner, [])!;
            var offer = Assert.Single(prompt.Affordances, a => a.AnchorId == card.ObjectId);
            Assert.Contains("WEAPON upgrades attached to your hero", offer.CostDescription);
            Assert.Contains("3 ATK", offer.Description);
            Assert.Contains("Current attack bonus: 0", prompt.Description);
            Assert.Equal(new[] { weapon.ObjectId }, offer.Targets!.Legal);
            if (ReferenceEquals(board, world) && invalidFirst)
            {
                var before = board.Digest().Canonical();
                var random = board.Random.Generator.GetState();
                var words = board.Random.Generator.WordsConsumed;
                var pending = board.Agenda.Outstanding.ToArray();
                Assert.Throws<RulesNotImplementedException>(() => Sequence.Answer(board, board.Facts, runner,
                    prompt, new Decision(offer.Id, [hero.ObjectId]), []));
                Assert.Equal(before, board.Digest().Canonical());
                Assert.Equal(random.Words, board.Random.Generator.GetState().Words);
                Assert.Equal(random.Index, board.Random.Generator.GetState().Index);
                Assert.Equal(words, board.Random.Generator.WordsConsumed);
                Assert.Equal(pending, board.Agenda.Outstanding);
            }
            Sequence.Answer(board, board.Facts, runner, prompt, new Decision(offer.Id, [weapon.ObjectId]), []);
            Sequence.Finish(board, board.Facts, runner, []);
            Assert.False(weapon.Ready);
            Assert.Equal(DeckType.DiscardPile, card.Area.Type);
            Assert.Equal(6, enemy.Damage);
            Assert.Equal(3, StateFields.Modified(board, hero, "attack", board.Facts, board.Players));
            hero.Refresh();
            BasicPowerInitiation.BasicAttack(board, board.Facts, 0, enemy, []);
            Sequence.Finish(board, board.Facts, runner, []);
            Assert.Equal(9, enemy.Damage);
            Assert.Empty(board.Agenda.Outstanding);
        }
        Assert.Equal(control.Digest().Canonical(), world.Digest().Canonical());
        Assert.Equal(control.Random.Generator.GetState().Words, world.Random.Generator.GetState().Words);
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("exhausted")]
    [InlineData("wrong-host")]
    public void UnpayableWeaponLeavesActualMeanSwingInHand(string reason)
    {
        var world = ThorActualFixture.Board("06001a", 2);
        var runner = AuthoredCards.Runner();
        var card = world.CreateCard("06015", world.Seats[0].Hand);
        if (reason != "missing")
        {
            var hammer = world.CreateCard("06009", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0),
                world.Seats[reason == "wrong-host" ? 1 : 0].IdentityCard.ObjectId, cardOwner: 0));
            if (reason == "exhausted") hammer.Exhaust();
        }
        BasicPowerInitiation.BasicAttack(world, world.Facts, 0, world.TheCardIn(DeckType.VillainArea)!, []);
        Assert.Null(Sequence.Work(world, world.Facts, runner, []));
        Assert.Equal(DeckType.HandsArea, card.Area.Type);
    }

    [Fact]
    public void ActualPaidSwingingWebKickNeverOffersMeanSwingDespiteReadyWeapon()
    {
        var world = ThorActualFixture.Board("06001a");
        var runner = AuthoredCards.Runner();
        var hero = world.Seats[0].IdentityCard;
        var weapon = world.CreateCard("06009", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), hero.ObjectId, cardOwner: 0));
        var swing = world.CreateCard("06015", world.Seats[0].Hand);
        var attack = world.CreateCard("01005", world.Seats[0].Hand);
        var first = world.CreateCard("06022", world.Seats[0].Hand);
        var second = world.CreateCard("01003", world.Seats[0].Hand);
        var action = Assert.Single(runner.Actions(world, 0), a => a.Card == attack.ObjectId);
        runner.ExecuteAction(world, new PlayerAction(action, [first.ObjectId, second.ObjectId], []));
        var target = Sequence.Work(world, world.Facts, runner, [])!;
        Assert.DoesNotContain(target.Affordances, a => a.AnchorId == swing.ObjectId);
        Sequence.Answer(world, world.Facts, runner, target,
            Decision.Take(world.TheCardIn(DeckType.VillainArea)!.ObjectId), []);
        Sequence.Finish(world, world.Facts, runner, []);
        Assert.Equal(8, world.TheCardIn(DeckType.VillainArea)!.Damage);
        Assert.Equal(DeckType.HandsArea, swing.Area.Type);
        Assert.True(weapon.Ready);
        Assert.Equal(DeckType.DiscardPile, attack.Area.Type);
        Assert.Empty(world.Agenda.Outstanding);
    }
}
