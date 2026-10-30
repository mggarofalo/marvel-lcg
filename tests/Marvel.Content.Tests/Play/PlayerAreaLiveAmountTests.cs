using System.Text.Json.Nodes;
using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>
/// Synthetic contracts for a lasting amount that counts the cards in the
/// resolving player's named play-area zone, including a lasting boost-icon
/// grant and a lasting ATK grant. Each replaces one row of the authored book; no played scenario is
/// claimed.
/// </summary>
public sealed class PlayerAreaLiveAmountTests : CoreCardTestBase
{
    // Stampede (01106) prints one boost icon; its boost gains one icon for each
    // card in the obligations area of the given player relation.
    private static string CountingBoost(string player) => """
        [{"trigger":{"event":"WhenCardRevealed","timing":"Boost","subject":"this"},
          "effect":{"grantUntil":{"card":"this","keyword":"boost_const",
            "amount":{"count":{"inPlayerArea":{"area":"obligations","player":"PLAYER"}}},
            "until":"EndOfActivation"}}}]
        """.Replace("PLAYER", player, StringComparison.Ordinal);

    // Avengers Mansion (01091): Action: exhaust this → your identity gets +1 ATK
    // for each obligation in your play area until the end of the round.
    private const string CountingGrant = """
        [{"trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"this"},
          "cost":{"exhaust":"this"},
          "effect":{"grantUntil":{"card":"you","keyword":"attack",
            "amount":{"count":{"inPlayerArea":{"area":"obligations","player":"you"}}},
            "until":"EndOfRound"}}}]
        """;

    [Fact]
    [Rule("rr:lasting-effects.3")]
    public void AGrantedAmountFollowsObligationsEnteringAndLeavingDuringItsDuration()
    {
        // rr:lasting-effects.3: "Lasting effects update whenever the game state
        // updates." Player 1, not the first player, resolves the grant with one
        // obligation; Spider-Man's ATK 2 then follows player 1's obligations area
        // as cards enter and leave it. Player 0's obligations never count.
        var (world, f) = Board(CountingGrant, "01091");
        var mansion = world.CreateCard("01091", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(1), cardOwner: 1));
        var identity = world.Seats[1].IdentityCard;
        KangTemporalFixture.Obligations(world, 1, 1);
        var action = Assert.Single(f.Runner.Actions(world, 1), ability => ability.Card == mansion.ObjectId);

        f.Runner.ExecuteAction(world, new PlayerAction(action, [], []));
        f.Finish();

        Assert.False(mansion.Ready);
        Assert.Equal(2 + 1, Modified(world, identity, "attack"));
        KangTemporalFixture.Obligations(world, 1, 2);
        KangTemporalFixture.Obligations(world, 0, 3);
        Assert.Equal(2 + 3, Modified(world, identity, "attack"));
        foreach (var obligation in world.AreaOf(DeckType.ObligationsArea, PlayArea.Of(1)).Cards.ToList())
            World.MoveToTop(obligation, world.AreaOf(DeckType.EncounterDiscardPile));
        Assert.Equal(2, Modified(world, identity, "attack"));
        Assert.Equal(2, Modified(world, world.Seats[0].IdentityCard, "attack"));
    }

    [Fact]
    [Rule("rr:lasting-effects.3")]
    public void TheAmountCountsOnlyTheCapturedPlayersZoneAndUpdatesWithTheBoard()
    {
        // rr:lasting-effects.3: "Lasting effects update whenever the game state
        // updates." Player 1's zone is read; player 0's obligations never count.
        var (world, _) = KangTemporalFixture.Board(players: 2, encounter: 0);
        KangTemporalFixture.Obligations(world, 0, 3);
        var amount = new ContinuousAmount.PlayerAreaCards(1, DeckType.ObligationsArea);

        Assert.Equal(0, amount.Evaluate(world));
        KangTemporalFixture.Obligations(world, 1, 2);
        Assert.Equal(2, amount.Evaluate(world));
        World.MoveToTop(world.AreaOf(DeckType.ObligationsArea, PlayArea.Of(1)).Cards[0],
            world.AreaOf(DeckType.EncounterDiscardPile));
        Assert.Equal(1, amount.Evaluate(world));
    }

    [Fact]
    [Rule("rr:boost-boost-icon")]
    [Rule("rr:attack-enemy-activation.step.3.c")]
    public void ABoostGrantRegistersALiveBoostIconAmountForTheActivatedPlayer()
    {
        // rr:attack-enemy-activation.step.3.c counts boost icons after the boost
        // ability. Rhino (ATK 2) attacks player 1, whose two obligations add two
        // icons to Stampede's one; player 0's three do not.
        var (world, f) = Board(CountingBoost("you"), "01106");
        var stampede = world.CreateCard("01106", world.AreaOf(DeckType.EncounterDeck));
        KangTemporalFixture.Obligations(world, 0, 3);
        KangTemporalFixture.Obligations(world, 1, 2);
        KangTemporalFixture.Attack(world, f.Villain, 1);

        f.Finish();

        Assert.Equal(DeckType.EncounterDiscardPile, stampede.Area.Type);
        Assert.Equal(2 + 1 + 2, world.Seats[1].IdentityCard.Damage);
        Assert.Equal(0, world.Seats[0].IdentityCard.Damage);
        Assert.False(world.Agenda.IsBusy);
    }

    [Fact]
    public void ALiveAreaCountForAnotherPlayerRelationRaises()
    {
        // Only the resolving player's zone is captured; another relation is not
        // guessed at.
        var (world, f) = Board(CountingBoost("engagedPlayer"), "01106");
        world.CreateCard("01106", world.AreaOf(DeckType.EncounterDeck));
        KangTemporalFixture.Attack(world, f.Villain, 1);

        var thrown = Assert.Throws<RulesNotImplementedException>(() => f.Finish());
        Assert.Contains("unsupported live lasting amount", thrown.Message, StringComparison.Ordinal);
    }

    // The authored book with one row's abilities replaced by the synthetic ones.
    private static (World World, RedSkullFixture Fixture) Board(string abilities, string face)
    {
        var book = JsonNode.Parse(File.ReadAllText(RepositoryPaths.Dataset("abilities", "abilities.json")))!;
        var row = book["cards"]!.AsArray().Single(card => (string?)card!["card"] == face)!;
        row["abilities"] = JsonNode.Parse(abilities);
        var runner = new AbilityRunner(AbilityCatalog.Parse(book.ToJsonString()));
        var (world, _) = KangTemporalFixture.Board(players: 2, encounter: 2);
        world.Abilities = runner;
        return (world, new RedSkullFixture(world, runner));
    }
}
