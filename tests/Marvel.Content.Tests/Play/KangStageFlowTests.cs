using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

// The full authored book from stage 1 to Kang III on a nonpublic dealt board.
public sealed class KangStageFlowTests : CoreCardTestBase
{
    private static readonly string[] DeckBottomFirst = ["11013a", "11008a"];
    private static readonly long[] StartingThreat = [0, 1, 1, 2];

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:appendix-ii-setup.step.12.b")]
    [Rule("rr:when-defeated-abilities.2.1")]
    [Rule("rr:main-scheme-main-scheme-deck.step.3")]
    [Rule("rr:winning-the-game.1")]
    public void TheAuthoredStagesSplitRejoinAndReachKangThree(bool expert)
    {
        var events = new List<GameEvent>();
        var world = KangStageFlowFixture.Board(2, expert, events: events);

        // 11007b: "When Revealed: Deal each player an encounter card." Stages 2 and 4
        // wait in the main scheme deck, 2 on top (MARVEL-894 Q13).
        var stageOne = Assert.Single(world.AreaOf(DeckType.MainSchemesArea).Cards);
        Assert.Equal("11007b", stageOne.FaceId);
        Assert.All(world.Seats, seat => Assert.Single(world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(seat.Index)).Cards));
        var deck = world.AreaOf(DeckType.MainSchemesDeck).Cards;
        Assert.Equal(DeckBottomFirst, deck.Select(card => card.FaceId));

        // 11001: "When Defeated: Advance the main scheme to stage 2 at the end of the phase."
        KangStageFlowFixture.Defeat(world, Assert.Single(world.AreaOf(DeckType.VillainArea).Cards), events);
        Assert.Equal(Outcome.Unfinished, world.Result);
        Assert.Null(KangStageFlowFixture.EndPlayerPhase(world, 1, events));

        // 11008a/11009a-11012a: each player has a game area, a stage 3B and its Kang II;
        // 2B stays central (pack:mc11:areas).
        Assert.Null(world.GameAreaOf(PlayArea.Villains));
        Assert.NotSame(world.GameAreaOf(PlayArea.Of(0)), world.GameAreaOf(PlayArea.Of(1)));
        var central = Assert.Single(world.AreaOf(DeckType.MainSchemesArea).Cards);
        Assert.Equal("11008b", central.FaceId);
        var removed = world.AreaOf(DeckType.RemovedArea).Cards.Count(card => card.FaceId.StartsWith("1100", StringComparison.Ordinal)
            || card.FaceId.StartsWith("1101", StringComparison.Ordinal));
        foreach (var seat in world.Seats)
        {
            var scheme = KangStageFlowFixture.LocalScheme(world, seat.Index);
            var kang = KangStageFlowFixture.LocalKang(world, seat.Index);
            int index = int.Parse(scheme.FaceId[..5], System.Globalization.CultureInfo.InvariantCulture) - 11009;
            Assert.Equal($"{11009 + index}b", scheme.FaceId);
            Assert.Equal((expert ? 11035 : 11002) + index, int.Parse(kang.FaceId, System.Globalization.CultureInfo.InvariantCulture));
            Assert.Equal(StartingThreat[index], scheme.Tokens.GetValueOrDefault("k_threat"));
            Assert.True(Statuses.Has(world, kang, Statuses.Tough));
        }
        Assert.Equal(2, world.Cards.Count(card => card.Faces.Count == 2 && card.Faces[0].StartsWith("110", StringComparison.Ordinal)
            && card.Area.Type == DeckType.RemovedArea && card.Faces[0] is "11009a" or "11010a" or "11011a" or "11012a"));
        Assert.True(removed >= 3);

        // 11002-11005: "At the end of the phase, join another game area."
        KangStageFlowFixture.Defeat(world, KangStageFlowFixture.LocalKang(world, 0), events);
        var join = KangStageFlowFixture.EndPlayerPhase(world, 2, events);
        Assert.Null(KangStageFlowFixture.JoinAreaOf(world, join, 1, events));
        Assert.Same(world.GameAreaOf(PlayArea.Of(0)), world.GameAreaOf(PlayArea.Of(1)));
        Assert.Null(world.GameAreaOf(PlayArea.Villains));

        // 11008b: "When all the players have joined this game area, advance to stage 4A."
        KangStageFlowFixture.Defeat(world, KangStageFlowFixture.LocalKang(world, 1), events);
        Assert.Null(KangStageFlowFixture.EndPlayerPhase(world, 3, events));
        Assert.Same(world.GameAreaOf(PlayArea.Villains), world.GameAreaOf(PlayArea.Of(0)));
        Assert.Equal("11013b", Assert.Single(world.AreaOf(DeckType.MainSchemesArea).Cards).FaceId);
        var three = Assert.Single(world.AreaOf(DeckType.VillainArea).Cards);
        Assert.Equal(expert ? "11039" : "11006", three.FaceId);
        foreach (var seat in world.Seats)
            Assert.Equal(DeckType.EngagedEnemiesArea,
                Assert.Single(seat.NemesisSet, card => Cards.IsNemesisMinion(card.FaceId)).Area.Type);
        Assert.Equal(Outcome.Unfinished, world.Result);

        // 11006: "When Defeated: The players win the game."
        KangStageFlowFixture.Defeat(world, three, events);
        Assert.Equal(Outcome.PlayersWin, world.Result);
    }

    [Theory]
    [InlineData(1, 7)]
    [InlineData(2, 14)]
    [Rule("rr:main-scheme-main-scheme-deck.2")]
    [Rule("rr:winning-the-game.1")]
    public void CompletingStageOneLosesAlthoughLaterStagesRemain(int players, int target)
    {
        // 11007b: "If this stage is completed, the players lose the game." Its printed
        // per-player target is 7 for each player who started the scenario.
        var events = new List<GameEvent>();
        var world = KangStageFlowFixture.Board(players, false, events: events);
        var stageOne = Assert.Single(world.AreaOf(DeckType.MainSchemesArea).Cards);

        Agendas.Happening(world);
        Threat.Schedule(world, new ThreatBatch([stageOne], null, target - 1, ThreatCause.CardAbility, "prepared threat", 0));
        Sequence.Finish(world, Cards, world.Abilities, events);
        Assert.Equal(Outcome.Unfinished, world.Result);
        Agendas.Happening(world);
        Threat.Schedule(world, new ThreatBatch([stageOne], null, 1, ThreatCause.CardAbility, "prepared threat", 0));
        Sequence.Finish(world, Cards, world.Abilities, events);

        Assert.Equal(Outcome.VillainWins, world.Result);
        Assert.Equal(2, world.AreaOf(DeckType.MainSchemesDeck).Cards.Count);
        Assert.Equal("11007b", stageOne.FaceId);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [Rule("rr:villain-phase.1")]
    public void EachLocalSchemeTakesItsOwnStepOneThreatInTurnOrder(int first)
    {
        // Ruling 97d62cdb9ce436a7: "each player should place threat on their main
        // scheme in turn order". Each stage 3B prints acceleration 1; central 2B has
        // no threat values and takes none (MARVEL-894 Q1).
        var events = new List<GameEvent>();
        var world = KangStageFlowFixture.Split(2, false, events);
        world.FirstPlayer = first;
        var central = Assert.Single(world.AreaOf(DeckType.MainSchemesArea).Cards);
        int before = events.Count;

        KangStageFlowFixture.VillainPhase(world, 2, events);

        var placed = events.Skip(before).OfType<FieldSet>()
            .Where(field => field.Field == "k_threat" && field.Trigger == "villain phase, place threat").ToArray();
        Assert.Equal(new[] { first, 1 - first }.Select(seat => KangStageFlowFixture.LocalScheme(world, seat).ObjectId),
            placed.Select(field => field.Card));
        Assert.All(placed, field => Assert.Equal(1, field.To - field.From));
        Assert.Equal(0, central.Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(Outcome.Unfinished, world.Result);
    }
}
