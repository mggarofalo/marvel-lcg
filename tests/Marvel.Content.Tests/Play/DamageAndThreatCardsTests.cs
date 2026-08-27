using Marvel.Content.Setup;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>
/// The Rhino scenario's cards that deal damage and place threat.
/// </summary>
/// <remarks>
/// <para>
/// Both nodes go through the engine's own rule rather than at the token, and
/// that is the whole of what is worth testing here. Damage written straight to
/// <c>k_damage</c> would walk past <c>rr:tough.2</c> and past <c>rr:defeat</c>,
/// leaving a defeated character standing; threat written straight to
/// <c>k_threat</c> would walk past
/// <c>rr:main-scheme-main-scheme-deck.2</c> and carry the game on past its own
/// ending.
/// </para>
/// <para>
/// The other half is <b>who counts</b>. "Each hero" is not each identity and
/// <c>1 per player</c> is not <c>1</c> — two readings that are invisible at one
/// player in hero form, which is the only board the recording has.
/// </para>
/// </remarks>
public sealed class DamageAndThreatCardsTests
{
    private const string Campaign = "rhino";
    private const uint Seed = 12345;
    /// <summary>Captain Marvel's hero face — somebody else standing up.</summary>
    private const string CaptainMarvel = "01010a";

    private static readonly SetupCatalog Setup =
        SetupCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("setup", "setup.json")));

    private static readonly CardCatalog Cards =
        CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));

    [Rule("rr:form-change-form.5")]
    [Fact]
    public void ShockerDamagesEveryHeroAndNoAlterEgo()
    {
        // "Deal 1 damage to each hero." **Not each identity.**
        // `rr:form-change-form.5`: "while a player is in alter-ego form, card
        // abilities that interact with their hero do not interact with their
        // identity." So a player who has flipped down takes nothing, and the
        // board here has one of each so that the difference is visible.
        // Three players, and the alter-ego sits between the two heroes: a
        // reading that stopped at the first player, or at the first alter-ego,
        // would pass on any board where the flipped-down player is last.
        var world = Deal("spider_man", "she_hulk", "captain_marvel");
        world.Seats[0].IdentityCard.TurnTo(AuthoredCards.SpiderMan);
        world.Seats[2].IdentityCard.TurnTo(CaptainMarvel);

        RevealTreachery(world, AuthoredCards.Shocker);

        Assert.Equal(1, world.Seats[0].IdentityCard.Damage);
        Assert.Equal(0, world.Seats[1].IdentityCard.Damage);
        Assert.Equal(1, world.Seats[2].IdentityCard.Damage);
    }

    [Rule("rr:tough.2")]
    [Fact]
    public void ShockerGoesThroughTheDamageRulesRatherThanAtTheToken()
    {
        // A tough hero takes none of it: `rr:tough.2` prevents *all* of the
        // damage and discards a tough status card instead. This is what
        // separates a card that deals damage from a card that writes to
        // `k_damage`, and nothing else in the ability data would notice.
        var world = Deal();
        world.Seats[0].IdentityCard.TurnTo(AuthoredCards.SpiderMan);
        var hero = world.Seats[0].IdentityCard;
        Statuses.Give(world, hero, Statuses.Tough);

        RevealTreachery(world, AuthoredCards.Shocker);

        Assert.Equal(0, hero.Damage);
        Assert.False(Statuses.Has(world, hero, Statuses.Tough));
    }

    [Rule("rr:per-player-icon")]
    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    public void BreakinAndTakinPlacesOneThreatPerPlayerOnTopOfItsStartingThreat(int players)
    {
        // "Place an **additional** 1 [per player] threat here." Additional to
        // the printed starting threat, which `rr:side-scheme.1` has already
        // placed by the time a When Revealed resolves -- so the card's own text
        // adds one per player and does not restate the two.
        string[] heroes = players == 1
            ? ["spider_man"]
            : ["spider_man", "she_hulk", "captain_marvel"];
        var world = Deal(heroes);

        var side = RevealInPlay(world, AuthoredCards.BreakinAndTakin, out _);

        Assert.Equal(2 + players, side.Tokens.GetValueOrDefault("k_threat"));
    }

    [Fact]
    public void BombScareIsTheSameCardFromADifferentSet()
    {
        // Word for word Breakin' & Takin', and authored as its own row rather
        // than shared: the acceleration icon differs from the hazard icon, and
        // two cards that agree today are still two cards.
        var world = Deal();

        var side = RevealInPlay(world, AuthoredCards.BombScare, out var events);

        Assert.Equal(3, side.Tokens.GetValueOrDefault("k_threat"));

        // And it is on the wire. A node that moved the token and said nothing
        // would leave the board right and the event stream wrong, which is the
        // half a state assertion cannot see -- the digest is built from these.
        var placed = Assert.Single(
            events.OfType<FieldSet>(), change => change.Verb == "Place_Threat");
        Assert.Equal(side.ObjectId, placed.Card);
        Assert.Equal(2, placed.From);
        Assert.Equal(3, placed.To);
    }

    [Rule("rr:main-scheme-main-scheme-deck.2")]
    [Fact]
    public void ThreatFromACardCompletesTheMainSchemeLikeAnyOther()
    {
        // The reason `placeThreat` goes through `Threat.Place`. Nothing in
        // `rr:main-scheme-main-scheme-deck.2` cares what put the threat there,
        // so a card that pushes the main scheme to its target ends the game --
        // and an ability writing to `k_threat` would leave it running.
        //
        // Bomb Scare places on itself, so this uses the main scheme directly
        // through the same node the card uses.
        var world = Deal();
        var scheme = world.TheCardIn(DeckType.MainSchemesArea)!;
        long target = Cards.PrintedValue(scheme.FaceId, "TargetThreat", world.Players);
        scheme.PlaceTokens("k_threat", target - 1);

        Threat.Place(
            world, Cards, AuthoredCards.Runner(), scheme, 1, "a card ability", []);

        Assert.Equal(Outcome.VillainWins, world.Result);
    }

    /// <summary>Reveals a card and answers it, after its ability has run.</summary>
    private static Card RevealInPlay(World world, string faceId, out List<GameEvent> events)
    {
        var card = world.CreateCard(faceId, world.AreaOf(DeckType.RevealingArea));
        var landing = new List<GameEvent>();

        // `rr:reveal.5` puts a side scheme into play and `rr:side-scheme.1`
        // gives it its starting threat, both before step 3's ability -- so the
        // card has to be resolved into play first or "additional" has nothing
        // to be additional to.
        Reveal.Resolve(world, world.Facts, card, 0, landing);
        var abilities = AuthoredCards.Runner();
        events = [.. abilities.WhenRevealed(world, card, 0)];
        var asked = Sequence.Work(world, Cards, abilities, events);
        while (asked is not null)
        {
            Sequence.Answer(
                world, Cards, abilities, asked, Decision.Decline, events);
            asked = Sequence.Work(world, Cards, abilities, events);
        }
        return card;
    }

    private static void RevealTreachery(World world, string faceId)
    {
        var card = world.CreateCard(faceId, world.AreaOf(DeckType.RevealingArea));
        AuthoredCards.Runner().WhenRevealed(world, card, 0);
    }

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
