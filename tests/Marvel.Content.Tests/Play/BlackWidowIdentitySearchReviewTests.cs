using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;
using static Marvel.Content.Tests.Play.BlackWidowIdentitySearchFixture;

namespace Marvel.Content.Tests.Play;

// Independent nonpublic actual-row probes; these do not admit the incomplete pack.
public sealed class BlackWidowIdentitySearchReviewTests
{
    private static readonly string[] PreparationFaces = ["08006", "08008", "08010", "08017", "08018"];

    [Fact]
    [Rule("rr:limit")]
    public void DecliningMissionPrepPreservesItsUseForAnotherPlayInTheSamePhase()
    {
        // "Each copy of an ability with such a limit may be used X times per the specified period."
        var world = Board();
        var seat = world.Seats[1];
        PlayCard(world, "08017", 1);
        var declined = Prompt(world);
        Sequence.Answer(world, world.Facts, world.Abilities!, declined, Decision.Decline, []);
        Finish(world);
        PlayCard(world, "08017", 1);
        var accepted = Prompt(world);
        int ownHand = seat.Hand.Cards.Count;
        int otherHand = world.Seats[0].Hand.Cards.Count;
        Answer(world, accepted, Assert.Single(accepted.Affordances,
            offer => offer.AnchorId == seat.IdentityCard.ObjectId).Id);
        Finish(world);
        Assert.Equal(ownHand + 1, seat.Hand.Cards.Count);
        Assert.Equal(otherHand, world.Seats[0].Hand.Cards.Count);
        Assert.Empty(world.Agenda.Outstanding);
    }

    [Fact]
    [Rule("rr:traits.1")]
    [Rule("rr:ownership-and-control.8")]
    public void ActualWinterSoldierRepricesDistinctPreparationsAfterTraitLossControlAndDeparture()
    {
        // "Some card abilities reference cards that possess or lack specific traits."
        // "You control" refers to cards in play currently under that player's control.
        var world = Board();
        var seat = world.Seats[1];
        var candidate = world.CreateCard("08002", seat.Hand);
        var preparations = PreparationFaces
            .Select(face => world.CreateCard(face,
                world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(1)))).ToArray();
        Assert.Equal(0, Cost());
        World.MoveToTop(preparations[0], world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0)));
        Assert.Equal(0, Cost());
        using (world.Effects.Register(new(EffectSource.LastingEffect,
            Characteristics.Lost + Traits.Granted + "PREPARATION", Affects: preparations[1].ObjectId)))
            Assert.Equal(1, Cost());
        Assert.Equal(0, Cost());
        Discard.Card(world, preparations[1], "test departure", []);
        Assert.Equal(1, Cost());
        World.MoveToTop(preparations[0], world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(1)));
        Assert.Equal(0, Cost());

        long Cost() => CardPayment.CostOf(world, world.Facts, seat, candidate).Amount;
    }

    [Fact]
    [Rule("rr:play-put-into-play.3")]
    [Rule("rr:search.1")]
    public void PutIntoPlayCoulsonSearchExcludesHandForeignDiscardAndNonPreparationDeckCards()
    {
        // "A card that is put into play is not considered to have been played."
        // "If a player finds multiple cards ... the player chooses among those options."
        // Printed Coulson responds to entry, including entry without playing him.
        var world = Board();
        var seat = world.Seats[1];
        var own = world.CreateCard("08017", world.AreaOf(DeckType.DiscardPile, PlayArea.Of(1)));
        var inHand = world.CreateCard("08018", seat.Hand);
        var foreign = world.CreateCard("08017", world.AreaOf(DeckType.DiscardPile, PlayArea.Of(0)));
        var nonPreparation = world.CreateCard("08015", seat.Deck);
        var coulson = world.CreateCard("08011", seat.Hand);
        CardPlay.PutAllyIntoPlay(world, world.Facts, world.Abilities!, coulson, 1, "test entry", []);
        var response = Prompt(world);
        Assert.Equal(1, response.Player);
        Answer(world, response, Assert.Single(response.Affordances,
            offer => offer.AnchorId == coulson.ObjectId).Id);
        var search = Prompt(world);
        Assert.Equal(1, search.Player);
        var offered = Assert.Single(search.Affordances);
        Assert.Equal(own.ObjectId, offered.AnchorId);
        long before = world.Random.Generator.WordsConsumed;
        Answer(world, search, offered.Id);
        Finish(world);
        Assert.Same(seat.Hand, own.Area);
        Assert.Same(seat.Hand, inHand.Area);
        Assert.Same(world.AreaOf(DeckType.DiscardPile, PlayArea.Of(0)), foreign.Area);
        Assert.Same(seat.Deck, nonPreparation.Area);
        Assert.True(world.Random.Generator.WordsConsumed > before);
        Assert.Empty(world.Agenda.Outstanding);
    }
}
