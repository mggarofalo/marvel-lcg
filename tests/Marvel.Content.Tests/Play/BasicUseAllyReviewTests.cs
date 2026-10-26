using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class BasicUseAllyReviewTests
{
    private const string Cost = """{"exhaustChosen":{"from":{"query":"alliesYouControl"},"count":1,"matchingBasicPower":"true"}}""";
    private const string Boost = """{"boostBasicUse":"exhaustedAllyPower"}""";
    private const string Draw = """{"draw":{"player":"you","count":1}}""";
    private const string Choice = """{"choose":{"options":[{"draw":{"player":"you","count":1}},{"draw":{"player":"you","count":2}}]}}""";
    private const string Attack = """{"attack":{"target":{"query":"villain"},"effect":{"dealAttackDamage":{"cards":{"query":"villain"},"amount":1}}}}""";

    // Synthetic receipt programs exercise engine contracts without product admission.
    private static AbilityRunner Runner(string effect = Boost, string cost = Cost, string face = "06032", string extra = "") =>
        new(AbilityCatalog.Parse($$$"""
        {"cards":[{"card":"{{{face}}}","abilities":[{
          "trigger":{"event":"WhenHeroBasicUseInitiated","timing":"Interrupt","actor":"you","form":"hero","player":"you"},
          "cost":{{{cost}}},"effect":{{{effect}}}
        }]}{{{extra}}}]}
        """));

    private static (World World, Card Source, Card Ally) Fixture(AbilityRunner runner, string face = "06032")
    {
        Card? source = null;
        Card? ally = null;
        var (_, world) = Playing(board =>
        {
            source = board.CreateCard(face, board.Seats[0].Hand);
            ally = board.CreateCard("01002", board.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        }, hero: true, heroes: ["spider_man", "captain_marvel"], abilities: runner);
        return (world, source!, ally!);
    }

    private static void Begin(World world) => BasicPowerInitiation.BasicAttack(world, world.Facts, 0,
        world.TheCardIn(DeckType.VillainArea)!, []);

    [Theory]
    [InlineData("current", 1)]
    [InlineData("departed", 0)]
    [InlineData("reentered", 0)]
    [InlineData("foreign", 0)]
    public void SuspendedReceiptBindsTheSelectedCopyAndItsPaidPower(string state, int expected)
    {
        var runner = Runner($$$"""{"seq":[{{{Choice}}},{{{Boost}}}]}""");
        var (world, source, ally) = Fixture(runner);
        // A second identical ally must never replace the selected physical receipt.
        var other = world.CreateCard("01002", ally.Area);
        Begin(world);
        var prompt = Sequence.Work(world, world.Facts, runner, [])!;
        Sequence.Answer(world, world.Facts, runner, prompt,
            new(Assert.Single(prompt.Affordances).Id, [ally.ObjectId]), []);
        var choice = Sequence.Work(world, world.Facts, runner, [])!;
        Assert.False(ally.Ready);
        Assert.True(other.Ready);
        Assert.Equal(0, world.TheCardIn(DeckType.VillainArea)!.Damage);
        var original = ally.Area;
        if (state is "departed" or "reentered") World.MoveToTop(ally, world.Seats[0].Hand);
        if (state == "reentered") World.MoveToTop(ally, original);
        if (state == "foreign") World.MoveToTop(ally,
            world.AreaOf(DeckType.AlliesArea, PlayArea.Of(1), cardOwner: 0));
        world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect,
            Kind: "attack", Amount: 5, Card: ally.ObjectId, Affects: ally.ObjectId));
        Sequence.Answer(world, world.Facts, runner, choice, new(choice.Affordances[0].Id, []), []);
        Sequence.Finish(world, world.Facts, runner, []);
        Assert.Equal(2 + expected, world.TheCardIn(DeckType.VillainArea)!.Damage);
        Assert.Equal(DeckType.DiscardPile, source.Area.Type);
        Assert.True(other.Ready);
        Assert.Equal(0, ally.Damage);
        Assert.False(world.Agenda.IsBusy);
    }

    [Fact]
    public void NestedLabeledAttackDoesNotReadOrConsumeTheOuterReceipt()
    {
        var runner = Runner($$$"""{"seq":[{{{Attack}}},{{{Boost}}}]}""");
        var (world, source, ally) = Fixture(runner);
        Begin(world);
        var prompt = Sequence.Work(world, world.Facts, runner, [])!;
        Sequence.Answer(world, world.Facts, runner, prompt,
            new(Assert.Single(prompt.Affordances).Id, [ally.ObjectId]), []);
        Sequence.Finish(world, world.Facts, runner, []);
        Assert.Equal(4, world.TheCardIn(DeckType.VillainArea)!.Damage);
        Assert.Equal(DeckType.DiscardPile, source.Area.Type);
        Assert.False(ally.Ready);
        Assert.False(world.Agenda.IsBusy);
    }

    [Fact]
    public void RepeatedQueriesMatchUnprobedDecisionDrawAndRandomContinuation()
    {
        var runner = Runner($$$"""{"seq":[{{{Boost}}},{{{Draw}}}]}""");
        var probed = Fixture(runner);
        var control = Fixture(runner);
        Begin(probed.World);
        Begin(control.World);
        // Sequence.Work establishes the current agenda occurrence. Snapshot before
        // the first explicit Waiting/Describe query against that occurrence.
        var prompt = Sequence.Work(probed.World, probed.World.Facts, runner, [])!;
        var otherPrompt = Sequence.Work(control.World, control.World.Facts, runner, [])!;
        string digest = probed.World.Digest().Canonical();
        var random = probed.World.Random.Generator.GetState();
        var pending = probed.World.Agenda.Outstanding.ToArray();
        var occurrence = probed.World.Agenda.Occurrence!;
        for (int index = 0; index < 4; index++)
        {
            var ability = Assert.Single(runner.Waiting(probed.World, occurrence, WindowKind.Interrupt));
            Assert.Single(runner.Describe(probed.World, ability).Targets!.Legal);
            Assert.Equal(digest, probed.World.Digest().Canonical());
            Assert.Equal(random.Words, probed.World.Random.Generator.GetState().Words);
            Assert.Equal(random.Index, probed.World.Random.Generator.GetState().Index);
            Assert.Equal(pending, probed.World.Agenda.Outstanding.ToArray());
            Assert.Equal(0, occurrence.BasicUse!.Bonus);
            Assert.Empty(occurrence.Resolutions);
        }
        var events = new List<GameEvent>();
        var otherEvents = new List<GameEvent>();
        Sequence.Answer(probed.World, probed.World.Facts, runner, prompt,
            new(Assert.Single(prompt.Affordances).Id, [probed.Ally.ObjectId]), events);
        Sequence.Answer(control.World, control.World.Facts, runner, otherPrompt,
            new(Assert.Single(otherPrompt.Affordances).Id, [control.Ally.ObjectId]), otherEvents);
        Sequence.Finish(probed.World, probed.World.Facts, runner, events);
        Sequence.Finish(control.World, control.World.Facts, runner, otherEvents);
        Assert.Equal(control.World.Digest().Canonical(), probed.World.Digest().Canonical());
        Assert.Equal(otherEvents.Select(EventJson.Write), events.Select(EventJson.Write));
        Assert.Equal(control.World.Seats[0].Hand.Cards.Select(card => card.ObjectId),
            probed.World.Seats[0].Hand.Cards.Select(card => card.ObjectId));
        Assert.Equal(control.World.Random.Generator.NextUInt32(), probed.World.Random.Generator.NextUInt32());
        Assert.False(probed.World.Agenda.IsBusy);
    }

    [Fact]
    public void OutOfAgendaOccurrenceIsRefusedBeforeAnyPayment()
    {
        var runner = Runner();
        var (world, source, ally) = Fixture(runner);
        var hero = world.Seats[0].IdentityCard;
        var occurrence = Occurrence.ForAttack(42, [BasicPowerTiming.HeroUseInitiated], world,
            world.Facts, hero.ObjectId, world.TheCardIn(DeckType.VillainArea)!.ObjectId, 0)
            with { BasicUse = new(hero.ObjectId, hero.Incarnation) };
        string before = world.Digest().Canonical();
        Assert.Empty(runner.Waiting(world, occurrence, WindowKind.Interrupt));
        Assert.Throws<RulesNotImplementedException>(() => runner.ResolveWindow(world, occurrence,
            new PendingAbility(source.ObjectId, AbilityType.Interrupt, 0), new(source.ObjectId, [ally.ObjectId])));
        Assert.Equal(before, world.Digest().Canonical());
        Assert.True(ally.Ready);
        Assert.Equal(0, occurrence.BasicUse.Bonus);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ResourceCompositionIsRefusedBeforeCostMutation(bool adjustedPrice)
    {
        string face = adjustedPrice ? "01005" : "06032";
        var runner = Runner(face: face);
        var (world, source, ally) = Fixture(runner, face);
        var resource = world.CreateCard("01088", world.Seats[0].Hand);
        if (adjustedPrice) world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect,
            Kind: CardPlay.CardCostReduction, Amount: 1, Card: source.ObjectId,
            Affects: world.Seats[0].IdentityCard.ObjectId));
        Begin(world);
        if (adjustedPrice)
        {
            string digest = world.Digest().Canonical();
            var error = Assert.Throws<RulesNotImplementedException>(() => Sequence.Work(world, world.Facts, runner, []));
            Assert.Contains("positive event price", error.Message);
            Assert.Equal(digest, world.Digest().Canonical());
        }
        else
        {
            var prompt = Sequence.Work(world, world.Facts, runner, [])!;
            string digest = world.Digest().Canonical();
            var error = Assert.Throws<RulesNotImplementedException>(() => Sequence.Answer(world, world.Facts, runner,
                prompt, new(Assert.Single(prompt.Affordances).Id, [ally.ObjectId], [resource.ObjectId]), []));
            Assert.Contains("resource generation requires simultaneous payment", error.Message);
            Assert.Equal(digest, world.Digest().Canonical());
        }
        Assert.True(ally.Ready);
        Assert.Equal(DeckType.HandsArea, source.Area.Type);
        Assert.Equal(DeckType.HandsArea, resource.Area.Type);
    }

    [Fact]
    public void PaidEventAttackFinishesWithoutOfferingOrPayingTeamwork()
    {
        var runner = Runner(extra: $$$"""
        ,{"card":"05003","abilities":[{
          "trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"this","form":"hero"},
          "effect":{{{Attack}}}
        }]}
        """);
        Card? played = null;
        Card? resource = null;
        Card? source = null;
        Card? ally = null;
        var (game, world) = Playing(board =>
        {
            played = board.CreateCard("05003", board.Seats[0].Hand);
            resource = board.CreateCard("01088", board.Seats[0].Hand);
            source = board.CreateCard("06032", board.Seats[0].Hand);
            ally = board.CreateCard("01002", board.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        }, hero: true, abilities: runner);
        var offer = Assert.Single(game.Pending!.Affordances, item => item.AnchorId == played!.ObjectId);
        game.Resolve(PaidDecision.Take(offer, [resource!.ObjectId]));
        Assert.True(game.IsRootPrompt);
        Assert.Equal(1, world.TheCardIn(DeckType.VillainArea)!.Damage);
        Assert.Equal(DeckType.DiscardPile, played!.Area.Type);
        Assert.Equal(DeckType.DiscardPile, resource.Area.Type);
        Assert.Equal(DeckType.HandsArea, source!.Area.Type);
        Assert.True(ally!.Ready);
        Assert.False(world.Agenda.IsBusy);
    }

    [Fact]
    public void AllyBasicAttackFinishesWithoutOfferingTeamwork()
    {
        var runner = Runner();
        var (world, source, ally) = Fixture(runner);
        AllyBasicPowers.AllyPower(world, world.Facts, ally, world.TheCardIn(DeckType.VillainArea)!, BasicPowers.AttackVerb, []);
        Sequence.Finish(world, world.Facts, runner, []);
        Assert.Equal(1, world.TheCardIn(DeckType.VillainArea)!.Damage);
        Assert.Equal(0, ally.Damage);
        Assert.Equal(DeckType.HandsArea, source.Area.Type);
        Assert.False(world.Agenda.IsBusy);
    }

    [Fact]
    public void SyntheticStatReplacementKeepsTheMatchingPowerOfTheBasicThwart()
    {
        var runner = Runner();
        var (world, _, ally) = Fixture(runner);
        var scheme = world.TheCardIn(DeckType.MainSchemesArea)!;
        scheme.PlaceTokens("k_threat", 10);
        BasicThwartPowers.BasicThwart(world, world.Facts, 0, scheme, []);
        var prompt = Sequence.Work(world, world.Facts, runner, [])!;
        // Printed 04139 replaces the hero's THW read; 06032 still matches the
        // nominal thwart power of the exhausted ally. This is a synthetic hook.
        ThwartStatSubstitution.UseAttack(world, world.Agenda.Occurrence!);
        Sequence.Answer(world, world.Facts, runner, prompt,
            new(Assert.Single(prompt.Affordances).Id, [ally.ObjectId]), []);
        Sequence.Finish(world, world.Facts, runner, []);
        Assert.Equal(7, scheme.Tokens["k_threat"]);
        Assert.Equal(0, ally.Damage);
    }
}
