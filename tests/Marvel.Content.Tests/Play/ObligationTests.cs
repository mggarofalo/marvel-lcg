using Marvel.Content.Setup;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>
/// An obligation naming a player, and the player it is actually given to.
/// </summary>
/// <remarks>
/// <para>
/// <c>rr:obligation.4</c>: "if an obligation card is revealed from the
/// encounter deck and that obligation instructs that it must be given to a
/// specific player <i>(such as 'Give to the Peter Parker player')</i>, place
/// that obligation into the play area of the player who controls the
/// associated identity."
/// </para>
/// <para>
/// <b>At one player the named player and the revealing player are the same
/// seat</b>, which is why this went unnoticed for so long. Every test here has
/// two, and the whole point of each is that the two seats disagree.
/// </para>
/// </remarks>
public sealed class ObligationTests
{
    /// <summary>"Eviction Notice" — give to the Peter Parker player.</summary>
    private const string EvictionNotice = "01165";

    /// <summary>She-Hulk's obligation — give to the Jennifer Walters player.</summary>
    private const string LegalWork = "01160";

    private static readonly SetupCatalog Setup =
        SetupCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("setup", "setup.json")));

    private static readonly CardCatalog Cards =
        CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));

    [Rule("rr:obligation.4")]
    [Rule("rr:reveal.4.1")]
    [Fact]
    public void AnObligationGoesToThePlayerItNamesAndNotTheOneWhoRevealedIt()
    {
        // Spider-Man is seat 0 and She-Hulk is seat 1, and She-Hulk reveals
        // Peter Parker's obligation. `rr:reveal.4.1` is the mechanism: "if the
        // card specifies a player to give it to, **that player is considered to
        // be revealing it**."
        var world = Deal("spider_man", "she_hulk");
        var card = Dealt(world, EvictionNotice, to: 1);

        // Worked as far as the card's own question and no further. Eviction
        // Notice offers "remove this from the game" as one of its options, and
        // a policy that answers it would take the card off the board before
        // this could look at where it landed.
        Sequence.Work(world, Cards, AuthoredCards.Runner(), []);

        Assert.Equal(DeckType.ObligationsArea, card.Area.Type);
        Assert.Equal(PlayArea.Of(0), card.Area.PlayArea);
    }

    [Rule("rr:obligation.1")]
    [Fact]
    public void TheObligationsYouIsTheNamedPlayerToo()
    {
        // "Abilities on obligations that use the words 'you' or 'your' apply
        // only to the player whose play area the obligation is in." Eviction
        // Notice's first choice is "you may flip to alter-ego form", and the
        // player who must answer it is Peter Parker's — not She-Hulk's, who
        // turned the card over.
        var world = Deal("spider_man", "she_hulk");
        world.Seats[0].IdentityCard.TurnTo(AuthoredCards.SpiderMan);
        Dealt(world, EvictionNotice, to: 1);

        var asked = Sequence.Work(world, Cards, AuthoredCards.Runner(), []);

        Assert.NotNull(asked);
        Assert.Equal(0, asked!.Player);
    }

    [Rule("rr:obligation.5")]
    [Fact]
    public void AnObligationNamingNobodyInTheGameIsRemovedAndAnotherIsRevealed()
    {
        // "If an obligation cannot be given to the specified player for any
        // reason, **ignore the card's ability, remove it from the game, and
        // reveal an additional encounter card**."
        //
        // She-Hulk's obligation at a table with no She-Hulk. It cannot happen
        // through setup, which only shuffles in the obligations of identities
        // being played -- but a card ability can put one anywhere, and the
        // rule exists because the situation does.
        var world = Deal("spider_man", "spider_man");
        var card = Dealt(world, LegalWork, to: 0);
        int queued = world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(0)).Cards.Count;

        Run(world);

        Assert.Equal(DeckType.RemovedArea, card.Area.Type);

        // One went and one arrived: the replacement is dealt into the same
        // queue, which is how `rr:surge` already works, so step 4's loop
        // reveals it without a second mechanism.
        Assert.Equal(
            queued, world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(0)).Cards.Count);
    }

    [Rule("rr:acceleration-token.2")]
    [Rule("rr:choose-option")]
    [Fact]
    public void LegalWorkCanGiveTheMainSchemeAnAccelerationToken()
    {
        var world = Deal("she_hulk");
        var card = Dealt(world, LegalWork, to: 0);
        var scheme = world.TheCardIn(DeckType.MainSchemesArea)!;
        var runner = AuthoredCards.Runner();
        var events = new List<GameEvent>();

        var flip = Sequence.Work(world, Cards, runner, events)!;
        Assert.Equal(2, flip.Affordances.Count);
        Sequence.Answer(
            world, Cards, runner, flip,
            Decision.Take(flip.Affordances[1].Id), events);

        var consequence = Sequence.Work(world, Cards, runner, events)!;
        Assert.Equal(2, consequence.Affordances.Count);
        Sequence.Answer(
            world, Cards, runner, consequence,
            Decision.Take(consequence.Affordances[1].Id), events);
        Assert.Null(Sequence.Work(world, Cards, runner, events));

        Assert.Equal(1, scheme.Tokens[EncounterDeck.AccelerationToken]);
        Assert.Equal(DeckType.EncounterDiscardPile, card.Area.Type);
    }

    /// <summary>Puts a card in a seat's dealt queue and schedules its reveal.</summary>
    private static Card Dealt(World world, string faceId, int to)
    {
        var card = world.CreateCard(
            faceId, world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(to)));
        world.Agenda.Add(new PhaseStep(
            Steps.RevealEncounterCard, 1, 4, Subject: card.ObjectId, Seat: to));
        return card;
    }

    private static void Run(World world)
    {
        var abilities = AuthoredCards.Runner();
        var events = new List<GameEvent>();
        var asked = Sequence.Work(world, Cards, abilities, events);
        for (int answered = 0; asked is not null; answered++)
        {
            Assert.True(answered < 12, $"'{asked.Label}' is still being asked");
            Sequence.Answer(
                world, Cards, abilities, asked,
                asked.Cancellable ? Decision.Decline : Decision.Take(asked.Affordances[0].Id),
                events);
            asked = Sequence.Work(world, Cards, abilities, events);
        }
    }

    private static World Deal(params string[] heroes) => WorldSetup.Deal(
        Cards,
        Blueprints.From(Dealer.DealOrder(Setup, "rhino", heroes), Cards),
        [.. heroes.Select(hero => Setup.Hero(hero).Name)],
        12345);
}
