using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class AbilityFormTimingReviewTests
{
    [Fact]
    [Rule("rr:interrupt.3")]
    [Rule("rr:then.2")]
    public void NestedInterruptChangingToTheDestinationLeavesTheOuterFlipIneffective()
    {
        // Synthetic boundary: rr:interrupt.3 resolves the nested change "before
        // that triggering condition resolves". rr:then.2 forbids a dependent
        // tail when the outer flip has no remaining change to perform.
        var (world, runner, source, observer, villain) = Board("""
            {"changeForm":{"player":"you","to":"hero"}}
            """, nestedCost: true);
        Start(world, runner, source);
        var events = new List<GameEvent>();
        Sequence.Finish(world, world.Facts, runner, events);
        Assert.Equal("01019a", world.Seats[0].IdentityCard.FaceId);
        Assert.False(observer.Ready);
        Assert.Single(events.OfType<CardsFlipped>());
        Assert.Equal(1, villain.Damage); // Independent suffix still resolves.
        Assert.Empty(world.Seats[0].Hand.Cards); // Outer dependent draw does not.
        Assert.False(world.Agenda.IsBusy);
    }

    [Fact]
    [Rule("rr:player-elimination.step.5")]
    [Rule("rr:player-elimination.5")]
    public void EliminationDuringTheInterruptCannotFlipTheRemovedIdentity()
    {
        // rr:player-elimination.step.5 removes the identity and other owned
        // elements. rr:player-elimination.5 still says "resolve the entire ability".
        var (world, runner, source, _, villain) = Board("""
            {"dealDamage":{"cards":"you","amount":15}}
            """);
        var identity = world.Seats[0].IdentityCard;
        Start(world, runner, source);
        var events = new List<GameEvent>();
        Sequence.Finish(world, world.Facts, runner, events);
        Assert.True(world.Seats[0].Eliminated);
        Assert.Equal(DeckType.RemovedArea, identity.Area.Type);
        Assert.Equal("01019b", identity.FaceId);
        Assert.Empty(events.OfType<CardsFlipped>());
        Assert.Equal(1, villain.Damage);
        Assert.False(world.Agenda.IsBusy);
    }

    [Fact]
    [Rule("rr:cancel.5.1")]
    [Rule("rr:then.2")]
    public void CancellationSuppressesTheDependentDrawButKeepsAnIndependentSuffix()
    {
        // rr:cancel.5.1: the canceled effect "is not considered to have occurred".
        var (world, runner, source, _, villain) = Board("""
            {"cancelOccurrence":"trigger"}
            """);
        Start(world, runner, source);
        var events = new List<GameEvent>();
        Sequence.Finish(world, world.Facts, runner, events);
        Assert.Equal("01019b", world.Seats[0].IdentityCard.FaceId);
        Assert.Empty(events.OfType<CardsFlipped>());
        Assert.Empty(world.Seats[0].Hand.Cards);
        Assert.Equal(1, villain.Damage);
        Assert.False(source.Ready);
        Assert.False(world.Agenda.IsBusy);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:ability.1")]
    [Rule("rr:cannot.1")]
    [Rule("rr:then.2")]
    public void ADeferredFlipRevalidatesItsOriginalIncarnationAndPermission(bool prohibited)
    {
        // Synthetic scheduler boundary: pending work cannot borrow a new copy.
        // rr:ability.1 restricts interaction to in-play cards; rr:cannot.1 gives
        // the ability with "cannot" precedence. Neither grants a successful flip.
        var (world, runner, source, _, villain) = Board("""
            {"heal":{"card":"you","amount":1}}
            """);
        Start(world, runner, source);
        var identity = world.Seats[0].IdentityCard;
        if (prohibited)
            world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect,
                FormChangePermissions.ProhibitedField, 1, Card: identity.ObjectId, Affects: identity.ObjectId));
        else
        {
            var originalArea = identity.Area;
            int originalIncarnation = identity.Incarnation;
            World.MoveToTop(identity, world.AreaOf(DeckType.DiscardPile, PlayArea.Of(0), cardOwner: 0));
            World.MoveToTop(identity, originalArea);
            Assert.NotEqual(originalIncarnation, identity.Incarnation);
        }
        var events = new List<GameEvent>();
        Sequence.Finish(world, world.Facts, runner, events);
        Assert.Equal("01019b", identity.FaceId);
        Assert.Empty(events.OfType<CardsFlipped>());
        Assert.Empty(world.Seats[0].Hand.Cards);
        Assert.Equal(1, villain.Damage);
        Assert.False(world.Agenda.IsBusy);
    }

    private static void Start(World world, AbilityRunner runner, Card source)
    {
        var action = Assert.Single(runner.Actions(world, 0), offer => offer.Card == source.ObjectId);
        runner.ExecuteAction(world, new PlayerAction(action, [], []));
        Assert.Equal("01019b", world.Seats[0].IdentityCard.FaceId);
        Assert.False(source.Ready);
    }

    private static (World World, AbilityRunner Runner, Card Source, Card Observer, Card Villain)
        Board(string interruption, bool nestedCost = false)
    {
        var local = AbilityCatalog.Parse($$$$$"""
            {"cards":[{"card":"01006","abilities":[{
              "trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"this"},
              "cost":{"exhaust":"this"},
              "effect":{"seq":[
                {"then":{"effect":{"changeForm":{"player":"you","to":"hero"}},
                  "then":{"draw":{"player":"you","count":1}}}},
                {"dealDamage":{"cards":{"query":"villain"},"amount":1}}]}
            }]},{"card":"01092","abilities":[{
              "trigger":{"event":"WhenFormChanged","timing":"ForcedInterrupt","subject":"game"},
              {{{{{(nestedCost ? "\"cost\":{\"exhaust\":\"this\"}," : "")}}}}}
              "effect":{{{{{interruption}}}}}
            }]}]}
            """);
        var runner = new AbilityRunner(local);
        var cards = CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));
        var world = new World(cards, 2, 241);
        var seat = world.CreateSeat("p0");
        seat.IdentityCard = world.CreateCard("01019a,01019b", seat.Hero);
        seat.IdentityCard.TurnTo("01019b");
        var neighbour = world.CreateSeat("p1");
        neighbour.IdentityCard = world.CreateCard("01001a,01001b", neighbour.Hero);
        var villain = world.CreateCard("04093", world.AreaOf(DeckType.VillainArea));
        var source = world.CreateCard("01006", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        var observer = world.CreateCard("01092", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        world.CreateCard("01087", seat.Deck);
        world.Abilities = runner;
        return (world, runner, source, observer, villain);
    }
}
