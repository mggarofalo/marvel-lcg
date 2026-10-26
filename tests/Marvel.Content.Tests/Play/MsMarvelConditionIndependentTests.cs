using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>Independent boundary checks using the unmodified full ability book.</summary>
public sealed class MsMarvelConditionIndependentTests
{
    [Fact]
    [Rule("rr:wild-resource.1.1")]
    public void RedDaggerRejectsEqualWildDeclarationsAndAcceptsDifferentDeclarationsOnRetry()
    {
        // "Each of its wild resources can be declared a different type."
        // Rulings 10293662ffefd5b2 and 2368dda1610740b3 assign payment to
        // the controller and the returned physical ally to its owner.
        var (_, world) = Playing(_ => { }, hero: true,
            heroes: ["spider_man", "captain_marvel"], abilities: AuthoredCards.Runner());
        foreach (var seat in world.Seats)
            foreach (var held in seat.Hand.Cards.ToArray()) Discard.Card(world, held, "test", []);
        var ally = world.CreateCard("05002", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(1), cardOwner: 0));
        var first = world.CreateCard("01055", world.Seats[1].Hand);
        var second = world.CreateCard("01055", world.Seats[1].Hand);
        var enemy = world.TheCardIn(DeckType.VillainArea)!;
        ally.TakeDamage(2);
        BasicPowerResolution.Consequential(world, world.Facts, ally, true, "Attack", []);
        var prompt = Sequence.Work(world, world.Facts, world.Abilities, [])!;
        var offer = Assert.Single(prompt.Affordances);
        Assert.Equal(1, prompt.Player);
        string digest = world.Digest().Canonical();
        var random = world.Random.Generator.GetState();
        var pending = world.Agenda.Outstanding.ToArray();
        int[] ids = [first.ObjectId, second.ObjectId];

        Assert.Throws<RulesNotImplementedException>(() => Sequence.Answer(world, world.Facts,
            world.Abilities, prompt, new Decision(offer.Id, [enemy.ObjectId], ids,
                Allocations: [new(first.ObjectId, 0, "Y"), new(second.ObjectId, 0, "Y")]), []));

        Assert.Equal(digest, world.Digest().Canonical());
        Assert.Equal(random.Index, world.Random.Generator.GetState().Index);
        Assert.Equal(random.Words, world.Random.Generator.GetState().Words);
        Assert.Equal(pending, world.Agenda.Outstanding.ToArray());
        Assert.Same(world.Seats[1].Hand, first.Area);
        Assert.Equal(DeckType.AlliesArea, ally.Area.Type);
        Sequence.Answer(world, world.Facts, world.Abilities, prompt,
            new Decision(offer.Id, [enemy.ObjectId], ids,
                Allocations: [new(first.ObjectId, 0, "Y"), new(second.ObjectId, 0, "B")]), []);
        Assert.Null(Sequence.Work(world, world.Facts, world.Abilities, []));
        Assert.Equal(2, enemy.Damage);
        Assert.Same(world.Seats[0].Hand, ally.Area);
        Assert.Equal(0, ally.Damage);
        Assert.Equal(DeckType.DiscardPile, first.Area.Type);
        Assert.Equal(DeckType.DiscardPile, second.Area.Type);
        Assert.False(world.Agenda.IsBusy);
    }

    [Fact]
    [Rule("rr:maximum-hit-points")]
    public void EnduranceChangesMaximumAndRemainingHealthWithoutHealingExistingDamage()
    {
        // Maximum hit points includes "all 'gets' hit point modifiers".
        var (_, world) = Playing(_ => { }, heroes: ["spider_man", "captain_marvel"],
            abilities: AuthoredCards.Runner());
        var owner = world.Seats[0].IdentityCard;
        var recipient = world.Seats[1].IdentityCard;
        recipient.TakeDamage(5);
        long maximum = Damage.Health(world, world.Facts, recipient);
        long ownerMaximum = Damage.Health(world, world.Facts, owner);
        var card = world.CreateCard("05023", world.Seats[0].Hand);
        var payment = world.CreateCard("01003", world.Seats[0].Hand);
        CardPlay.Play(world, world.Facts, world.Abilities, world.Seats[0], card,
            [payment.ObjectId], [], [recipient.ObjectId]);
        Assert.Equal(5, recipient.Damage);
        Assert.Equal(maximum + 3, Damage.Health(world, world.Facts, recipient));
        Assert.Equal(ownerMaximum, Damage.Health(world, world.Facts, owner));
        recipient.TurnTo("01010b");
        Assert.Equal(maximum + 3 - 5, Damage.Health(world, world.Facts, recipient) - recipient.Damage);
        Discard.Card(world, card, "test", []);
        Assert.Equal(5, recipient.Damage);
        Assert.Equal(maximum - 5, Damage.Health(world, world.Facts, recipient) - recipient.Damage);
        Assert.Equal(0, card.Owner);
        Assert.Same(world.AreaOf(DeckType.DiscardPile, PlayArea.Of(0), cardOwner: 0), card.Area);
    }

    [Fact]
    [Rule("rr:recover-recovery")]
    public void DownTimeFollowsRecipientFormAndActuallyAddsTwoToRecovery()
    {
        // Recovery "heals a number of hit points equal to their REC value".
        var (_, world) = Playing(_ => { }, heroes: ["spider_man", "captain_marvel"],
            abilities: AuthoredCards.Runner());
        var recipient = world.Seats[1].IdentityCard;
        recipient.TurnTo("01010b");
        long recovery = StateFields.Modified(world, recipient, "recover", world.Facts, world.Players);
        recipient.TakeDamage(8);
        var card = world.CreateCard("05033", world.Seats[0].Hand);
        var payment = world.CreateCard("01003", world.Seats[0].Hand);
        CardPlay.Play(world, world.Facts, world.Abilities, world.Seats[0], card,
            [payment.ObjectId], [], [recipient.ObjectId]);
        recipient.TurnTo("01010a");
        Assert.Equal(0, StateFields.Modified(world, recipient, "recover", world.Facts, world.Players));
        recipient.TurnTo("01010b");
        Assert.Equal(recovery + 2, StateFields.Modified(world, recipient, "recover", world.Facts, world.Players));
        Assert.Equal(8, recipient.Damage);
        BasicPowers.BasicRecovery(world, world.Facts, 1, []);
        Assert.Equal(8 - recovery - 2, recipient.Damage);
        Assert.False(recipient.Ready);
        Sequence.Finish(world, world.Facts, world.Abilities, []);
        Discard.Card(world, card, "test", []);
        Assert.Equal(recovery, StateFields.Modified(world, recipient, "recover", world.Facts, world.Players));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void MoraleBoostRequiresActingHeroAndOnlyOffersHeroRecipients(bool hero)
    {
        var runner = AuthoredCards.Runner();
        var (_, world) = Playing(_ => { }, hero: hero,
            heroes: ["spider_man", "captain_marvel"], abilities: runner);
        world.Seats[1].IdentityCard.TurnTo("01010b");
        var card = world.CreateCard("05032", world.Seats[0].Hand);
        var payment = world.CreateCard("01003", world.Seats[0].Hand);
        var actions = runner.Actions(world, 0).Where(a => a.Card == card.ObjectId).ToArray();
        if (!hero)
        {
            Assert.Empty(actions);
            Assert.Same(world.Seats[0].Hand, card.Area);
            return;
        }
        runner.ExecuteAction(world, new PlayerAction(Assert.Single(actions), [payment.ObjectId], []));
        var selection = Sequence.Work(world, world.Facts, runner, [])!;
        Assert.Equal(world.Seats[0].IdentityCard.ObjectId, Assert.Single(selection.Affordances).AnchorId);
        Sequence.Answer(world, world.Facts, runner, selection,
            Decision.Take(selection.Affordances[0].Id), []);
        Assert.Null(Sequence.Work(world, world.Facts, runner, []));
        Assert.Equal(DeckType.DiscardPile, card.Area.Type);
    }
}
