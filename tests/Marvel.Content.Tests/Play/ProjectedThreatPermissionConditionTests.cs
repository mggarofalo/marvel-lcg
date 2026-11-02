using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>A conditional threat-removal prohibition evaluated against a projected board.</summary>
public sealed class ProjectedThreatPermissionConditionTests : CoreCardTestBase
{
    private const string Present = """{"exists":{"titled":"Abomination"}}""";

    // Total Destruction: "Threat cannot be removed from this scheme while Abomination is in play."
    [Rule("rr:in-play-and-out-of-play.4")]
    [Theory]
    [InlineData(true, false, false, false)]
    [InlineData(true, true, false, true)]
    [InlineData(false, false, true, false)]
    [InlineData(false, false, false, true)]
    public void TotalDestructionReadsProjectedAbominationMembership(
        bool inPlay, bool departed, bool entered, bool removable)
    {
        // rr:in-play-and-out-of-play.4: "Card abilities only interact with, and can only
        // target, cards that are in play". A projected defeat removes Abomination; a
        // projected entry adds it.
        var (world, scheme, abomination) = Board(inPlay);
        var projection = Projection(world, departed ? abomination : null, entered ? abomination : null);
        Assert.Equal(removable, AbilityThreatPermissions.CanRemoveThreat(
            world, AuthoredCards.Program, scheme, projection: projection));
    }

    [Theory]
    [InlineData("""{"not":PRESENT}""", true)]
    [InlineData("""{"and":[PRESENT,{"not":PRESENT}]}""", true)]
    [InlineData("""{"or":[PRESENT,{"not":PRESENT}]}""", false)]
    public void ProjectedPresenceTestsCombine(string test, bool removable)
    {
        var (world, scheme, _) = Board(inPlay: true);
        Assert.Equal(removable, AbilityThreatPermissions.CanRemoveThreat(
            world, Prohibition(test.Replace("PRESENT", Present, StringComparison.Ordinal)), scheme,
            projection: Projection(world, null, null)));
    }

    [Fact]
    public void AnUnprojectedPredicateStillRaises()
    {
        var (world, scheme, _) = Board(inPlay: true);
        var program = Prohibition("""{"inForm":{"player":"firstPlayer","form":"hero"}}""");
        var error = Assert.Throws<RulesNotImplementedException>(() => AbilityThreatPermissions.CanRemoveThreat(
            world, program, scheme, projection: Projection(world, null, null)));
        Assert.Contains("projected predicate evaluation", error.Message);
    }

    private static (World World, Card Scheme, Card Abomination) Board(bool inPlay)
    {
        var world = Hero("01001a");
        world.CreateCard("01094", world.AreaOf(DeckType.VillainArea));
        var scheme = world.CreateCard("10027", world.AreaOf(DeckType.SideSchemesArea));
        scheme.PlaceTokens("k_threat", 4);
        var abomination = world.CreateCard("10026", inPlay
            ? world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0))
            : world.AreaOf(DeckType.EncounterDiscardPile));
        return (world, scheme, abomination);
    }

    private static AbilityThreatPermissionProjection Projection(World world, Card? departed, Card? entered) =>
        new(world, departed is null ? new HashSet<int>() : [departed.ObjectId],
            entered is null ? new HashSet<int>() : [entered.ObjectId], new Dictionary<int, int>());

    private static AbilityProgram Prohibition(string test) => AbilityLowering.Book(AbilityCatalog.Parse($$"""
        { "cards": [ { "card": "10027", "abilities": [ {
            "trigger": { "timing": "Constant", "subject": "this" },
            "effect": { "if": { "test": {{test}}, "then": { "preventThreatRemoval": "this" } } }
        } ] } ] }
        """));
}
