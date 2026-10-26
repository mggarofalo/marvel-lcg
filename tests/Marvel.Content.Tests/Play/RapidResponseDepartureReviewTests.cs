using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class RapidResponseDepartureReviewTests
{
    [Theory]
    [InlineData(DeckType.HandsArea)]
    [InlineData(DeckType.DiscardPile)]
    [Rule("rr:leaves-play.1")]
    public void DepartureDuringEntryCannotReceiveThePendingDamage(DeckType destination)
    {
        // "When a card leaves play ... it is considered to be a new copy".
        // The synthetic entry choice suspends the printed Rapid Response damage suffix.
        var fixture = new RapidResponseFixture(entryChoice: true);
        fixture.Answer(fixture.Defeat(), fixture.Rapid.ObjectId);
        fixture.Answer(fixture.Work(), fixture.Ally.ObjectId);
        var choice = fixture.Work();
        var area = fixture.World.AreaOf(destination, PlayArea.Of(0), cardOwner: 0);
        World.MoveToTop(fixture.Ally, area);
        int incarnation = fixture.Ally.Incarnation;
        Sequence.Answer(fixture.World, fixture.World.Facts, fixture.Runner, choice,
            Decision.Take(choice.Affordances[0].Id), fixture.Events);
        var completion = fixture.Work();
        Assert.Equal(destination, fixture.Ally.Area.Type);
        Assert.Equal(incarnation, fixture.Ally.Incarnation);
        Assert.Equal(0, fixture.Ally.Damage);
        Assert.Equal(0, fixture.Ally.Tokens["c_arrow"]);
        Assert.Equal(DeckType.DiscardPile, fixture.Rapid.Area.Type);
        Assert.NotNull(fixture.World.Agenda.Occurrence!.ActorFacts!.PreparationAbility);
        Assert.Contains(completion.Affordances,
            offer => offer.AnchorId == fixture.World.Seats[0].IdentityCard.ObjectId);
    }
}
