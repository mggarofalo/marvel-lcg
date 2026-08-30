using Marvel.Content.Setup;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>
/// Choosing a card on the board, and finding one that is not.
/// </summary>
/// <remarks>
/// <para>
/// <c>rr:choose-option</c> and <c>rr:choose-game-element</c> are two questions:
/// an option is a branch the card lists, an element is a card on the table.
/// Caught Off Guard asks the second, so the answer is an object id rather than
/// an index, and <b>which</b> card goes is the player's to say.
/// </para>
/// <para>
/// Rhino's second stage is the other direction — a card that is nowhere yet.
/// <c>rr:search.2</c> makes looking free ("cards being searched are not
/// considered to leave the searched area"), so only the card found moves, and
/// the reveal is scheduled rather than run so that it gets the windows every
/// other reveal has.
/// </para>
/// </remarks>
public sealed class SearchAndDiscardCardsTests
{
    private const string Campaign = "rhino";
    private const uint Seed = 12345;

    /// <summary>"Breakin' &amp; Takin'", the side scheme Rhino II fetches.</summary>
    private const string BreakinAndTakin = "01107";

    /// <summary>Spider-Man's "Web-Shooter" upgrade.</summary>
    private const string WebShooter = "01008";

    /// <summary>Spider-Man's "Aunt May" support.</summary>
    private const string AuntMay = "01006";

    private static readonly SetupCatalog Setup =
        SetupCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("setup", "setup.json")));

    private static readonly CardCatalog Cards =
        CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));

    [Rule("rr:choose-game-element.1")]
    [Fact]
    public void CaughtOffGuardAsksWhichCardAndDiscardsThatOne()
    {
        // Two candidates, so "which" is a real question and not a formality.
        // The answer is an object id -- `rr:choose-game-element` chooses a game
        // element -- rather than the index an option would carry.
        var world = Deal();
        var upgrade = world.CreateCard(
            WebShooter, world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0)));
        var support = world.CreateCard(
            AuntMay, world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        var runner = AuthoredCards.Runner();

        var card = world.CreateCard(
            AuthoredCards.CaughtOffGuard, world.AreaOf(DeckType.RevealingArea));
        runner.WhenRevealed(world, card, 0);

        var asked = runner.Choosing(world, card, 0, stoppedAt: 1)!;
        Assert.Equal(Question.Element, asked.Asking);
        Assert.Equal(
            [upgrade.ObjectId, support.ObjectId],
            asked.Affordances.Select(option => option.Id));

        runner.Chose(world, card, 0, 1, Decision.Take(support.ObjectId));

        Assert.Equal(DeckType.DiscardPile, support.Area.Type);
        Assert.Equal(DeckType.UpgradesArea, upgrade.Area.Type);
    }

    [Fact]
    public void CaughtOffGuardSurgesWhenThereIsNothingToDiscard()
    {
        // "If no cards were discarded this way, this card gains surge." With
        // nothing in play there is nothing to choose, so nothing is asked --
        // the card's answer for the empty case is in the branch that got here
        // rather than after a choice that never happened.
        var world = Deal();
        int queued = Queue(world).Cards.Count;

        var card = world.CreateCard(
            AuthoredCards.CaughtOffGuard, world.AreaOf(DeckType.RevealingArea));
        AuthoredCards.Runner().WhenRevealed(world, card, 0);

        Assert.Empty(world.Agenda.Outstanding);
        Assert.Equal(queued + 1, Queue(world).Cards.Count);
    }

    [Rule("rr:permanent.4")]
    [Fact]
    public void CaughtOffGuardDoesNotOfferACrossSetPermanentAndSurges()
    {
        // A permanent is "not [a] valid target" for an effect that would make
        // it leave play. With no legal upgrade or support, Caught Off Guard
        // asks no question and gains surge instead of offering a doomed choice.
        var world = Deal();
        var permanent = world.CreateCard(
            "27182a",
            world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
        int queued = Queue(world).Cards.Count;

        var card = world.CreateCard(
            AuthoredCards.CaughtOffGuard, world.AreaOf(DeckType.RevealingArea));
        AuthoredCards.Runner().WhenRevealed(world, card, 0);

        Assert.Empty(world.Agenda.Outstanding);
        Assert.Equal(queued + 1, Queue(world).Cards.Count);
        Assert.Equal(DeckType.UpgradesArea, permanent.Area.Type);
    }

    [Rule("rr:ownership-and-control.8")]
    [Fact]
    public void AnotherPlayersUpgradeIsNotYoursToDiscard()
    {
        // "An upgrade or support **you control**." That phrase "only refers to
        // cards in play currently under that player's control." Control is
        // which play area the card is in, so the other player's upgrade is not
        // offered -- and with nothing of your own, the card surges instead of
        // reaching across the table.
        var world = Deal("spider_man", "she_hulk");
        world.CreateCard(
            WebShooter, world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
        int queued = Queue(world, 1).Cards.Count;

        var card = world.CreateCard(
            AuthoredCards.CaughtOffGuard, world.AreaOf(DeckType.RevealingArea));
        AuthoredCards.Runner().WhenRevealed(world, card, 1);

        Assert.Empty(world.Agenda.Outstanding);
        Assert.Equal(queued + 1, Queue(world, 1).Cards.Count);
    }

    [Rule("rr:search.2")]
    [Fact]
    public void RhinosSecondStageFindsTheSideSchemeAndSchedulesItsReveal()
    {
        // The card is in the encounter deck at the deal. Searching finds it and
        // schedules the reveal rather than running it, so the side scheme goes
        // through `rr:reveal`'s four steps with the windows every other reveal
        // gets.
        var world = Deal();
        var wanted = world.AreaOf(DeckType.EncounterDeck).Cards
            .Single(card => card.FaceId == BreakinAndTakin);

        var stage = world.CreateCard(
            AuthoredCards.RhinoTwo, world.AreaOf(DeckType.RevealingArea));
        AuthoredCards.Runner().WhenRevealed(world, stage, 0);

        var scheduled = Assert.Single(world.Agenda.Outstanding);
        Assert.Equal(Steps.RevealEncounterCard, scheduled.What);
        Assert.Equal(wanted.ObjectId, scheduled.Subject);
    }

    [Rule("rr:search.3")]
    [Rule("rr:shuffle.1")]
    [Rule("rr:shuffle.2")]
    [Fact]
    public void SearchingTheEncounterDeckShufflesIt()
    {
        // "If any portion of a deck is searched [...] shuffle that entire
        // deck." The discard pile is searched too and is not shuffled, because
        // it is not a deck -- and shuffling one would draw from the game's
        // single random stream, which is a wire format.
        var world = Deal();
        var discard = world.AreaOf(DeckType.EncounterDiscardPile);
        foreach (var card in world.AreaOf(DeckType.EncounterDeck).Cards.Take(4).ToList())
        {
            World.MoveToTop(card, discard);
        }

        var before = world.AreaOf(DeckType.EncounterDeck).Cards.Select(c => c.ObjectId).ToList();
        var pile = discard.Cards.Select(c => c.ObjectId).ToList();

        var stage = world.CreateCard(
            AuthoredCards.RhinoTwo, world.AreaOf(DeckType.RevealingArea));
        AuthoredCards.Runner().WhenRevealed(world, stage, 0);

        var after = world.AreaOf(DeckType.EncounterDeck).Cards.Select(c => c.ObjectId).ToList();
        Assert.NotEqual(before, after);
        Assert.Equal(before.Order(), after.Order());
        Assert.Equal(pile, discard.Cards.Select(c => c.ObjectId));
    }

    [Fact]
    public void TheSideSchemeIsFoundInTheDiscardPileToo()
    {
        // "Search the encounter deck **and discard pile**." A game that has
        // already revealed and discarded Breakin' & Takin' fetches it back, and
        // a search of only the deck would find nothing and do nothing.
        var world = Deal();
        var wanted = world.AreaOf(DeckType.EncounterDeck).Cards
            .Single(card => card.FaceId == BreakinAndTakin);
        World.MoveToTop(wanted, world.AreaOf(DeckType.EncounterDiscardPile));

        var stage = world.CreateCard(
            AuthoredCards.RhinoTwo, world.AreaOf(DeckType.RevealingArea));
        AuthoredCards.Runner().WhenRevealed(world, stage, 0);

        Assert.Equal(
            wanted.ObjectId, Assert.Single(world.Agenda.Outstanding).Subject);
    }

    private static Area Queue(World world, int player = 0) =>
        world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(player));

    private static World Deal(params string[] heroes)
    {
        string[] playing = heroes.Length > 0 ? heroes : ["spider_man"];
        return WorldSetup.Deal(
            Cards,
            Blueprints.From(Dealer.DealOrder(Setup, Campaign, playing), Cards),
            [.. playing.Select(hero => Setup.Hero(hero).Name)],
            Seed);
    }
}
