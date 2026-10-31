using System.Text.Json;
using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

// Independent prepared nonpublic review of actual stage-I transition and its fail-closed boundary.
public sealed class KangFirstSplitReviewTests : CoreCardTestBase
{
    [Fact]
    public void AccelerationCarriesForwardAndEverySideSchemeIsDiscardedWithItsAttachments()
    {
        // 11008a: "Place 1 acceleration token here for each side scheme in play,
        // then discard each side scheme." pack:mc11:rules-clarifications forbids
        // transferring Kang I's attachments and statuses to Kang II.
        var (world, kang, firstSide, _) = Board(1, false);
        var prior = world.TheCardIn(DeckType.MainSchemesArea)!;
        prior.PlaceTokens(EncounterDeck.AccelerationToken, 3);
        var otherSide = world.CreateCard("01098", world.AreaOf(DeckType.SideSchemesArea, PlayArea.Of(1)));
        var sideAttachment = world.CreateCard("01099", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(1), otherSide.ObjectId));
        var kangAttachment = world.CreateCard("01099", world.AreaOf(DeckType.UpgradesArea, PlayArea.Villains, kang.ObjectId));
        var status = world.CreateCard(Statuses.Confused, world.AreaOf(DeckType.StatusArea, PlayArea.Villains, kang.ObjectId));
        var events = new List<GameEvent>();
        Split(world, kang, events);
        var central = world.Cards.Single(card => card.FaceId == "11008b");
        Assert.Equal(5, central.Tokens.GetValueOrDefault(EncounterDeck.AccelerationToken));
        Assert.Equal(DeckType.EncounterDiscardPile, firstSide.Area.Type);
        Assert.Equal(DeckType.EncounterDiscardPile, otherSide.Area.Type);
        Assert.Equal(DeckType.EncounterDiscardPile, sideAttachment.Area.Type);
        Assert.Equal(DeckType.EncounterDiscardPile, kangAttachment.Area.Type);
        Assert.Equal(DeckType.RemovedArea, status.Area.Type);
        Assert.All(world.PlayerOrder, player => Assert.False(Statuses.Has(world,
            Assert.Single(world.AreaOf(DeckType.VillainArea, PlayArea.Of(player)).Cards), Statuses.Confused)));
        Assert.Equal(Outcome.Unfinished, world.Result);
    }

    [Theory]
    [Rule("rr:player-elimination.6")]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public void AnEliminatedPlayerRevealsNoStageThreeAtTheSplit(int players)
    {
        // "Effects that refer to the players in the game ignore eliminated
        // players." 11008a: "Each player reveals a random stage 3A in turn order."
        var (world, kang, _, _) = Board(0, false, players: players);
        var events = new List<GameEvent>();
        int eliminated = players - 1;
        Elimination.Eliminate(world, Cards, eliminated, "prepared elimination", events);
        Split(world, kang, events);

        Assert.Null(world.GameAreaOf(PlayArea.Of(eliminated)));
        Assert.Empty(world.AreaOf(DeckType.MainSchemesArea, PlayArea.Of(eliminated)).Cards);
        Assert.All(world.PlayerOrder, player =>
        {
            Assert.Single(world.AreaOf(DeckType.MainSchemesArea, PlayArea.Of(player)).Cards);
            Assert.Single(world.AreaOf(DeckType.VillainArea, PlayArea.Of(player)).Cards);
        });
        Assert.Equal(players - 1, world.Cards.Count(card => card.FaceId is "11009b" or "11010b" or "11011b" or "11012b"));
        Assert.Equal(Outcome.Unfinished, world.Result);
    }

    [Fact]
    public void ReachedUnimplementedLocalSchemeCompletionRaisesInsteadOfDeclaringDefeat()
    {
        // 11009b-11012b say "After this stage is completed" place Kang's Dominion
        // under stage 4A, then remove the local stage and join at phase end.
        // This slice does not implement that consequence, so it must raise.
        var (world, kang, _, _) = Board(0, false);
        Split(world, kang, []);
        var scheme = Assert.Single(world.AreaOf(DeckType.MainSchemesArea, PlayArea.Of(0)).Cards);
        Threat.Schedule(world, new ThreatBatch([scheme], null, 9, ThreatCause.CardAbility, "prepared local completion", 0));
        var error = Record.Exception(() => Sequence.Finish(world, Cards, world.Abilities, []));
        Assert.Equal(Outcome.Unfinished, world.Result);
        Assert.IsType<RulesNotImplementedException>(error);
    }

    private static void Split(World world, Card kang, List<GameEvent> events)
    {
        Agendas.Happening(world);
        Defeat.Character(world, Cards, kang, "prepared defeat", events);
        Sequence.Finish(world, Cards, world.Abilities, events);
        world.Agenda.Add(new PhaseStep(new PhaseTransitionOperation(PhaseTransitionKind.EndPlayerPhase), 1, 4));
        Sequence.Finish(world, Cards, world.Abilities, events);
    }

    private static (World World, Card Kang, Card Side, Card[] Minions) Board(int first, bool expert, bool observer = false, uint seed = 42, int players = 2)
    {
        var world = new World(Cards, players, seed) { FirstPlayer = first };
        for (int player = 0; player < players; player++)
        {
            var seat = world.CreateSeat($"p{player}");
            seat.IdentityCard = world.CreateCard(player == 0 ? "01001a,01001b" : "01019a,01019b", seat.Hero);
        }
        var scheme = world.CreateCard("11007a,11007b", world.AreaOf(DeckType.MainSchemesArea));
        scheme.TurnTo("11007b");
        var kang = world.CreateCard(expert ? "11034" : "11001", world.AreaOf(DeckType.VillainArea));
        var aside = world.AreaOf(DeckType.AsideDeck);
        world.CreateCard("11008a,11008b", world.AreaOf(DeckType.MainSchemesDeck));
        for (int index = 0; index < 4; index++)
        {
            world.CreateCard($"{11009 + index}a,{11009 + index}b", aside);
            world.CreateCard((expert ? 11035 + index : 11002 + index).ToString(System.Globalization.CultureInfo.InvariantCulture), aside);
        }
        var side = world.CreateCard("01098", world.AreaOf(DeckType.SideSchemesArea));
        Card[] minions = [world.CreateCard("01101", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0))),
            world.CreateCard("01101", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1)))];
        for (int count = 0; count < 6; count++) world.CreateCard("01101", world.AreaOf(DeckType.EncounterDeck));
        var rows = new List<object>();
        object Row(string card, string timing, string effect) => new { card, abilities = new[] { new {
            trigger = new { @event = timing == "WhenDefeated" ? "WhenCardDefeated" : "WhenCardRevealed", timing, subject = "this" },
            effect = new Dictionary<string, string> { [effect] = "this" } } } };
        rows.Add(Row(kang.FaceId, "WhenDefeated", "captureKangStageOneDefeat"));
        rows.Add(Row("11008a", "WhenRevealed", "splitKangStageTwo"));
        rows.Add(new { card = "11008b", abilities = Array.Empty<object>() });
        for (int index = 0; index < 4; index++)
        {
            rows.Add(Row($"{11009 + index}a", "WhenRevealed", "createKangGameArea"));
            rows.Add(new { card = $"{11009 + index}b", abilities = Array.Empty<object>() });
        }
        if (observer) rows.Add(new { card = "01099", abilities = new[] { new {
            trigger = new { @event = "WhenCardDefeated", timing = "ForcedInterrupt", subject = "attachedTo" },
            effect = new { placeThreat = new { scheme = new { query = "mainScheme" }, amount = 1 } } } } });
        world.Abilities = new AbilityRunner(AbilityCatalog.Parse(JsonSerializer.Serialize(new { cards = rows })));
        return (world, kang, side, minions);
    }
}

