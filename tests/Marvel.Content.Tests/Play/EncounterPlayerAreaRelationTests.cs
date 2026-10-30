using System.Text.Json.Nodes;
using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>
/// Synthetic contracts for the <c>obligations</c> player area, the
/// <c>activatedOrEngagedPlayer</c> relation and the <c>putIntoPlayEngaged</c>
/// discard result. Each replaces one row of the authored book; no played
/// scenario is claimed.
/// </summary>
public sealed class EncounterPlayerAreaRelationTests : CoreCardTestBase
{
    // Hydra Mercenary (01101) counts the obligations of the player it is
    // activating against, or else of its engaged player, as ATK; and gains 1 SCH
    // while that player is in hero form.
    private const string CountingMinion = """
        [{"trigger":{"timing":"Constant"},"effect":{"seq":[
          {"grant":{"card":"this","keyword":"attack","amount":{"count":{"inPlayerArea":
            {"area":"obligations","player":"activatedOrEngagedPlayer"}}}}},
          {"if":{"test":{"inForm":{"player":"activatedOrEngagedPlayer","form":"hero"}},
            "then":{"grant":{"card":"this","keyword":"scheme","amount":1}}}}]}}]
        """;

    // Breakin' & Takin' (01107) puts the first discarded minion into play
    // engaged with the player who defeated it.
    private const string EngagingScheme = """
        [{"trigger":{"event":"WhenCardDefeated","timing":"WhenDefeated","subject":"this"},
          "effect":{"discardUntil":{"from":"encounterDeck","kind":"minion",
            "then":"putIntoPlayEngaged","player":"defeater"}}}]
        """;

    [Fact]
    public void PutIntoPlayEngagedWithoutAPlayerIsRefused()
    {
        // The engaged player is never assumed.
        var thrown = Assert.Throws<AbilityException>(() => AbilityLowering.Book(AbilityCatalog.Parse("""
            {"cards":[{"card":"01107","abilities":[{
              "trigger":{"event":"WhenCardDefeated","timing":"WhenDefeated","subject":"this"},
              "effect":{"discardUntil":{"from":"encounterDeck","kind":"minion","then":"putIntoPlayEngaged"}}}]}]}
            """)));
        Assert.Contains("requires the engaged 'player'", thrown.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:obligation.5")]
    [Rule("rr:activation.2")]
    public void TheRelationReadsTheEngagedPlayerOutsideTheMinionsActivation(bool otherActivation)
    {
        // rr:obligation.5: a revealed obligation goes into the revealing player's
        // play area. rr:activation.2: "each minion engaged with a player activates
        // against that player". With no activation, or another enemy's activation
        // against player 1, the minion engaged with player 0 reads player 0.
        var (world, _) = Board(CountingMinion, "01101");
        var minion = KangTemporalFixture.Engaged(world, "01101", 0);
        var other = KangTemporalFixture.Engaged(world, "01101", 1);
        Obligations(world, 0, 2);
        Obligations(world, 1, 1);
        world.Seats[1].IdentityCard.TurnTo("01001b");
        if (otherActivation) world.Activation = new EnemyActivation(other.ObjectId, 1, Attacking: true);

        Assert.Equal(1 + 2, Modified(world, minion, "attack"));
        Assert.Equal(0 + 1, Modified(world, minion, "scheme"));
    }

    [Fact]
    [Rule("rr:activation.4")]
    public void TheRelationReadsTheActivatedPlayerDuringTheMinionsOwnActivation()
    {
        // rr:activation.4: card abilities "can also cause enemies to attack or
        // scheme". During its own activation against player 1, the minion engaged
        // with player 0 reads player 1's single obligation and alter-ego form.
        var (world, _) = Board(CountingMinion, "01101");
        var minion = KangTemporalFixture.Engaged(world, "01101", 0);
        Obligations(world, 0, 2);
        Obligations(world, 1, 1);
        world.Seats[1].IdentityCard.TurnTo("01001b");
        world.Activation = new EnemyActivation(minion.ObjectId, 1, Attacking: false);

        Assert.Equal(1 + 1, Modified(world, minion, "attack"));
        Assert.Equal(0, Modified(world, minion, "scheme"));
    }

    [Fact]
    [Rule("rr:when-defeated-abilities.2")]
    [Rule("rr:when-revealed-abilities.2")]
    public void PutIntoPlayEngagedEngagesTheDeclaredPlayerWithoutRevealing()
    {
        // rr:when-defeated-abilities.2 resolves the scheme's When Defeated text.
        // Player 1, not the first player, defeats it. rr:when-revealed-abilities.2:
        // a card put into play does not resolve When Revealed, so Wildrun (11044)
        // discards nothing from player 1's hand.
        var (world, f) = Board(EngagingScheme, "01107");
        var below = world.CreateCard("01104", world.AreaOf(DeckType.EncounterDeck));
        var wildrun = world.CreateCard("11044", world.AreaOf(DeckType.EncounterDeck));
        var top = world.CreateCard("01104", world.AreaOf(DeckType.EncounterDeck));
        world.CreateCard("01088", world.Seats[1].Hand);
        var scheme = world.CreateCard("01107", world.AreaOf(DeckType.SideSchemesArea));
        scheme.PlaceTokens("k_threat", 1);

        BasicThwartPowers.BasicThwart(world, Cards, 1, scheme, []);
        f.Finish();

        Assert.Equal(world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1)), wildrun.Area);
        Assert.Empty(world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)).Cards);
        Assert.Equal(DeckType.EncounterDiscardPile, top.Area.Type);
        Assert.Equal(DeckType.EncounterDeck, below.Area.Type);
        Assert.Single(world.Seats[1].Hand.Cards);
        Assert.False(world.Agenda.IsBusy);
    }

    private static void Obligations(World world, int player, int count)
    {
        for (int i = 0; i < count; i++)
            world.CreateCard("04163", world.AreaOf(DeckType.ObligationsArea, PlayArea.Of(player)));
    }

    // The authored book with one row's abilities replaced by the synthetic ones.
    private static (World World, RedSkullFixture Fixture) Board(string abilities, string face)
    {
        var book = JsonNode.Parse(File.ReadAllText(RepositoryPaths.Dataset("abilities", "abilities.json")))!;
        var row = book["cards"]!.AsArray().Single(card => (string?)card!["card"] == face)!;
        row["abilities"] = JsonNode.Parse(abilities);
        var runner = new AbilityRunner(AbilityCatalog.Parse(book.ToJsonString()));
        var (world, _) = KangTemporalFixture.Board(players: 2, encounter: 0);
        world.Abilities = runner;
        return (world, new RedSkullFixture(world, runner));
    }
}
