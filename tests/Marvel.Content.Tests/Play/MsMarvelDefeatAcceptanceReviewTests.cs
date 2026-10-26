using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>Actual Red Dagger checks through the production window decorator.</summary>
public sealed class MsMarvelDefeatAcceptanceReviewTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:triggering-condition.1")]
    [Rule("rr:ownership-and-control.7.2")]
    public void InvalidRecipientAndOwnersPaymentLeaveControllerFreeToRetryOrDecline(bool decline)
    {
        // Each interrupt "can only be triggered once" per occurrence.
        // A departing card is "placed in its owner's equivalent out-of-play area".
        // Red Dagger ruling 10293662ffefd5b2 assigns its payment to the controller.
        var (_, world) = Playing(_ => { }, hero: true,
            heroes: ["spider_man", "captain_marvel"], abilities: AuthoredCards.Runner());
        foreach (var seat in world.Seats)
            foreach (var held in seat.Hand.Cards.ToArray()) Discard.Card(world, held, "test", []);
        var ally = world.CreateCard("05002", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(1), cardOwner: 0));
        var first = world.CreateCard("01055", world.Seats[1].Hand);
        var second = world.CreateCard("01055", world.Seats[1].Hand);
        var foreign = world.CreateCard("01055", world.Seats[0].Hand);
        var enemy = world.TheCardIn(DeckType.VillainArea)!;
        ally.TakeDamage(2);
        BasicPowerResolution.Consequential(world, world.Facts, ally, true, "Attack", []);
        var prompt = Sequence.Work(world, world.Facts, world.Abilities, [])!;
        var offer = Assert.Single(prompt.Affordances);
        Assert.Equal(1, prompt.Player);
        var operation = world.Agenda.Current!.Value.Require<DefeatAbilityOperation>();
        var origin = operation.Window.Origin!;
        var containing = world.Agenda.Occurrence!;
        var ability = Assert.Single(world.Abilities.Waiting(world, origin, WindowKind.Interrupt));
        string digest = world.Digest().Canonical();
        var random = world.Random.Generator.GetState();
        var pending = world.Agenda.Outstanding.ToArray();

        Reject(new(offer.Id, [world.Seats[0].IdentityCard.ObjectId], [first.ObjectId, second.ObjectId],
            Allocations: [new(first.ObjectId, 0, "Y"), new(second.ObjectId, 0, "B")]));
        Reject(new(offer.Id, [enemy.ObjectId], [first.ObjectId, foreign.ObjectId],
            Allocations: [new(first.ObjectId, 0, "Y"), new(foreign.ObjectId, 0, "B")]));

        Sequence.Answer(world, world.Facts, world.Abilities, prompt,
            decline ? Decision.Decline : new(offer.Id, [enemy.ObjectId], [first.ObjectId, second.ObjectId],
                Allocations: [new(first.ObjectId, 0, "Y"), new(second.ObjectId, 0, "B")]), []);
        Assert.Null(Sequence.Work(world, world.Facts, world.Abilities, []));
        Assert.Equal(decline ? 0 : 2, enemy.Damage);
        Assert.Equal(decline ? DeckType.DiscardPile : DeckType.HandsArea, ally.Area.Type);
        Assert.Equal(PlayArea.Of(0), ally.Area.PlayArea);
        Assert.Same(world.Seats[0].Hand, foreign.Area);
        Assert.Equal(decline ? DeckType.HandsArea : DeckType.DiscardPile, first.Area.Type);
        Assert.Equal(decline ? DeckType.HandsArea : DeckType.DiscardPile, second.Area.Type);
        Assert.Equal(decline, origin.MayTrigger(WindowKind.Interrupt, ability));
        Assert.False(world.Agenda.IsBusy);

        void Reject(Decision answer)
        {
            Assert.Throws<RulesNotImplementedException>(() =>
                Sequence.Answer(world, world.Facts, world.Abilities, prompt, answer, []));
            Assert.Equal(digest, world.Digest().Canonical());
            Assert.Equal(random.Index, world.Random.Generator.GetState().Index);
            Assert.Equal(random.Words, world.Random.Generator.GetState().Words);
            Assert.Equal(pending, world.Agenda.Outstanding.ToArray());
            Assert.True(origin.MayTrigger(WindowKind.Interrupt, ability));
            Assert.True(containing.MayTrigger(WindowKind.Interrupt, ability));
            Assert.Empty(origin.Resolutions);
            Assert.Same(world.Seats[1].Hand, first.Area);
            Assert.Same(world.Seats[1].Hand, second.Area);
        }
    }
}
