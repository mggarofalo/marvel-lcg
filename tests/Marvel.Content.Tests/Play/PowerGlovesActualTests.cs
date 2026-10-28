using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class PowerGlovesActualTests
{
    [Fact]
    [Rule("rr:attach-to.3")]
    [Rule("rr:max-maximum.4")]
    [Rule("rr:ownership-and-control.2.1")]
    public void PaidPlayRequiresAnAvengerAllyAndOneCopyPerHostAcrossPlayers()
    {
        // The attach phrase "is checked for legality when the card would be attached".
        // "Max 1 per [game element]" limits copies attached to that element.
        // Attached upgrades "are controlled by that other player".
        var world = ThorActualFixture.Board("06001a", 2);
        var runner = AuthoredCards.Runner();
        var avenger = world.CreateCard("01068", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(1), cardOwner: 1));
        var ownAvenger = world.CreateCard("01068", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        var nonAvenger = world.CreateCard("01002", avenger.Area);
        var gloves = world.CreateCard("12017", world.Seats[0].Hand);
        var payment = world.CreateCard("01088", world.Seats[0].Hand);
        var targets = runner.AttachmentTargets(world, gloves)!;
        Assert.Contains(avenger.ObjectId, targets);
        Assert.Contains(ownAvenger.ObjectId, targets);
        Assert.DoesNotContain(nonAvenger.ObjectId, targets);
        Assert.DoesNotContain(world.Seats[0].IdentityCard.ObjectId, targets);
        CardPlay.Play(world, world.Facts, runner, world.Seats[0], gloves, [payment.ObjectId], [], [avenger.ObjectId]);
        Sequence.Finish(world, world.Facts, runner, []);
        Assert.Equal(DeckType.DiscardPile, payment.Area.Type);
        Assert.Equal(0, gloves.Owner);
        Assert.Equal(PlayArea.Of(1), gloves.Area.PlayArea);
        Assert.Equal(avenger.ObjectId, gloves.Area.Host);
        var duplicate = world.CreateCard("12017", world.Seats[1].Hand);
        targets = runner.AttachmentTargets(world, duplicate)!;
        Assert.DoesNotContain(avenger.ObjectId, targets);
        Assert.Contains(ownAvenger.ObjectId, targets);
        Discard.Card(world, gloves, "test", []);
        Assert.Contains(avenger.ObjectId, runner.AttachmentTargets(world, duplicate)!);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    [Rule("rr:response.1")]
    [Rule("rr:ability.11")]
    [Rule("rr:consequential-damage.1")]
    public void ActualBasicPowersOfferControllerOneOptionalDamageBeforeLethalConsequentialDamage(bool attack, bool accept)
    {
        // Responses are "optional" and players trigger them "on cards they control".
        // Consequential damage follows "abilities ... triggered by the ally attacking or thwarting".
        var world = ThorActualFixture.Board("06001a", 2);
        var runner = AuthoredCards.Runner();
        var ally = world.CreateCard("01068", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(1), cardOwner: 1));
        var gloves = world.CreateCard("12017", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(1), ally.ObjectId, 0));
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        var scheme = world.TheCardIn(DeckType.MainSchemesArea)!;
        scheme.PlaceTokens("k_threat", 3);
        ally.TakeDamage(2);
        AllyBasicPowers.AllyPower(world, world.Facts, ally, attack ? villain : scheme,
            attack ? BasicPowers.AttackVerb : BasicPowers.ThwartVerb, []);
        var response = Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, runner, []));
        Assert.Equal(1, response.Player);
        Assert.Equal(TimingPriority.Response, response.When);
        Assert.True(response.Cancellable);
        Assert.Equal(2, ally.Damage);
        Assert.Equal(DeckType.AlliesArea, ally.Area.Type);
        var offer = Assert.Single(response.Affordances, option => option.AnchorId == gloves.ObjectId);
        Sequence.Answer(world, world.Facts, runner, response, accept ? Decision.Take(offer.Id) : Decision.Decline, []);
        if (accept)
        {
            var target = Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, runner, []));
            Assert.Equal(1, target.Player);
            Assert.Contains(target.Affordances, option => option.Id == villain.ObjectId);
            Assert.DoesNotContain(target.Affordances, option => option.Id == ally.ObjectId);
            Assert.DoesNotContain(target.Affordances, option => option.Id == world.Seats[0].IdentityCard.ObjectId);
            Assert.Equal(DeckType.AlliesArea, ally.Area.Type);
            Sequence.Answer(world, world.Facts, runner, target, Decision.Take(villain.ObjectId), []);
        }
        Assert.Null(Sequence.Work(world, world.Facts, runner, []));
        Assert.Equal((attack ? 2 : 0) + (accept ? 1 : 0), villain.Damage);
        Assert.Equal(attack ? 3 : 2, scheme.Tokens["k_threat"]);
        Assert.Equal(DeckType.DiscardPile, ally.Area.Type);
        Assert.Equal(DeckType.DiscardPile, gloves.Area.Type);
        Assert.Equal(0, gloves.Owner);
        Assert.False(world.Agenda.IsBusy);
    }

    [Fact]
    [Rule("rr:enemy")]
    public void ResponseMayDamageAMinionEngagedWithEitherPlayer()
    {
        // "Enemy refers to a minion or villain."
        var fixture = new AllyThwartCompletionFixture();
        var first = fixture.World.CreateCard("01120", fixture.World.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        var second = fixture.World.CreateCard("01120", fixture.World.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1)));
        var response = fixture.BeginLabeledThwart();
        fixture.Answer(response, Decision.Take(Assert.Single(response.Affordances).Id));
        var target = Assert.IsType<Prompt>(fixture.Work());
        Assert.Contains(target.Affordances, option => option.Id == first.ObjectId);
        Assert.Contains(target.Affordances, option => option.Id == second.ObjectId);
        fixture.Answer(target, Decision.Take(first.ObjectId));
        Assert.Null(fixture.Work());
        Assert.Equal(1, first.Damage);
        Assert.Equal(0, second.Damage);
        Assert.Equal(0, fixture.Villain.Damage);
    }
}
