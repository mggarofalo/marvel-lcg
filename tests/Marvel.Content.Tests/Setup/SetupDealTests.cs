using Marvel.Content.Setup;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Setup;

/// <summary>
/// What dealing a game produces — <c>rr:appendix-ii-setup</c>.
/// </summary>
/// <remarks>
/// <para>
/// <b>Two kinds of claim, kept apart.</b> Where a card ends up is the rulebook's
/// business and every test below that makes such a claim cites the step that
/// decides it. <i>Which id it is given</i> is not: no rule mentions object ids,
/// and the engine allocates them in creation order because
/// <c>docs/state-digest-v2.md</c> makes an id a wire format. Those tests say so
/// rather than citing a rule they would be misreading.
/// </para>
/// <para>
/// The allocation matters as much as the placement. An id is the one thing
/// every other record points at, so a deal that puts the right cards in the
/// right zones under shifted ids has produced a board that is wrong in a way
/// no zone-by-zone check can see.
/// </para>
/// </remarks>
public sealed class SetupDealTests
{
    private const string Campaign = "rhino";
    private const uint Seed = 12345;

    private static readonly SetupCatalog Setup =
        SetupCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("setup", "setup.json")));

    private static readonly CardCatalog Cards =
        CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));

    [Fact]
    public void EveryDealtCardIsOnTheBoardExactlyOnce()
    {
        // The completeness claim, and the one a zone-by-zone check cannot make.
        // A card created and then left in no area at all is invisible to every
        // other test here: its zone is never asserted because nothing knows to
        // look for it.
        var (world, order) = Deal();

        Assert.Equal(order.Count, world.Cards.Count);
        Assert.Equal(
            Enumerable.Range(0, order.Count),
            world.Cards.Select(card => card.ObjectId));

        foreach (var card in world.Cards)
        {
            Assert.True(
                world.Areas.Any(area => area.Cards.Contains(card)),
                $"card {card.ObjectId} ({card.FaceId}) is in no area");
        }
    }

    [Fact]
    public void EveryCardIsCreatedInTheOrderItsSourceSays()
    {
        // Ids are allocated in creation order, so the sources have to form
        // unbroken runs: interleaving two would leave both correct
        // card-by-card and wrong everywhere an id is read.
        //
        // Not a rules claim. `rr:appendix-ii-setup` orders the *steps*, and
        // this order follows it -- identities before obligations before nemesis
        // sets before player decks before the scenario -- but the rulebook says
        // nothing about ids.
        var (_, order) = Deal();

        var runs = new List<CreationSource>();
        foreach (var creation in order)
        {
            if (runs.Count == 0 || runs[^1] != creation.Source)
            {
                runs.Add(creation.Source);
            }
        }

        Assert.Equal(
            [
                CreationSource.Rules,
                CreationSource.Identity,
                CreationSource.Obligation,
                CreationSource.Nemesis,
                CreationSource.HeroDeck,
                CreationSource.PlayerDeck,
                CreationSource.MainScheme,
                CreationSource.Villain,
                CreationSource.Encounter,
                CreationSource.EncounterSet,
            ],
            runs);
    }

    [Rule("rr:appendix-ii-setup.step.5")]
    [Fact]
    public void TheNemesisSetIsSetAsideAndReachesNoDeck()
    {
        // "For each identity being played, set aside their nemesis and the
        // encounter cards of that nemesis." Set aside, so it is neither in the
        // encounter deck step 10 builds nor in the player deck.
        var (world, order) = Deal();

        var nemesis = Ids(order, CreationSource.Nemesis).Select(id => world.Cards[id]).ToList();

        Assert.NotEmpty(nemesis);
        Assert.All(nemesis, card => Assert.Equal(DeckType.AsideDeck, card.Area.Type));
    }

    [Rule("rr:appendix-ii-setup.step.10")]
    [Fact]
    public void TheEncounterDeckIsTheListedSetsAndTheObligationsAndNothingElse()
    {
        // "Shuffle the encounter sets listed on side 1A of the main scheme card
        // with the obligation cards set aside during setup step four to create
        // the encounter deck." Three sources exactly: an encounter deck holding
        // anything else is a deck the rule did not describe.
        var (world, order) = Deal();

        var expected = Ids(order, CreationSource.Obligation)
            .Concat(Ids(order, CreationSource.Encounter))
            .Concat(Ids(order, CreationSource.EncounterSet))
            .ToHashSet();

        var deck = world.AreaOf(DeckType.EncounterDeck).Cards
            .Select(card => card.ObjectId)
            .ToHashSet();

        Assert.Equal(expected, deck);
    }

    [Rule("rr:appendix-ii-setup.step.8")]
    [Fact]
    public void TheVillainDeckAndMainSchemeDeckArePutIntoPlay()
    {
        // "Select a scenario and put its villain deck and main scheme deck into
        // play near the center of the play area."
        var (world, _) = Deal();

        Assert.NotNull(world.TheCardIn(DeckType.VillainArea));
        Assert.NotNull(world.TheCardIn(DeckType.MainSchemesArea));
    }

    [Rule("rr:appendix-ii-setup.step.1")]
    [Rule("rr:appendix-ii-setup.step.6")]
    [Fact]
    public void EachSeatOwnsItsOwnIdentityAndItsOwnDeck()
    {
        // "**Each player** selects one identity" and "**each player** shuffles
        // their player deck." At one seat every ownership bug looks like
        // success, so this deals two.
        var (world, order) = Deal(["spider_man", "she_hulk"]);

        CreationSource[] theirs =
        [
            CreationSource.Identity, CreationSource.HeroDeck, CreationSource.PlayerDeck,
        ];

        for (int seat = 0; seat < 2; seat++)
        {
            var mine = Dealt(order, seat)
                .Where(x => theirs.Contains(x.Creation.Source))
                .Select(x => world.Cards[x.Id])
                .ToList();

            Assert.NotEmpty(mine);
            Assert.All(mine, card => Assert.Equal(seat, card.Owner));
        }

        // And the two decks are disjoint, which is the failure a single seat
        // could never show.
        var first = world.Seats[0].Deck.Cards.Select(card => card.ObjectId).ToHashSet();
        var second = world.Seats[1].Deck.Cards.Select(card => card.ObjectId).ToHashSet();

        Assert.NotEmpty(first);
        Assert.NotEmpty(second);
        Assert.Empty(first.Intersect(second));
    }

    [Rule("rr:obligation")]
    [Rule("rr:nemesis-encounter-set")]
    [Fact]
    public void ACardDealtForAPlayerIsNotThereforeTheirs()
    {
        // Obligations and nemesis sets are dealt **per identity** and belong to
        // the scenario anyway, because both are encounter cards:
        // `rr:obligation` calls an obligation "an encounter card type", and
        // `rr:encounter-card` lists obligations among the eight.
        //
        // So "dealt for" and "owned by" are two questions, and a deal that
        // conflated them would hand a player cards they can see in a hand-like
        // sense and hand the scenario nothing.
        var (world, order) = Deal(["spider_man", "she_hulk"]);

        CreationSource[] encounter = [CreationSource.Obligation, CreationSource.Nemesis];

        for (int seat = 0; seat < 2; seat++)
        {
            var forThem = Dealt(order, seat)
                .Where(x => encounter.Contains(x.Creation.Source))
                .Select(x => world.Cards[x.Id])
                .ToList();

            Assert.NotEmpty(forThem);
            Assert.All(forThem, card => Assert.Equal(Creation.Scenario, card.Owner));
        }
    }

    [Rule("rr:appendix-ii-setup.step.6")]
    [Fact]
    public void ThePlayerDeckIsShuffledAndTheShuffleFollowsTheSeed()
    {
        // "Each player shuffles their player deck." Two claims in one step: it
        // is shuffled at all -- a deck still in creation order has skipped the
        // step -- and the shuffle is the seed's, so one seed is one game.
        var (dealt, order) = Deal();
        var creationOrder = Ids(order, CreationSource.HeroDeck)
            .Concat(Ids(order, CreationSource.PlayerDeck))
            .ToList();

        var inDeck = dealt.Seats[0].Deck.Cards.Select(card => card.ObjectId).ToList();
        Assert.NotEqual(creationOrder.Take(inDeck.Count), inDeck);

        var (again, _) = Deal();
        Assert.Equal(inDeck, again.Seats[0].Deck.Cards.Select(card => card.ObjectId));

        var (different, _) = Deal(seed: Seed + 1);
        Assert.NotEqual(inDeck, different.Seats[0].Deck.Cards.Select(card => card.ObjectId));
    }

    [Rule("rr:appendix-ii-setup.step.3")]
    [Fact]
    public void TheFirstPlayerTokenSitsWithExactlyOneSeat()
    {
        // "The players select a first player and place the first player token
        // in front of that player." One token, so one seat -- and the engine
        // reads it off the identity, so two seats holding it would be two
        // players each believing the round starts with them.
        var (world, _) = Deal(["spider_man", "she_hulk"]);

        Assert.InRange(world.FirstPlayer, 0, world.Players - 1);

        int holding = world.Seats.Count(
            seat => seat.IdentityCard.Owner == world.FirstPlayer);
        Assert.Equal(1, holding);
    }

    [Rule("rr:ownership-and-control.1")]
    [Fact]
    public void EachIdentityIsOwnedAndControlledByItsPlayer()
    {
        // "Identity cards are owned and controlled by the player playing as
        // that identity." Ownership is the card field; control is the seat's
        // play area containing it.
        var (world, _) = Deal(["spider_man", "she_hulk"]);

        Assert.All(world.Seats, seat =>
        {
            Assert.Equal(seat.Index, seat.IdentityCard.Owner);
            Assert.Equal(PlayArea.Of(seat.Index), seat.IdentityCard.Area.PlayArea);
        });
    }

    [Fact]
    public void TheHeroDeckAndThePlayerDeckAreOneUnbrokenRunOfIds()
    {
        // A hero's own cards and the rest of their deck are allocated as one
        // run rather than two, so the whole forty is contiguous. Engine
        // contract, not a rule: what depends on it is that an id names the same
        // card on both sides of the wire.
        var (_, order) = Deal();

        var hero = Ids(order, CreationSource.HeroDeck).ToList();
        var player = Ids(order, CreationSource.PlayerDeck).ToList();

        Assert.NotEmpty(hero);
        Assert.NotEmpty(player);
        Assert.Equal(hero[^1] + 1, player[0]);
        Assert.Equal(hero.Count + player.Count, player[^1] - hero[0] + 1);
    }

    private static IEnumerable<(Creation Creation, int Id)> Dealt(
        IReadOnlyList<Creation> order, int seat) =>
        order.Select((creation, id) => (Creation: creation, Id: id))
            .Where(x => x.Creation.Player == seat);

    private static IEnumerable<int> Ids(
        IReadOnlyList<Creation> order, CreationSource source) =>
        order.Select((creation, id) => (creation, id))
            .Where(x => x.creation.Source == source)
            .Select(x => x.id);

    private static (World World, IReadOnlyList<Creation> Order) Deal(
        string[]? heroes = null, uint seed = Seed)
    {
        string[] playing = heroes ?? ["spider_man"];
        var order = Dealer.DealOrder(Setup, Campaign, playing);
        var world = WorldSetup.DealWithoutCardAbilities(
            Cards,
            Blueprints.From(order, Cards),
            [.. playing.Select(name => Setup.Hero(name).Name)],
            seed);

        return (world, order);
    }
}
