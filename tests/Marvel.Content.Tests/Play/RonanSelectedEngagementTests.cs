using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>Production Ronan phase body on nonpublic boards; synthetic Vulture isolates engagement hooks.</summary>
public sealed class RonanSelectedEngagementTests : CoreCardTestBase
{
    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    [Rule("rr:first-player.1")]
    [Rule("rr:engage.3")]
    [Rule("rr:form-change-form.5")]
    public void FirstPlayerSelectsEveryTiedHeroIncludingTheCurrentEngagement(int selected)
    {
        // "the first player selects among the eligible options"; abilities cannot
        // cause a minion "to engage with the same player again". Alter-egos do not
        // interact with effects referring to their hero.
        var world = Board();
        world.FirstPlayer = 1;
        world.Seats[0].IdentityCard.TakeDamage(4);
        world.Seats[2].IdentityCard.TakeDamage(4);
        world.Seats[1].IdentityCard.TurnTo("01001b");
        world.Seats[1].IdentityCard.TakeDamage(9);
        var ally = world.CreateCard("01083", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(3), cardOwner: 3));
        ally.TakeDamage(2);
        var ronan = Ronan(world, 0);
        ronan.TakeDamage(2);
        ronan.Exhaust();
        ronan.PlaceTokens("c_test", 3);
        Statuses.Give(world, ronan, Statuses.Confused);
        var attachment = world.CreateCard("01101", world.AreaOf(DeckType.UpgradesArea,
            PlayArea.Of(0), ronan.ObjectId));
        var retained = ronan.InstanceState;
        int incarnation = ronan.Incarnation;
        var runner = Runner();
        Begin(world);
        var events = new List<GameEvent>();
        var prompt = Assert.IsType<Prompt>(Sequence.Work(world, Cards, runner, events));
        Assert.Equal(1, prompt.Player);
        Assert.Equal(new[] { world.Seats[0].IdentityCard.ObjectId, world.Seats[2].IdentityCard.ObjectId },
            prompt.Affordances.Select(a => a.Id).Order());
        Sequence.Answer(world, Cards, runner, prompt,
            Decision.Take(world.Seats[selected].IdentityCard.ObjectId), events);
        Finish(world, runner, events);
        Assert.Equal(PlayArea.Of(selected), ronan.Area.PlayArea);
        Assert.Same(retained, ronan.InstanceState);
        Assert.Equal(incarnation, ronan.Incarnation);
        Assert.Equal(2, ronan.Damage);
        Assert.False(ronan.Ready);
        Assert.Equal(3, ronan.Tokens["c_test"]);
        Assert.True(Statuses.Has(world, ronan, Statuses.Confused));
        Assert.False(Statuses.Has(world, ronan, Statuses.Tough));
        Assert.Equal(ronan.ObjectId, attachment.Area.Host);
        Assert.Equal(selected == 0 ? 0 : 1, events.OfType<CardsMoved>().Count(e =>
            e.Cards.Any(c => c.Card == ronan.ObjectId)));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:engage.3")]
    public void UniqueCurrentMinimumAndNoHeroesDoNotMove(bool noHeroes)
    {
        // Already engaged minions cannot engage the same player again.
        var world = Board();
        world.Seats[2].IdentityCard.TakeDamage(5);
        if (noHeroes)
            foreach (var seat in world.Seats) seat.IdentityCard.TurnTo("01001b");
        var ronan = Ronan(world, 2);
        var runner = Runner();
        string before = world.Digest().Canonical();
        Begin(world);
        var events = new List<GameEvent>();
        Finish(world, runner, events);
        Assert.Equal(before, world.Digest().Canonical());
        Assert.Empty(events.OfType<CardsMoved>());
    }

    [Fact]
    public void UniqueMinimumMovesToTheHeroRatherThanResolverOrFirstPlayer()
    {
        var world = Board();
        world.FirstPlayer = 1;
        world.Seats[2].IdentityCard.TakeDamage(5);
        var ronan = Ronan(world, 3);
        var runner = Runner();
        Begin(world);
        var events = new List<GameEvent>();
        Finish(world, runner, events);
        Assert.Equal(PlayArea.Of(2), ronan.Area.PlayArea);
        Assert.Single(events.OfType<CardsMoved>(), e => e.Cards.Any(c => c.Card == ronan.ObjectId));
    }

    [Theory]
    [InlineData("form")]
    [InlineData("departure")]
    [InlineData("incarnation")]
    public void SelectedHeroBecomingUnavailableDoesNotUseTheResolver(string change)
    {
        var world = Board();
        world.FirstPlayer = 1;
        world.Seats[0].IdentityCard.TakeDamage(4);
        world.Seats[2].IdentityCard.TakeDamage(4);
        var ronan = Ronan(world, 3);
        var runner = Runner();
        Begin(world);
        var prompt = Assert.IsType<Prompt>(Sequence.Work(world, Cards, runner, []));
        Sequence.Answer(world, Cards, runner, prompt,
            Decision.Take(world.Seats[2].IdentityCard.ObjectId), []);
        var hero = world.Seats[2].IdentityCard;
        if (change == "form") hero.TurnTo("01001b");
        else
        {
            World.MoveToTop(hero, world.AreaOf(change == "incarnation"
                ? DeckType.DiscardPile : DeckType.RemovedArea, PlayArea.Of(2), cardOwner: 2));
            if (change == "incarnation") World.MoveToTop(hero, world.Seats[2].Hero);
        }
        var events = new List<GameEvent>();
        Finish(world, runner, events);
        Assert.Equal(PlayArea.Of(3), ronan.Area.PlayArea);
        Assert.Empty(events.OfType<CardsMoved>());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:quickstrike.1")]
    [Rule("rr:engage.3")]
    public void SelectedSeatReceivesQuickstrikeAndThorResponseOnlyAfterActualMovement(bool sameSeat)
    {
        // Quickstrike: "After this minion engages a player, it attacks that player."
        // Same-player engagement cannot repeat; transfer does not enter play anew.
        var world = Board(thor: true);
        world.Seats[2].IdentityCard.TakeDamage(9);
        for (int i = 0; i < 5; i++) world.CreateCard("01088", world.Seats[2].Deck);
        var minion = world.CreateCard("01167", world.AreaOf(DeckType.EngagedEnemiesArea,
            PlayArea.Of(sameSeat ? 2 : 3)));
        minion.TakeDamage(1);
        int incarnation = minion.Incarnation;
        var runner = Runner("01167");
        var hawkeye = world.CreateCard("01066", world.AreaOf(DeckType.AlliesArea,
            PlayArea.Of(2), cardOwner: 2));
        Reveal.EnterPlay(world, Cards, hawkeye, [], abilities: runner);
        runner.ExecuteReveal(world, minion, 0);
        var events = new List<GameEvent>();
        int responses = 0;
        for (int i = 0; i < 40; i++)
        {
            var prompt = Sequence.Work(world, Cards, runner, events);
            if (prompt is null) break;
            Assert.DoesNotContain(prompt.Affordances, a =>
                a.AnchorId == hawkeye.ObjectId && a.Timing == AbilityType.Response);
            var thor = prompt.Affordances.SingleOrDefault(a =>
                a.AnchorId == world.Seats[2].IdentityCard.ObjectId && a.Timing == AbilityType.Response);
            if (thor is not null)
            {
                responses++;
                Assert.Equal(12, world.Seats[2].IdentityCard.Damage);
                Assert.Equal(minion.ObjectId, world.Agenda.Occurrence!.Subject);
                Assert.Equal(2, world.Agenda.Occurrence.Player);
                Assert.False(world.Agenda.Occurrence.Is(Steps.CardEntersPlay));
            }
            else Assert.True(prompt.Cancellable, prompt.Description);
            Sequence.Answer(world, Cards, runner, prompt,
                thor is not null ? Decision.Take(thor.Id) : Decision.Decline, events);
        }
        Assert.Empty(world.Agenda.Outstanding);
        int expectedEngagements = sameSeat ? 0 : 1;
        Assert.Equal(expectedEngagements, responses);
        Assert.Equal(9 + 3 * expectedEngagements, world.Seats[2].IdentityCard.Damage);
        Assert.Equal(2 * expectedEngagements, world.Seats[2].Hand.Cards.Count);
        Assert.Equal(PlayArea.Of(2), minion.Area.PlayArea);
        Assert.Equal(1, minion.Damage);
        Assert.Equal(incarnation, minion.Incarnation);
        Assert.Equal(4, hawkeye.Tokens["c_arrow"]);
    }

    private static World Board(bool thor = false)
    {
        var world = new World(Cards, 4);
        for (int player = 0; player < 4; player++)
        {
            var seat = world.CreateSeat($"p{player}");
            seat.IdentityCard = world.CreateCard(thor && player == 2 ? "06001a,06001b" : "01001a,01001b", seat.Hero);
        }
        return world;
    }

    private static Card Ronan(World world, int player) => world.CreateCard("90001",
        world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(player)));

    private static AbilityRunner Runner(string face = "90001")
    {
        if (face == "90001") return AuthoredCards.Runner();
        var local = AbilityCatalog.Parse("""
        {"cards":[{"card":"90001","abilities":[{
          "trigger":{"event":"WhenCardRevealed","timing":"WhenRevealed","subject":"this"},
          "effect":{"chooseCard":{"chooser":"firstPlayer",
            "from":{"minBy":{"of":{"query":"heroes"},"by":"remainingHealth"}},
            "effect":{"eachPlayer":{"order":"playerOrder","effect":{"if":{"test":{"and":[
              {"isYourIdentity":"chosen"},{"isInPlay":"chosen"},
              {"inForm":{"player":"you","form":"hero"}}]},
              "then":{"putIntoPlay":{"card":"this","where":"engagedWithYou"}}
            }}}}}}
        }]}]}
        """.Replace("90001", face, StringComparison.Ordinal));
        return new AbilityRunner(AuthoredCards.Book with
        {
            Abilities = [.. AuthoredCards.Book.Abilities.Where(a => !local.Authored.Contains(a.Card)), .. local.Abilities],
            Authored = AuthoredCards.Book.Authored.Union(local.Authored).ToHashSet(StringComparer.Ordinal),
        });
    }

    private static void Begin(World world) => world.Agenda.Add(new PhaseStep(
        new PhaseTransitionOperation(PhaseTransitionKind.BeginVillainPhase), Round: 1, Number: 1, Index: -1));

    private static void Finish(World world, AbilityRunner runner, List<GameEvent> events)
    {
        for (int i = 0; i < 40; i++)
        {
            var prompt = Sequence.Work(world, Cards, runner, events);
            if (prompt is null)
            {
                Assert.Empty(world.Agenda.Outstanding);
                return;
            }
            Assert.Fail($"Unexpected prompt: {prompt.Asking} {prompt.Description}");
        }
        Assert.Fail("selected engagement did not finish");
    }
}
