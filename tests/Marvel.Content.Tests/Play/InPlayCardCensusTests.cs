using System.Collections.Immutable;
using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

// Synthetic selector and local-book proofs; Thor remains publicly unsupported.
public sealed class InPlayCardCensusTests : CoreCardTestBase
{
    private static readonly AbilityCardSelection Selection = new AbilityCardSelection.WithTrait(
        new AbilityCardSelection.Query(AbilityCardQuery.CardsInPlay), "ASGARD");

    [Theory]
    [InlineData("06001a")]
    [InlineData("06001b")]
    [Rule("rr:in-play-and-out-of-play.1")]
    [Rule("rr:in-play-and-out-of-play.2")]
    [Rule("rr:in-play-and-out-of-play.6")]
    public void CensusCountsEachPhysicalCardAcrossKindsHostsAndSeats(string form)
    {
        // "the faceup side of a player's identity card is in play";
        // "faceup ally cards, support cards, and upgrade cards ... are in play".
        // "If a card is double-sided ... the faceup side ... is in play."
        var world = Hero("06001a,06001b", 2);
        world.Seats[0].IdentityCard.TurnTo(form);
        var source = world.CreateCard("06027", world.AreaOf(DeckType.SideSchemesArea));
        var ally = Add(world, "06002", DeckType.AlliesArea, 1);
        var hammer = world.CreateCard("06009", world.AreaOf(DeckType.UpgradesArea,
            PlayArea.Of(1), host: ally.ObjectId, cardOwner: 0));
        var unhosted = Add(world, "06010", DeckType.UpgradesArea, 0);
        var support = Add(world, "06007", DeckType.SupportsArea, 1);
        var loki = Add(world, "06028", DeckType.EngagedEnemiesArea, 1);
        var expected = new[] { world.Seats[0].IdentityCard, ally, hammer, unhosted, support, loki };
        Assert.Equal(expected.OrderBy(card => card.ObjectId), Evaluate(world, source).Every(Selection));
        Assert.Equal(6, Evaluate(world, source).Every(Selection).Distinct().Count());
        // The relation is generic, including encounter objects and every shared in-play area.
        var all = new AbilityCardSelection.Query(AbilityCardQuery.CardsInPlay);
        foreach (var area in new[] { DeckType.VillainArea, DeckType.MainSchemesArea,
            DeckType.EnvironmentArea, DeckType.ObligationsArea })
        {
            var card = world.CreateCard("06028", world.AreaOf(area));
            Assert.Contains(card, Evaluate(world, source).Every(all));
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:in-play-and-out-of-play.8")]
    [Rule("rr:in-play-and-out-of-play.9")]
    [Rule("rr:in-play-and-out-of-play.10")]
    [Rule("rr:in-play-and-out-of-play.11")]
    public void IdenticalOutOfPlayCardsCannotJoinOrExposeTheCensus(bool faceUp)
    {
        // "Cards in a player's hand, deck, and discard pile are out of play."
        // "facedown encounter cards dealt to a player are out of play";
        // "Facedown cards attached to in-play cards are out of play";
        // "removed from the game or ... set aside are out of play".
        var world = Hero("01001a", 2);
        var source = world.CreateCard("06027", world.AreaOf(DeckType.SideSchemesArea));
        foreach (var area in new[] { world.Seats[1].Hand, world.Seats[1].Deck,
            world.AreaOf(DeckType.DiscardPile, PlayArea.Of(1)),
            world.AreaOf(DeckType.AsideDeck), world.AreaOf(DeckType.RemovedArea),
            world.AreaOf(DeckType.EncounterDeck), world.AreaOf(DeckType.EncounterDiscardPile),
            world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(1)),
            world.AreaOf(DeckType.VillainDeck), world.AreaOf(DeckType.MainSchemesDeck) })
            world.CreateCard("06007", area);
        var stored = world.CreateCard("06007", world.Seats[1].Hand);
        BeneathCards.Place(world, stored, source, faceUp, "test", []);
        var evaluation = Evaluate(world, source);
        Assert.Empty(evaluation.Every(Selection));
        Assert.Empty(evaluation.Result(0).Information);
        var visible = Add(world, "06007", DeckType.SupportsArea, 1);
        Assert.Equal([visible], evaluation.Every(Selection));
    }

    [Fact]
    public void GameAreaPartitionsAndCentralCardsUseEngineTopology()
    {
        // pack:mc11:game-areas: "Cards ... in one game area cannot affect another".
        // Central reach is the existing symmetric Places engine choice.
        var world = Hero("01001a", 2);
        var source = Add(world, "06007", DeckType.SupportsArea, 0);
        var foreign = Add(world, "06007", DeckType.SupportsArea, 1);
        var central = world.CreateCard("06028", world.AreaOf(DeckType.EngagedEnemiesArea));
        world.Join(PlayArea.Of(0), world.CreateGameArea(), "test", []);
        world.Join(PlayArea.Of(1), world.CreateGameArea(), "test", []);
        Assert.Equal([source], Evaluate(world, source).Every(Selection));
        world.Detach(PlayArea.Villains, "test", []);
        Assert.Equal(new[] { source, central }, Evaluate(world, source).Every(Selection));
        Assert.Contains(foreign, Evaluate(world, central).Every(Selection));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(4)]
    public void SyntheticRevealAddsOneFrozenCountAfterPrintedStartingThreat(int count)
    {
        // 06027: "Place 1 additional threat here for each Asgard card in play."
        // A single amount expression schedules one threat placement; later entries
        // cannot change the committed quantity. This is not an authored card row.
        var runner = Runner("06027", "WhenRevealed", """
            {"placeThreat":{"scheme":"this","amount":{"count":{
              "withTrait":{"cards":{"query":"cardsInPlay"},"trait":"ASGARD"}}}}}
            """, eventName: Steps.CardRevealed);
        var world = Hero("01001a", 2);
        world.Abilities = runner;
        world.CreateCard("01094", world.AreaOf(DeckType.VillainArea));
        world.CreateCard("01097b", world.AreaOf(DeckType.MainSchemesArea));
        for (int i = 0; i < count; i++) Add(world, "06007", DeckType.SupportsArea, 1);
        var f = new RedSkullFixture(world, runner);
        var scheme = f.Reveal("06027", 1);
        f.Finish();
        Assert.Equal(2 + count, scheme.Tokens["k_threat"]);
        Add(world, "06007", DeckType.SupportsArea, 1);
        Assert.Equal(2 + count, scheme.Tokens["k_threat"]);
        Assert.False(world.Agenda.IsBusy);
    }

    [Fact]
    public void RepeatedQueriesPreserveCardsRandomAgendaAndSubsequentRandomDraw()
    {
        var world = Hero("06001a", 2);
        var control = Hero("06001a", 2);
        var source = Add(world, "06007", DeckType.SupportsArea, 1);
        Add(control, "06007", DeckType.SupportsArea, 1);
        var runner = Runner("06027", "WhenRevealed", """
            {"placeThreat":{"scheme":"this","amount":1}}
            """, eventName: Steps.CardRevealed);
        world.Abilities = runner;
        control.Abilities = runner;
        var probed = new RedSkullFixture(world, runner);
        var unprobed = new RedSkullFixture(control, runner);
        probed.Reveal("06027", 1);
        unprobed.Reveal("06027", 1);
        int pending = world.Agenda.Count;
        string digest = world.Digest().Canonical();
        var random = world.Random.Generator.GetState();
        long words = world.Random.Generator.WordsConsumed;
        int areas = world.Areas.Count;
        bool busy = world.Agenda.IsBusy;
        for (int i = 0; i < 5; i++) Assert.Equal(2, Evaluate(world, source).Every(Selection).Count);
        Assert.Equal(digest, world.Digest().Canonical());
        Assert.Equal(areas, world.Areas.Count);
        Assert.Equal(busy, world.Agenda.IsBusy);
        Assert.Equal(pending, world.Agenda.Count);
        Assert.Equal(words, world.Random.Generator.WordsConsumed);
        Assert.Equal(random.Index, world.Random.Generator.GetState().Index);
        Assert.Equal(random.Words, world.Random.Generator.GetState().Words);
        probed.Finish();
        unprobed.Finish();
        Assert.Equal(control.Digest().Canonical(), world.Digest().Canonical());
        Assert.Equal(control.Agenda.Count, world.Agenda.Count);
        Assert.Equal(control.Random.Generator.NextUInt32(), world.Random.Generator.NextUInt32());
    }

    [Fact]
    public void IncompleteProjectedPlacementRaisesInsteadOfUsingTheLiveCensus()
    {
        var world = Hero("06001a", 2);
        var source = Add(world, "06007", DeckType.SupportsArea, 1);
        var query = new AbilityQueryContext(world, source, new Occurrence(1, []), 1,
            source.Incarnation, null, null, null, []);
        var expressions = new AbilityExpressionContext(query, ImmutableDictionary<string, long>.Empty,
            [], "", 0, false, null);
        var context = new AbilityAdmissionContext(AbilityLowering.Book(AbilityCatalog.Parse("{\"cards\":[]}")),
            world.ResourceAbilities, expressions, new(), null);
        var state = new AreaProjectionState(context);
        state.Departed.Add(source.ObjectId);
        Assert.Contains("complete projected placement", Assert.Throws<RulesNotImplementedException>(() =>
            AbilityProjectedStateQueries.ProjectedEvery(Selection, state, context)).Message);
        var scope = new AbilityAdmission.AbilityAdmissionScope(context, []);
        var trace = new AbilitySelectorTraceContext(-1, scope, [], [], [], []);
        Assert.Contains("complete projected placement", Assert.Throws<RulesNotImplementedException>(() =>
            AbilityRepeatedSelectorTrace.TraceQueryMatches(AbilityCardQuery.CardsInPlay, source, trace)).Message);
    }

    private static Card Add(World world, string face, DeckType type, int player) =>
        world.CreateCard(face, world.AreaOf(type, PlayArea.Of(player), cardOwner: player));

    private static AbilitySelectorEvaluation Evaluate(World world, Card source) =>
        new(new AbilityQueryContext(world, source, new Occurrence(1, []), 1,
            source.Incarnation, null, null, null, []));
}
