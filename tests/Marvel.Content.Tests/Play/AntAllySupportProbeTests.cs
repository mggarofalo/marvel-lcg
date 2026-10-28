using Marvel.Content.Tests.Cards;
using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

// Actual printed cards on synthetic boards; this does not admit Ant-Man publicly.
public sealed class AntAllySupportProbeTests
{
    [Fact]
    [Rule("rr:identity.4")]
    public void StingerRequiresTheCurrentIdentityTraitWithSameCostControl()
    {
        // Only the identity's "faceup side" is in play.
        var world = ThorActualFixture.Board("06001a");
        var seat = world.Seats[0];
        var stinger = world.CreateCard("12014", seat.Hand);
        var blade = world.CreateCard("21019", seat.Hand);
        world.CreateCard("01003", seat.Hand);
        Assert.Equal("1", CardPayment.Price(world, world.Facts, seat, stinger)!.Cost);
        Assert.Equal("1", CardPayment.Price(world, world.Facts, seat, blade)!.Cost);
        seat.IdentityCard.TurnTo("06001b");
        Assert.Null(CardPayment.Price(world, world.Facts, seat, stinger));
        Assert.Equal("1", CardPayment.Price(world, world.Facts, seat, blade)!.Cost);
        seat.IdentityCard.TurnTo("06001a");
        Assert.Equal("1", CardPayment.Price(world, world.Facts, seat, stinger)!.Cost);
    }

    [Theory]
    [InlineData("12014", false)]
    [InlineData("21019", true)]
    [Rule("rr:ally-limit")]
    [Rule("rr:ability.8.2")]
    public void ActualOneCostAllyEntersAtLimit(string face, bool requiresChoice)
    {
        // The excess choice happens "before abilities that resolve upon entering play".
        // A constant ability becomes active "as soon as its card enters play".
        var world = ThorActualFixture.Board("06001a");
        // Synthetic interpretation of Stinger's printed exclusion; not an authored row.
        var runner = new AbilityRunner(AbilityCatalog.Parse("""
            {"cards":[{"card":"12014","abilities":[{
              "trigger":{"timing":"Constant"},
              "effect":{"grant":{"card":"this","keyword":"ally_limit_excluded"}}
            }]}]}
            """));
        world.Abilities = runner;
        var seat = world.Seats[0];
        var area = world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0);
        world.CreateCard("01067", area);
        world.CreateCard("01068", area);
        world.CreateCard("01041", area);
        var played = world.CreateCard(face, seat.Hand);
        var payment = world.CreateCard("01003", seat.Hand);
        Assert.Equal("1", CardPayment.Price(world, world.Facts, seat, played)!.Cost);
        CardPlay.Play(world, world.Facts, runner, seat, played, [payment.ObjectId], []);
        Assert.Equal(DeckType.AlliesArea, played.Area.Type);
        Assert.Equal(4, area.Cards.Count);
        Assert.Equal(requiresChoice, world.Agenda.Outstanding.Any(step => step.What == Steps.ChooseAllyForLimit));
    }
}
