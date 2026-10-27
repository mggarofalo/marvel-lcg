using System.Text.Json;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class HulkSignaturePermanentTests
{
    [Fact]
    public void SevenSignatureFacesAppendExactlyWithoutStartingCounters()
    {
        using var document = JsonDocument.Parse(AuthoredCards.Text);
        var rows = document.RootElement.GetProperty("cards").EnumerateArray().ToArray();
        string[] expected = ["10002", "10003", "10006", "10007", "10008", "10009", "10010"];
        Assert.Equal(721, rows.Length);
        Assert.Equal(expected, rows.Skip(693).Take(7).Take(7).Select(row => row.GetProperty("card").GetString()));
        Assert.All(rows.Skip(693).Take(7).Take(7), row => Assert.False(row.TryGetProperty("startingCounters", out _)));
        Assert.DoesNotContain("11001", AuthoredCards.Book.Authored);
    }

    [Fact]
    [Rule("rr:identity.2")]
    public void LabNamesBruceAcrossControllersButItsResourceBelongsToController()
    {
        // A title refers "only to the identity with that title, and not to the other side".
        var (_, world) = HulkFirstSliceFixture.PlayingHulk(_ => { }, owner: 1);
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        var hulk = world.Seats[1].IdentityCard;
        world.Seats[0].IdentityCard.TurnTo("01001a");
        var lab = world.CreateCard("10008", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        Assert.DoesNotContain(runner.ResourceAbilities(world, 0), a => a.Effect == lab.ObjectId);
        hulk.TurnTo("10001b");
        Assert.Equal(6, StateFields.Modified(world, hulk, "recover", world.Facts, world.Players));
        Assert.DoesNotContain(runner.ResourceAbilities(world, 1), a => a.Effect == lab.ObjectId);
        world.Seats[0].IdentityCard.TurnTo("01001b");
        Assert.Equal("B", Assert.Single(runner.ResourceAbilities(world, 0), a => a.Effect == lab.ObjectId).Generates);
        // A generic unrestricted one-cost support provides a legal alter-ego payment target.
        var target = world.CreateCard("01092", world.Seats[0].Hand);
        CardPayment.ReduceNextCardCost(world, world.Seats[0].IdentityCard, 0, 2);
        CardPlay.Play(world, world.Facts, runner, world.Seats[0], target, [lab.ObjectId], []);
        Assert.False(lab.Ready);
        Assert.Equal(DeckType.SupportsArea, target.Area.Type);
        Assert.DoesNotContain(runner.ResourceAbilities(world, 0), a => a.Effect == lab.ObjectId);
        hulk.TurnTo("10001a");
        Assert.Equal(0, StateFields.Modified(world, hulk, "recover", world.Facts, world.Players));
    }

    [Fact]
    [Rule("rr:identity.2")]
    public void ObjectHealthFollowsControllerInBothFormsWhileRetaliateNamesHulk()
    {
        // The title "refers only to the identity with that title".
        var (_, world) = HulkFirstSliceFixture.PlayingHulk(_ => { }, owner: 1);
        world.Abilities = AuthoredCards.Runner();
        var other = world.Seats[0].IdentityCard;
        var hulk = world.Seats[1].IdentityCard;
        var source = world.CreateCard("10010", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
        Assert.Equal(14, DamagePlacement.Health(world, world.Facts, other));
        Assert.Equal(18, DamagePlacement.Health(world, world.Facts, hulk));
        Assert.Equal(1, StateFields.Modified(world, hulk, "retaliate", world.Facts, world.Players));
        Assert.Equal(0, StateFields.Modified(world, other, "retaliate", world.Facts, world.Players));
        hulk.TurnTo("10001b");
        other.TurnTo("01001b");
        Assert.Equal(14, DamagePlacement.Health(world, world.Facts, other));
        Assert.Equal(0, StateFields.Modified(world, hulk, "retaliate", world.Facts, world.Players));
        Discard.Card(world, source, "test", []);
        Assert.Equal(10, DamagePlacement.Health(world, world.Facts, other));
    }

    [Fact]
    [Rule("rr:form-change-form.2")]
    public void RageNamesHulkButOnlyItsControllersFormChangeDiscardsThePhysicalUpgrade()
    {
        // "When a player changes form, only the form changes"; the printed Forced Response discards Rage.
        var (_, world) = HulkFirstSliceFixture.PlayingHulk(_ => { }, owner: 1);
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        var source = world.CreateCard("10009", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
        var hulk = world.Seats[1].IdentityCard;
        Assert.Equal(4, StateFields.Modified(world, hulk, "attack", world.Facts, world.Players));
        Forms.ChangeAndSchedule(world, world.Seats[1], world.Facts, 1);
        Sequence.Finish(world, world.Facts, runner, []);
        Assert.Equal(DeckType.UpgradesArea, source.Area.Type);
        Assert.Equal(0, StateFields.Modified(world, hulk, "attack", world.Facts, world.Players));
        Forms.ChangeAndSchedule(world, world.Seats[0], world.Facts, 1);
        Sequence.Finish(world, world.Facts, runner, []);
        Assert.Equal(DeckType.DiscardPile, source.Area.Type);
        hulk.TurnTo("10001a");
        Assert.Equal(3, StateFields.Modified(world, hulk, "attack", world.Facts, world.Players));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void RagePlayRequiresHeroWithSamePriceControl(bool hero)
    {
        var (_, world) = HulkFirstSliceFixture.PlayingHulk(_ => { }, hero: hero);
        HulkFirstSliceFixture.EmptyHand(world, 0);
        var runner = AuthoredCards.Runner();
        var source = world.CreateCard("10009", world.Seats[0].Hand);
        var payment = world.CreateCard("01003", world.Seats[0].Hand);
        var control = world.CreateCard("01073", world.Seats[0].Hand);
        if (hero)
        {
            CardPlay.Play(world, world.Facts, runner, world.Seats[0], source, [payment.ObjectId], []);
            Assert.Equal(DeckType.UpgradesArea, source.Area.Type);
            payment = world.CreateCard("01003", world.Seats[0].Hand);
        }
        else
        {
            string before = world.Digest().Canonical();
            Assert.Throws<RulesNotImplementedException>(() => CardPlay.Play(world, world.Facts, runner,
                world.Seats[0], source, [payment.ObjectId], []));
            Assert.Equal(before, world.Digest().Canonical());
        }
        CardPlay.Play(world, world.Facts, runner, world.Seats[0], control, [payment.ObjectId], []);
        Assert.Equal(DeckType.SupportsArea, control.Area.Type);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    [Rule("rr:resource.1")]
    public void LimitlessStrengthRetainsPrintedTriplePhysicalButOnlyHeroCanSpend(bool hero)
    {
        // Resources may be generated "by discarding cards from their hand"; printed 10007 restricts form.
        var (_, world) = HulkFirstSliceFixture.PlayingHulk(_ => { }, hero: hero);
        HulkFirstSliceFixture.EmptyHand(world, 0);
        var runner = AuthoredCards.Runner();
        var source = world.CreateCard("10007", world.Seats[0].Hand);
        var target = world.CreateCard("10024", world.Seats[0].Hand);
        Assert.Equal(3, Resources.PrintedCount([source], 'R', world.Facts));
        Assert.Empty(AuthoredCards.Book.On("10007"));
        Assert.Equal(hero, CardPayment.Generators(world, world.Facts, world.Seats[0], runner, target)
            .Any(a => a.Effect == source.ObjectId && a.Generates == "RRR"));
        if (!hero)
        {
            string before = world.Digest().Canonical();
            Assert.Throws<RulesNotImplementedException>(() => CardPlay.Play(world, world.Facts, runner,
                world.Seats[0], target, [source.ObjectId], []));
            Assert.Equal(before, world.Digest().Canonical());
            world.Seats[0].IdentityCard.TurnTo("10001a");
        }
        CardPlay.Play(world, world.Facts, runner, world.Seats[0], target, [source.ObjectId], []);
        Assert.Equal(DeckType.SupportsArea, target.Area.Type);
        Assert.Equal(DeckType.DiscardPile, source.Area.Type);
    }
}
