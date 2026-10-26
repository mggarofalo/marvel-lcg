using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class BasicUseWeaponTests
{
    private const string Cost = """
        {"exhaustChosen":{"from":{"query":"upgradesOnYourHero"},"trait":"WEAPON","count":1}}
        """;

    // Printed 06015 is the authority; this isolated runner does not admit the Thor product.
    private static AbilityRunner Abilities(string cost = Cost, string face = "06015") => new(AbilityCatalog.Parse($$"""
        {"cards":[{"card":"{{face}}","abilities":[{
          "trigger":{"event":"WhenHeroBasicAttackInitiated","timing":"Interrupt","actor":"you","form":"hero","player":"you"},
          "cost":{{cost}},"effect":{"boostBasicUse":3}
        }]}]}
        """));

    private static Card Weapon(World world, int host, int controller = 0, string face = "06019") =>
        world.CreateCard(face, world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(controller), host, cardOwner: controller));

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [Rule("rr:cost.7")]
    [Rule("rr:attack-player-ability-type.1")]
    public void ChosenAttachedWeaponPaysAndBonusBelongsOnlyToThisAttack(int selection)
    {
        // "must pay costs with cards and/or game elements they control";
        // basic damage is "equal to the character's ATK value".
        var runner = Abilities();
        Card? source = null;
        Card[] weapons = [];
        var (_, world) = Playing(board =>
        {
            source = board.CreateCard("06015", board.Seats[0].Hand);
            int hero = board.Seats[0].IdentityCard.ObjectId;
            weapons = [Weapon(board, hero), Weapon(board, hero)];
        }, hero: true, abilities: runner);
        var enemy = world.TheCardIn(DeckType.VillainArea)!;
        long power = StateFields.Modified(world, world.Seats[0].IdentityCard, "attack", world.Facts, world.Players);
        BasicPowerInitiation.BasicAttack(world, world.Facts, 0, enemy, []);
        var prompt = Sequence.Work(world, world.Facts, runner, [])!;
        var offer = Assert.Single(prompt.Affordances);
        Assert.Equal(source!.ObjectId, offer.AnchorId);
        Assert.Equal(weapons.Select(card => card.ObjectId), offer.Targets!.Legal);
        Assert.Contains("WEAPON upgrades attached to your hero", offer.CostDescription);
        Assert.Contains("3 ATK", offer.Description);
        Assert.Contains("Current attack bonus: 0", prompt.Description);
        Assert.Equal(0, enemy.Damage);
        Sequence.Answer(world, world.Facts, runner, prompt,
            new Decision(offer.Id, [weapons[selection].ObjectId]), []);
        Sequence.Finish(world, world.Facts, runner, []);
        Assert.False(weapons[selection].Ready);
        Assert.True(weapons[1 - selection].Ready);
        Assert.Equal(DeckType.DiscardPile, source.Area.Type);
        Assert.Equal(power + 3, enemy.Damage);
        Assert.Equal(power, StateFields.Modified(world, world.Seats[0].IdentityCard, "attack", world.Facts, world.Players));
        world.Seats[0].IdentityCard.Refresh();
        BasicPowerInitiation.BasicAttack(world, world.Facts, 0, enemy, []);
        Sequence.Finish(world, world.Facts, runner, []);
        Assert.Equal(power * 2 + 3, enemy.Damage);
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("exhausted")]
    [InlineData("wrong-host")]
    [InlineData("wrong-controller")]
    [InlineData("not-weapon")]
    [Rule("rr:exhausted.2")]
    public void UnpayableWeaponCostIsNeverOffered(string reason)
    {
        // "An exhausted card cannot be exhausted again until it is ready."
        var runner = Abilities();
        Card? source = null;
        var (_, world) = Playing(board =>
        {
            source = board.CreateCard("06015", board.Seats[0].Hand);
            if (reason == "missing") return;
            int host = board.Seats[reason == "wrong-host" ? 1 : 0].IdentityCard.ObjectId;
            var weapon = Weapon(board, host, reason == "wrong-controller" ? 1 : 0,
                reason == "not-weapon" ? "01008" : "06019");
            if (reason == "exhausted") weapon.Exhaust();
        }, hero: true, heroes: ["spider_man", "captain_marvel"], abilities: runner);
        BasicPowerInitiation.BasicAttack(world, world.Facts, 0, world.TheCardIn(DeckType.VillainArea)!, []);
        Assert.Null(Sequence.Work(world, world.Facts, runner, []));
        Assert.Equal(DeckType.HandsArea, source!.Area.Type);
    }

    [Fact]
    [Rule("rr:initiating-abilities.step.5")]
    public void RejectedCostChoicesPreserveTheWholeOpportunityAndCanRetry()
    {
        // If costs cannot be paid, "abort this process without paying any costs".
        var runner = Abilities();
        Card? weapon = null;
        Card? foreign = null;
        var (_, world) = Playing(board =>
        {
            board.CreateCard("06015", board.Seats[0].Hand);
            weapon = Weapon(board, board.Seats[0].IdentityCard.ObjectId);
            foreign = Weapon(board, board.Seats[1].IdentityCard.ObjectId, 1);
        }, hero: true, heroes: ["spider_man", "captain_marvel"], abilities: runner);
        BasicPowerInitiation.BasicAttack(world, world.Facts, 0, world.TheCardIn(DeckType.VillainArea)!, []);
        var prompt = Sequence.Work(world, world.Facts, runner, [])!;
        var offer = Assert.Single(prompt.Affordances);
        string digest = world.Digest().Canonical();
        var random = world.Random.Generator.GetState();
        var pending = world.Agenda.Outstanding.ToArray();
        var occurrence = world.Agenda.Occurrence!;
        foreach (int[] ids in new int[][] { [], [foreign!.ObjectId], [weapon!.ObjectId, weapon.ObjectId], [int.MaxValue] })
        {
            Assert.Throws<RulesNotImplementedException>(() => Sequence.Answer(world, world.Facts, runner, prompt, new(offer.Id, ids), []));
            Assert.Equal(digest, world.Digest().Canonical());
            Assert.Equal(random.Index, world.Random.Generator.GetState().Index);
            Assert.Equal(random.Words, world.Random.Generator.GetState().Words);
            Assert.Equal(pending, world.Agenda.Outstanding.ToArray());
            Assert.Equal(0, occurrence.BasicUse!.Bonus);
            Assert.Empty(occurrence.Resolutions);
        }
        Sequence.Answer(world, world.Facts, runner, prompt, new(offer.Id, [weapon.ObjectId]), []);
        Sequence.Finish(world, world.Facts, runner, []);
        Assert.False(weapon.Ready);
    }

    [Theory]
    [InlineData("thwart")]
    [InlineData("ally-attack")]
    [InlineData("card-attack")]
    [Rule("rr:attack-player-ability-type.1")]
    [Rule("rr:attack-player-ability-type.2")]
    public void OnlyTheHeroBasicAttackCreatesTheWeaponOpportunity(string use)
    {
        // A basic attack uses ATK; an ability "labeled as an attack" is a separate path.
        var runner = Abilities();
        Card? source = null;
        Card? ally = null;
        var (_, world) = Playing(board =>
        {
            source = board.CreateCard("06015", board.Seats[0].Hand);
            Weapon(board, board.Seats[0].IdentityCard.ObjectId);
            ally = board.CreateCard("01002", board.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        }, hero: true, abilities: runner);
        var enemy = world.TheCardIn(DeckType.VillainArea)!;
        if (use == "thwart")
        {
            var scheme = world.TheCardIn(DeckType.MainSchemesArea)!;
            scheme.PlaceTokens("k_threat", 3);
            BasicThwartPowers.BasicThwart(world, world.Facts, 0, scheme, []);
        }
        else if (use == "ally-attack")
            AllyBasicPowers.AllyPower(world, world.Facts, ally!, enemy, BasicPowers.AttackVerb, []);
        else
        {
            // The same scheduled payload used by a card-provided fixed-damage attack.
            world.Agenda.Add(new PhaseStep(new CharacterAttackOperation(new CharacterAttack(
                world.Seats[0].IdentityCard.ObjectId, enemy.ObjectId, 0,
                Amount: 1, Source: source!.ObjectId, TargetIncarnation: enemy.Incarnation)), 1, 1));
        }
        Assert.Null(Sequence.Work(world, world.Facts, runner, []));
        Assert.Equal(DeckType.HandsArea, source!.Area.Type);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:cost.5")]
    public void UnsupportedResourceCompositionRaisesBeforeAnyCostChanges(bool positivePrice)
    {
        // Multiple costs "must be paid simultaneously"; unsupported composition raises.
        string face = positivePrice ? "01005" : "06015";
        var runner = Abilities(face: face);
        Card? source = null;
        Card? weapon = null;
        Card? generator = null;
        var (_, world) = Playing(board =>
        {
            source = board.CreateCard(face, board.Seats[0].Hand);
            weapon = Weapon(board, board.Seats[0].IdentityCard.ObjectId);
            generator = board.CreateCard("01088", board.Seats[0].Hand);
        }, hero: true, abilities: runner);
        BasicPowerInitiation.BasicAttack(world, world.Facts, 0, world.TheCardIn(DeckType.VillainArea)!, []);
        var pending = world.Agenda.Outstanding.ToArray();
        if (positivePrice)
        {
            string before = world.Digest().Canonical();
            var error = Assert.Throws<RulesNotImplementedException>(() => Sequence.Work(world, world.Facts, runner, []));
            Assert.Contains("positive event price", error.Message);
            Assert.Equal(before, world.Digest().Canonical());
        }
        else
        {
            var prompt = Sequence.Work(world, world.Facts, runner, [])!;
            var offer = Assert.Single(prompt.Affordances);
            string before = world.Digest().Canonical();
            var random = world.Random.Generator.GetState();
            pending = world.Agenda.Outstanding.ToArray();
            var error = Assert.Throws<RulesNotImplementedException>(() => Sequence.Answer(world, world.Facts,
                runner, prompt, new(offer.Id, [weapon!.ObjectId], [generator!.ObjectId]), []));
            Assert.Contains("resource generation requires simultaneous payment", error.Message);
            Assert.Equal(before, world.Digest().Canonical());
            Assert.Equal(random.Index, world.Random.Generator.GetState().Index);
            Assert.Equal(random.Words, world.Random.Generator.GetState().Words);
            Assert.Equal(pending, world.Agenda.Outstanding.ToArray());
        }
        Assert.True(weapon!.Ready);
        Assert.Equal(DeckType.HandsArea, source!.Area.Type);
        Assert.Equal(DeckType.HandsArea, generator!.Area.Type);
    }

    [Fact]
    public void CompoundAttachedUpgradeCostFailsBeforeExecution() =>
        Assert.Throws<RulesNotImplementedException>(() => Abilities("""
            {"seq":[{"exhaust":"you"},{"exhaustChosen":{"from":{"query":"upgradesOnYourHero"},"trait":"WEAPON"}}]}
            """));
}
