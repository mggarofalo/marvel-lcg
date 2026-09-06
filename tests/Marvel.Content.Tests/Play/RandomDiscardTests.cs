using Marvel.Content.Setup;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>
/// Discarding a card at random — and what that costs the random stream.
/// </summary>
/// <remarks>
/// <para>
/// <b>The draw is a wire format.</b> One MT19937 stream runs the whole game, so
/// how many numbers this takes and in what order decides every later shuffle
/// and every later random card. <c>EngineRandom.Choice</c> is the ported
/// primitive and is already pinned against recorded RNG vectors; what is new
/// here is a card reaching it.
/// </para>
/// <para>
/// So the tests below assert <i>determinism</i> rather than which card went:
/// the same seed must take the same card, and a differently ordered hand is a
/// different game.
/// </para>
/// </remarks>
public sealed class RandomDiscardTests
{
    private static readonly SetupCatalog Setup =
        SetupCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("setup", "setup.json")));

    private static readonly CardCatalog Cards =
        CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));

    [Fact]
    public void OneCardLeavesEachHand()
    {
        var world = Deal("spider_man", "she_hulk");
        int mine = world.Seats[0].Hand.Cards.Count;
        int theirs = world.Seats[1].Hand.Cards.Count;

        Reveal(world, AuthoredCards.VulturesPlans);

        Assert.Equal(mine - 1, world.Seats[0].Hand.Cards.Count);
        Assert.Equal(theirs - 1, world.Seats[1].Hand.Cards.Count);
    }

    [Fact]
    public void TheSameSeedTakesTheSameCards()
    {
        // Determinism is non-negotiable 1, and a random discard is the first
        // card ability to draw on the stream at all.
        var first = Deal();
        var second = Deal();

        Reveal(first, AuthoredCards.VulturesPlans);
        Reveal(second, AuthoredCards.VulturesPlans);

        Assert.Equal(
            first.Seats[0].Hand.Cards.Select(card => card.ObjectId),
            second.Seats[0].Hand.Cards.Select(card => card.ObjectId));
        Assert.Equal(first.Digest().Canonical(), second.Digest().Canonical());
    }

    [Rule("rr:each-player.1")]
    [Fact]
    public void ThreatIsOnePerDifferentResourceTypeAndNotPerCard()
    {
        // "Place 1 threat on the main scheme **for each different resource
        // type** discarded this way." Two players holding one card each, both
        // printing the same letter, is one type -- so one threat, not two.
        var world = Deal("spider_man", "she_hulk");
        Hand(world, 0, "01003");
        Hand(world, 1, "01003");

        Reveal(world, AuthoredCards.VulturesPlans);

        Assert.Equal(
            1, world.TheCardIn(DeckType.MainSchemesArea)!.Tokens.GetValueOrDefault("k_threat"));
    }

    [Fact]
    public void TwoDifferentTypesArePlacedAsTwo()
    {
        // The other side of the same claim, on a board identical but for the
        // letter on one card.
        var world = Deal("spider_man", "she_hulk");
        Hand(world, 0, "01003");
        Hand(world, 1, "01004");

        Reveal(world, AuthoredCards.VulturesPlans);

        Assert.Equal(
            2, world.TheCardIn(DeckType.MainSchemesArea)!.Tokens.GetValueOrDefault("k_threat"));
    }

    [Fact]
    public void AnEmptyHandDiscardsNothingAndTakesNoDraw()
    {
        // A player with nothing to lose costs the stream nothing, which is why
        // the draw is inside the loop rather than counted ahead: an empty hand
        // that consumed a number would desynchronise every later shuffle.
        var world = Deal("spider_man", "she_hulk");
        Hand(world, 0, "01003");
        Empty(world, 1);

        Reveal(world, AuthoredCards.VulturesPlans);

        Assert.Empty(world.Seats[0].Hand.Cards);
        Assert.Empty(world.Seats[1].Hand.Cards);
        Assert.Equal(
            1, world.TheCardIn(DeckType.MainSchemesArea)!.Tokens.GetValueOrDefault("k_threat"));

        // **The stream is where the claim lives.** A board that took one draw
        // and a board that took two are the same board and different games, so
        // the assertion is what the *next* number is: identical to a control
        // that made exactly one `Choice` by hand.
        var control = Deal("spider_man", "she_hulk");
        Hand(control, 0, "01003");
        Empty(control, 1);
        control.Random.Choice(control.Seats[0].Hand.Cards);

        Assert.Equal(Next(control), Next(world));
    }

    /// <summary>The next number off the stream, as a probe of its position.</summary>
    private static int Next(World world) =>
        world.Random.Choice(Enumerable.Range(0, 1000).ToList());

    [Fact]
    public void TwoDrawsAndOneAreDifferentGames()
    {
        // The converse, and what makes the test above mean something: a board
        // where both players discard is one draw further on.
        var one = Deal("spider_man", "she_hulk");
        Hand(one, 0, "01003");
        Empty(one, 1);
        Reveal(one, AuthoredCards.VulturesPlans);

        var two = Deal("spider_man", "she_hulk");
        Hand(two, 0, "01003");
        Hand(two, 1, "01003");
        Reveal(two, AuthoredCards.VulturesPlans);

        Assert.NotEqual(Next(one), Next(two));
    }

    /// <summary>Replaces a player's hand with one named card.</summary>
    private static void Hand(World world, int seat, string faceId)
    {
        Empty(world, seat);
        world.CreateCard(faceId, world.Seats[seat].Hand);
    }

    private static void Empty(World world, int seat)
    {
        foreach (var card in world.Seats[seat].Hand.Cards.ToList())
        {
            World.MoveToTop(card, world.Seats[seat].Deck);
        }
    }

    private static void Reveal(World world, string faceId)
    {
        var card = world.CreateCard(faceId, world.AreaOf(DeckType.RevealingArea));
        var abilities = AuthoredCards.Runner();
        abilities.WhenRevealed(world, card, 0);
        var asked = Sequence.Work(world, Cards, abilities, []);
        while (asked is not null)
        {
            Sequence.Answer(
                world, Cards, abilities, asked, Decision.Decline, []);
            asked = Sequence.Work(world, Cards, abilities, []);
        }
    }

    private static World Deal(params string[] heroes)
    {
        string[] playing = heroes.Length > 0 ? heroes : ["spider_man"];
        var world = WorldSetup.DealWithoutCardAbilities(
            Cards,
            Blueprints.From(Dealer.DealOrder(Setup, "rhino", playing), Cards),
            [.. playing.Select(hero => Setup.Hero(hero).Name)],
            12345);
        world.Abilities = AuthoredCards.Runner();
        return world;
    }
}
