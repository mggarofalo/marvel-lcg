using Marvel.Cards.Run;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

// The full authored book on a nonpublic dealt Kang board, from the split onward.
public sealed class KangSeparateAreaTests : CoreCardTestBase
{
    private const string StepOneTrigger = "villain phase, place threat";

    [Fact]
    [Rule("rr:acceleration-token.2.1")]
    [Rule("rr:main-scheme-main-scheme-deck.5")]
    public void AccelerationTokensParkOnStageTwoAndCarryIntoStageFour()
    {
        // 11008a: "Place 1 acceleration token here for each side scheme in play, then
        // discard each side scheme." 11008b: "When an acceleration token would be
        // placed on another scheme, place it here instead." MARVEL-894 Q1: the tokens
        // add no threat while the table is separated and carry into 4A.
        var events = new List<GameEvent>();
        var world = KangStageFlowFixture.Board(2, false, events: events);
        var bomb = world.CreateCard("01109", world.AreaOf(DeckType.SideSchemesArea));
        KangStageFlowFixture.Defeat(world, Assert.Single(world.AreaOf(DeckType.VillainArea).Cards), events);
        Assert.Null(KangStageFlowFixture.EndPlayerPhase(world, 1, events));
        var central = Assert.Single(world.AreaOf(DeckType.MainSchemesArea).Cards);
        Assert.Equal(DeckType.EncounterDiscardPile, bomb.Area.Type);
        Assert.Equal(1, central.Tokens.GetValueOrDefault(EncounterDeck.AccelerationToken));

        // An emptied encounter deck places its token (rr:encounter-deck.1) on 2B.
        foreach (var card in world.AreaOf(DeckType.EncounterDeck).Cards.ToArray())
            World.MoveToTop(card, world.AreaOf(DeckType.EncounterDiscardPile));
        Deal.EncounterCard(world, 0, "prepared deal", events);
        Assert.Equal(2, central.Tokens.GetValueOrDefault(EncounterDeck.AccelerationToken));
        Assert.All(world.Seats, seat => Assert.Equal(0, KangStageFlowFixture.LocalScheme(world, seat.Index)
            .Tokens.GetValueOrDefault(EncounterDeck.AccelerationToken)));
        int before = events.Count;
        KangStageFlowFixture.VillainPhase(world, 2, events);
        Assert.All(StepOneAmounts(events, before), amount => Assert.Equal(1, amount));

        // Both Kang II defeated: the last departure advances to 4A with the tokens.
        KangStageFlowFixture.Defeat(world, KangStageFlowFixture.LocalKang(world, 0), events);
        KangStageFlowFixture.JoinAreaOf(world, KangStageFlowFixture.EndPlayerPhase(world, 3, events), 1, events);
        KangStageFlowFixture.Defeat(world, KangStageFlowFixture.LocalKang(world, 1), events);
        Assert.Null(KangStageFlowFixture.EndPlayerPhase(world, 4, events));
        var four = Assert.Single(world.AreaOf(DeckType.MainSchemesArea).Cards);
        Assert.Equal("11013b", four.FaceId);
        Assert.Equal(2, four.Tokens.GetValueOrDefault(EncounterDeck.AccelerationToken));
        before = events.Count;
        KangStageFlowFixture.VillainPhase(world, 4, events);
        // 4B prints acceleration 1 per player: 2, plus the two tokens.
        Assert.Equal(new long[] { 4 }, StepOneAmounts(events, before));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [Rule("rr:acceleration-icon.1")]
    [Rule("rr:reveal.5")]
    public void ARevealedSideSchemeAcceleratesOnlyItsAreaAndFollowsItsGroup(int revealer)
    {
        // Bomb Scare's acceleration icon, revealed during the split, enters the
        // revealer's game area (MARVEL-894 Q15) and adds threat only to that area's
        // 3B (Q1). pack:mc11:game-areas: "Any side schemes that were in play in your
        // previous game area become part of the game area that you join."
        var events = new List<GameEvent>();
        var world = KangStageFlowFixture.Split(2, false, events);
        new RedSkullFixture(world, (AbilityRunner)world.Abilities).Reveal("01109", revealer);
        Sequence.Finish(world, Cards, world.Abilities, events);
        var bomb = Assert.Single(world.Cards, card => card.FaceId == "01109");
        Assert.Same(world.GameAreaOf(PlayArea.Of(revealer)), Places.GameAreaOf(world, bomb));
        var schemes = world.Seats.Select(seat => KangStageFlowFixture.LocalScheme(world, seat.Index).ObjectId).ToArray();
        int before = events.Count;
        KangStageFlowFixture.VillainPhase(world, 2, events);
        var placed = StepOne(events, before);
        Assert.Equal(2, placed.Single(field => field.Card == schemes[revealer]).To - placed.Single(field => field.Card == schemes[revealer]).From);
        Assert.Equal(1, placed.Single(field => field.Card == schemes[1 - revealer]).To - placed.Single(field => field.Card == schemes[1 - revealer]).From);

        KangStageFlowFixture.Defeat(world, KangStageFlowFixture.LocalKang(world, revealer), events);
        KangStageFlowFixture.JoinAreaOf(world, KangStageFlowFixture.EndPlayerPhase(world, 3, events), 1 - revealer, events);
        Assert.Same(world.GameAreaOf(PlayArea.Of(1 - revealer)), Places.GameAreaOf(world, bomb));
        before = events.Count;
        KangStageFlowFixture.VillainPhase(world, 3, events);
        Assert.Equal(new long[] { 2 }, StepOneAmounts(events, before));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [Rule("rr:hazard-icon")]
    public void AHazardIconDealsOnlyToPlayersInItsGameArea(int first)
    {
        // Breakin' & Takin's hazard icon in player 1's game area deals that player its
        // additional card whoever is first player.
        var events = new List<GameEvent>();
        var world = KangStageFlowFixture.Split(2, false, events);
        world.FirstPlayer = first;
        world.CreateCard("01107", world.AreaOf(DeckType.SideSchemesArea, PlayArea.Of(1)));
        int before = events.Count;

        KangStageFlowFixture.VillainPhase(world, 2, events);

        var hazards = events.Skip(before).OfType<CardsMoved>().Where(move => move.Trigger == "hazard").ToArray();
        Assert.Single(Assert.Single(hazards).Cards);
        Assert.Equal(1, Assert.Single(hazards).To.Owner);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:when-completed-abilities.1")]
    [Rule("rr:reveal.step.2")]
    public void ACompletedLocalStageStoresADominionThatStageFourReveals(bool expert)
    {
        // 11009b-11012b: "After this stage is completed, place 1 set-aside Kang's
        // Dominion facedown under stage 4A. At the end of the phase, remove Kang (...)
        // and this stage from the game and combine your game area with another game
        // area." 11013a: "Reveal each face down Kang's Dominion under this stage."
        var events = new List<GameEvent>();
        var world = KangStageFlowFixture.Split(2, expert, events);
        var scheme = KangStageFlowFixture.LocalScheme(world, 0);
        var kang = KangStageFlowFixture.LocalKang(world, 0);
        Agendas.Happening(world);
        Threat.Schedule(world, new ThreatBatch([scheme], null, 9, ThreatCause.CardAbility, "prepared threat", 0));
        Sequence.Finish(world, Cards, world.Abilities, events);
        Assert.Equal(Outcome.Unfinished, world.Result);
        var host = Assert.Single(world.Cards, card => card.FaceId == "11013a");
        var stored = Assert.Single(BeneathCards.Of(world, host));
        Assert.Equal("11023", stored.FaceId);
        Assert.False(stored.FaceUp);

        KangStageFlowFixture.JoinAreaOf(world, KangStageFlowFixture.EndPlayerPhase(world, 2, events), 1, events);
        Assert.Equal(DeckType.RemovedArea, scheme.Area.Type);
        Assert.Equal(DeckType.RemovedArea, kang.Area.Type);
        KangStageFlowFixture.Defeat(world, KangStageFlowFixture.LocalKang(world, 1), events);
        Assert.Null(KangStageFlowFixture.EndPlayerPhase(world, 3, events));

        Assert.Equal(DeckType.SideSchemesArea, stored.Area.Type);
        Assert.True(stored.FaceUp);
        var three = Assert.Single(world.AreaOf(DeckType.VillainArea).Cards);
        Assert.False(world.DamageAbilities.CanTakeDamage(world, three, world.Seats[0].IdentityCard));
        Assert.Equal(Outcome.Unfinished, world.Result);
    }

    private static FieldSet[] StepOne(List<GameEvent> events, int from) => [.. events.Skip(from).OfType<FieldSet>()
        .Where(field => field.Field == "k_threat" && field.Trigger == StepOneTrigger)];

    private static long[] StepOneAmounts(List<GameEvent> events, int from) =>
        [.. StepOne(events, from).Select(field => (field.To ?? 0) - (field.From ?? 0))];
}
