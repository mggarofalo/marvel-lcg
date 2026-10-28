using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class AllyPowerIndependentReviewTests
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    [Rule("rr:consequential-damage.1")]
    [Rule("rr:ally.1")]
    public void LabeledPowerResolvesResponseThenLethalConsequentialBeforeOuterChoice(bool attack, bool accept)
    {
        // Consequential damage follows "abilities that are triggered by the ally
        // attacking or thwarting"; at zero remaining hit points "it is defeated and discarded".
        var fixture = new IndependentAllyPowerFixture(attack);
        fixture.Ally.TakeDamage(2);
        int hand = fixture.World.Seats[1].Hand.Cards.Count;
        var response = fixture.Begin();
        Assert.Equal(TimingPriority.Response, response.When);
        Assert.Equal(1, response.Player);
        Assert.Equal(2, fixture.Ally.Damage);
        Assert.Equal(DeckType.AlliesArea, fixture.Ally.Area.Type);
        Assert.Equal(hand, fixture.World.Seats[1].Hand.Cards.Count);
        fixture.Answer(response, accept
            ? Decision.Take(Assert.Single(response.Affordances).Id) : Decision.Decline);
        var outer = Assert.IsType<Prompt>(fixture.Work());
        Assert.Equal(2, outer.Affordances.Count);
        Assert.Equal(DeckType.DiscardPile, fixture.Ally.Area.Type);
        Assert.Equal(DeckType.DiscardPile, fixture.Gloves.Area.Type);
        Assert.Equal(hand + (accept ? 1 : 0), fixture.World.Seats[1].Hand.Cards.Count);
        Assert.Equal(attack ? 1 : 0, fixture.Villain.Damage);
        Assert.Equal(attack ? 5 : 4, fixture.Scheme.Tokens["k_threat"]);
        fixture.Answer(outer, Decision.Take(outer.Affordances[0].Id));
        Assert.Null(fixture.Work());
        Assert.Equal(hand + (accept ? 1 : 0) + 1, fixture.World.Seats[1].Hand.Cards.Count);
        Assert.False(fixture.World.Agenda.IsBusy);
    }

    [Fact]
    [Rule("rr:initiating-abilities.2")]
    [Rule("rr:consequential-damage.1")]
    public void NestedResponseThwartCompletesBeforeOuterAllyConsequentialAndChoice()
    {
        // A response is initiated "immediately after that triggering condition becomes true";
        // consequential damage follows resolution of those triggered abilities.
        var fixture = new IndependentAllyPowerFixture(attack: false, nestedThwart: true);
        var response = fixture.Begin();
        Assert.Equal(4, fixture.Scheme.Tokens["k_threat"]);
        Assert.Equal(0, fixture.Ally.Damage);
        fixture.Answer(response, Decision.Take(Assert.Single(response.Affordances).Id));
        var nested = Assert.IsType<Prompt>(fixture.Work());
        Assert.Equal(3, fixture.Scheme.Tokens["k_threat"]);
        Assert.Equal(TimingPriority.Response, nested.When);
        Assert.Equal(0, fixture.Ally.Damage);
        fixture.Answer(nested, Decision.Decline);
        var outer = Assert.IsType<Prompt>(fixture.Work());
        Assert.Equal(2, fixture.Ally.Damage);
        Assert.Equal(2, outer.Affordances.Count);
        fixture.Answer(outer, Decision.Take(outer.Affordances[0].Id));
        Assert.Null(fixture.Work());
        Assert.Equal(2, fixture.Ally.Damage);
        Assert.False(fixture.World.Agenda.IsBusy);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:leaves-play.1")]
    public void LabeledConsequentialDoesNotFollowAReenteredPerformerIntoOuterChoice(bool attack)
    {
        // A card that leaves play "is considered to be a new copy of the card".
        var fixture = new IndependentAllyPowerFixture(attack);
        var response = fixture.Begin();
        var area = fixture.Ally.Area;
        Discard.Card(fixture.World, fixture.Ally, "test", []);
        World.MoveToTop(fixture.Ally, area);
        fixture.Answer(response, Decision.Decline);
        var outer = Assert.IsType<Prompt>(fixture.Work());
        Assert.Equal(0, fixture.Ally.Damage);
        Assert.Equal(attack ? 1 : 0, fixture.Villain.Damage);
        Assert.Equal(attack ? 5 : 4, fixture.Scheme.Tokens["k_threat"]);
        fixture.Answer(outer, Decision.Take(outer.Affordances[0].Id));
        Assert.Null(fixture.Work());
        Assert.Equal(0, fixture.Ally.Damage);
        Assert.False(fixture.World.Agenda.IsBusy);
    }
}
