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

// Test-local interpretation of 12005; this fixture does not admit the pack.
public sealed class ResizeSequenceTests
{
    private const string Effect = """{"seq":[{"changeForm":{"player":"you","to":"other-hero"}},{"draw":{"player":"you","count":1}}]}""";

    [Theory]
    [InlineData("12001a", "12001c")]
    [InlineData("12001c", "12001a")]
    [Rule("rr:initiating-abilities.2")]
    public void ResizeWaitsForTheFormResponseBeforeDrawing(string from, string to)
    {
        // rr:initiating-abilities.2: a response initiates immediately after its condition.
        var (world, runner, resize) = Board(from, response: true);
        var seat = world.Seats[0];
        var action = Assert.Single(runner.Actions(world, 0), a => a.Card == resize.ObjectId);
        var events = runner.ExecuteAction(world, new PlayerAction(action, [], [])).Events.ToList();
        var prompt = Sequence.Work(world, world.Facts, runner, events);
        Assert.NotNull(prompt);
        Assert.Equal(to, seat.IdentityCard.FaceId);
        Assert.Empty(seat.Hand.Cards);
        Assert.Single(events.OfType<CardsFlipped>());
        Sequence.Answer(world, world.Facts, runner, prompt, Decision.Decline, events);
        Sequence.Finish(world, world.Facts, runner, events);
        Assert.Single(seat.Hand.Cards);
        Assert.Equal(DeckType.DiscardPile, resize.Area.Type);
        Assert.False(world.Agenda.IsBusy);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:cannot.1")]
    public void IndependentDrawStillResolvesWhenTheFlipIsProhibited(bool prohibitAfterInitiation)
    {
        // rr:cannot.1: the word cannot is absolute; the separate draw remains independent.
        var (world, runner, resize) = Board("12001a", response: false);
        var identity = world.Seats[0].IdentityCard;
        void Prohibit() => world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect,
            FormChangePermissions.ProhibitedField, 1, Card: identity.ObjectId, Affects: identity.ObjectId));
        if (!prohibitAfterInitiation) Prohibit();
        var action = Assert.Single(runner.Actions(world, 0), a => a.Card == resize.ObjectId);
        var events = runner.ExecuteAction(world, new PlayerAction(action, [], [])).Events.ToList();
        if (prohibitAfterInitiation) Prohibit();
        Sequence.Finish(world, world.Facts, runner, events);
        Assert.Equal("12001a", identity.FaceId);
        Assert.Empty(events.OfType<CardsFlipped>());
        Assert.Single(world.Seats[0].Hand.Cards);
        Assert.Equal(DeckType.DiscardPile, resize.Area.Type);
        Assert.False(world.Agenda.IsBusy);
    }

    [Fact]
    public void ResizeRequiresHeroFormAndAtLeastOneEffectThatCanResolve()
    {
        var (alterWorld, alterRunner, alterResize) = Board("12001b", response: false);
        Assert.DoesNotContain(alterRunner.Actions(alterWorld, 0), a => a.Card == alterResize.ObjectId);

        var (world, runner, resize) = Board("12001a", response: false);
        var identity = world.Seats[0].IdentityCard;
        world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect,
            FormChangePermissions.ProhibitedField, 1, Card: identity.ObjectId, Affects: identity.ObjectId));
        foreach (var card in world.Seats[0].Deck.Cards.ToArray())
            World.MoveToTop(card, world.AreaOf(DeckType.RemovedArea));
        Assert.DoesNotContain(runner.Actions(world, 0), a => a.Card == resize.ObjectId);
        var (control, controlRunner, controlResize) = Board("12001a", response: false);
        Assert.Contains(controlRunner.Actions(control, 0), a => a.Card == controlResize.ObjectId);
    }

    [Fact]
    public void QueryPreservesRandomAndPendingWorkAndTheNextCommit()
    {
        var (world, runner, resize) = Board("12001a", response: false);
        var (control, controlRunner, controlResize) = Board("12001a", response: false);
        string before = world.Digest().Canonical();
        var random = world.Random.Generator.GetState();
        long words = world.Random.Generator.WordsConsumed;
        var pending = world.Agenda.Outstanding.ToArray();
        var action = Assert.Single(runner.Actions(world, 0), a => a.Card == resize.ObjectId);
        Assert.Equal(before, world.Digest().Canonical());
        Assert.Equal(random.Index, world.Random.Generator.GetState().Index);
        Assert.Equal(random.Words, world.Random.Generator.GetState().Words);
        Assert.Equal(words, world.Random.Generator.WordsConsumed);
        Assert.Equal(pending, world.Agenda.Outstanding);
        var controlAction = Assert.Single(controlRunner.Actions(control, 0), a => a.Card == controlResize.ObjectId);
        var events = runner.ExecuteAction(world, new PlayerAction(action, [], [])).Events.ToList();
        var controlEvents = controlRunner.ExecuteAction(control, new PlayerAction(controlAction, [], [])).Events.ToList();
        Sequence.Finish(world, world.Facts, runner, events);
        Sequence.Finish(control, control.Facts, controlRunner, controlEvents);
        Assert.Equal(control.Digest().Canonical(), world.Digest().Canonical());
        Assert.Equal(controlEvents.Select(e => e.GetType().Name), events.Select(e => e.GetType().Name));
        Assert.Equal(control.Random.Generator.WordsConsumed, world.Random.Generator.WordsConsumed);
        Assert.Equal(control.Agenda.Outstanding, world.Agenda.Outstanding);
    }

    private static (World World, AbilityRunner Runner, Card Resize) Board(string face, bool response)
    {
        var cards = CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));
        var book = AbilityCatalog.Parse($$$$"""
            {"cards":[{"card":"12005","abilities":[{
              "trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"this","form":"hero"},
              "effect":{{{{Effect}}}}
            }]},{"card":"01006","abilities":[{
              "trigger":{"event":"WhenFormChanged","timing":"Response","subject":"game"},
              "effect":{"draw":{"player":"you","count":1}}
            }]}]}
            """);
        var runner = new AbilityRunner(book);
        var world = new World(cards, 1, 241);
        var seat = world.CreateSeat("p0");
        seat.IdentityCard = world.CreateCard("12001a,12001b,12001c", seat.Hero);
        seat.IdentityCard.TurnTo(face);
        for (int i = 0; i < 8; i++) world.CreateCard("01087", seat.Deck);
        var resize = world.CreateCard("12005", seat.Hand);
        if (response) world.CreateCard("01006", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        world.Abilities = runner;
        return (world, runner, resize);
    }
}
