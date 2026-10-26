using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class ThorWeaponFuryCardTests : CoreCardTestBase
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void BattleFuryFollowsControllerAndRequiresFullyPaidDamage(bool transfer, bool tough)
    {
        // Vendored 06018 FAQ: "If ... you prevent the damage, then you have not paid the cost".
        var world = ThorActualFixture.Board("06001a", 2);
        var runner = AuthoredCards.Runner();
        var seat = world.Seats[0];
        var fury = world.CreateCard("06018", seat.Hand);
        var payment = world.CreateCard("01003", seat.Hand);
        int player = transfer ? 1 : 0;
        var hero = world.Seats[player].IdentityCard;
        CardPlay.Play(world, world.Facts, runner, seat, fury, [payment.ObjectId], [], [hero.ObjectId]);
        Sequence.Finish(world, world.Facts, runner, []);
        Assert.Equal(player, CardControl.ControllerOf(world, fury));
        Assert.Equal(0, fury.Owner);
        var duplicate = world.CreateCard("06018", world.Seats[player].Hand);
        world.CreateCard("01003", world.Seats[player].Hand);
        Assert.DoesNotContain(runner.AttachmentTargets(world, duplicate)!, id => id == hero.ObjectId);
        var minion = world.CreateCard("01101", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(player)));
        minion.TakeDamage(2);
        if (tough) Statuses.Give(world, hero, Statuses.Tough);
        BasicPowerInitiation.BasicAttack(world, world.Facts, player, minion, []);
        var prompt = Sequence.Work(world, world.Facts, runner, []);
        if (tough)
        {
            Assert.True(prompt is null || prompt.Affordances.All(a => a.AnchorId != fury.ObjectId));
            Sequence.Finish(world, world.Facts, runner, []);
            Assert.False(hero.Ready);
            Assert.Equal(0, hero.Damage);
            Assert.Equal(DeckType.UpgradesArea, fury.Area.Type);
            Assert.True(Statuses.Has(world, hero, Statuses.Tough));
        }
        else
        {
            var response = Assert.Single(prompt!.Affordances, a => a.AnchorId == fury.ObjectId);
            Assert.Equal(player, prompt.Player);
            Sequence.Answer(world, world.Facts, runner, prompt, Decision.Take(response.Id), []);
            Sequence.Finish(world, world.Facts, runner, []);
            Assert.True(hero.Ready);
            Assert.Equal(1, hero.Damage);
            Assert.Equal(DeckType.DiscardPile, fury.Area.Type);
            Assert.Equal(PlayArea.Of(0), fury.Area.PlayArea);
        }
        Assert.Equal(DeckType.EncounterDiscardPile, minion.Area.Type);
        Assert.Empty(world.Agenda.Outstanding);
    }

    [Theory]
    [InlineData("basic", "01003", true)]
    [InlineData("basic", "01090", true)]
    [InlineData("basic", "01088", false)]
    [InlineData("event", "01003", true)]
    [InlineData("ally", "01003", false)]
    public void JarnbjornPaysTypedResourceAfterHeroAttackAndDealsNonattackDamage(string kind, string resource, bool eligible)
    {
        var world = ThorActualFixture.Board("06001a");
        var runner = AuthoredCards.Runner();
        var seat = world.Seats[0];
        var weapon = world.CreateCard("06019", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), seat.IdentityCard.ObjectId, cardOwner: 0));
        var hall = world.CreateCard("06017", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        var fury = world.CreateCard("06018", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), seat.IdentityCard.ObjectId, cardOwner: 0));
        var paying = world.CreateCard(resource, seat.Hand);
        var victim = world.CreateCard("01102", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        Assert.Equal(4, world.Facts.PrintedValue(victim.FaceId, "HP", 1));
        victim.TakeDamage(2);
        BeginAttack(world, runner, kind);
        int triggers = 0;
        for (int step = 0; step < 25; step++)
        {
            var prompt = Sequence.Work(world, world.Facts, runner, []);
            if (prompt is null) break;
            Assert.DoesNotContain(prompt.Affordances, a => a.AnchorId == fury.ObjectId);
            var response = prompt.Affordances.SingleOrDefault(a => a.AnchorId == weapon.ObjectId);
            if (response is not null)
            {
                triggers++;
                var before = world.Digest().Canonical();
                var rng = world.Random.Generator.GetState();
                var pending = world.Agenda.Outstanding.ToArray();
                Assert.Throws<RulesNotImplementedException>(() => Sequence.Answer(world, world.Facts, runner,
                    prompt, Decision.Take(response.Id, [], []), []));
                Assert.Equal(before, world.Digest().Canonical());
                Assert.Equal(rng.Words, world.Random.Generator.GetState().Words);
                Assert.Equal(rng.Index, world.Random.Generator.GetState().Index);
                Assert.Equal(pending, world.Agenda.Outstanding);
                Sequence.Answer(world, world.Facts, runner, prompt, Decision.Take(response.Id, [], [paying.ObjectId]), []);
            }
            else if (prompt.Affordances.Any(a => a.AnchorId == hall.ObjectId))
                Sequence.Answer(world, world.Facts, runner, prompt, Decision.Take(prompt.Affordances.Single(a => a.AnchorId == hall.ObjectId).Id), []);
            else Sequence.Answer(world, world.Facts, runner, prompt,
                prompt.Cancellable ? Decision.Decline : Decision.Take(victim.ObjectId), []);
        }
        Assert.Equal(eligible ? 1 : 0, triggers);
        Assert.Equal(eligible ? DeckType.DiscardPile : DeckType.HandsArea, paying.Area.Type);
        Assert.Equal(eligible ? DeckType.EncounterDiscardPile : DeckType.EngagedEnemiesArea, victim.Area.Type);
        Assert.Equal(eligible ? 1 : 0, hall.Tokens.GetValueOrDefault("c_glory"));
        Assert.Equal(DeckType.UpgradesArea, fury.Area.Type);
        Assert.Empty(world.Agenda.Outstanding);
    }
    private static void BeginAttack(World world, Marvel.Cards.Run.AbilityRunner runner, string kind)
    {
        var seat = world.Seats[0];
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        if (kind == "event")
        {
            var attack = world.CreateCard("01005", seat.Hand);
            var first = world.CreateCard("06022", seat.Hand);
            var second = world.CreateCard("01003", seat.Hand);
            var action = Assert.Single(runner.Actions(world, 0), a => a.Card == attack.ObjectId);
            runner.ExecuteAction(world, new PlayerAction(action, [first.ObjectId, second.ObjectId], []));
            var target = Sequence.Work(world, world.Facts, runner, [])!;
            Sequence.Answer(world, world.Facts, runner, target, Decision.Take(villain.ObjectId), []);
        }
        else if (kind == "ally")
        {
            var ally = world.CreateCard("01083", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
            AllyBasicPowers.AllyPower(world, world.Facts, ally, villain, BasicPowers.AttackVerb, []);
        }
        else BasicPowerInitiation.BasicAttack(world, world.Facts, 0, villain, []);
    }

}
