using System.Collections.Immutable;
using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>Independent synthetic contracts for the global support query.</summary>
public sealed class GlobalSupportQueryReviewTests : CoreCardTestBase
{
    [Fact]
    public void EffectiveSupportKindOverridesPrintedKindAndStorageArea()
    {
        // Engine profiles replace active characteristics; storage alone does not
        // make a card a support. The printed catalog remains unchanged.
        var world = Hero("01001a", 2);
        var changedAway = world.CreateCard("05006", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0)));
        changedAway.AssignProfile(new("review-ally", "Synthetic ally", CardKind.Ally, [], ImmutableDictionary<string, long>.Empty));
        var changedInto = world.CreateCard("01008", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(1)));
        changedInto.AssignProfile(new("review-support", "Synthetic support", CardKind.Support, ["PERSONA"], ImmutableDictionary<string, long>.Empty));
        var ordinary = world.CreateCard("01091", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(1)));
        var evaluation = new AbilitySelectorEvaluation(Query(world));
        var supports = new AbilityCardSelection.Query(AbilityCardQuery.Supports);

        Assert.Equal([changedInto, ordinary], evaluation.Every(supports));
        Assert.Equal([changedInto], evaluation.Every(new AbilityCardSelection.WithTrait(supports, "PERSONA")));
        Assert.Equal(CardKind.Support, Cards.Kind(changedAway.FaceId));
        Assert.Equal(CardKind.Upgrade, Cards.Kind(changedInto.FaceId));
        var expressions = new AbilityExpressionContext(Query(world), ImmutableDictionary<string, long>.Empty, [], "", 0, false, null);
        var context = new AbilityAdmissionContext(AbilityLowering.Book(AbilityCatalog.Parse("{\"cards\":[]}")),
            world.ResourceAbilities, expressions, new(), null);
        var scope = new AbilityAdmission.AbilityAdmissionScope(context, []);
        var trace = new AbilitySelectorTraceContext(-1, scope, [], [], [], []);
        Assert.Equal(changedInto.ObjectId, AbilityRepeatedSelectorTrace.TraceSelectorIncludesCard(supports, changedInto.ObjectId, trace));
        Assert.Null(AbilityRepeatedSelectorTrace.TraceSelectorIncludesCard(supports, changedAway.ObjectId, trace));
        trace.Discarded.Add(changedInto.ObjectId);
        Assert.Null(AbilityRepeatedSelectorTrace.TraceSelectorIncludesCard(supports, changedInto.ObjectId, trace));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:in-play-and-out-of-play.8")]
    [Rule("rr:in-play-and-out-of-play.10")]
    [Rule("rr:in-play-and-out-of-play.11")]
    public void BeneathCardsAndOtherOutOfPlayPersonasDoNotJoinTheSupportCount(bool faceUp)
    {
        // "Cards in a player's hand, deck, and discard pile are out of play."
        // "Facedown cards attached to in-play cards are out of play."
        // "Any cards ... removed from the game or ... set aside are out of play."
        // Engine choice: the beneath pile is out of play for either orientation.
        var world = Hero("01001a", 2);
        var host = world.CreateCard("01091", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(1)));
        var persona = world.CreateCard("05006", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0)));
        var beneath = world.CreateCard("05008", world.Seats[1].Hand);
        BeneathCards.Place(world, beneath, host, faceUp, "review", []);
        foreach (var area in new[] { world.Seats[0].Hand, world.Seats[0].Deck,
            world.AreaOf(DeckType.DiscardPile, PlayArea.Of(0)),
            world.AreaOf(DeckType.RemovedArea), world.AreaOf(DeckType.AsideDeck) })
            world.CreateCard("05008", area);
        var evaluation = new AbilitySelectorEvaluation(Query(world));
        var selected = new AbilityCardSelection.WithTrait(new AbilityCardSelection.Query(AbilityCardQuery.Supports), "PERSONA");

        Assert.Equal(faceUp, beneath.FaceUp);
        Assert.Equal([persona], evaluation.Every(selected));
    }

    [Fact]
    public void ProjectedSupportEntryAndDepartureUpdateMembershipAcrossSeats()
    {
        // Synthetic admission projection: entering a support must supply the
        // later query, while an earlier departure removes an existing support.
        var world = Hero("01001a", 2);
        var leaving = world.CreateCard("05006", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0)));
        var entering = world.CreateCard("05008", world.Seats[1].Hand);
        var query = Query(world);
        var expression = new AbilityExpressionContext(query, ImmutableDictionary<string, long>.Empty, [], "", 0, false, null);
        var context = new AbilityAdmissionContext(AbilityLowering.Book(AbilityCatalog.Parse("{\"cards\":[]}")),
            world.ResourceAbilities, expression, new(), null);
        var state = new AreaProjectionState(context);
        state.Departed.Add(leaving.ObjectId);
        state.Entered.Add(entering.ObjectId);
        var selector = new AbilityCardSelection.Query(AbilityCardQuery.Supports);

        Assert.Equal([leaving], context.Selectors().Every(selector));
        Assert.Equal([entering], AbilityProjectedStateQueries.ProjectedEvery(selector, state, context));
        Assert.Same(world.Seats[1].Hand, entering.Area);
    }

    private static AbilityQueryContext Query(World world) => new(world, world.Seats[1].IdentityCard,
        new Occurrence(1, []), 1, world.Seats[1].IdentityCard.Incarnation, null, null, null, []);
}


