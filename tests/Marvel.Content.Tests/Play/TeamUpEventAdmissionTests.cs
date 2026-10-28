using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

// Printed cards on a nonpublic research board; the event effects are synthetic.
public sealed class TeamUpEventAdmissionTests
{
    private static readonly CardCatalog Cards = CardCatalog.Parse(
        File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:team-up")]
    [Rule("rr:friendly")]
    public void ActionRequiresBothCharactersAndAcceptsAnotherPlayersAlly(bool hasWasp)
    {
        // rr:team-up: both named friendly identities or allies must be in play.
        var runner = Runner("Action");
        var (world, source, _, wasp) = Board(runner, ally: true);
        if (!hasWasp) World.MoveToTop(wasp!, world.AreaOf(DeckType.DiscardPile, PlayArea.Of(0), cardOwner: 0));
        var game = Start(world, runner);
        var offers = game.PromptFor(1)!.Affordances;
        Assert.Contains(offers, a => a.AnchorId == Control(world).ObjectId && a.PlaysCard);
        Assert.Equal(hasWasp, offers.Any(a => a.AnchorId == source.ObjectId && a.PlaysCard));
    }

    [Theory]
    [InlineData("13001a", true)]
    [InlineData("13001b", false)]
    [Rule("rr:team-up.1")]
    [Rule("rr:identity.4")]
    public void IdentityMatchesItsFaceInPlay(string face, bool offered)
    {
        // rr:identity.4: only the faceup side of the identity is in play.
        var runner = Runner("Action");
        var (world, source, _, _) = Board(runner, identity: face);
        var game = Start(world, runner);
        Assert.Equal(offered, game.PromptFor(1)!.Affordances.Any(a => a.AnchorId == source.ObjectId));
        Assert.Contains(game.PromptFor(1)!.Affordances, a => a.AnchorId == Control(world).ObjectId);
    }

    [Fact]
    [Rule("rr:loses")]
    [Rule("rr:team-up")]
    public void LostKeywordRemovesItsPlayRestriction()
    {
        // rr:loses: the lost characteristic cannot be used while it is lost.
        var runner = Runner("Action");
        var (world, source, _, _) = Board(runner);
        world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect,
            Characteristics.LossOf("team-up"), Affects: source.ObjectId));
        var game = Start(world, runner);
        Assert.Contains(game.PromptFor(1)!.Affordances, a => a.AnchorId == source.ObjectId && a.PlaysCard);
    }

    [Fact]
    [Rule("rr:team-up")]
    public void CapturedActionRejectsDepartedPartnerWithoutMutationAndCanRetry()
    {
        // rr:team-up requires both friendly characters in play when the card is played.
        var runner = Runner("Action");
        var (world, source, payment, wasp) = Board(runner, ally: true);
        var game = Start(world, runner);
        var offer = Assert.Single(game.PromptFor(1)!.Affordances, a => a.AnchorId == source.ObjectId);
        Assert.True(offer.PlaysCard);
        World.MoveToTop(wasp!, world.AreaOf(DeckType.DiscardPile, PlayArea.Of(0), cardOwner: 0));
        var prompt = game.Pending;
        string digest = world.Digest().Canonical();
        var rng = world.Random.Generator.GetState();
        long words = world.Random.Generator.WordsConsumed;
        var outstanding = world.Agenda.Outstanding.ToArray();
        Assert.Throws<ArgumentException>(() => game.Resolve(PaidDecision.Take(offer, [payment.ObjectId])));
        Assert.Same(prompt, game.Pending);
        Assert.Equal(digest, world.Digest().Canonical());
        Assert.Equal(rng.Words, world.Random.Generator.GetState().Words);
        Assert.Equal(rng.Index, world.Random.Generator.GetState().Index);
        Assert.Equal(words, world.Random.Generator.WordsConsumed);
        Assert.Equal(outstanding, world.Agenda.Outstanding);
        World.MoveToTop(wasp!, world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        game.Resolve(PaidDecision.Take(offer, [payment.ObjectId]));
        Assert.Equal(DeckType.DiscardPile, source.Area.Type);
        Assert.Equal(DeckType.DiscardPile, payment.Area.Type);
        Assert.Equal("12001c", world.Seats[1].IdentityCard.FaceId);
    }

    [Theory]
    [InlineData("Response", WindowKind.Response)]
    [InlineData("Interrupt", WindowKind.Interrupt)]
    [Rule("rr:team-up")]
    public void WindowEventRequiresBothCharacters(string timing, WindowKind window)
    {
        // rr:team-up applies to playing a card, including event window abilities.
        var runner = Runner(timing);
        var (world, source, _, wasp) = Board(runner, ally: true);
        var occurrence = new Occurrence(1, ["WhenFormChanged"], Player: 1);
        Assert.Contains(runner.Waiting(world, occurrence, window), a => a.Card == source.ObjectId);
        World.MoveToTop(wasp!, world.AreaOf(DeckType.DiscardPile, PlayArea.Of(0), cardOwner: 0));
        var waiting = runner.Waiting(world, occurrence, window);
        Assert.DoesNotContain(waiting, a => a.Card == source.ObjectId);
        Assert.Contains(waiting, a => a.Card == Control(world).ObjectId);
    }

    [Theory]
    [InlineData("Response")]
    [InlineData("Interrupt")]
    [Rule("rr:team-up")]
    public void CapturedWindowEventRejectsDepartedPartnerAndCanRetry(string timing)
    {
        // rr:team-up requires both friendly characters in play when the card is played.
        var runner = Runner(timing);
        var (world, source, payment, wasp) = Board(runner, ally: true);
        var game = Start(world, runner);
        var change = Assert.Single(game.Pending!.Affordances, a => a.Verb == Game.ChangeForm);
        game.Resolve(Decision.Take(change.Id));
        var offer = Assert.Single(game.Pending!.Affordances, a => a.AnchorId == source.ObjectId);
        Assert.True(offer.PlaysCard);
        World.MoveToTop(wasp!, world.AreaOf(DeckType.DiscardPile, PlayArea.Of(0), cardOwner: 0));
        var prompt = game.Pending;
        string digest = world.Digest().Canonical();
        var rng = world.Random.Generator.GetState();
        long words = world.Random.Generator.WordsConsumed;
        var outstanding = world.Agenda.Outstanding.ToArray();
        Assert.Throws<ArgumentException>(() => game.Resolve(PaidDecision.Take(offer, [payment.ObjectId])));
        Assert.Same(prompt, game.Pending);
        Assert.Equal(digest, world.Digest().Canonical());
        Assert.Equal(rng.Words, world.Random.Generator.GetState().Words);
        Assert.Equal(rng.Index, world.Random.Generator.GetState().Index);
        Assert.Equal(words, world.Random.Generator.WordsConsumed);
        Assert.Equal(outstanding, world.Agenda.Outstanding);
        World.MoveToTop(wasp!, world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        game.Resolve(PaidDecision.Take(offer, [payment.ObjectId]));
        Assert.Equal(DeckType.DiscardPile, source.Area.Type);
        Assert.Equal(DeckType.DiscardPile, payment.Area.Type);
    }

    [Theory]
    [InlineData("Response", WindowKind.Response)]
    [InlineData("Interrupt", WindowKind.Interrupt)]
    [Rule("rr:team-up")]
    public void InPlayEventTriggerIsNotANewCardPlayRestriction(string timing, WindowKind window)
    {
        // Synthetic in-play event: the team-up restriction governs playing it.
        var runner = Runner(timing);
        var (world, source, _, _) = Board(runner);
        World.MoveToTop(source, world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(1), cardOwner: 1));
        var occurrence = new Occurrence(1, ["WhenFormChanged"], Player: 1);
        Assert.Contains(runner.Waiting(world, occurrence, window), a => a.Card == source.ObjectId);
    }

    [Theory]
    [InlineData("Response")]
    [InlineData("Interrupt")]
    [Rule("rr:team-up")]
    public void InPlayEventTriggerCanResolveWithoutRecheckingTeamUp(string timing)
    {
        // Synthetic in-play event: resolving its trigger is not playing the card.
        var runner = Runner(timing);
        var (world, source, payment, _) = Board(runner);
        World.MoveToTop(source, world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(1), cardOwner: 1));
        var game = Start(world, runner);
        var change = Assert.Single(game.Pending!.Affordances, a => a.Verb == Game.ChangeForm);
        game.Resolve(Decision.Take(change.Id));
        var offer = Assert.Single(game.Pending!.Affordances, a => a.AnchorId == source.ObjectId);
        int deckCount = world.Seats[1].Deck.Cards.Count;
        game.Resolve(PaidDecision.Take(offer, [payment.ObjectId]));
        Assert.Equal(deckCount - 1, world.Seats[1].Deck.Cards.Count);
    }

    [Theory]
    [InlineData("Action", WindowKind.Interrupt)]
    [InlineData("Interrupt", WindowKind.Interrupt)]
    [Rule("rr:team-up")]
    [Rule("rr:in-play-and-out-of-play.10")]
    public void StoredEventStillRequiresBothCharacters(string timing, WindowKind window)
    {
        // rr:team-up: a card cannot be played unless both named friendly characters are in play.
        // Synthetic storage permission: a stored event remains out of play.
        var runner = Runner(timing, storage: true);
        var (world, source, _, wasp) = Board(runner, ally: true);
        var host = world.CreateCard("01006", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(1), cardOwner: 1));
        BeneathCards.Place(world, source, host, false, "synthetic", []);
        var occurrence = new Occurrence(1, ["WhenFormChanged"], Player: 1);
        Assert.True(CardPlayOrigin.CanPlayEvent(world, source, 1));
        Assert.False(DeckTypes.IsInPlay(source.Area.Type));
        bool Offered() => timing == "Action"
            ? runner.Actions(world, 1).Any(a => a.Card == source.ObjectId)
            : runner.Waiting(world, occurrence, window).Any(a => a.Card == source.ObjectId);
        Assert.True(Offered());
        World.MoveToTop(wasp!, world.AreaOf(DeckType.DiscardPile, PlayArea.Of(0), cardOwner: 0));
        Assert.False(Offered());
        World.MoveToTop(wasp!, world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        Assert.True(Offered());
    }

    private static AbilityRunner Runner(string timing, bool storage = false)
    {
        string trigger = timing == "Action" ? "WhenActionTriggered" : "WhenFormChanged";
        string subject = timing == "Action" ? "this" : "game";
        string effect = timing == "Action"
            ? """{"seq":[{"changeForm":{"player":"you","to":"other-hero"}},{"draw":{"player":"you","count":1}}]}"""
            : """{"draw":{"player":"you","count":1}}""";
        return new AbilityRunner(AbilityCatalog.Parse($$"""
            {"cards":[
              {"card":"12020","abilities":[{"trigger":{"event":"{{trigger}}","timing":"{{timing}}","subject":"{{subject}}"},"effect":{{effect}}}]},
              {"card":"01004","abilities":[{"trigger":{"event":"{{trigger}}","timing":"{{timing}}","subject":"{{subject}}"},"effect":{{effect}}}]}
              {{(storage ? ", {\"card\":\"01006\",\"abilities\":[{\"trigger\":{\"timing\":\"Constant\",\"subject\":\"this\"},\"effect\":{\"allowPlayBeneath\":{\"kind\":\"Event\",\"trait\":\"TACTIC\"}}}]}" : "")}}
            ]}
            """));
    }

    private static (World World, Card Source, Card Payment, Card? Wasp) Board(
        AbilityRunner runner, bool ally = false, string? identity = null)
    {
        var world = new World(Cards, 2, 241);
        var first = world.CreateSeat("p0");
        first.IdentityCard = world.CreateCard(identity is null ? "01001a,01001b" : "13001a,13001b,13001c", first.Hero);
        if (identity is not null) first.IdentityCard.TurnTo(identity);
        var seat = world.CreateSeat("p1");
        seat.IdentityCard = world.CreateCard("12001a,12001b,12001c", seat.Hero);
        for (int i = 0; i < 20; i++)
        {
            world.CreateCard("01087", first.Deck);
            world.CreateCard("01087", seat.Deck);
            world.CreateCard("01120", world.AreaOf(DeckType.EncounterDeck));
        }
        var wasp = ally ? world.CreateCard("12002", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0)) : null;
        world.CreateCard("01094", world.AreaOf(DeckType.VillainArea));
        world.CreateCard("01097b", world.AreaOf(DeckType.MainSchemesArea));
        var source = world.CreateCard("12020", seat.Hand);
        var payment = world.CreateCard("01088", seat.Hand);
        world.CreateCard("01004", seat.Hand);
        world.Abilities = runner;
        return (world, source, payment, wasp);
    }

    private static Card Control(World world) => Assert.Single(world.Seats[1].Hand.Cards, card => card.FaceId == "01004");

    private static Game Start(World world, AbilityRunner runner)
    {
        var game = Game.Begin(world, Cards, runner);
        for (int i = 0; i < 2; i++)
            game.Resolve(Decision.Take(Assert.Single(game.Pending!.Affordances, a => a.Verb == Game.ResolveMulligans).Id));
        return game;
    }
}
