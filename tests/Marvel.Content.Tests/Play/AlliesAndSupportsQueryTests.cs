using System.Collections.Immutable;
using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>Synthetic contracts for the alliesAndSupportsYouControl card query.</summary>
public sealed class AlliesAndSupportsQueryTests : CoreCardTestBase
{
    private static readonly AbilityCardSelection.Query AlliesAndSupports =
        new(AbilityCardQuery.AlliesAndSupportsYouControl);

    [Fact]
    public void TheSpellingLowersToTheQuery()
    {
        // The engine chooses the spelling; it lowers to one card query.
        Assert.Equal(AbilityCardQuery.AlliesAndSupportsYouControl,
            AbilitySelectorLowering.Query("alliesAndSupportsYouControl", new AbilityLocation("test", 0, "query")));
    }

    [Fact]
    [Rule("rr:you-your.10")]
    public void OnlyTheResolvingPlayersAlliesAndSupportsAreSelected()
    {
        // rr:you-your.10: "performs that game function against the card in their
        // play area." Player 1 resolves. Their upgrade and identity, and player 0's
        // ally and support, are not allies or supports player 1 controls.
        var world = Hero("01010a", 2);
        var ally = world.CreateCard("01059", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(1), cardOwner: 1));
        var support = world.CreateCard("01092", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(1), cardOwner: 1));
        world.CreateCard("01008", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(1), cardOwner: 1));
        world.CreateCard("01067", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        world.CreateCard("01091", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        world.CreateCard("01087", world.Seats[1].Hand);

        var selected = new AbilitySelectorEvaluation(Query(world)).Every(AlliesAndSupports);

        Assert.Equal(new[] { ally.ObjectId, support.ObjectId }.Order(), selected.Select(card => card.ObjectId).Order());
    }

    [Fact]
    public void APlayerWithoutAlliesOrSupportsSelectsNothing()
    {
        // Player 0's ally and support and player 1's upgrade do not count for player 1.
        var world = Hero("01010a", 2);
        world.CreateCard("01067", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        world.CreateCard("01091", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        world.CreateCard("01008", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(1), cardOwner: 1));

        Assert.Empty(new AbilitySelectorEvaluation(Query(world)).Every(AlliesAndSupports));
    }

    [Fact]
    public void TheAdmissionTraceIncludesOnlyTheResolvingPlayersAlliesAndSupports()
    {
        // Admission traces the same membership before an effect resolves.
        var world = Hero("01010a", 2);
        var ally = world.CreateCard("01059", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(1), cardOwner: 1));
        var support = world.CreateCard("01092", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(1), cardOwner: 1));
        var upgrade = world.CreateCard("01008", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(1), cardOwner: 1));
        var other = world.CreateCard("01067", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        var trace = Trace(world);

        Assert.Equal(ally.ObjectId, AbilityRepeatedSelectorTrace.TraceSelectorIncludesCard(AlliesAndSupports, ally.ObjectId, trace));
        Assert.Equal(support.ObjectId, AbilityRepeatedSelectorTrace.TraceSelectorIncludesCard(AlliesAndSupports, support.ObjectId, trace));
        Assert.Null(AbilityRepeatedSelectorTrace.TraceSelectorIncludesCard(AlliesAndSupports, upgrade.ObjectId, trace));
        Assert.Null(AbilityRepeatedSelectorTrace.TraceSelectorIncludesCard(AlliesAndSupports, other.ObjectId, trace));
    }

    [Fact]
    public void TheAdmissionTraceFollowsProjectedPlacementAcrossPlayAreas()
    {
        // Admission projects earlier placement changes in the same ability. Player
        // 0's support projected into player 1's play area is a candidate for player
        // 1, and player 1's ally projected away is not; the board itself is unchanged.
        var world = Hero("01010a", 2);
        var arriving = world.CreateCard("01091", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        var leaving = world.CreateCard("01059", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(1), cardOwner: 1));
        var staying = world.CreateCard("01092", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(1), cardOwner: 1));
        var trace = Trace(world);
        trace.Engagement[arriving.ObjectId] = 1;
        trace.Engagement[leaving.ObjectId] = 0;

        var candidates = AbilitySelectorTrace.MatchingCandidates(AlliesAndSupports, trace).Select(card => card.ObjectId);

        Assert.Equal(new[] { arriving.ObjectId, staying.ObjectId }.Order(), candidates.Order());
        Assert.Equal(PlayArea.Of(0), arriving.Area.PlayArea);
    }

    private static AbilitySelectorTraceContext Trace(World world)
    {
        var expressions = new AbilityExpressionContext(Query(world), ImmutableDictionary<string, long>.Empty, [], "", 1, false, null);
        var context = new AbilityAdmissionContext(AbilityLowering.Book(AbilityCatalog.Parse("{\"cards\":[]}")),
            world.ResourceAbilities, expressions, new(), null);
        return new AbilitySelectorTraceContext(-1, new AbilityAdmission.AbilityAdmissionScope(context, []), [], [], [], []);
    }

    private static AbilityQueryContext Query(World world) => new(world, world.Seats[1].IdentityCard,
        new Occurrence(1, []), 1, world.Seats[1].IdentityCard.Incarnation, null, null, null, []);
}
