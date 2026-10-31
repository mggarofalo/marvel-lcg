using Marvel.Rules.Setup;
using Marvel.Cards;
using Marvel.Cards.Dsl;
using Marvel.Content.Setup;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class WouldAttackPriorityTests
{
    private static readonly CardCatalog Cards = CardCatalog.Parse(
        File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));
    private static readonly SetupCatalog Setup = SetupCatalog.Parse(
        File.ReadAllText(RepositoryPaths.Dataset("setup", "setup.json")));

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    [Rule("rr:would")]
    [Rule("rr:would.1")]
    [Rule("rr:ability.step.2.a")]
    public void ReplacementPrecedesOrdinaryAttackTriggers(bool ultron, bool stunned)
    {
        // "Would" has higher priority than ordinary interrupts to the same
        // condition; after replacement no further original interrupt may be used.
        // Status forced interrupts precede card forced interrupts.
        var world = WorldSetup.DealWithoutCardAbilities(Cards,
            Blueprints.From(Dealer.DealOrder(Setup, ultron ? "ultron" : "rhino", ["spider_man"]), Cards),
            ["Spider-Man"], 12345);
        world.Seats[0].IdentityCard.TurnTo("01001a");
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        if (ultron)
        {
            World.MoveToTop(villain, world.AreaOf(DeckType.RemovedArea));
            villain = world.Cards.Single(card => card.FaceId == "01135");
            World.MoveToTop(villain, world.AreaOf(DeckType.VillainArea));
        }
        var charge = ultron ? null : world.CreateCard("01099",
            world.AreaOf(DeckType.UpgradesArea, host: villain.ObjectId));
        var web = world.CreateCard("01009", world.Seats[0].Hand);
        World.MoveToTop(web, world.AreaOf(DeckType.UpgradesArea, host: villain.ObjectId));
        if (stunned) Statuses.Give(world, villain, Statuses.Stunned);
        int drones = world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)).Cards.Count;
        int hand = world.Seats[0].Hand.Cards.Count;
        world.Agenda.Add(new PhaseStep(new EnemyAttackOperation(), Round: 1, Number: 2, Subject: villain.ObjectId, Seat: 0));
        var events = new List<GameEvent>();
        Assert.Null(Sequence.Work(world, Cards, AuthoredCards.Runner(), events));
        Assert.Equal(stunned ? DeckType.UpgradesArea : DeckType.DiscardPile, web.Area.Type);
        Assert.Equal(!stunned, Statuses.Has(world, villain, Statuses.Stunned));
        Assert.Equal(drones, world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)).Cards.Count);
        Assert.Equal(hand, world.Seats[0].Hand.Cards.Count);
        if (charge is not null) Assert.Equal(DeckType.UpgradesArea, charge.Area.Type);
        Assert.Empty(world.Effects.Registered);
        Assert.DoesNotContain(events, change => change is AttackCompleted);
        Assert.Null(world.Attack);
    }

    [Fact]
    [Rule("rr:would")]
    public void WouldTimingOutsideAnInterruptIsRejectedExplicitly()
    {
        // rr:would: "The word 'would' is used to define the triggering condition of
        // some interrupt abilities." A response cannot carry it.
        const string json = """
            { "cards": [{ "card": "01001a", "name": "Synthetic", "abilities": [{
              "trigger": { "event": "WhenAttackEnds", "timing": "Response", "would": true },
              "effect": { "draw": { "player": "you", "count": 1 } }
            }] }] }
            """;
        var error = Assert.Throws<AbilityException>(() => AbilityCatalog.Parse(json));
        Assert.Contains("would timing", error.Message, StringComparison.Ordinal);
    }
}
