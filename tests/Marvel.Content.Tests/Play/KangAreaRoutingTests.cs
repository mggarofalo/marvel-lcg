using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Xunit;

namespace Marvel.Content.Tests.Play;

// Nonpublic prepared stage-II composition, not Kang setup or product admission.
public sealed class KangAreaRoutingTests : CoreCardTestBase
{
    [Theory]
    [InlineData(0, false)]
    [InlineData(1, false)]
    [InlineData(0, true)]
    [InlineData(1, true)]
    public void EachLocalKangSchemesOnceAgainstItsOwnPlayer(int firstPlayer, bool interpreter)
    {
        // pack:mc11:game-areas: "Cards and components in one game area cannot
        // affect another game area"; play still proceeds in turn order.
        var (world, central, schemes, villains) = Split(firstPlayer);
        ICardAbilities abilities = interpreter
            ? new AbilityRunner(AbilityCatalog.Parse("""{"cards":[]}""")) : new NoCardAbilities();
        world.Agenda.Add(new PhaseStep(new ActivationPlanOperation(), 1, 2, Plan: true));
        var events = new List<GameEvent>();

        Sequence.Finish(world, Cards, abilities, events);

        Assert.Equal(3, schemes[0].Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(3, schemes[1].Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(0, central.Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(2, world.AreaOf(DeckType.EncounterDiscardPile).Cards.Count);
        Assert.Equal(2, world.AreaOf(DeckType.EncounterDeck).Cards.Count);
        var boosts = events.OfType<CardsMoved>()
            .Where(moved => moved.From == Places.Reference(world.AreaOf(DeckType.EncounterDeck)))
            .Select(moved => moved.To.Host).ToArray();
        Assert.Equal(world.PlayerOrder.Select(player => villains[player].ObjectId), boosts);
        Assert.False(Places.CanAffect(world, villains[0], schemes[1]));
        Assert.True(Places.CanAffect(world, villains[0], schemes[0]));
        Assert.False(world.Agenda.IsBusy);
        Assert.Null(world.Activation);
        Assert.Equal(0, world.Random.Generator.WordsConsumed);
        Assert.Equal(Outcome.Unfinished, world.Result);
    }

    [Fact]
    public void OrdinarySharedVillainAndMainSchemeStillServeBothPlayers()
    {
        var world = new World(Cards, 2);
        for (int player = 0; player < 2; player++)
        {
            var seat = world.CreateSeat($"p{player}");
            seat.IdentityCard = world.CreateCard("01001a,01001b", seat.Hero);
            seat.IdentityCard.TurnTo("01001b");
        }
        var villain = world.CreateCard("01094", world.AreaOf(DeckType.VillainArea));
        var scheme = world.CreateCard("01097b", world.AreaOf(DeckType.MainSchemesArea));
        FillEncounterDeck(world);
        world.Agenda.Add(new PhaseStep(new ActivationPlanOperation(), 1, 2, Plan: true));

        Sequence.Finish(world, Cards, new NoCardAbilities(), []);

        Assert.Same(villain, EncounterRouting.VillainForPlayer(world, 0));
        Assert.Same(villain, EncounterRouting.VillainForPlayer(world, 1));
        Assert.Equal(4, scheme.Tokens.GetValueOrDefault("k_threat"));
        Assert.False(world.Agenda.IsBusy);
        Assert.Equal(Outcome.Unfinished, world.Result);
    }

    [Fact]
    public void AreaWithoutVillainStillActivatesItsMinionThenContinuesToTheNextPlayer()
    {
        var (world, central, schemes, villains) = Split(0);
        World.MoveToTop(villains[0], world.AreaOf(DeckType.RemovedArea));
        var minion = world.CreateCard("01103", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        world.CreateCard("01121", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        world.Agenda.Add(new PhaseStep(new ActivationPlanOperation(), 1, 2, Plan: true));
        var abilities = new NoCardAbilities();
        var events = new List<GameEvent>();
        var prompt = Assert.IsType<Prompt>(Sequence.Work(world, Cards, abilities, events));
        Assert.Equal(0, prompt.Player);
        Assert.Contains("There is no villain", prompt.Description);
        Sequence.Answer(world, Cards, abilities, prompt, Decision.Take(minion.ObjectId), events);
        Sequence.Finish(world, Cards, abilities, events);

        Assert.Null(EncounterRouting.VillainForPlayer(world, 0));
        Assert.Equal(2, schemes[0].Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(3, schemes[1].Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(0, central.Tokens.GetValueOrDefault("k_threat"));
        Assert.Single(world.AreaOf(DeckType.EncounterDiscardPile).Cards);
        Assert.False(world.Agenda.IsBusy);
        Assert.Equal(Outcome.Unfinished, world.Result);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void PlayersSharingAnAreaUseTheSameLocalVillainAndScheme(bool interpreter)
    {
        var (world, central, schemes, villains) = Split(1);
        world.Join(PlayArea.Of(1), world.GameAreaOf(PlayArea.Of(0))!, "prepared shared area", []);
        World.MoveToTop(villains[1], world.AreaOf(DeckType.RemovedArea));
        World.MoveToTop(schemes[1], world.AreaOf(DeckType.RemovedArea));
        world.CreateCard("01121", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1)));
        world.Agenda.Add(new PhaseStep(new ActivationPlanOperation(), 1, 2, Plan: true));
        ICardAbilities abilities = interpreter
            ? new AbilityRunner(AbilityCatalog.Parse("""{"cards":[]}""")) : new NoCardAbilities();

        Sequence.Finish(world, Cards, abilities, []);

        Assert.Equal(7, schemes[0].Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(0, central.Tokens.GetValueOrDefault("k_threat"));
        Assert.Same(villains[0], EncounterRouting.VillainForPlayer(world, 1));
        Assert.False(world.Agenda.IsBusy);
    }

    [Fact]
    public void MinionWithoutALocalSchemeDoesNotPlaceThreatOnCentralStageTwo()
    {
        var (world, central, schemes, villains) = Split(0);
        World.MoveToTop(villains[0], world.AreaOf(DeckType.RemovedArea));
        World.MoveToTop(schemes[0], world.AreaOf(DeckType.RemovedArea));
        world.CreateCard("01121", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        world.Agenda.Add(new PhaseStep(new ActivationPlanOperation(), 1, 2, Plan: true));

        Sequence.Finish(world, Cards, new NoCardAbilities(), []);

        Assert.Equal(0, central.Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(3, schemes[1].Tokens.GetValueOrDefault("k_threat"));
        Assert.False(world.Agenda.IsBusy);
        Assert.Null(world.Activation);
    }

    [Fact]
    public void AmbiguousVillainDoesNotChooseByPhysicalCardOrder()
    {
        var (world, _, _, _) = Split(0);
        world.CreateCard("11004",
            world.AreaOf(DeckType.VillainArea, PlayArea.Of(0)));
        Assert.Throws<RulesNotImplementedException>(() => EncounterRouting.VillainForPlayer(world, 0));
    }

    [Fact]
    public void AmbiguousMainSchemeDoesNotChooseByPhysicalCardOrder()
    {
        var (world, _, _, villains) = Split(0);
        world.CreateCard("11011b", world.AreaOf(DeckType.MainSchemesArea, PlayArea.Of(0)));
        Assert.Throws<RulesNotImplementedException>(() => EnemySchemeDestination.Resolve(world, villains[0]));
    }

    private static (World World, Card Central, Card[] Schemes, Card[] Villains) Split(int firstPlayer)
    {
        var world = new World(Cards, 2, 42) { FirstPlayer = firstPlayer };
        for (int player = 0; player < 2; player++)
        {
            var seat = world.CreateSeat($"p{player}");
            seat.IdentityCard = world.CreateCard("01001a,01001b", seat.Hero);
            seat.IdentityCard.TurnTo("01001b");
            world.Join(PlayArea.Of(player), world.CreateGameArea(), "prepared split", []);
        }
        var central = world.CreateCard("11008a,11008b", world.AreaOf(DeckType.MainSchemesArea));
        central.TurnTo("11008b");
        world.Detach(PlayArea.Villains, "prepared split", []);
        Card[] schemes = new Card[2];
        Card[] villains = new Card[2];
        for (int player = 0; player < 2; player++)
        {
            var local = PlayArea.Of(player);
            schemes[player] = world.CreateCard(player == 0 ? "11009b" : "11010b",
                world.AreaOf(DeckType.MainSchemesArea, local));
            villains[player] = world.CreateCard(player == 0 ? "11002" : "11003",
                world.AreaOf(DeckType.VillainArea, local));
            schemes[player].PlaceTokens("k_threat", player);
            Reveal.EnterPlay(world, Cards, villains[player], []);
        }
        FillEncounterDeck(world);
        return (world, central, schemes, villains);
    }

    private static void FillEncounterDeck(World world)
    {
        world.CreateCard("01104", world.AreaOf(DeckType.EncounterDeck));
        for (int count = 0; count < 3; count++)
            world.CreateCard("01101", world.AreaOf(DeckType.EncounterDeck));
    }
}
