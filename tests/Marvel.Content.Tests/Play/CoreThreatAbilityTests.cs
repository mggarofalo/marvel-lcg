using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class CoreThreatAbilityTests
{
    private static readonly CardCatalog Cards = CardCatalog.Parse(
        File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));

    [Rule("rr:prevent.2")]
    [Rule("rr:limit")]
    [Fact]
    public void IObjectPreventsOneThreatOnlyOncePerRound()
    {
        var world = Board(hero: false);
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        var first = ThreatOccurrence(world, ThreatCause.VillainPhase, amount: 3);
        var ability = Assert.Single(runner.Waiting(world, first, WindowKind.Interrupt));

        runner.Resolve(world, first, ability, [], []);

        Assert.Equal(2, first.Threat!.Remaining);
        var second = ThreatOccurrence(world, ThreatCause.VillainPhase, amount: 3);
        Assert.Empty(runner.Waiting(world, second, WindowKind.Interrupt));
    }

    [Rule("rr:replacement-effect.1")]
    [Fact]
    public void GreatResponsibilityReplacesThreatWithDamage()
    {
        var world = Board(hero: true);
        world.CreateCard("01087", world.Seats[0].Deck);
        var responsibility = world.CreateCard("01061", world.Seats[0].Hand);
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        var occurrence = ThreatOccurrence(world, ThreatCause.CardAbility, amount: 4);
        var ability = Assert.Single(
            runner.Waiting(world, occurrence, WindowKind.Interrupt),
            pending => pending.Card == responsibility.ObjectId);

        runner.Resolve(world, occurrence, ability, [], []);

        Assert.True(occurrence.Threat!.Replaced);
        Assert.Equal(0, occurrence.Threat.Remaining);
        Assert.Equal(4, world.Seats[0].IdentityCard.Damage);
        Assert.Equal(DeckType.DiscardPile, responsibility.Area.Type);
    }

    [Rule("rr:prevent.2")]
    [Fact]
    public void EmergencyReducesOnlyVillainSchemeThreat()
    {
        var world = Board(hero: true);
        world.CreateCard("01087", world.Seats[0].Deck);
        var emergency = world.CreateCard("01085", world.Seats[0].Hand);
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        var villainScheme = ThreatOccurrence(world, ThreatCause.EnemyScheme, amount: 3);
        var ability = Assert.Single(
            runner.Waiting(world, villainScheme, WindowKind.Interrupt),
            pending => pending.Card == emergency.ObjectId);

        runner.Resolve(world, villainScheme, ability, [], []);

        Assert.Equal(2, villainScheme.Threat!.Remaining);

        var another = world.CreateCard("01085", world.Seats[0].Hand);
        var villainPhase = ThreatOccurrence(world, ThreatCause.VillainPhase, amount: 3);
        Assert.DoesNotContain(
            runner.Waiting(world, villainPhase, WindowKind.Interrupt),
            pending => pending.Card == another.ObjectId);
    }

    private static Occurrence ThreatOccurrence(World world, ThreatCause cause, long amount)
    {
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        var scheme = world.TheCardIn(DeckType.MainSchemesArea)!;
        int source = cause == ThreatCause.VillainPhase ? scheme.ObjectId : villain.ObjectId;
        return Occurrence.ForThreat(
            1, [Steps.ThreatWouldBePlaced], world, Cards,
            new ThreatPlacement(scheme.ObjectId, source, amount, cause, "test", player: 0));
    }

    private static World Board(bool hero)
    {
        var world = new World(Cards, players: 1);
        var seat = world.CreateSeat("p0");
        seat.IdentityCard = world.CreateCard("01019a,01019b", seat.Hero);
        seat.IdentityCard.TurnTo(hero ? "01019a" : "01019b");
        world.CreateCard("01134", world.AreaOf(DeckType.VillainArea));
        world.CreateCard("01137b", world.AreaOf(DeckType.MainSchemesArea));
        return world;
    }
}
