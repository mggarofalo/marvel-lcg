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
    [InlineData(false, true, true, true)]
    [InlineData(false, false, false, true)]
    [InlineData(false, false, true, true, "01182")]
    public void TotalDestructionReadsProjectedAbominationMembership(
        bool inPlay, bool departed, bool entered, bool removable, string? enteringFace = null)
    {
        // rr:in-play-and-out-of-play.4: "Card abilities only interact with, and can only
        // target, cards that are in play". A projected defeat removes Abomination; a
        // projected entry adds it, and a differently titled entry does not.
        var (world, scheme, abomination) = Board(inPlay);
        var entering = enteringFace is null ? abomination
            : world.CreateCard(enteringFace, world.AreaOf(DeckType.EncounterDiscardPile));
        var projection = Projection(world, departed ? abomination : null, entered ? entering : null);
        Assert.Equal(removable, AbilityThreatPermissions.CanRemoveThreat(
            world, AuthoredCards.Program, scheme, projection: projection));
    }

    [Rule("rr:reveal.step.1")]
    [Fact]
    public void AFacedownEntryRaisesRatherThanCountAsAbsent()
    {
        // rr:reveal.step.1: "Turn the encounter card faceup." The projected entry has
        // not settled its face, so its title is not compared.
        var (world, scheme, abomination) = Board(inPlay: false);
        abomination.TurnFaceDown();
        var error = Assert.Throws<RulesNotImplementedException>(() => AbilityThreatPermissions.CanRemoveThreat(
            world, AuthoredCards.Program, scheme, projection: Projection(world, null, abomination)));
        Assert.Contains("entering play facedown", error.Message);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, true)]
    public void APowerStateShowsAnEnteredMinionAsPresent(bool entered, bool prohibited)
    {
        // The power state starts every unengaged minion as unavailable; an entry
        // restores it, so Abomination outside play and no longer unavailable has entered.
        var (world, scheme, abomination) = Board(inPlay: false);
        var state = new AbilityPowerState(0, 0, 0, false, [], [], [], [], [],
            entered ? [] : [abomination.ObjectId], [], [], [], [], [], -1, 0, false);
        Assert.Equal(!prohibited, AbilityThreatPermissions.CanRemoveThreat(world, AuthoredCards.Program, scheme,
            projection: AbilityThreatPermissionProjection.From(world, state)));
    }

    [Theory]
    [InlineData("Hydra Patrol", "Hydra Patrol", true)]
    [InlineData("Hydra Patrol", "Abomination", false)]
    [InlineData("Abomination", "Abomination", false)]
    public void APowerEnteringAnUntrackedTestedCardRaisesBeforeAnyChange(string entering, string tested, bool raises)
    {
        // A power state shows no entry of a non-minion, so a presence test of the
        // side scheme it puts into play raises instead of reading it as absent. It
        // shows a minion's entry, so Abomination entering is read as present.
        var local = AbilityCatalog.Parse("""
            { "cards": [
              { "card": "AUNT", "abilities": [ {
                "trigger": { "event": "WhenActionTriggered", "timing": "Action", "subject": "game" },
                "cost": { "exhaust": "this" }, "labels": ["thwart"],
                "effect": {"thwart":{"target":{"titled":"Total Destruction"},"effect":{"seq":[
                  {"putIntoPlay":{"card":{"cardsIn":{"areas":["encounterDiscardPile"],"title":"ENTERING"}},"where":"WHERE"}},
                  {"removeThreat":{"scheme":{"titled":"Total Destruction"},"amount":2}}
                ]}}} } ] },
              { "card": "10027", "abilities": [ {
                "trigger": { "timing": "Constant", "subject": "this" },
                "effect": { "if": { "test": {"exists":{"titled":"TESTED"}}, "then": { "preventThreatRemoval": "this" } } } } ] }
            ] }
            """.Replace("AUNT", AuthoredCards.AuntMay, StringComparison.Ordinal)
            .Replace("TESTED", tested, StringComparison.Ordinal)
            .Replace("ENTERING", entering, StringComparison.Ordinal)
            .Replace("WHERE", entering == "Abomination" ? "engagedWithYou" : "printedDestination", StringComparison.Ordinal));
        var runner = new AbilityRunner(new AbilityBook(
            [.. AuthoredCards.Book.Abilities.Where(row => row.Card is not "10027" and not AuthoredCards.AuntMay), .. local.Abilities],
            AuthoredCards.Book.Authored.Concat(local.Authored).ToHashSet(StringComparer.Ordinal), AuthoredCards.Book.AttachTo));
        var (world, scheme, _) = Board(inPlay: false);
        world.Abilities = runner;
        world.CreateCard("04154", world.AreaOf(DeckType.EncounterDiscardPile));
        var source = InPlay(world, AuthoredCards.AuntMay);
        string before = world.Digest().Canonical();
        if (raises)
            Assert.Contains("tests its presence", Assert.Throws<RulesNotImplementedException>(
                () => runner.Actions(world, 0).ToArray()).Message);
        else
            Assert.Contains(runner.Actions(world, 0), offer => offer.Card == source.ObjectId);
        Assert.Equal(before, world.Digest().Canonical());
        Assert.Equal(4, scheme.Tokens["k_threat"]);
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
