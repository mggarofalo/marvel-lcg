using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

// Prepared nonpublic area-routing probes; no Kang setup or ability admission.
public sealed class KangRoutingIndependentReviewTests : CoreCardTestBase
{
    [Theory]
    [InlineData(0, false)]
    [InlineData(1, false)]
    [InlineData(0, true)]
    [InlineData(1, true)]
    [Rule("rr:villain-phase.step.2.a")]
    public void MixedFormsRouteAttackAndSchemeIndependentlyOfPhysicalVillainOrder(
        int firstPlayer, bool interpreter)
    {
        // "The villain activates against the player"; pack:mc11:game-areas
        // forbids affecting another game area and retains player turn order.
        var (world, central, schemes, villains) = Board(firstPlayer);
        world.Seats[0].IdentityCard.TurnTo("01001a");
        ICardAbilities abilities = interpreter
            ? new AbilityRunner(AbilityCatalog.Parse("""{"cards":[]}""")) : new NoCardAbilities();
        world.Agenda.Add(new PhaseStep(new ActivationPlanOperation(), 1, 2, Plan: true));
        var events = new List<GameEvent>();
        int defenses = 0;

        while (Sequence.Work(world, Cards, abilities, events) is { } prompt)
        {
            Assert.Equal(Question.Defender, prompt.Asking);
            Assert.Equal(0, prompt.Player);
            Assert.True(prompt.Cancellable);
            Assert.True(++defenses <= 1);
            Sequence.Answer(world, Cards, abilities, prompt, Decision.Decline, events);
        }

        Assert.Equal(1, defenses);
        Assert.Equal(Modified(world, villains[0], "attack"), world.Seats[0].IdentityCard.Damage);
        Assert.Equal(0, world.Seats[1].IdentityCard.Damage);
        Assert.Equal(0, schemes[0].Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(Modified(world, villains[1], "scheme"), schemes[1].Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(0, central.Tokens.GetValueOrDefault("k_threat"));
        Assert.Single(events.OfType<AttackCompleted>());
        var boosts = events.OfType<CardsMoved>()
            .Where(moved => moved.From == Places.Reference(world.AreaOf(DeckType.EncounterDeck)))
            .Select(moved => moved.To.Host).ToArray();
        Assert.Equal(world.PlayerOrder.Select(seat => villains[seat].ObjectId), boosts);
        Assert.False(world.Agenda.IsBusy);
        Assert.Null(world.Activation);
        Assert.Equal(Outcome.Unfinished, world.Result);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SchemeDestinationFollowsSourcePlayAreaRatherThanPhysicalOwner(bool interpreter)
    {
        // pack:mc11:game-areas: cards in one game area cannot affect another.
        // The synthetic control deliberately gives the local enemy a foreign
        // physical owner; ownership and physical play-area placement are distinct.
        var (world, central, schemes, _) = Board(0);
        var minion = world.CreateCard("01121",
            world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1), cardOwner: 0));
        ICardAbilities abilities = interpreter
            ? new AbilityRunner(AbilityCatalog.Parse("""{"cards":[]}""")) : new NoCardAbilities();
        Assert.Equal(PlayArea.Of(1), Places.PlayAreaOf(minion));
        Assert.Equal(0, minion.Owner);
        Assert.Same(schemes[1], abilities.SchemeDestination(world, minion));
        Assert.NotSame(central, abilities.SchemeDestination(world, minion));
        Assert.NotSame(schemes[0], abilities.SchemeDestination(world, minion));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:scheme-enemy-activation.step.3")]
    public void MinionSchemeRetainsItsResponseWhenLocalSchemeIsAbsent(bool removeScheme)
    {
        // "Place threat on the main scheme equal to the scheming enemy's
        // modified SCH value." Quake's printed response follows a minion
        // scheming; it does not require threat to have been placed.
        var (world, central, schemes, _) = Board(0);
        if (removeScheme) World.MoveToTop(schemes[0], world.AreaOf(DeckType.RemovedArea));
        var minion = world.CreateCard("01121",
            world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        var quake = world.CreateCard("08012",
            world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        // Test-local copy of Quake's authored response; no product admission.
        var runner = new AbilityRunner(AbilityCatalog.Parse("""
            {"cards":[{"card":"08012","abilities":[{
              "trigger":{"event":"WhenSchemeEnds","timing":"Response","actor":"minion"},
              "cost":{"exhaust":"this"},
              "effect":{"dealDamage":{"cards":"trigger.actor","amount":2}}
            }]}]}
            """));
        world.Agenda.Add(new PhaseStep(new EnemySchemeOperation(), 1, 2,
            Subject: minion.ObjectId, Seat: 0));
        var events = new List<GameEvent>();

        var response = Assert.IsType<Prompt>(Sequence.Work(world, Cards, runner, events));
        Assert.Equal(0, response.Player);
        var offer = Assert.Single(response.Affordances, item => item.AnchorId == quake.ObjectId);
        Sequence.Answer(world, Cards, runner, response, Decision.Take(offer.Id), events);
        Sequence.Finish(world, Cards, runner, events);

        Assert.Equal(DeckType.EncounterDiscardPile, minion.Area.Type);
        Assert.Equal(0, central.Tokens.GetValueOrDefault("k_threat"));
        Assert.False(world.Agenda.IsBusy);
        Assert.Null(world.Activation);
        Assert.Equal(Outcome.Unfinished, world.Result);
    }

    private static (World World, Card Central, Card[] Schemes, Card[] Villains) Board(int firstPlayer)
    {
        var world = new World(Cards, 2, 97) { FirstPlayer = firstPlayer };
        for (int seat = 0; seat < 2; seat++)
        {
            var player = world.CreateSeat($"p{seat}");
            player.IdentityCard = world.CreateCard("01001a,01001b", player.Hero);
            player.IdentityCard.TurnTo("01001b");
            world.Join(PlayArea.Of(seat), world.CreateGameArea(), "prepared split", []);
        }
        var central = world.CreateCard("11008b", world.AreaOf(DeckType.MainSchemesArea));
        world.Detach(PlayArea.Villains, "prepared central scheme", []);
        Card[] villains = new Card[2];
        Card[] schemes = new Card[2];
        for (int seat = 1; seat >= 0; seat--)
        {
            var area = PlayArea.Of(seat);
            villains[seat] = world.CreateCard(seat == 0 ? "11002" : "11003",
                world.AreaOf(DeckType.VillainArea, area));
            schemes[seat] = world.CreateCard(seat == 0 ? "11009b" : "11010b",
                world.AreaOf(DeckType.MainSchemesArea, area));
            Reveal.EnterPlay(world, Cards, villains[seat], []);
        }
        // Hard to Keep Down has zero numeric boost icons and no boost ability.
        for (int card = 0; card < 4; card++)
            world.CreateCard("01104", world.AreaOf(DeckType.EncounterDeck));
        return (world, central, schemes, villains);
    }
}
