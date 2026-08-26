using Marvel.Content.Setup;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>
/// Cards that leave a hand and come back — Highway Robbery.
/// </summary>
/// <remarks>
/// "When Revealed: Each player places a random card from their hand facedown
/// here. <b>When Defeated:</b> Return each facedown card here to its owner's
/// hand." Placed and not discarded — the cards come back, which is what the
/// second ability is for — so they sit on the scheme as attachments, facedown.
/// </remarks>
public sealed class HighwayRobberyTests
{
    private static readonly SetupCatalog Setup =
        SetupCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("setup", "setup.json")));

    private static readonly CardCatalog Cards =
        CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));

    [Fact]
    public void EachPlayerLosesOneCardFaceDownOntoTheScheme()
    {
        var world = Deal("spider_man", "she_hulk");
        int mine = world.Seats[0].Hand.Cards.Count;
        int theirs = world.Seats[1].Hand.Cards.Count;

        var scheme = Reveal(world);

        Assert.Equal(mine - 1, world.Seats[0].Hand.Cards.Count);
        Assert.Equal(theirs - 1, world.Seats[1].Hand.Cards.Count);

        var taken = Attached(world, scheme);
        Assert.Equal(2, taken.Count);
        Assert.All(taken, card => Assert.False(card.FaceUp));
    }

    [Rule("rr:when-defeated-abilities.2.1")]
    [Fact]
    public void DefeatingItGivesEachCardBackToItsOwner()
    {
        // "**Its owner's** hand", not the hand of whoever defeated the scheme.
        // Ownership is the card's, so each player gets their own back -- which
        // at one player is invisible and at two is the whole claim.
        var world = Deal("spider_man", "she_hulk");
        int mine = world.Seats[0].Hand.Cards.Count;
        int theirs = world.Seats[1].Hand.Cards.Count;
        var scheme = Reveal(world);
        var taken = Attached(world, scheme);

        Agendas.Happening(world);
        Defeat.Scheme(world, Cards, scheme, "test", []);

        Assert.Equal(mine, world.Seats[0].Hand.Cards.Count);
        Assert.Equal(theirs, world.Seats[1].Hand.Cards.Count);
        Assert.All(taken, card => Assert.Same(world.Seats[card.Owner].Hand, card.Area));
        Assert.All(taken, card => Assert.True(card.FaceUp));
    }

    [Fact]
    public void AnEmptyHandGivesNothingAndTakesNoDraw()
    {
        // The same bargain the random discard makes: a player with nothing to
        // lose costs the random stream nothing, and a board that took one draw
        // is a different game from one that took two.
        var world = Deal("spider_man", "she_hulk");
        foreach (var card in world.Seats[1].Hand.Cards.ToList())
        {
            World.MoveToTop(card, world.Seats[1].Deck);
        }

        var scheme = Reveal(world);

        Assert.Single(Attached(world, scheme));
        Assert.Empty(world.Seats[1].Hand.Cards);
    }

    private static IReadOnlyList<Card> Attached(World world, Card scheme) =>
    [
        .. world.Areas
            .Where(area => area.Host == scheme.ObjectId)
            .SelectMany(area => area.Cards),
    ];

    private static Card Reveal(World world)
    {
        var scheme = world.CreateCard(
            AuthoredCards.HighwayRobbery, world.AreaOf(DeckType.SideSchemesArea));
        AuthoredCards.Runner().WhenRevealed(world, scheme, 0);
        return scheme;
    }

    private static World Deal(params string[] heroes)
    {
        var world = WorldSetup.Deal(
            Cards,
            Blueprints.From(Dealer.DealOrder(Setup, "rhino", heroes), Cards),
            [.. heroes.Select(hero => Setup.Hero(hero).Name)],
            12345);
        world.Abilities = AuthoredCards.Runner();
        return world;
    }
}
