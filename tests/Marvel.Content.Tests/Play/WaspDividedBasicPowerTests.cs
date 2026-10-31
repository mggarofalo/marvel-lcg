using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

// Actual production rows for Wasp's identity on synthetic boards; no public pack admission.
public sealed class WaspDividedBasicPowerTests : CoreCardTestBase
{
    private const string Mercenary = "01101";
    private const string Whiplash = "01172";
    private const string CrowdControl = "01108";
    private const string DefenseNetwork = "01125";

    [Fact]
    [Rule("rr:guard.1")]
    [Rule("rr:retaliate.1")]
    public void GiantBasicAttackDividesAmongAttackableEnemiesAndEachRecipientRetaliates()
    {
        // Ruling 1b420f298dc7b045: each enemy dealt damage by the divided attack
        // retaliates. Ruling 116e678d0ad83200: Guard keeps every point off the villain.
        var world = Board("13001c");
        var runner = AuthoredCards.Runner();
        var guard = Engaged(world, Mercenary);
        var retaliator = Engaged(world, Whiplash);
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        var wasp = world.Seats[0].IdentityCard;
        BasicPowerInitiation.BasicAttack(world, Cards, 0, guard, []);
        var prompt = Assert.IsType<Prompt>(Sequence.Work(world, Cards, runner, []));
        var offer = Assert.Single(prompt.Affordances);
        Assert.Equal(BasicPowerDivision.Verb, offer.Verb);
        Assert.Equal([guard.ObjectId, retaliator.ObjectId], offer.Targets!.Legal.Order());
        Assert.Equal(2, offer.Targets.Min);
        Assert.Equal(2, offer.Targets.Max);
        Assert.True(offer.Targets.AllowRepeated);
        Assert.DoesNotContain(villain.ObjectId, offer.Targets.Legal);
        Sequence.Answer(world, Cards, runner, prompt,
            Decision.Take(offer.Id) with { Targets = [guard.ObjectId, retaliator.ObjectId] }, []);
        Sequence.Finish(world, Cards, runner, []);
        Assert.Equal(1, guard.Damage);
        Assert.Equal(1, retaliator.Damage);
        Assert.Equal(0, villain.Damage);
        Assert.Equal(1, wasp.Damage);
        Assert.False(wasp.Ready);
        Assert.False(world.Agenda.IsBusy);
    }

    [Fact]
    [Rule("rr:attack-player-ability-type.5")]
    public void AnEnemyGivenNoShareIsNotAttacked()
    {
        // Ruling 116e678d0ad83200: an enemy is attacked only when it is dealt at least 1.
        var world = Board("13001c");
        var runner = AuthoredCards.Runner();
        var declared = Engaged(world, Whiplash);
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        var wasp = world.Seats[0].IdentityCard;
        BasicPowerInitiation.BasicAttack(world, Cards, 0, declared, []);
        var prompt = Assert.IsType<Prompt>(Sequence.Work(world, Cards, runner, []));
        var offer = Assert.Single(prompt.Affordances);
        Assert.Contains(villain.ObjectId, offer.Targets!.Legal);
        Sequence.Answer(world, Cards, runner, prompt,
            Decision.Take(offer.Id) with { Targets = [villain.ObjectId, villain.ObjectId] }, []);
        Sequence.Finish(world, Cards, runner, []);
        Assert.Equal(0, declared.Damage);
        Assert.Equal(2, villain.Damage);
        Assert.Equal(0, wasp.Damage);
    }

    [Fact]
    [Rule("rr:interrupt.3")]
    public void ALateRecipientIsAnnouncedAsAttackedBeforeItsShare()
    {
        // rr:attack-player-ability-type.5: the villain given a share is attacked, so
        // Temporal Shield's "When Kang is attacked" resolves before the damage.
        var world = Board("13001c");
        var runner = AuthoredCards.Runner();
        var declared = Engaged(world, Whiplash);
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        var shield = world.CreateCard("11014", world.AreaOf(DeckType.UpgradesArea, PlayArea.Villains, villain.ObjectId));
        var wasp = world.Seats[0].IdentityCard;
        BasicPowerInitiation.BasicAttack(world, Cards, 0, declared, []);
        var prompt = Assert.IsType<Prompt>(Sequence.Work(world, Cards, runner, []));
        var offer = Assert.Single(prompt.Affordances);
        Sequence.Answer(world, Cards, runner, prompt,
            Decision.Take(offer.Id) with { Targets = [declared.ObjectId, villain.ObjectId] }, []);
        Sequence.Finish(world, Cards, runner, []);
        Assert.Equal(DeckType.EncounterDiscardPile, shield.Area.Type);
        Assert.Equal(0, villain.Damage);
        Assert.Equal(1, declared.Damage);
        Assert.Equal(2, wasp.Damage);
        Assert.False(world.Agenda.IsBusy);
    }

    [Fact]
    [Rule("rr:cost.10")]
    public void AnInvalidAllocationLeavesTheBoardUnchanged()
    {
        var world = Board("13001c");
        var runner = AuthoredCards.Runner();
        var declared = Engaged(world, Whiplash);
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        BasicPowerInitiation.BasicAttack(world, Cards, 0, declared, []);
        var prompt = Assert.IsType<Prompt>(Sequence.Work(world, Cards, runner, []));
        var offer = Assert.Single(prompt.Affordances);
        string before = world.Digest().Canonical();
        var random = world.Random.Generator.GetState();
        Assert.Throws<ArgumentException>(() => Sequence.Answer(world, Cards, runner, prompt,
            Decision.Take(offer.Id) with { Targets = [villain.ObjectId] }, []));
        Assert.Throws<ArgumentException>(() => Sequence.Answer(world, Cards, runner, prompt,
            Decision.Take(offer.Id) with { Targets = [villain.ObjectId, villain.ObjectId, declared.ObjectId] }, []));
        Assert.Equal(before, world.Digest().Canonical());
        Assert.Equal(random.Words, world.Random.Generator.GetState().Words);
        Assert.Equal(random.Index, world.Random.Generator.GetState().Index);
    }

    [Theory]
    [InlineData("13001a")]
    [InlineData("13001c")]
    [Rule("rr:attack-player-ability-type.1")]
    public void OnlyGiantDividesAndASingleTargetNeedsNoAllocation(string face)
    {
        // Tiny prints no division; a lone legal enemy leaves nothing to divide.
        var world = Board(face);
        var runner = AuthoredCards.Runner();
        var guard = Engaged(world, Mercenary);
        BasicPowerInitiation.BasicAttack(world, Cards, 0, guard, []);
        Assert.Null(Sequence.Work(world, Cards, runner, []));
        Assert.Equal(face == "13001c" ? 2 : 1, guard.Damage);
    }

    [Fact]
    [Rule("rr:crisis-icon.1")]
    public void GiantBasicThwartDividesAmongThwartableSchemesWithinTheirThreat()
    {
        // Ruling f216f6359fb0bf96: crisis keeps every point off the main scheme.
        var world = Board("13001c");
        var runner = AuthoredCards.Runner();
        var main = world.TheCardIn(DeckType.MainSchemesArea)!;
        main.PlaceTokens("k_threat", 5);
        var crisis = Side(world, CrowdControl, 1);
        var other = Side(world, DefenseNetwork, 4);
        BasicThwartPowers.BasicThwart(world, Cards, 0, other, []);
        var prompt = Assert.IsType<Prompt>(Sequence.Work(world, Cards, runner, []));
        var offer = Assert.Single(prompt.Affordances);
        Assert.Equal([crisis.ObjectId, other.ObjectId], offer.Targets!.Legal.Order());
        Assert.Equal(1, offer.Targets.MaximumOccurrences![crisis.ObjectId]);
        Assert.False(offer.Targets.Allows([crisis.ObjectId, crisis.ObjectId]));
        Sequence.Answer(world, Cards, runner, prompt,
            Decision.Take(offer.Id) with { Targets = [crisis.ObjectId, other.ObjectId] }, []);
        Sequence.Finish(world, Cards, runner, []);
        Assert.Equal(DeckType.EncounterDiscardPile, crisis.Area.Type);
        Assert.Equal(3, other.Tokens["k_threat"]);
        Assert.Equal(5, main.Tokens["k_threat"]);
        Assert.False(world.Agenda.IsBusy);
    }

    private static Card Engaged(World world, string face) =>
        world.CreateCard(face, world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));

    private static Card Side(World world, string face, int threat)
    {
        var scheme = world.CreateCard(face, world.AreaOf(DeckType.SideSchemesArea));
        scheme.PlaceTokens("k_threat", threat);
        return scheme;
    }

    private static World Board(string face)
    {
        var world = new World(Cards, 1, 241);
        var seat = world.CreateSeat("p0");
        seat.IdentityCard = world.CreateCard("13001a,13001b,13001c", seat.Hero);
        seat.IdentityCard.TurnTo(face);
        for (int i = 0; i < 12; i++) world.CreateCard("01087", seat.Deck);
        world.CreateCard("01094", world.AreaOf(DeckType.VillainArea));
        world.CreateCard("01097a", world.AreaOf(DeckType.MainSchemesArea));
        for (int i = 0; i < 12; i++) world.CreateCard("01120", world.AreaOf(DeckType.EncounterDeck));
        world.Abilities = AuthoredCards.Runner();
        return world;
    }
}
