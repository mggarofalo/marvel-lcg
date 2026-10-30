using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class PymParticlesCardTests
{
    [Theory]
    [InlineData("12001b", false, 3, 0)]
    [InlineData("12001a", true, 3, 1)]
    [InlineData("12001c", true, 1, 0)]
    [Rule("rr:cost.4")]
    public void WhollyExcessStillOffersOnlyInHeroForm(string form, bool offered, int damage, int draws)
    {
        // Excess resources "were not paid" but the physical card was spent.
        var runner = AuthoredCards.Runner();
        var world = ThorActualFixture.Board("06001a");
        world.Abilities = runner;
        var seat = world.Seats[0];
        World.MoveToTop(seat.IdentityCard, world.AreaOf(DeckType.RemovedArea));
        seat.IdentityCard = world.CreateCard("12001a,12001b,12001c", seat.Hero);
        seat.IdentityCard.TurnTo(form);
        seat.IdentityCard.TakeDamage(3);
        var played = world.CreateCard("01069", seat.Hand);
        var spent = world.CreateCard("12006", seat.Hand);
        for (int n = 0; n < 3; n++) world.CreateCard("01087", seat.Deck);
        int held = seat.Hand.Cards.Count;
        Assert.Equal(0, CardPayment.CostOf(world, world.Facts, seat, played).Amount);
        CardPlay.Play(world, world.Facts, runner, seat, played, [spent.ObjectId], []);
        var prompt = Sequence.Work(world, world.Facts, runner, []);
        if (offered)
        {
            var response = Assert.Single(prompt!.Affordances, item => item.AnchorId == spent.ObjectId);
            Sequence.Answer(world, world.Facts, runner, prompt, Decision.Take(response.Id), []);
            Sequence.Finish(world, world.Facts, runner, []);
        }
        else Assert.Null(prompt);
        Assert.Equal(damage, seat.IdentityCard.Damage);
        Assert.Equal(held - 2 + draws, seat.Hand.Cards.Count);
        Assert.Equal(DeckType.DiscardPile, played.Area.Type);
    }


    [Theory]
    [InlineData("12001a", 5, 2)]
    [InlineData("12001c", 1, 0)]
    [Rule("rr:response.2.1")]
    [Rule("rr:initiating-abilities.step.5")]
    [Rule("rr:initiating-abilities.step.6")]
    public void TwoSpentCopiesEachRespondBeforePaidStingerEnters(string form, int damage, int draws)
    {
        // "Multiple copies of a card with a response can each be triggered";
        // step 5 pays costs before step 6 commences the card being played.
        var runner = AuthoredCards.Runner();
        var world = Board(form);
        world.Abilities = runner;
        var seat = world.Seats[0];
        seat.IdentityCard.TakeDamage(5);
        var played = world.CreateCard("12014", seat.Hand);
        var first = world.CreateCard("12006", seat.Hand);
        var second = world.CreateCard("12006", seat.Hand);
        int held = seat.Hand.Cards.Count;
        Assert.Equal(1, CardPayment.CostOf(world, world.Facts, seat, played).Amount);
        CardPlay.Play(world, world.Facts, runner, seat, played, [first.ObjectId, second.ObjectId], []);
        var prompt = Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, runner, []));
        Assert.NotEqual(DeckType.AlliesArea, played.Area.Type);
        var response = Assert.Single(prompt.Affordances, item => item.AnchorId == first.ObjectId);
        Assert.Contains(prompt.Affordances, item => item.AnchorId == second.ObjectId);
        Sequence.Answer(world, world.Facts, runner, prompt, Decision.Take(response.Id), []);
        prompt = Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, runner, []));
        Assert.NotEqual(DeckType.AlliesArea, played.Area.Type);
        Assert.DoesNotContain(prompt.Affordances, item => item.AnchorId == first.ObjectId);
        response = Assert.Single(prompt.Affordances, item => item.AnchorId == second.ObjectId);
        Sequence.Answer(world, world.Facts, runner, prompt, Decision.Take(response.Id), []);
        Sequence.Finish(world, world.Facts, runner, []);
        Assert.Equal(damage, seat.IdentityCard.Damage);
        Assert.Equal(held - 3 + draws, seat.Hand.Cards.Count);
        Assert.Equal(DeckType.AlliesArea, played.Area.Type);
        Assert.Equal(DeckType.DiscardPile, first.Area.Type);
        Assert.Equal(DeckType.DiscardPile, second.Area.Type);
        Assert.False(world.Agenda.IsBusy);
    }

    [Theory]
    [InlineData("12001a")]
    [InlineData("12001c")]
    [Rule("rr:resource.1")]
    [Rule("rr:response")]
    public void OrdinaryDiscardDoesNotRespondAndSpentResponseCanBeDeclined(string form)
    {
        // Resources are generated "to pay costs"; responses "may be resolved".
        var runner = AuthoredCards.Runner();
        var world = Board(form);
        world.Abilities = runner;
        var seat = world.Seats[0];
        seat.IdentityCard.TakeDamage(3);
        var discarded = world.CreateCard("12006", seat.Hand);
        Discard.Card(world, discarded, "test", []);
        var played = world.CreateCard("12014", seat.Hand);
        var spent = world.CreateCard("12006", seat.Hand);
        int held = seat.Hand.Cards.Count;
        CardPlay.Play(world, world.Facts, runner, seat, played, [spent.ObjectId], []);
        var prompt = Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, runner, []));
        Assert.Contains(prompt.Affordances, item => item.AnchorId == spent.ObjectId);
        Assert.DoesNotContain(prompt.Affordances, item => item.AnchorId == discarded.ObjectId);
        Sequence.Answer(world, world.Facts, runner, prompt, Decision.Decline, []);
        Sequence.Finish(world, world.Facts, runner, []);
        Assert.Equal(3, seat.IdentityCard.Damage);
        Assert.Equal(held - 2, seat.Hand.Cards.Count);
        Assert.Equal(DeckType.AlliesArea, played.Area.Type);
        Assert.False(world.Agenda.IsBusy);
    }

    [Theory]
    [InlineData("12001a", 3, 1)]
    [InlineData("12001c", 1, 0)]
    [Rule("rr:response.1")]
    public void ContributingPlayerResolvesActualEffectOnTheirOwnIdentityOrHand(string form, int damage, int draws)
    {
        // Players can only trigger responses on cards they control.
        var runner = AuthoredCards.Runner();
        var world = ThorActualFixture.Board("06001a", 2);
        world.Abilities = runner;
        var first = world.Seats[0];
        var contributor = world.Seats[1];
        World.MoveToTop(contributor.IdentityCard, world.AreaOf(DeckType.RemovedArea));
        contributor.IdentityCard = world.CreateCard("12001a,12001b,12001c", contributor.Hero);
        contributor.IdentityCard.TurnTo(form);
        first.IdentityCard.TakeDamage(3);
        contributor.IdentityCard.TakeDamage(3);
        for (int n = 0; n < 3; n++) world.CreateCard("01087", contributor.Deck);
        var played = world.CreateCard("21019", first.Hand);
        world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect, "alliance", 1, Affects: played.ObjectId));
        var spent = world.CreateCard("12006", contributor.Hand);
        int firstHeld = first.Hand.Cards.Count;
        int contributorHeld = contributor.Hand.Cards.Count;
        CardPlay.Play(world, world.Facts, runner, first, played, [spent.ObjectId], []);
        var prompt = Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, runner, []));
        Assert.Equal(1, prompt.Player);
        Assert.NotEqual(DeckType.AlliesArea, played.Area.Type);
        var response = Assert.Single(prompt.Affordances, item => item.AnchorId == spent.ObjectId);
        Sequence.Answer(world, world.Facts, runner, prompt, Decision.Take(response.Id), []);
        Sequence.Finish(world, world.Facts, runner, []);
        Assert.Equal(3, first.IdentityCard.Damage);
        Assert.Equal(damage, contributor.IdentityCard.Damage);
        Assert.Equal(firstHeld - 1, first.Hand.Cards.Count);
        Assert.Equal(contributorHeld - 1 + draws, contributor.Hand.Cards.Count);
        Assert.Equal(DeckType.AlliesArea, played.Area.Type);
        Assert.Equal(DeckType.DiscardPile, spent.Area.Type);
        Assert.False(world.Agenda.IsBusy);
    }

    private static World Board(string form)
    {
        var world = ThorActualFixture.Board("06001a");
        var seat = world.Seats[0];
        World.MoveToTop(seat.IdentityCard, world.AreaOf(DeckType.RemovedArea));
        seat.IdentityCard = world.CreateCard("12001a,12001b,12001c", seat.Hero);
        seat.IdentityCard.TurnTo(form);
        for (int n = 0; n < 4; n++) world.CreateCard("01087", seat.Deck);
        return world;
    }
}
