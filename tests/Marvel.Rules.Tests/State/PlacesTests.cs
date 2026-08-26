using Marvel.Tests;
using Marvel.Rules.Events;
using Marvel.Rules.State;
using Xunit;

namespace Marvel.Rules.Tests.State;

/// <summary>
/// Rules that resolve by where a card is, held against the published text.
/// </summary>
/// <remarks>
/// <para>
/// <b>Nothing but the rulebook can check any of this.</b> The digest cannot see a
/// play area — moving 47 cards into a freshly created game area left it
/// byte-identical (MARVEL-174). So these tests are written from the rulebooks,
/// and each one names the rule it comes from.
/// </para>
/// <para>
/// The boards are built by hand rather than dealt, because the point is the
/// shape of a placement and not any particular scenario's contents.
/// </para>
/// </remarks>
public sealed class PlacesTests
{
    // ---------------------------------------------------------------- rr:play-area

    [Rule("rr:play-area")]
    [Rule("rr:play-area.2")]
    [Fact]
    public void TheVillainsPlayAreaIsAPlaceAndNotTheAbsenceOfOne()
    {
        // rr:play-area.2 gives it contents of its own -- the villain deck, the
        // main scheme deck, the encounter deck and discard. Modelling it as
        // "no play area" would make the villain's cards homeless and would
        // collide with a card that is genuinely in no game area, which is a
        // different thing entirely (Kang's stage 2B).
        Assert.True(PlayArea.Villains.IsVillains);
        Assert.False(PlayArea.Villains.IsPlayers);
        Assert.True(PlayArea.Of(0).IsPlayers);
        Assert.False(PlayArea.Of(0).IsVillains);
    }

    [Rule("rr:play-area.3")]
    [Fact]
    public void EveryCardIsInExactlyOnePlayArea()
    {
        // rr:play-area.3, "A card cannot be in more than one play area at a
        // time." True by construction here -- a card is in an area and an area
        // sits in one play area -- so what this checks is that the construction
        // is total: no card anywhere answers "nowhere".
        var world = Ordinary(players: 2);
        MainScheme(world, PlayArea.Villains);
        InPlayArea(world, DeckType.HeroArea, PlayArea.Of(0));
        InPlayArea(world, DeckType.EngagedEnemiesArea, PlayArea.Of(1));
        InPlayArea(world, DeckType.EncounterDeck, PlayArea.Villains);

        Assert.NotEmpty(world.Cards);
        foreach (var card in world.Cards)
        {
            var area = Places.PlayAreaOf(card);
            Assert.True(area.IsVillains || area.Player < world.Players);
        }
    }

    [Fact]
    public void ASeatIndexIsNotNegativeAndTheVillainIsNotASeat()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => PlayArea.Of(-1));
    }

    // -------------------------------------------------------- an ordinary game

    [Fact]
    public void AnOrdinaryGameHasOneGameAreaHoldingEveryPlayArea()
    {
        var world = Ordinary(players: 3);

        var whole = Assert.Single(world.GameAreas);
        Assert.Equal(4, whole.PlayAreas.Count);       // three players plus the villain
        Assert.True(whole.Contains(PlayArea.Villains));
        for (int seat = 0; seat < 3; seat++)
        {
            Assert.True(whole.Contains(PlayArea.Of(seat)), $"seat {seat}");
        }
    }

    [Fact]
    public void AnOrdinaryGameIsUnaffectedByAnyOfThis()
    {
        // The property that matters most, because everything else here is paid
        // for by scenarios almost nobody plays: with one game area and one main
        // scheme, every predicate answers what it would answer if none of this
        // existed. A regression here is the model leaking into normal play.
        var world = Ordinary(players: 2);
        var scheme = MainScheme(world, PlayArea.Villains);
        var hero = InPlayArea(world, DeckType.HeroArea, PlayArea.Of(0));
        var villain = InPlayArea(world, DeckType.VillainArea, PlayArea.Villains);

        Assert.True(Places.CanAffect(world, hero, villain));
        Assert.True(Places.CanAffect(world, villain, hero));
        Assert.Equal([0, 1], Places.EachPlayer(world, hero));
        Assert.Equal([scheme], Places.MainSchemes(world, hero));
        Assert.Equal([scheme], Places.MainSchemes(world, villain));
    }

    // ------------------------------------------- pack:mc60:separate-main-schemes

    [Fact]
    public void ACardInAPlayersPlayAreaSeesOnlyThatPlayersMainScheme()
    {
        // "Cards in a player's play area (identities, allies, upgrades,
        // minions, etc.) that refer to 'the main scheme' refer only to the main
        // scheme in the same play area."
        var world = Ordinary(players: 2);
        var mine = MainScheme(world, PlayArea.Of(0));
        var theirs = MainScheme(world, PlayArea.Of(1));

        var myAlly = InPlayArea(world, DeckType.AlliesArea, PlayArea.Of(0));
        var theirAlly = InPlayArea(world, DeckType.AlliesArea, PlayArea.Of(1));

        Assert.Equal([mine], Places.MainSchemes(world, myAlly));
        Assert.Equal([theirs], Places.MainSchemes(world, theirAlly));
    }

    [Fact]
    public void ACardInNoPlayersPlayAreaSeesEveryMainScheme()
    {
        // "Cards that are not in any player's play area (the villain, side
        // schemes, and environments) that refer to 'the main scheme' apply to
        // all main schemes." The worked example in the same paragraph is a
        // crisis icon on a side scheme preventing threat being removed from any
        // main scheme -- so this is the rule that makes crisis icons work,
        // rather than a clause about crisis icons.
        var world = Ordinary(players: 2);
        var mine = MainScheme(world, PlayArea.Of(0));
        var theirs = MainScheme(world, PlayArea.Of(1));

        var sideScheme = InPlayArea(world, DeckType.SideSchemesArea, PlayArea.Villains);
        var villain = InPlayArea(world, DeckType.VillainArea, PlayArea.Villains);

        Assert.Equal([mine, theirs], Places.MainSchemes(world, sideScheme));
        Assert.Equal([mine, theirs], Places.MainSchemes(world, villain));
    }

    [Rule("rr:play-area.1")]
    [Fact]
    public void AnEventAPlayerPlaysResolvesFromTheirPlayArea()
    {
        // Fear No Evil states events and treacheries separately: "When a player
        // plays an event or reveals a treachery that refers to 'the main
        // scheme', that card only applies to the main scheme in that player's
        // play area." It needs no separate case, because rr:play-area.1 puts a
        // player's hand and discard pile in their play area -- so the card is
        // already somewhere and the general rule finds it.
        var world = Ordinary(players: 2);
        var mine = MainScheme(world, PlayArea.Of(0));
        MainScheme(world, PlayArea.Of(1));

        var fromHand = InPlayArea(world, DeckType.HandsArea, PlayArea.Of(0));
        var fromDiscard = InPlayArea(world, DeckType.DiscardPile, PlayArea.Of(0));

        Assert.Equal([mine], Places.MainSchemes(world, fromHand));
        Assert.Equal([mine], Places.MainSchemes(world, fromDiscard));
    }

    // ------------------------------------------------------ pack:mc11:game-areas

    [Fact]
    public void CardsInOneGameAreaCannotAffectAnother()
    {
        // "Cards and components in one game area cannot affect another game
        // area [...] Players cannot attack or defend enemies in other game
        // areas, and they cannot target any game elements in the other game
        // areas."
        var world = Ordinary(players: 2);
        var (mine, theirs) = Split(world);

        var myHero = InPlayArea(world, DeckType.HeroArea, PlayArea.Of(0));
        var theirHero = InPlayArea(world, DeckType.HeroArea, PlayArea.Of(1));

        Assert.False(Places.CanAffect(world, myHero, theirHero));
        Assert.False(Places.CanAffect(world, theirHero, myHero));
        Assert.True(Places.CanAffect(world, myHero, myHero));
        Assert.Same(mine, Places.GameAreaOf(world, myHero));
        Assert.Same(theirs, Places.GameAreaOf(world, theirHero));
    }

    [Fact]
    public void ACardInNoGameAreaReachesEveryone()
    {
        // pack:mc11:areas: "Stage 2B remains in play in a central location and
        // its text remains active for all players, though it is not part of any
        // other game area." mc11 names 2B as *the* exception to the partition,
        // so the exception has to fall out of where it sits -- if it needed a
        // flag on the card, the model would be wrong.
        var world = Ordinary(players: 2);
        Split(world);

        var central = InPlayArea(world, DeckType.MainSchemesArea, PlayArea.Villains);
        world.Detach(PlayArea.Villains);

        var myHero = InPlayArea(world, DeckType.HeroArea, PlayArea.Of(0));
        var theirHero = InPlayArea(world, DeckType.HeroArea, PlayArea.Of(1));

        Assert.Null(Places.GameAreaOf(world, central));
        Assert.True(Places.CanAffect(world, central, myHero));
        Assert.True(Places.CanAffect(world, central, theirHero));

        // The inferred half: reach is symmetric, so the players racing 2B can
        // still thwart it. See the remarks on `Places.CanAffect`.
        Assert.True(Places.CanAffect(world, myHero, central));
    }

    [Rule("rr:each-player")]
    [Fact]
    public void EachPlayerMeansThePlayersInYourGameAreaIncludingYou()
    {
        // pack:mc11:rules-clarifications: "'Each player' refers to each player
        // in the same game area. If you are the only person in your game area,
        // then 'each player' refers only to you."
        //
        // The second sentence is the one that matters: the answer is not "the
        // others", and an implementation that read it that way would be wrong
        // by one in exactly the case the clarification exists to settle.
        var world = Ordinary(players: 3);
        var mine = world.CreateGameArea();
        world.Join(PlayArea.Of(0), mine);

        var myHero = InPlayArea(world, DeckType.HeroArea, PlayArea.Of(0));
        Assert.Equal([0], Places.EachPlayer(world, myHero));

        // Seats 1 and 2 are still together in the original area.
        var theirHero = InPlayArea(world, DeckType.HeroArea, PlayArea.Of(1));
        Assert.Equal([1, 2], Places.EachPlayer(world, theirHero));
    }

    [Fact]
    public void AGameAreaHoldsAnyNumberOfPlayersIncludingNone()
    {
        // Kang gives each player their own, which makes "one game area per
        // player" the tempting model. God of Lies rules it out twice:
        // pack:mc55:game-areas has "a collection of 1 to 4 players who work as a
        // team to fight the villain in their game area", and puts Loki in "a
        // neutral game area that is outside of any group's game area".
        var world = Ordinary(players: 4);
        var groupA = world.CreateGameArea();
        var groupB = world.CreateGameArea();
        var neutral = world.CreateGameArea();

        world.Join(PlayArea.Of(0), groupA);
        world.Join(PlayArea.Of(1), groupA);
        world.Join(PlayArea.Of(2), groupB);
        world.Join(PlayArea.Of(3), groupB);
        world.Join(PlayArea.Villains, neutral);

        var teammate = InPlayArea(world, DeckType.HeroArea, PlayArea.Of(0));
        Assert.Equal([0, 1], Places.EachPlayer(world, teammate));
        Assert.DoesNotContain(neutral.PlayAreas, area => area.IsPlayers);
        Assert.Single(neutral.PlayAreas);
    }

    [Fact]
    public void JoiningIsOneOperationAndEveryCardComesAlong()
    {
        // pack:mc11:game-areas: "choose a game area and reorient the cards on
        // the table to indicate that you have joined that game area."
        //
        // The unit is the player, not the cards. PR #115 modelled a Kang split
        // as 47 cards changing a tag and was reverted for it, so this asserts
        // the shape rather than the outcome: one call moves the play area, and
        // every card in it follows because no card stores a game area.
        var world = Ordinary(players: 2);
        var (mine, theirs) = Split(world);

        var cards = new[]
        {
            InPlayArea(world, DeckType.HeroArea, PlayArea.Of(0)),
            InPlayArea(world, DeckType.AlliesArea, PlayArea.Of(0)),
            InPlayArea(world, DeckType.UpgradesArea, PlayArea.Of(0)),
        };
        Assert.All(cards, card => Assert.Same(mine, Places.GameAreaOf(world, card)));

        world.Join(PlayArea.Of(0), theirs);

        Assert.All(cards, card => Assert.Same(theirs, Places.GameAreaOf(world, card)));
        Assert.False(mine.Contains(PlayArea.Of(0)));
    }

    [Fact]
    public void APlayAreaIsInAtMostOneGameArea()
    {
        // Joining leaves whichever held it. Without that, a play area would
        // accumulate memberships and `GameAreaOf` would answer whichever was
        // made first -- which passes the split test and fails on the join.
        var world = Ordinary(players: 2);
        var (mine, theirs) = Split(world);

        world.Join(PlayArea.Of(0), theirs);

        Assert.Equal(1, world.GameAreas.Count(area => area.Contains(PlayArea.Of(0))));
        Assert.False(mine.Contains(PlayArea.Of(0)));
    }

    [Fact]
    public void JoiningAGameAreaFromAnotherWorldIsRefused()
    {
        var world = Ordinary(players: 1);
        var stranger = Ordinary(players: 1).CreateGameArea();

        Assert.Throws<ArgumentException>(() => world.Join(PlayArea.Of(0), stranger));
    }

    // ---------------------------------------------------------------- the wire

    [Fact]
    public void AnAreaReferenceCarriesThePlayAreaAndNotTheCardOwner()
    {
        // The confusion this whole model exists to end. A player's nemesis pile
        // is *theirs* -- so its play area is theirs -- and is the scenario's
        // property, so a card made in it is owned by -1. The wire type carries
        // the first. Picking the second would be invisible in an ordinary game
        // and wrong on exactly the cards where whose-is-it drives rules.
        var world = Ordinary(players: 2);
        var nemesis = world.Seats[1].Nemesis;

        Assert.Equal(World.Scenario, nemesis.CardOwner);
        Assert.Equal(PlayArea.Of(1), nemesis.PlayArea);

        var reference = Places.Reference(nemesis);
        Assert.Equal(1, reference.Owner);
        Assert.Equal("AsideDeck", reference.Zone);
        Assert.True(reference.IsIdentified);
    }

    [Fact]
    public void TheVillainsPlayAreaIsMinusOneOnTheWire()
    {
        var world = Ordinary(players: 1);
        var encounter = world.CreateArea(DeckType.EncounterDeck);

        Assert.Equal(PlayArea.Villains, encounter.PlayArea);
        Assert.Equal(-1, Places.Reference(encounter).Owner);
    }

    // ------------------------------------------------------------- composition

    [Fact]
    public void TheTwoPartitionsCompose()
    {
        // Nothing published needs both at once, but both say what a card cannot
        // reach, so the answer is the intersection rather than one overriding
        // the other. Asserted because getting it wrong is invisible until a
        // scenario does need both, and then it is a rules bug rather than a
        // crash.
        var world = Ordinary(players: 2);
        var mine = MainScheme(world, PlayArea.Of(0));
        MainScheme(world, PlayArea.Of(1));
        Split(world);

        // A side scheme in the villain's play area would see both main schemes
        // by the Fear No Evil rule -- but it is in the villain's game area, and
        // both main schemes are in the players'.
        var sideScheme = InPlayArea(world, DeckType.SideSchemesArea, PlayArea.Villains);
        Assert.Empty(Places.MainSchemes(world, sideScheme));

        // An ally beside the main scheme still sees it.
        var myAlly = InPlayArea(world, DeckType.AlliesArea, PlayArea.Of(0));
        Assert.Equal([mine], Places.MainSchemes(world, myAlly));
    }

    // ------------------------------------------------------------------ boards

    /// <summary>A world with seats and the single default game area.</summary>
    private static World Ordinary(int players)
    {
        var world = new World(new Printed(), players);
        for (int seat = 0; seat < players; seat++)
        {
            world.CreateSeat($"p{seat}");
        }

        return world;
    }

    /// <summary>Kang's split: one game area per player, plus the villain's.</summary>
    private static (GameArea Mine, GameArea Theirs) Split(World world)
    {
        var mine = world.CreateGameArea();
        var theirs = world.CreateGameArea();
        world.Join(PlayArea.Of(0), mine);
        world.Join(PlayArea.Of(1), theirs);
        return (mine, theirs);
    }

    private static Card MainScheme(World world, PlayArea where) =>
        InPlayArea(world, DeckType.MainSchemesArea, where);

    private static Card InPlayArea(World world, DeckType type, PlayArea where) =>
        world.CreateCard("01097b", world.CreateArea(type, where.Player, where));

    /// <summary>Enough printed data to make a card. None of it is read here.</summary>
    private sealed class Printed : ICardFacts
    {
        public CardKind Kind(string faceId) => CardKind.Unknown;

        public IReadOnlyList<string> Traits(string faceId) => [];

        public IReadOnlyDictionary<string, string> Attributes(string faceId) =>
            new Dictionary<string, string>();

        public long PrintedValue(string faceId, string attribute, int players, long fallback = 0) =>
            fallback;
    }
}
