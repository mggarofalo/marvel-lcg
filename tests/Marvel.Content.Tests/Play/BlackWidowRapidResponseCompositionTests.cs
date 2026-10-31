using Marvel.Cards.Run;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

// Actual authored rows; Black Widow remains outside public admission.
public sealed class BlackWidowRapidResponseCompositionTests
{
    [Theory]
    [InlineData(0, 0, false)]
    [InlineData(0, 1, false)]
    [InlineData(1, 0, false)]
    [InlineData(1, 1, false)]
    [InlineData(1, 1, true)]
    [Rule("rr:leaves-play.1")]
    [Rule("rr:damage.step.9")]
    public void ActualRapidResponseReturnsOnlyTheOwnDefeatedAllyOrDeclinesWithoutCost(int controller, int owner, bool decline)
    {
        // "When a card leaves play, there is no memory of its previous state"
        // and responses to defeat occur after "Discarding of a defeated character."
        // 08031: "After an ally you control is defeated"; return "that ally"
        // "from your discard pile and deal 1 damage to it."
        var runner = AuthoredCards.Runner();
        var world = ThorActualFixture.Board("06001a", 2);
        var seat = world.Seats[1];
        World.MoveToTop(seat.IdentityCard, world.AreaOf(DeckType.RemovedArea));
        var hero = seat.IdentityCard = world.CreateCard("08001a,08001b", seat.Hero);
        hero.Exhaust();
        var suit = world.CreateCard("08009",
            world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(1), cardOwner: 1));
        var rapid = world.CreateCard("08031",
            world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(1), cardOwner: 1));
        // Hawkeye is a printed 3-HP ally, with four arrow counters on every entry.
        var ally = world.CreateCard("01066",
            world.AreaOf(DeckType.AlliesArea, PlayArea.Of(controller), cardOwner: owner));
        var other = world.CreateCard("01066",
            world.AreaOf(DeckType.DiscardPile, PlayArea.Of(controller), cardOwner: controller));
        ally.Exhaust();
        ally.PlaceTokens("c_arrow", 1);
        int incarnation = ally.Incarnation;
        world.Abilities = runner;
        var events = new List<GameEvent>();
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        DamagePlacement.Resolve(world, world.Facts, new(villain, ally, 3, "Fixture", "Damage"), events);
        var prompt = Sequence.Work(world, world.Facts, runner, events);
        if (controller != 1 || owner != 1)
        {
            Assert.Null(prompt);
            Assert.Equal(DeckType.DiscardPile, ally.Area.Type);
            Assert.Equal(DeckType.UpgradesArea, rapid.Area.Type);
            return;
        }
        Assert.NotNull(prompt);
        Assert.Equal(1, prompt.Player);
        Assert.True(prompt.Cancellable);
        var offer = Assert.Single(prompt.Affordances, a => a.AnchorId == rapid.ObjectId);
        Sequence.Answer(world, world.Facts, runner, prompt,
            decline ? Decision.Decline : Decision.Take(offer.Id), events);
        if (decline)
        {
            Assert.Equal(DeckType.DiscardPile, ally.Area.Type);
            Assert.Equal(DeckType.UpgradesArea, rapid.Area.Type);
            Assert.Null(Sequence.Work(world, world.Facts, runner, events));
            return;
        }
        var preparation = Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, runner, events));
        var receipt = Assert.IsType<ResolvedPreparationAbility>(world.Agenda.Occurrence!.ActorFacts!.PreparationAbility);
        Assert.Equal("08031", receipt.Face);
        Assert.Equal(1, receipt.Controller);
        Assert.Contains(preparation.Affordances, a => a.AnchorId == hero.ObjectId);
        Assert.Contains(preparation.Affordances, a => a.AnchorId == suit.ObjectId);
        Assert.Equal(DeckType.AlliesArea, ally.Area.Type);
        Assert.Equal(PlayArea.Of(1), ally.Area.PlayArea);
        Assert.True(ally.Incarnation > incarnation);
        Assert.True(ally.Ready);
        Assert.Equal(1, ally.Damage);
        Assert.Equal(4, ally.Tokens["c_arrow"]);
        Assert.Equal(DeckType.DiscardPile, other.Area.Type);
        Assert.Equal(DeckType.DiscardPile, rapid.Area.Type);
        Sequence.Answer(world, world.Facts, runner, preparation,
            Decision.Take(Assert.Single(preparation.Affordances, a => a.AnchorId == suit.ObjectId).Id), events);
        Assert.True(hero.Ready);
        Assert.False(suit.Ready);
        var widowmaker = Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, runner, events));
        Assert.Contains(widowmaker.Affordances, a => a.AnchorId == hero.ObjectId);
        Sequence.Answer(world, world.Facts, runner, widowmaker, Decision.Decline, events);
        Assert.Null(Sequence.Work(world, world.Facts, runner, events));
    }

}
