using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Play;
using Marvel.Rules.Events;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class BasicUseWeaponReviewTests
{
    private const string Cost = """
        {"exhaustChosen":{"from":{"query":"upgradesOnYourHero"},"trait":"WEAPON","count":1}}
        """;
    private const string Attack = """
        {"attack":{"target":{"query":"villain"},"effect":{"dealAttackDamage":{"cards":{"query":"villain"},"amount":1}}}}
        """;

    // Synthetic programs isolate the prerequisite; they do not admit Thor.
    private static AbilityRunner Runner(string effect = "{\"boostBasicUse\":3}", string extra = "") =>
        new(AbilityCatalog.Parse($$"""
        {"cards":[{"card":"06015","abilities":[{
          "trigger":{"event":"WhenHeroBasicAttackInitiated","timing":"Interrupt","actor":"you","form":"hero","player":"you"},
          "cost":{{Cost}},"effect":{{effect}}
        }]}{{extra}}]}
        """));

    private static Card Weapon(World world) => world.CreateCard("06019", world.AreaOf(
        DeckType.UpgradesArea, PlayArea.Of(0), world.Seats[0].IdentityCard.ObjectId, cardOwner: 0));

    [Fact]
    [Rule("rr:attack-player-ability-type.2")]
    public void PaidEventAttackCompletesWithoutOfferingTheBasicAttackInterrupt()
    {
        // An ability "labeled as an attack" is distinct from a basic attack.
        var runner = Runner(extra: $$"""
            ,{"card":"05003","abilities":[{
              "trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"this","form":"hero"},
              "effect":{{Attack}}
            }]}
            """);
        Card? played = null;
        Card? resource = null;
        Card? source = null;
        Card? weapon = null;
        var (game, world) = Playing(board =>
        {
            played = board.CreateCard("05003", board.Seats[0].Hand);
            resource = board.CreateCard("01088", board.Seats[0].Hand);
            source = board.CreateCard("06015", board.Seats[0].Hand);
            weapon = Weapon(board);
        }, hero: true, abilities: runner);
        var action = Assert.Single(game.Pending!.Affordances, offer => offer.AnchorId == played!.ObjectId);
        game.Resolve(PaidDecision.Take(action, [resource!.ObjectId]));
        Assert.True(game.IsRootPrompt);
        Assert.Equal(1, world.TheCardIn(DeckType.VillainArea)!.Damage);
        Assert.Equal(DeckType.DiscardPile, played!.Area.Type);
        Assert.Equal(DeckType.DiscardPile, resource.Area.Type);
        Assert.Equal(DeckType.HandsArea, source!.Area.Type);
        Assert.True(weapon!.Ready);
        Assert.False(world.Agenda.IsBusy);
    }

    [Fact]
    [Rule("rr:lasting-effects.1")]
    public void NestedLabeledAttackDoesNotConsumeOrInheritTheOuterBasicBonus()
    {
        // "for a specified duration" here is the printed "for this attack".
        var runner = Runner($$"""{"seq":[{"boostBasicUse":3},{{Attack}}]}""");
        Card? weapon = null;
        var (_, world) = Playing(board =>
        {
            board.CreateCard("06015", board.Seats[0].Hand);
            weapon = Weapon(board);
        }, hero: true, abilities: runner);
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        var hero = world.Seats[0].IdentityCard;
        long power = StateFields.Modified(world, hero, "attack", world.Facts, world.Players);
        BasicPowerInitiation.BasicAttack(world, world.Facts, 0, villain, []);
        var prompt = Sequence.Work(world, world.Facts, runner, [])!;
        Sequence.Answer(world, world.Facts, runner, prompt,
            new(Assert.Single(prompt.Affordances).Id, [weapon!.ObjectId]), []);
        Sequence.Finish(world, world.Facts, runner, []);
        Assert.Equal(power + 3 + 1, villain.Damage);
        Assert.False(world.Agenda.IsBusy);
        Assert.False(weapon.Ready);
        hero.Refresh();
        BasicPowerInitiation.BasicAttack(world, world.Facts, 0, villain, []);
        Sequence.Finish(world, world.Facts, runner, []);
        Assert.Equal(power * 2 + 4, villain.Damage);
    }

    [Fact]
    [Rule("rr:initiating-abilities.step.5")]
    public void StaleAttachedCopyIsRejectedAndAnotherCurrentCopyCanPayOnRetry()
    {
        // "abort this process without paying any costs" when payment is invalid.
        var runner = Runner();
        Card? first = null;
        Card? second = null;
        var (_, world) = Playing(board =>
        {
            board.CreateCard("06015", board.Seats[0].Hand);
            first = Weapon(board);
            second = Weapon(board);
        }, hero: true, abilities: runner);
        BasicPowerInitiation.BasicAttack(world, world.Facts, 0, world.TheCardIn(DeckType.VillainArea)!, []);
        var prompt = Sequence.Work(world, world.Facts, runner, [])!;
        var offer = Assert.Single(prompt.Affordances);
        first!.Exhaust();
        string digest = world.Digest().Canonical();
        var random = world.Random.Generator.GetState();
        var pending = world.Agenda.Outstanding.ToArray();
        Assert.Throws<RulesNotImplementedException>(() => Sequence.Answer(world, world.Facts,
            runner, prompt, new(offer.Id, [first.ObjectId]), []));
        Assert.Equal(digest, world.Digest().Canonical());
        Assert.Equal(random.Words, world.Random.Generator.GetState().Words);
        Assert.Equal(random.Index, world.Random.Generator.GetState().Index);
        Assert.Equal(pending, world.Agenda.Outstanding.ToArray());
        Assert.Equal(0, world.Agenda.Occurrence!.BasicUse!.Bonus);
        Sequence.Answer(world, world.Facts, runner, prompt, new(offer.Id, [second!.ObjectId]), []);
        Sequence.Finish(world, world.Facts, runner, []);
        Assert.False(second.Ready);
        Assert.Equal(5, world.TheCardIn(DeckType.VillainArea)!.Damage);
    }

    [Theory]
    [InlineData("window")]
    [InlineData("prepare")]
    [InlineData("action")]
    [Rule("rr:cost.5")]
    public void GratuitousResourcePaymentIsRefusedAcrossDirectInvocationPaths(string path)
    {
        // Multiple costs "must be paid simultaneously"; this composition is bounded out.
        var runner = path == "action" ? new AbilityRunner(AbilityCatalog.Parse("""
            {"cards":[{"card":"06015","abilities":[{
              "trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"this","form":"hero"},
              "cost":COST,"effect":{"draw":{"player":"you","count":1}}
            }]}]}
            """.Replace("COST", Cost, StringComparison.Ordinal))) : Runner();
        Card? source = null;
        Card? weapon = null;
        Card? resource = null;
        var (_, world) = Playing(board =>
        {
            source = board.CreateCard("06015", board.Seats[0].Hand);
            weapon = Weapon(board);
            resource = board.CreateCard("01088", board.Seats[0].Hand);
        }, hero: true, abilities: runner);
        var hero = world.Seats[0].IdentityCard;
        var occurrence = Occurrence.ForAttack(42, [BasicPowerTiming.HeroAttackInitiated],
            world, world.Facts, hero.ObjectId, world.TheCardIn(DeckType.VillainArea)!.ObjectId, 0)
            with { BasicUse = new(hero.ObjectId, hero.Incarnation) };
        var ability = path == "action" ? Assert.Single(runner.Actions(world, 0))
            : Assert.Single(runner.Waiting(world, occurrence, WindowKind.Interrupt));
        string digest = world.Digest().Canonical();
        var random = world.Random.Generator.GetState();
        var pending = world.Agenda.Outstanding.ToArray();
        var error = Assert.Throws<RulesNotImplementedException>(() =>
        {
            var answer = new Decision(ability.Card, [weapon!.ObjectId], [resource!.ObjectId]);
            if (path == "action") runner.ExecuteAction(world, new PlayerAction(ability, answer.Spent, answer.Targets));
            else if (path == "prepare") runner.PrepareWindow(world, occurrence, ability, answer);
            else runner.ResolveWindow(world, occurrence, ability, answer);
        });
        Assert.Contains("resource generation requires simultaneous payment", error.Message);
        Assert.Equal(digest, world.Digest().Canonical());
        Assert.Equal(random.Words, world.Random.Generator.GetState().Words);
        Assert.Equal(random.Index, world.Random.Generator.GetState().Index);
        Assert.Equal(pending, world.Agenda.Outstanding.ToArray());
        Assert.Equal(DeckType.HandsArea, source!.Area.Type);
        Assert.Equal(DeckType.HandsArea, resource!.Area.Type);
        Assert.True(weapon!.Ready);
    }

    [Fact]
    public void RepeatedWindowQueriesPreserveTheBaselineAndMatchAnUnprobedExecution()
    {
        var runner = Runner();
        var probed = QueryFixture(runner);
        var control = QueryFixture(runner);
        string digest = probed.World.Digest().Canonical();
        var random = probed.World.Random.Generator.GetState();
        var pending = probed.World.Agenda.Outstanding.ToArray();
        PendingAbility selected = default;
        for (int index = 0; index < 4; index++)
        {
            selected = Assert.Single(runner.Waiting(probed.World, probed.Occurrence, WindowKind.Interrupt));
            var offer = runner.Describe(probed.World, selected);
            Assert.Contains("WEAPON upgrades attached to your hero", offer.CostDescription);
            Assert.Contains("3 ATK", offer.Description);
            Assert.Equal(digest, probed.World.Digest().Canonical());
            Assert.Equal(random.Words, probed.World.Random.Generator.GetState().Words);
            Assert.Equal(random.Index, probed.World.Random.Generator.GetState().Index);
            Assert.Equal(pending, probed.World.Agenda.Outstanding.ToArray());
            Assert.Equal(0, probed.Occurrence.BasicUse!.Bonus);
            Assert.Empty(probed.Occurrence.Resolutions);
        }
        var unprobed = new PendingAbility(control.Source.ObjectId, AbilityType.Interrupt, 0);
        var probedResult = runner.ResolveWindow(probed.World, probed.Occurrence, selected,
            new(selected.Card, [probed.Weapon.ObjectId]));
        var controlResult = runner.ResolveWindow(control.World, control.Occurrence, unprobed,
            new(unprobed.Card, [control.Weapon.ObjectId]));
        Assert.Equal(control.World.Digest().Canonical(), probed.World.Digest().Canonical());
        Assert.Equal(control.World.Random.Generator.GetState().Words, probed.World.Random.Generator.GetState().Words);
        Assert.Equal(control.Occurrence.BasicUse!.Bonus, probed.Occurrence.BasicUse!.Bonus);
        Assert.Equal(3, probed.Occurrence.BasicUse.Bonus);
        Assert.False(probed.Weapon.Ready);
        Assert.Equal(DeckType.DiscardPile, probed.Source.Area.Type);
        Assert.Equal(controlResult.Events.Select(EventJson.Write), probedResult.Events.Select(EventJson.Write));
    }

    private static (World World, Occurrence Occurrence, Card Source, Card Weapon) QueryFixture(AbilityRunner runner)
    {
        Card? source = null;
        Card? weapon = null;
        var (_, world) = Playing(board =>
        {
            source = board.CreateCard("06015", board.Seats[0].Hand);
            weapon = Weapon(board);
        }, hero: true, abilities: runner);
        var hero = world.Seats[0].IdentityCard;
        var occurrence = Occurrence.ForAttack(42, [BasicPowerTiming.HeroAttackInitiated], world,
            world.Facts, hero.ObjectId, world.TheCardIn(DeckType.VillainArea)!.ObjectId, 0)
            with { BasicUse = new(hero.ObjectId, hero.Incarnation) };
        return (world, occurrence, source!, weapon!);
    }
}
