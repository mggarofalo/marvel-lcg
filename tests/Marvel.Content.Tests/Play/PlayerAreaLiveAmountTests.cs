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
/// grant. Each replaces one row of the authored book; no played scenario is
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
        var (world, f) = Board(CountingBoost("you"));
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
        var (world, f) = Board(CountingBoost("engagedPlayer"));
        world.CreateCard("01106", world.AreaOf(DeckType.EncounterDeck));
        KangTemporalFixture.Attack(world, f.Villain, 1);

        var thrown = Assert.Throws<RulesNotImplementedException>(() => f.Finish());
        Assert.Contains("unsupported live lasting amount", thrown.Message, StringComparison.Ordinal);
    }

    // The authored book with Stampede's abilities replaced by the synthetic ones.
    private static (World World, RedSkullFixture Fixture) Board(string abilities)
    {
        var book = JsonNode.Parse(File.ReadAllText(RepositoryPaths.Dataset("abilities", "abilities.json")))!;
        var row = book["cards"]!.AsArray().Single(card => (string?)card!["card"] == "01106")!;
        row["abilities"] = JsonNode.Parse(abilities);
        var runner = new AbilityRunner(AbilityCatalog.Parse(book.ToJsonString()));
        var (world, _) = KangTemporalFixture.Board(players: 2, encounter: 2);
        world.Abilities = runner;
        return (world, new RedSkullFixture(world, runner));
    }
}
