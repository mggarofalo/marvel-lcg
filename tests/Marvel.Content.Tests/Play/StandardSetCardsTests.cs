using Marvel.Content.Setup;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>
/// The Standard set, which nearly every scenario in the game uses.
/// </summary>
/// <remarks>
/// <para>
/// Worth its own file because of how far these cards reach. Across the 135
/// campaigns in the dataset, "Shadow of the Past" appears in 132 and
/// "Exhaustion", "Masterplan" and "Under Fire" in 75 each — no other encounter
/// card in the pool comes close. A scenario that could not resolve them could
/// not be played at all.
/// </para>
/// </remarks>
public sealed class StandardSetCardsTests
{
    private const string Campaign = "rhino";
    private const uint Seed = 12345;

    private static readonly SetupCatalog Setup =
        SetupCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("setup", "setup.json")));

    private static readonly CardCatalog Cards =
        CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));

    [Rule("rr:exhausted")]
    [Rule("rr:you-your")]
    [Rule("rr:you-your.4")]
    [Fact]
    public void ExhaustionExhaustsTheRevealingPlayersIdentity()
    {
        // If an ability exhausts "you", its resolving player "exhausts their
        // identity." This is the second player's on a two-player board, so
        // "your" is a claim rather than a coincidence.
        var world = Deal("spider_man", "she_hulk");

        Reveal(world, AuthoredCards.Exhaustion, player: 1);

        Assert.True(world.Seats[0].IdentityCard.Ready);
        Assert.False(world.Seats[1].IdentityCard.Ready);
    }

    [Rule("rr:forced.5")]
    [Rule("rr:surge.1")]
    [Rule("rr:when-revealed-abilities.1")]
    [Fact]
    public void TheFirstPlayerOrdersPrintedAndKeywordWhenRevealedAbilities()
    {
        // Exhaustion has one printed When Revealed ability and the Surge
        // keyword supplies another at the same moment. The first player chooses
        // which initiates first; the remaining forced ability then resolves
        // without a second, vacuous ordering question.
        var world = Deal();
        var card = world.CreateCard(
            AuthoredCards.Exhaustion,
            world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(0)));
        world.Agenda.Add(new PhaseStep(
            Steps.RevealEncounterCard, 1, 4,
            Subject: card.ObjectId, Seat: 0));
        var runner = AuthoredCards.Runner();
        var events = new List<Marvel.Rules.Events.GameEvent>();
        int dealt = world.AreaOf(
            DeckType.DealtEncounterCardsDeck, PlayArea.Of(0)).Cards.Count;

        var asked = Sequence.Work(world, Cards, runner, events);

        Assert.NotNull(asked);
        Assert.Equal(world.FirstPlayer, asked.Player);
        Assert.Equal(Question.Order, asked.Asking);
        Assert.Contains(asked.Affordances, option => option.Label == "Surge");
        var printed = Assert.Single(
            asked.Affordances,
            option => option.Label.Contains("When Revealed", StringComparison.Ordinal));
        Sequence.Answer(
            world, Cards, runner, asked, Decision.Take(printed.Id), events);
        Assert.Null(Sequence.Work(world, Cards, runner, events));

        Assert.False(world.Seats[0].IdentityCard.Ready);
        Assert.Equal(
            dealt,
            world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(0)).Cards.Count);
        Assert.Equal(DeckType.EncounterDiscardPile, card.Area.Type);
    }

    [Rule("rr:exhausted")]
    [Fact]
    public void ExhaustingAnAlreadyExhaustedIdentityReportsNothing()
    {
        // Exhausted is a state and not a counter, so exhausting twice is not
        // two exhaustions -- and must not be two events, because the digest is
        // built from them.
        var world = Deal();
        world.Seats[0].IdentityCard.Exhaust();

        var events = Reveal(world, AuthoredCards.Exhaustion);

        Assert.False(world.Seats[0].IdentityCard.Ready);
        Assert.Empty(events);
    }

    [Rule("rr:reveal")]
    [Fact]
    public void UnderFireRevealsTheTopCardRatherThanDealingIt()
    {
        // **Revealed, not dealt.** `rr:deal-deal-an-encounter-card` puts a card
        // facedown in a queue that the *next* villain phase resolves; this one
        // is turned over now. The difference is a whole phase, and the card
        // says "reveal".
        var world = Deal();
        var deck = world.AreaOf(DeckType.EncounterDeck);
        var top = deck.Cards[^1];
        int queued = world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(0)).Cards.Count;

        Reveal(world, AuthoredCards.UnderFire);

        // Off the deck, on its way through a reveal step, and not in the queue.
        Assert.Equal(DeckType.RevealingArea, top.Area.Type);
        Assert.Equal(
            queued,
            world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(0)).Cards.Count);

        var scheduled = Assert.Single(world.Agenda.Outstanding);
        Assert.Equal(Steps.RevealEncounterCard, scheduled.What);
        Assert.Equal(top.ObjectId, scheduled.Subject);
        Assert.Equal(0, scheduled.Seat);
    }

    [Rule("rr:encounter-deck")]
    [Fact]
    public void UnderFireOnAnEmptyDeckReshufflesRatherThanDoingNothing()
    {
        // The deck can be empty when this resolves, and `EncounterDeck.TakeTop`
        // is what makes that a reshuffle rather than a silent no-op.
        var world = Deal();
        var deck = world.AreaOf(DeckType.EncounterDeck);
        var discard = world.AreaOf(DeckType.EncounterDiscardPile);
        foreach (var card in deck.Cards.ToList())
        {
            World.MoveToTop(card, discard);
        }

        Reveal(world, AuthoredCards.UnderFire);

        Assert.NotEmpty(deck.Cards);
        Assert.Single(world.Agenda.Outstanding);
    }

    [Rule("rr:reveal.step.2")]
    [Rule("rr:nemesis-encounter-set.3")]
    [Rule("rr:nemesis-encounter-set.4")]
    [Fact]
    public void ShadowOfThePastBringsTheNemesisSetIn()
    {
        // The "nemesis minion" is the minion in the identity's nemesis set,
        // and its "nemesis side scheme" is that set's side scheme. The card
        // that reaches furthest of any in the pool -- 132 of the 135
        // campaigns. Spider-Man's nemesis set is Vulture, Highway Robbery and
        // three treacheries, all set aside at the deal.
        //
        // "Put into play engaged with you" and "put it into play" are both
        // `rr:reveal.step.2` by card type, so both are a reveal rather than a
        // placement -- which is why the minion arrives engaged and the side
        // scheme arrives with its starting threat, neither of them stated here.
        var world = Deal();
        var aside = world.Seats[0].Nemesis;
        var minion = aside.Cards.Single(card => Cards.Kind(card.FaceId) == CardKind.Minion);
        var scheme = aside.Cards.Single(
            card => Cards.Kind(card.FaceId) == CardKind.EncounterSideScheme);
        int deck = world.AreaOf(DeckType.EncounterDeck).Cards.Count;

        Reveal(world, AuthoredCards.ShadowOfThePast);

        // Both taken out of the pile and both on their way through a reveal.
        Assert.Equal(DeckType.RevealingArea, minion.Area.Type);
        Assert.Equal(DeckType.RevealingArea, scheme.Area.Type);
        Assert.Equal(
            [minion.ObjectId, scheme.ObjectId],
            world.Agenda.Outstanding.Select(step => step.Subject));

        // "The rest" -- the three treacheries -- into the encounter deck, and
        // the pile empty. **The two revealed cards are not among the rest**,
        // which is why the reveal moves the card now rather than only
        // scheduling it.
        Assert.Empty(aside.Cards);
        Assert.Equal(deck + 3, world.AreaOf(DeckType.EncounterDeck).Cards.Count);
    }

    [Rule("rr:linked-card-title.1")]
    [Fact]
    public void ShadowOfThePastDoesNotShuffleLinkedCardsIntoTheEncounterDeck()
    {
        var world = Deal();
        var linked = world.CreateCard("53034", world.Seats[0].SetAside);

        Reveal(world, AuthoredCards.ShadowOfThePast);

        Assert.Contains(linked, world.Seats[0].SetAside.Cards);
        Assert.DoesNotContain(linked, world.AreaOf(DeckType.EncounterDeck).Cards);
    }

    [Fact]
    public void ShadowOfThePastSurgesWhenTheNemesisMinionHasGone()
    {
        // "If your nemesis minion does not enter the game this way, this card
        // gains surge." A second copy of the card, after the first has already
        // emptied the pile.
        var world = Deal();
        foreach (var card in world.Seats[0].Nemesis.Cards.ToList())
        {
            World.MoveToTop(card, world.AreaOf(DeckType.EncounterDeck));
        }

        int queued = world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(0)).Cards.Count;

        Reveal(world, AuthoredCards.ShadowOfThePast);

        Assert.Empty(world.Agenda.Outstanding);
        Assert.Equal(
            queued + 1,
            world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(0)).Cards.Count);
    }

    [Rule("rr:you-your")]
    [Fact]
    public void ItIsYourNemesisSetAndNotTheOtherPlayers()
    {
        // Every player has one, and this card takes the resolving player's.
        // Revealed by the second player of two, so a reading that took the
        // first would pass at one player and fail on a table.
        var world = Deal("spider_man", "she_hulk");
        var mine = world.Seats[1].Nemesis;
        var theirs = world.Seats[0].Nemesis.Cards.Count;

        Reveal(world, AuthoredCards.ShadowOfThePast, player: 1);

        Assert.Empty(mine.Cards);
        Assert.Equal(theirs, world.Seats[0].Nemesis.Cards.Count);
        Assert.All(world.Agenda.Outstanding, step => Assert.Equal(1, step.Seat));
    }

    [Rule("rr:player-side-scheme")]
    [Fact]
    public void MasterplanPilesThreatOnEverySideSchemeInPlay()
    {
        // "Place 4 threat on each side scheme." Four flat, not per player, and
        // **each** -- two schemes take four apiece rather than four between
        // them. `rr:player-side-scheme` calls a player's "the player card
        // equivalent of the side schemes found in the encounter deck" and puts
        // it in the same place, so it counts too.
        var world = Deal();
        var area = world.AreaOf(DeckType.SideSchemesArea);
        var scenarios = world.CreateCard("01107", area);
        var players = world.CreateCard("01108", area);

        Reveal(world, AuthoredCards.Masterplan);
        Finish(world, AuthoredCards.Runner(), []);

        Assert.Equal(4, scenarios.Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(4, players.Tokens.GetValueOrDefault("k_threat"));
        Assert.Empty(world.Agenda.Outstanding);
    }

    [Rule("rr:discard.4")]
    [Fact]
    public void MasterplanDigsForOneWhenThereIsNoneInPlay()
    {
        // "If there are no side schemes in play, discard cards from the top of
        // the encounter deck until a side scheme is discarded. Reveal that side
        // scheme." One card at a time and in order -- `rr:discard.4` -- so the
        // pile below the side scheme is everything the search passed over.
        var world = Deal();
        var deck = world.AreaOf(DeckType.EncounterDeck);
        var discard = world.AreaOf(DeckType.EncounterDiscardPile);
        int before = deck.Cards.Count;

        Reveal(world, AuthoredCards.Masterplan);

        var found = Assert.Single(world.Agenda.Outstanding);
        Assert.Equal(Steps.RevealEncounterCard, found.What);
        Assert.Equal(
            CardKind.EncounterSideScheme,
            Cards.Kind(world.Cards[found.Subject].FaceId));

        // Everything it passed over is in the discard pile, and the card it
        // found is out of it and on its way through a reveal.
        Assert.Equal(before - discard.Cards.Count - 1, deck.Cards.Count);
        Assert.Equal(DeckType.RevealingArea, world.Cards[found.Subject].Area.Type);
    }

    [Fact]
    public void MasterplanStopsWhenThereIsNoSideSchemeToFind()
    {
        // The bound, and it is a rule rather than a fear: `EncounterDeck.TakeTop`
        // reshuffles an empty deck, so a search for a card that is in neither
        // the deck nor the discard pile would go round for ever.
        var world = Deal();
        var discard = world.AreaOf(DeckType.EncounterDiscardPile);
        foreach (var card in world.AreaOf(DeckType.EncounterDeck).Cards.ToList())
        {
            if (Cards.Kind(card.FaceId) == CardKind.EncounterSideScheme)
            {
                World.MoveToTop(card, world.AreaOf(DeckType.RemovedArea));
            }
        }

        Reveal(world, AuthoredCards.Masterplan);

        Assert.Empty(world.Agenda.Outstanding);
        Assert.Empty(discard.Cards);
        Assert.NotEmpty(world.AreaOf(DeckType.EncounterDeck).Cards);
    }

    private static IReadOnlyList<Marvel.Rules.Events.GameEvent> Reveal(
        World world, string faceId, int player = 0)
    {
        var card = world.CreateCard(faceId, world.AreaOf(DeckType.RevealingArea));
        return AuthoredCards.Runner().WhenRevealed(world, card, player);
    }

    private static void Finish(
        World world, ICardAbilities abilities, List<Marvel.Rules.Events.GameEvent> events)
    {
        var asked = Sequence.Work(world, Cards, abilities, events);
        while (asked is not null)
        {
            Sequence.Answer(
                world, Cards, abilities, asked, Decision.Decline, events);
            asked = Sequence.Work(world, Cards, abilities, events);
        }
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
