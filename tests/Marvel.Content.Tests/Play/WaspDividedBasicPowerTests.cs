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
    [Rule("rr:retaliate-x.1")]
    public void GiantBasicAttackDividesAmongAttackableEnemiesAndEachRecipientRetaliates()
    {
        // rr:guard.1: "The guard keyword is equivalent to the following constant ability: “The engaged player cannot attack any villain.”"
        // rr:retaliate-x.1: "The retaliate X keyword is equivalent to the following triggered ability: “Forced Response: After this character is attacked, deal X damage to the attacker.”"
        // Ruling 1b420f298dc7b045: each enemy dealt damage by the divided attack
        // retaliates. Ruling 116e678d0ad83200: Guard keeps every point off the villain.
        var world = WaspBoards.Board("13001c");
        var runner = AuthoredCards.Runner();
        var guard = WaspBoards.Engaged(world, Mercenary);
        var retaliator = WaspBoards.Engaged(world, Whiplash);
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        var wasp = world.Seats[0].IdentityCard;
        BasicPowerInitiation.BasicAttack(world, Cards, 0, guard, []);
        var prompt = Assert.IsType<Prompt>(Sequence.Work(world, Cards, runner, []));
        var offer = Assert.Single(prompt.Affordances);
        Assert.Equal(BasicPowerDivision.Verb, offer.Verb);
        Assert.Equal(PublicDecisionKind.VisibleCardSelection, prompt.PublicKind);
        Assert.Equal([guard.ObjectId, retaliator.ObjectId], offer.Targets!.Legal.Order());
        Assert.Equal(2, offer.Targets.Min);
        Assert.Equal(2, offer.Targets.Max);
        Assert.True(offer.Targets.AllowRepeated);
        Assert.DoesNotContain(villain.ObjectId, offer.Targets.Legal);
        Assert.Contains("so its Retaliate applies", prompt.Description);
        Assert.Equal("Attacked if given any damage · first point: 4/4 → 3/4 HP · Retaliate 1 will hit Wasp",
            offer.Targets.Details![retaliator.ObjectId]);
        Assert.Equal("Attacked if given any damage · first point: 3/3 → 2/3 HP",
            offer.Targets.Details[guard.ObjectId]);
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
        // rr:attack-player-ability-type.5: "When an attack targets multiple enemies, the attacking character is considered to have attacked each of those enemies."
        // Ruling 116e678d0ad83200: an enemy is attacked only when it is dealt at least 1.
        var world = WaspBoards.Board("13001c");
        var runner = AuthoredCards.Runner();
        var declared = WaspBoards.Engaged(world, Whiplash);
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
        // rr:interrupt.3: "An interrupt ability is resolved when its triggering condition initiates, but before that triggering condition resolves."
        // rr:attack-player-ability-type.5: the villain given a share is attacked, so
        // Temporal Shield's "When Kang is attacked" resolves before the damage.
        var world = WaspBoards.Board("13001c");
        var runner = AuthoredCards.Runner();
        var declared = WaspBoards.Engaged(world, Whiplash);
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
        // rr:cost.10: "Some card abilities may reference an “additional cost.” A player must pay all additional costs simultaneously with the cost that is being added to."
        var world = WaspBoards.Board("13001c");
        var runner = AuthoredCards.Runner();
        var declared = WaspBoards.Engaged(world, Whiplash);
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
        // rr:attack-player-ability-type.1: "A hero or ally can use their basic attack power to attack an enemy."
        // Tiny prints no division; a lone legal enemy leaves nothing to divide.
        var world = WaspBoards.Board(face);
        var runner = AuthoredCards.Runner();
        var guard = WaspBoards.Engaged(world, Mercenary);
        BasicPowerInitiation.BasicAttack(world, Cards, 0, guard, []);
        Assert.Null(Sequence.Work(world, Cards, runner, []));
        Assert.Equal(face == "13001c" ? 2 : 1, guard.Damage);
    }

    [Fact]
    [Rule("rr:crisis-icon.1")]
    public void GiantBasicThwartDividesAmongThwartableSchemesWithinTheirThreat()
    {
        // rr:crisis-icon.1: "Each crisis icon is equivalent to the following constant ability: “Player cards cannot remove threat from the main scheme.”"
        // Ruling f216f6359fb0bf96: crisis keeps every point off the main scheme.
        var world = WaspBoards.Board("13001c");
        var runner = AuthoredCards.Runner();
        var main = world.TheCardIn(DeckType.MainSchemesArea)!;
        main.PlaceTokens("k_threat", 5);
        var crisis = WaspBoards.Side(world, CrowdControl, 1);
        var other = WaspBoards.Side(world, DefenseNetwork, 4);
        BasicThwartPowers.BasicThwart(world, Cards, 0, other, []);
        var prompt = Assert.IsType<Prompt>(Sequence.Work(world, Cards, runner, []));
        var offer = Assert.Single(prompt.Affordances);
        Assert.Equal([crisis.ObjectId, other.ObjectId], offer.Targets!.Legal.Order());
        Assert.Equal(1, offer.Targets.MaximumOccurrences![crisis.ObjectId]);
        Assert.False(offer.Targets.Allows([crisis.ObjectId, crisis.ObjectId]));
        Assert.Equal("Thwarted if given any threat removal · 4 threat", offer.Targets.Details![other.ObjectId]);
        Sequence.Answer(world, Cards, runner, prompt,
            Decision.Take(offer.Id) with { Targets = [crisis.ObjectId, other.ObjectId] }, []);
        Sequence.Finish(world, Cards, runner, []);
        Assert.Equal(DeckType.EncounterDiscardPile, crisis.Area.Type);
        Assert.Equal(3, other.Tokens["k_threat"]);
        Assert.Equal(5, main.Tokens["k_threat"]);
        Assert.False(world.Agenda.IsBusy);
    }

    [Fact]
    [Rule("rr:interrupt")]
    public void TheDeclaredTargetsAttackInterruptsResolveEvenWhenItIsGivenNoShare()
    {
        // rr:interrupt: an interrupt "resolves immediately before that triggering condition resolves."
        // Ruling 78f96d5c7974afaa: Giant Wasp "will initially choose one target for the attack",
        // and that target's attack interrupt (Befuddle) resolves before her damage is divided.
        // Ruling 116e678d0ad83200 decides only who counts as attacked after the damage: a
        // target given no share is not attacked and does not retaliate.
        var world = WaspBoards.Board("13001c");
        var runner = AuthoredCards.Runner();
        World.MoveToTop(world.TheCardIn(DeckType.VillainArea)!, world.AreaOf(DeckType.RemovedArea));
        var taskmaster = world.CreateCard("04093", world.AreaOf(DeckType.VillainArea));
        var reflexes = world.CreateCard("04104", world.AreaOf(DeckType.UpgradesArea, PlayArea.Villains, taskmaster.ObjectId));
        var minion = WaspBoards.Engaged(world, "01121");
        var wasp = world.Seats[0].IdentityCard;
        BasicPowerInitiation.BasicAttack(world, Cards, 0, taskmaster, []);
        var prompt = Assert.IsType<Prompt>(Sequence.Work(world, Cards, runner, []));
        while (prompt.Affordances.All(offer => offer.Verb != BasicPowerDivision.Verb))
        {
            Sequence.Answer(world, Cards, runner, prompt,
                prompt.Cancellable ? Decision.Decline : Decision.Take(prompt.Affordances[0].Id), []);
            prompt = Assert.IsType<Prompt>(Sequence.Work(world, Cards, runner, []));
        }
        Assert.Equal(DeckType.EncounterDiscardPile, reflexes.Area.Type);
        var divide = Assert.Single(prompt.Affordances);
        Sequence.Answer(world, Cards, runner, prompt,
            Decision.Take(divide.Id) with { Targets = [minion.ObjectId, minion.ObjectId] }, []);
        Sequence.Finish(world, Cards, runner, []);
        Assert.Equal(0, taskmaster.Damage);
        Assert.Equal(DeckType.EncounterDiscardPile, minion.Area.Type);
        Assert.Equal(0, wasp.Damage);
    }

    [Theory]
    [Rule("rr:stun-stunned.5")]
    [InlineData("13001c", true, false, true)]
    [InlineData("13001c", false, false, false)]
    [InlineData("13001a", true, false, false)]
    [InlineData("13001c", true, true, false)]
    public void TheBasicAttackOfferSaysWhenItsDamageWillBeDivided(string face, bool minion, bool stunned, bool divided)
    {
        // rr:stun-stunned.5: "If a stunned identity or ally attempts to attack or use an attack ability, discard the stunned card instead."
        // rr:stun-stunned.5: a stunned attacker's attempt is cancelled, so nothing is divided.
        // Ruling 78f96d5c7974afaa: the hero declares one target before dividing, so the
        // declared target's preview holds only if every point is assigned to it. With
        // the villain alone there is nothing to divide among.
        Card? enemy = null;
        var (game, world) = Playing(board =>
        {
            board.Seats[0].IdentityCard.TurnTo(face);
            enemy = minion ? WaspBoards.Engaged(board, Whiplash) : board.TheCardIn(DeckType.VillainArea);
            if (stunned) Statuses.Give(board, board.Seats[0].IdentityCard, Statuses.Stunned);
        }, heroes: ["wasp"]);
        var wasp = world.Seats[0].IdentityCard;
        var attack = Assert.Single(game.Pending!.Affordances,
            offer => offer.AnchorId == wasp.ObjectId && offer.Verb == BasicPowers.AttackVerb);
        Assert.Equal(divided, attack.Description!.EndsWith(" · Then divide the damage among enemies you can attack",
            StringComparison.Ordinal));
        Assert.Equal(divided, attack.Targets!.Details![enemy!.ObjectId].EndsWith(" if all of it is assigned here",
            StringComparison.Ordinal));
    }
}
