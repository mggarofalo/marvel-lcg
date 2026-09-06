using Marvel.Content.Setup;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>The printed behavior of the core set's Doomsday Chair modular set.</summary>
public sealed class DoomsdayChairTests
{
    private const string Campaign = "rhino";
    private const uint Seed = 12345;

    private static readonly SetupCatalog Setup =
        SetupCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("setup", "setup.json")));

    private static readonly CardCatalog Cards =
        CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));

    [Rule("rr:search.2")]
    [Rule("rr:when-revealed-abilities.2")]
    [Fact]
    public void TheChairFindsModokInTheEncounterDeckAndEngagesHimWithYou()
    {
        // Searching does not move a card by itself; "put him into play engaged
        // with you" moves M.O.D.O.K. to the revealing player's play area, and
        // is deliberately not a reveal.
        var world = Deal("spider_man", "she_hulk");
        var modok = world.CreateCard(
            AuthoredCards.Modok, world.AreaOf(DeckType.EncounterDeck));
        modok.TurnFaceDown();
        var deck = world.AreaOf(DeckType.EncounterDeck);
        var before = deck.Cards
            .Where(card => card != modok)
            .Select(card => card.ObjectId)
            .ToList();

        RevealChair(world, player: 1);

        Assert.Equal(DeckType.EngagedEnemiesArea, modok.Area.Type);
        Assert.Equal(PlayArea.Of(1), modok.Area.PlayArea);
        Assert.True(modok.FaceUp);
        Assert.NotEqual(before, deck.Cards.Select(card => card.ObjectId));
        Assert.Equal(before.Order(), deck.Cards.Select(card => card.ObjectId).Order());
    }

    [Rule("rr:search")]
    [Fact]
    public void TheChairFindsModokInTheEncounterDiscardPile()
    {
        // The printed search names both places. A deck-only implementation
        // would leave this copy in the discard pile and produce a legal-looking
        // board without the minion.
        var world = Deal();
        var modok = world.CreateCard(
            AuthoredCards.Modok, world.AreaOf(DeckType.EncounterDiscardPile));

        RevealChair(world);

        Assert.Equal(DeckType.EngagedEnemiesArea, modok.Area.Type);
        Assert.Equal(PlayArea.Of(0), modok.Area.PlayArea);
    }

    [Rule("rr:search.3")]
    [Fact]
    public void TheEncounterDeckIsShuffledAfterAnEmptySearch()
    {
        // "If any portion of a deck is searched [...] shuffle that entire
        // deck." Finding nothing changes no card, but it does not cancel the
        // shuffle or its draw from the game's deterministic random stream.
        var world = Deal();
        var deck = world.AreaOf(DeckType.EncounterDeck);
        var before = deck.Cards.Select(card => card.ObjectId).ToList();

        RevealChair(world);

        Assert.NotEqual(before, deck.Cards.Select(card => card.ObjectId));
        Assert.Equal(before.Order(), deck.Cards.Select(card => card.ObjectId).Order());
    }

    [Rule("rr:search.3")]
    [Fact]
    public void APluralAreaSearchRecordsInformationWithoutAMatchOrShuffleRng()
    {
        var world = Deal();
        var deck = world.AreaOf(DeckType.EncounterDeck);
        var discard = world.AreaOf(DeckType.EncounterDiscardPile);
        foreach (Card extra in deck.Cards.Skip(1).ToList())
        {
            World.MoveToTop(extra, discard);
        }

        RevealChair(world);
        var resolved = new Resolution(world, Prompt: null, Events: []);

        Assert.Single(deck.Cards);
        Assert.Contains(
            resolved.Information,
            signal => signal.Kind == InformationKind.Search);
    }

    [Fact]
    public void ModokAlreadyInPlayStopsTheWholeSearchAndShuffle()
    {
        // The "if" governs every word after it. In particular, M.O.D.O.K.
        // engaged with another player is still in play, and the encounter deck
        // is not searched or shuffled merely because he is not engaged with
        // the resolving player.
        var world = Deal("spider_man", "she_hulk");
        var modok = world.CreateCard(
            AuthoredCards.Modok,
            world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1)));
        var deck = world.AreaOf(DeckType.EncounterDeck);
        var before = deck.Cards.Select(card => card.ObjectId).ToList();

        RevealChair(world, player: 0);

        Assert.Same(modok, Assert.Single(
            world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1)).Cards));
        Assert.Equal(before, deck.Cards.Select(card => card.ObjectId));
    }

    [Rule("rr:search.1")]
    [Fact]
    public void ASearchWithSeveralMatchingCardsRefusesToChooseForThePlayer()
    {
        // The core set supplies one copy. If an artificial or future board has
        // several, rr:search.1 gives that decision to the player; the singular
        // card source raises until it can ask instead of taking area order.
        var world = Deal();
        world.CreateCard(AuthoredCards.Modok, world.AreaOf(DeckType.EncounterDeck));
        world.CreateCard(
            AuthoredCards.Modok, world.AreaOf(DeckType.EncounterDiscardPile));

        Assert.Throws<RulesNotImplementedException>(() => RevealChair(world));
    }

    [Rule("rr:retaliate-x")]
    [Fact]
    public void ModokDealsTwoDamageToTheCharacterThatAttacksHim()
    {
        // "After a character with the retaliate X keyword is attacked, deal X
        // damage to the attacker." This card's empty DSL row is intentional:
        // the printed Retaliate 2 field is executed by the keyword engine.
        var world = Deal();
        var hero = world.Seats[0].IdentityCard;
        hero.TurnTo(AuthoredCards.SpiderMan);
        var modok = world.CreateCard(
            AuthoredCards.Modok,
            world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));

        BasicPowers.BasicAttack(world, Cards, 0, modok, []);
        Agendas.Finish(world, Cards, AuthoredCards.Runner());

        Assert.Equal(2, modok.Damage);
        Assert.Equal(2, hero.Damage);
    }

    private static void RevealChair(World world, int player = 0)
    {
        var chair = world.CreateCard(
            AuthoredCards.DoomsdayChair, world.AreaOf(DeckType.RevealingArea));
        AuthoredCards.Runner().WhenRevealed(world, chair, player);
    }

    private static World Deal(params string[] heroes)
    {
        string[] playing = heroes.Length > 0 ? heroes : ["spider_man"];
        return WorldSetup.DealWithoutCardAbilities(
            Cards,
            Blueprints.From(Dealer.DealOrder(Setup, Campaign, playing), Cards),
            [.. playing.Select(hero => Setup.Hero(hero).Name)],
            Seed);
    }
}
