using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Rules.Tests.Play;

/// <summary>The lifecycle transitions cards may answer.</summary>
public sealed class LifecycleOccurrenceTests
{
    [Rule("rr:form-change-form.1")]
    [Rule("rr:after")]
    [Fact]
    public void ChangingFormSchedulesTheResponseAfterTheNewFaceIsShowing()
    {
        var facts = new Facts();
        var world = Board(facts);
        var identity = world.Seats[0].IdentityCard;

        string was = Forms.ChangeAndSchedule(world, world.Seats[0], facts, round: 3);

        Assert.Equal("alterego", was);
        Assert.Equal("hero", identity.FaceId);
        var step = Assert.Single(world.Agenda.Outstanding);
        Assert.Equal(Steps.FormChanged, step.What);
        Assert.Equal(identity.ObjectId, step.Subject);
        Assert.Equal(0, step.Seat);
    }

    [Rule("rr:ability.11")]
    [Rule("rr:after")]
    [Fact]
    public void AVoluntaryFormChangeOffersTheNewFacesOptionalResponse()
    {
        var facts = new Facts();
        var world = Board(facts);
        var abilities = new FormResponse(world.Seats[0].IdentityCard.ObjectId);
        var game = Game.Begin(world, facts, abilities);
        game.Resolve(Decision.Decline);
        int change = game.Pending!.Affordances.Single(option => option.Verb == Game.ChangeForm).Id;

        var result = game.Resolve(Decision.Take(change));

        Assert.Equal("hero", world.Seats[0].IdentityCard.FaceId);
        Assert.NotNull(result.Prompt);
        Assert.Equal(TimingPriority.Response, result.Prompt.When);
        Assert.True(result.Prompt.Cancellable);
    }

    [Rule("rr:triggering-condition.2")]
    [Fact]
    public void PlayingACardCreatesOneOccurrenceForPlayedAndEnteredPlay()
    {
        var facts = new Facts();
        var world = Board(facts);
        var seat = world.Seats[0];
        var ally = world.CreateCard("ally", seat.Hand);

        CardPlay.Play(world, facts, new NoCardAbilities(), seat, ally, [], []);

        var step = Assert.Single(world.Agenda.Outstanding);
        Assert.Equal(Steps.CardPlayed, step.What);
        Assert.Equal([Steps.CardPlayed, Steps.CardEntersPlay], step.Conditions);
        Assert.Equal(ally.ObjectId, step.Subject);
    }

    [Rule("rr:ability.11")]
    [Rule("rr:response")]
    [Fact]
    public void AnOptionalEntersPlayResponseIsOfferedByTheAgenda()
    {
        var facts = new Facts();
        var world = Board(facts);
        var seat = world.Seats[0];
        var ally = world.CreateCard("ally", seat.Hand);
        var abilities = new EntryResponse(ally.ObjectId);
        ally.PlaceTokens("c_arrow", 2);

        CardPlay.Play(world, facts, abilities, seat, ally, [], []);
        var asked = Sequence.Work(world, facts, abilities, []);

        Assert.NotNull(asked);
        Assert.True(asked.Cancellable);
        Assert.Equal(TimingPriority.Response, asked.When);
        Assert.Equal(ally.ObjectId, Assert.Single(asked.Affordances).AnchorId);
    }

    [Rule("rr:enters-play")]
    [Fact]
    public void ARevealedCardAddsEntryToTheRevealOccurrence()
    {
        var facts = new Facts();
        var world = Board(facts);
        var minion = world.CreateCard("minion", world.AreaOf(DeckType.RevealingArea));
        var occurrence = new Occurrence(
            7, [Steps.CardRevealed], Subject: minion.ObjectId, Player: 0);

        Reveal.Resolve(world, facts, minion, 0, [], occurrence);

        Assert.Equal([Steps.CardRevealed, Steps.CardEntersPlay], occurrence.Conditions);
        Assert.Equal(DeckType.EngagedEnemiesArea, minion.Area.Type);
    }

    [Rule("rr:enters-play")]
    [Fact]
    public void CardSpecificEntryStateExistsBeforeTheResponseWindow()
    {
        var facts = new Facts();
        var world = Board(facts);
        var seat = world.Seats[0];
        var ally = world.CreateCard("ally", seat.Hand);
        var abilities = new EntryResponse(ally.ObjectId);
        world.Abilities = abilities;

        CardPlay.Play(world, facts, abilities, seat, ally, [], []);
        var asked = Sequence.Work(world, facts, abilities, []);

        Assert.NotNull(asked);
        Assert.Equal(4, ally.Tokens["c_arrow"]);
    }

    private static World Board(Facts facts)
    {
        var world = new World(facts, players: 1);
        world.CreateSeat("p0");
        world.Seats[0].IdentityCard =
            world.CreateCard("alterego,hero", world.Seats[0].Hero);
        return world;
    }

    private sealed class EntryResponse(int card) : NoCardAbilities
    {
        public override IReadOnlyList<GameEvent> EntersPlay(World world, Card entered)
        {
            long before = entered.Tokens.GetValueOrDefault("c_arrow");
            entered.PlaceTokens("c_arrow", 4 - before);
            return [];
        }

        public override IReadOnlyList<PendingAbility> Waiting(
            World world, Occurrence occurrence, WindowKind window) =>
            window == WindowKind.Response
            && occurrence.Is(Steps.CardEntersPlay)
            && occurrence.Subject == card
            && world.Cards[card].Tokens.GetValueOrDefault("c_arrow") == 4
                ? [new PendingAbility(card, AbilityType.Response, 0)]
                : [];

        public override Affordance Describe(World world, PendingAbility ability) =>
            new(1, "Response", ability.Card, 0, "entry response");
    }

    private sealed class FormResponse(int card) : NoCardAbilities
    {
        public override IReadOnlyList<PendingAbility> Waiting(
            World world, Occurrence occurrence, WindowKind window) =>
            window == WindowKind.Response
            && occurrence.Is(Steps.FormChanged)
            && occurrence.Subject == card
            && world.Cards[card].FaceId == "hero"
                ? [new PendingAbility(card, AbilityType.Response, 0)]
                : [];

        public override Affordance Describe(World world, PendingAbility ability) =>
            new(2, "Response", ability.Card, 0, "form response");
    }

    private sealed class Facts : ICardFacts
    {
        public CardKind Kind(string faceId) => faceId switch
        {
            "alterego" => CardKind.AlterEgo,
            "hero" => CardKind.Hero,
            "ally" => CardKind.Ally,
            "minion" => CardKind.Minion,
            _ => CardKind.Unknown,
        };

        public IReadOnlyList<string> Traits(string faceId) => [];

        public IReadOnlyDictionary<string, string> Attributes(string faceId) =>
            new Dictionary<string, string>(StringComparer.Ordinal);

        public long PrintedValue(
            string faceId, string attribute, int players, long fallback = 0) =>
            attribute == "Cost" ? 0 : fallback;
    }
}
