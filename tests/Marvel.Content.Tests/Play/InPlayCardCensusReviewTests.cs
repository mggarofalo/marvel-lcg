using System.Collections.Immutable;
using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

// Independent synthetic prerequisite checks; no production Thor ability is admitted.
public sealed class InPlayCardCensusReviewTests : CoreCardTestBase
{
    private static readonly AbilityCardSelection Asgard = new AbilityCardSelection.WithTrait(
        new AbilityCardSelection.Query(AbilityCardQuery.CardsInPlay), "ASGARD");

    [Fact]
    [Rule("rr:traits.2")]
    public void CensusUsesEffectiveTraitsAndRetainsExhaustedBlankedPrintedTraits()
    {
        // "Traits are not considered to be part of a card's printed text box."
        var world = Hero("01001a", 2);
        var source = world.Seats[0].IdentityCard;
        var printed = world.CreateCard("06028", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1)));
        printed.Exhaust();
        world.Effects.Register(PrintedText.UntilEndOfPhase(world, source, printed));
        var changedAway = world.CreateCard("06028", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1)));
        changedAway.AssignProfile(new("review-without-asgard", "Synthetic minion", CardKind.Minion, [], ImmutableDictionary<string, long>.Empty));
        var changedInto = world.CreateCard("01008", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(1), source.ObjectId));
        changedInto.AssignProfile(new("review-with-asgard", "Synthetic upgrade", CardKind.Upgrade, ["ASGARD"], ImmutableDictionary<string, long>.Empty));
        var gained = world.Seats[1].IdentityCard;
        world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect, Traits.Granted + "ASGARD",
            Card: source.ObjectId, Affects: gained.ObjectId, Lasts: Duration.UntilEndOf(TimingPoints.EndOfRound)));

        Assert.True(PrintedText.IsBlanked(world, printed));
        Assert.False(printed.Ready);
        Assert.Equal(new[] { gained, printed, changedInto }, Evaluate(world, source).Every(Asgard));
        Assert.Contains("ASGARD", Cards.Traits(changedAway.FaceId));
    }

    [Fact]
    public void EveryInPlayAreaContributesOnceWhileTransientAndConcealedAreasContributeNothing()
    {
        var world = Hero("01001a", 2);
        var source = world.Seats[1].IdentityCard;
        var expected = new List<Card> { world.Seats[0].IdentityCard, source };
        DeckType[] liveAreas = [DeckType.UpgradesArea, DeckType.AlliesArea, DeckType.SupportsArea,
            DeckType.EngagedEnemiesArea, DeckType.ObligationsArea, DeckType.MainSchemesArea,
            DeckType.SideSchemesArea, DeckType.VillainArea, DeckType.EnvironmentArea,
            DeckType.EvidenceArea, DeckType.RuleArea];
        foreach (var type in Enum.GetValues<DeckType>())
        {
            if (type == DeckType.HeroArea) continue;
            var card = world.CreateCard("06007", world.AreaOf(type));
            if (liveAreas.Contains(type)) expected.Add(card);
        }
        var all = new AbilityCardSelection.Query(AbilityCardQuery.CardsInPlay);
        var query = Evaluate(world, source);
        Assert.Equal(expected.OrderBy(card => card.ObjectId), query.Every(all));
        Assert.Equal(expected.Count, query.Every(all).Select(card => card.ObjectId).Distinct().Count());
        Assert.Empty(query.Result(0).Information);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:initiating-abilities.step.1")]
    public void ProjectedCensusAfterDepartureRefusesBeforeCostWhileUnchangedPlacementCanPay(bool departure)
    {
        // "Check play restrictions" precedes payment. A discarded source changes
        // placement; the control uses the same singular census and exhaustion cost.
        string effect = departure ? """
            {"seq":[{"discard":"this"},{"discard":{"last":{"query":"cardsInPlay"}}}]}
            """ : """
            {"discard":{"last":{"query":"cardsInPlay"}}}
            """;
        var runner = Runner(AuthoredCards.AuntMay, "Action", effect, cost: """{"exhaust":"this"}""");
        var world = Hero("01001a");
        world.Abilities = runner;
        var source = InPlay(world, AuthoredCards.AuntMay);
        var target = world.CreateCard("06007", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0)));
        world.CreateCard("01089", world.Seats[0].Deck);
        string before = world.Digest().Canonical();
        if (departure)
        {
            var error = Assert.Throws<RulesNotImplementedException>(() => runner.Actions(world, 0));
            Assert.Contains("singular area query", error.Message);
            Assert.Equal(before, world.Digest().Canonical());
            Assert.True(source.Ready);
            Assert.Equal(DeckType.SupportsArea, source.Area.Type);
        }
        else
        {
            var action = Assert.Single(runner.Actions(world, 0), action => action.Card == source.ObjectId);
            runner.ExecuteAction(world, new PlayerAction(action, [], []));
            Assert.False(source.Ready);
            Assert.Equal(DeckType.DiscardPile, target.Area.Type);
        }
    }

    [Fact]
    public void OneAggregateCountRemainsFrozenAcrossSeparateThreatPlacementOperations()
    {
        // Synthetic two-scheme batch: its single amount is captured before the
        // first placement interrupt removes the only Asgard card from play.
        var runner = new AbilityRunner(AbilityCatalog.Parse("""
            {"cards":[
              {"card":"02031","abilities":[{
                "trigger":{"event":"WhenCardRevealed","timing":"WhenRevealed","subject":"this"},
                "effect":{"placeThreat":{"scheme":{"query":"sideSchemes"},"amount":{"count":{
                  "withTrait":{"cards":{"query":"cardsInPlay"},"trait":"ASGARD"}}}}}}]},
              {"card":"06007","abilities":[{
                "trigger":{"event":"WhenThreatWouldBePlaced","timing":"ForcedInterrupt","subject":"game"},
                "effect":{"discard":"this"}}]}]}
            """));
        var world = Hero("01001a");
        world.Abilities = runner;
        world.CreateCard("01094", world.AreaOf(DeckType.VillainArea));
        world.CreateCard("01097b", world.AreaOf(DeckType.MainSchemesArea));
        world.CreateCard("01089", world.Seats[0].Deck);
        var asgard = world.CreateCard("06007", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        var first = world.CreateCard("06027", world.AreaOf(DeckType.SideSchemesArea));
        var second = world.CreateCard("06027", world.AreaOf(DeckType.SideSchemesArea));
        runner.ExecuteReveal(world, world.CreateCard("02031", world.AreaOf(DeckType.RevealingArea)), 0);
        new RedSkullFixture(world, runner).Finish();

        Assert.Equal(DeckType.DiscardPile, asgard.Area.Type);
        Assert.Empty(Evaluate(world, first).Every(Asgard));
        Assert.Equal(1, first.Tokens["k_threat"]);
        Assert.Equal(1, second.Tokens["k_threat"]);
        Assert.False(world.Agenda.IsBusy);
    }
    private static AbilitySelectorEvaluation Evaluate(World world, Card source) =>
        new(new AbilityQueryContext(world, source, new Occurrence(1, []), 0,
            source.Incarnation, null, null, null, []));
}





