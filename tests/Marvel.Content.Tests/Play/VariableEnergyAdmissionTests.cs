using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>Synthetic variable-payment effects; no expansion ability is admitted.</summary>
public sealed class VariableEnergyAdmissionTests
{
    private const string Damage = """{"dealDamage":{"cards":{"query":"villain"},"amount":{"result":"energy"}}}""";
    private const string Control = """{"placeCounters":{"card":"you","counter":"test","count":1}}""";

    [Theory]
    [InlineData(false, 0, 1)]
    [InlineData(false, 0, 2)]
    [InlineData(true, 0, 2)]
    [InlineData(true, 1, 3)]
    [Rule("rr:target.3")]
    public void FutureEnergyAdmitsDamageWithoutChangingTheWorld(bool aerial, int discount, int x)
    {
        // "A target is valid ... if any part of that ability can affect that target."
        string marked = Damage.Replace("\"amount\":{\"result\":\"energy\"}",
            "\"amount\":{\"result\":\"energy\"},\"ignoresTough\":1", StringComparison.Ordinal);
        string effect = """{"if":{"test":{"hasTrait":{"card":"you","trait":"AERIAL"}},"then":THEN,"else":ELSE}}"""
            .Replace("THEN", marked, StringComparison.Ordinal).Replace("ELSE", Damage, StringComparison.Ordinal);
        var (world, runner, source) = Prepare(effect, "01014", discount);
        if (aerial) world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect,
            Kind: Traits.Granted + "AERIAL", Affects: world.Seats[0].IdentityCard.ObjectId));
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        Statuses.Give(world, villain, Statuses.Tough);
        var before = world.Digest().Canonical();
        var random = world.Random.Generator.GetState();
        long words = world.Random.Generator.WordsConsumed;
        var agenda = world.Agenda.Outstanding.ToArray();
        PendingAbility action = default;
        for (int repeat = 0; repeat < 3; repeat++)
            action = Assert.Single(runner.Actions(world, 0), offer => offer.Card == source.ObjectId);
        Assert.Equal(before, world.Digest().Canonical());
        Assert.Equal(random.Index, world.Random.Generator.GetState().Index);
        Assert.Equal(random.Words, world.Random.Generator.GetState().Words);
        Assert.Equal(words, world.Random.Generator.WordsConsumed);
        Assert.Equal(agenda, world.Agenda.Outstanding.ToArray());
        var values = new Dictionary<string, long> { ["X"] = x };
        var price = Assert.Single(runner.Describe(world, action).CostOptions);
        int[] paying = [.. world.Seats[0].Hand.Cards.Where(card => card != source).Select(card => card.ObjectId)];
        var allocation = ResourcePayment.Allocate(price, paying, values);
        Assert.NotNull(allocation);
        runner.ExecuteAction(world, new PlayerAction(action, paying, [], values, allocation));
        Sequence.Finish(world, world.Facts, runner, []);
        Assert.Equal(aerial ? x : 0, villain.Damage);
        Assert.Equal(aerial, Statuses.Has(world, villain, Statuses.Tough));
        Assert.Equal(DeckType.DiscardPile, source.Area.Type);
    }

    [Fact]
    public void ProbedAndUnprobedActionsResolveToTheSameStateAndRandomState()
    {
        var (world, runner, source) = Prepare(Damage, "01014");
        var (controlWorld, controlRunner, controlSource) = Prepare(Damage, "01014");
        for (int repeat = 0; repeat < 3; repeat++)
            Assert.Single(runner.Actions(world, 0), action => action.Card == source.ObjectId);
        foreach (var (board, abilities, card) in new[]
            { (world, runner, source), (controlWorld, controlRunner, controlSource) })
        {
            var action = new PendingAbility(card.ObjectId, AbilityType.Action, 0);
            var price = Assert.Single(abilities.Describe(board, action).CostOptions);
            int[] paying = [.. board.Seats[0].Hand.Cards.Where(held => held != card).Select(held => held.ObjectId)];
            var values = new Dictionary<string, long> { ["X"] = 2 };
            abilities.ExecuteAction(board, new PlayerAction(action, paying, [], values,
                ResourcePayment.Allocate(price, paying, values)));
            Sequence.Finish(board, board.Facts, abilities, []);
        }
        Assert.Equal(controlWorld.Digest().Canonical(), world.Digest().Canonical());
        Assert.Equal(controlWorld.Random.Generator.GetState().Index, world.Random.Generator.GetState().Index);
        Assert.Equal(controlWorld.Random.Generator.GetState().Words, world.Random.Generator.GetState().Words);
        Assert.Equal(controlWorld.Agenda.Outstanding.ToArray(), world.Agenda.Outstanding.ToArray());
    }

    [Theory]
    [InlineData("01089", 0, false)]
    [InlineData("01012", 0, false)]
    [InlineData("01012", 1, true)]
    [InlineData("01055", 0, false)]
    [InlineData("01055", 1, true)]
    public void PrintedAndEnergyCostsNeedDistinctIconsIncludingDiscountedWild(string resource, int discount, bool offered)
    {
        var (world, runner, source) = Prepare(Damage, resource, discount);
        Assert.Equal(offered, runner.Actions(world, 0).Any(action => action.Card == source.ObjectId));
        var control = Runner(Control);
        Assert.Equal(offered, control.Actions(world, 0).Any(action => action.Card == source.ObjectId));
    }

    [Fact]
    [Rule("rr:target.3")]
    public void AFeasiblePaymentCannotMakeZeroDamageSignificant()
    {
        // "A target is valid ... if any part of that ability can affect that target."
        var (world, runner, source) = Prepare(Damage.Replace("{\"result\":\"energy\"}",
            "{\"add\":[{\"result\":\"energy\"},-2]}", StringComparison.Ordinal), "01014");
        Assert.Single(Runner(Control).Actions(world, 0), action => action.Card == source.ObjectId);
        Assert.DoesNotContain(runner.Actions(world, 0), action => action.Card == source.ObjectId);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void OnlyTheCurrentTraitBranchEstablishesAnEffect(bool aerial)
    {
        string effect = """{"if":{"test":{"hasTrait":{"card":"you","trait":"AERIAL"}},"then":DAMAGE,"else":{"dealDamage":{"cards":{"query":"villain"},"amount":0}}}}"""
            .Replace("DAMAGE", Damage, StringComparison.Ordinal);
        var (world, runner, source) = Prepare(effect, "01014");
        if (aerial) world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect,
            Kind: Traits.Granted + "AERIAL", Affects: world.Seats[0].IdentityCard.ObjectId));
        Assert.Single(Runner(Control).Actions(world, 0), action => action.Card == source.ObjectId);
        Assert.Equal(aerial, runner.Actions(world, 0).Any(action => action.Card == source.ObjectId));
    }

    [Fact]
    [Rule("rr:initiating-abilities.step.5")]
    public void InsignificantChosenXRejectsAtomicallyAndCanBeRetried()
    {
        // "Abort this process without paying any costs."
        var (world, runner, source) = Prepare(Damage.Replace("{\"result\":\"energy\"}",
            "{\"add\":[{\"result\":\"energy\"},-1]}", StringComparison.Ordinal), "01014");
        var action = Assert.Single(runner.Actions(world, 0), offer => offer.Card == source.ObjectId);
        var price = Assert.Single(runner.Describe(world, action).CostOptions);
        int[] paying = [.. world.Seats[0].Hand.Cards.Where(card => card != source).Select(card => card.ObjectId)];
        var values = new Dictionary<string, long> { ["X"] = 1 };
        var before = world.Digest().Canonical();
        var random = world.Random.Generator.GetState();
        var agenda = world.Agenda.Outstanding.ToArray();
        Assert.Throws<RulesNotImplementedException>(() => runner.ExecuteAction(world,
            new PlayerAction(action, paying, [], values, ResourcePayment.Allocate(price, paying, values))));
        Assert.Equal(before, world.Digest().Canonical());
        Assert.Equal(random.Index, world.Random.Generator.GetState().Index);
        Assert.Equal(random.Words, world.Random.Generator.GetState().Words);
        Assert.Equal(agenda, world.Agenda.Outstanding.ToArray());
        values["X"] = 2;
        runner.ExecuteAction(world, new PlayerAction(action, paying, [], values,
            ResourcePayment.Allocate(price, paying, values)));
        Sequence.Finish(world, world.Facts, runner, []);
        Assert.Equal(1, world.TheCardIn(DeckType.VillainArea)!.Damage);
        Assert.Equal(DeckType.DiscardPile, source.Area.Type);
    }

    [Theory]
    [InlineData(false, 0, false)]
    [InlineData(true, 0, true)]
    [InlineData(true, 1, false)]
    public void DeclaredResourceBranchMustShareTheFeasibleCombinedReceipt(bool wild, int discount, bool offered)
    {
        string effect = """{"if":{"test":{"paidWithResource":"R"},"then":DAMAGE,"else":{"dealDamage":{"cards":{"query":"villain"},"amount":0}}}}"""
            .Replace("DAMAGE", Damage, StringComparison.Ordinal);
        var (world, runner, source) = Prepare(effect, "01088", discount);
        world.CreateCard(wild ? "01055" : "01089", world.Seats[0].Hand);
        Assert.Single(Runner(Control).Actions(world, 0), action => action.Card == source.ObjectId);
        Assert.Equal(offered, runner.Actions(world, 0).Any(action => action.Card == source.ObjectId));
    }

    [Fact]
    public void DamageReceiptSurvivesTheResourceGeneratorsDefeatWindow()
    {
        var runner = Runner(Damage);
        Card? source = null;
        Card? generator = null;
        var (game, world) = Playing(board =>
        {
            source = board.CreateCard("06006", board.Seats[0].Hand);
            generator = board.CreateCard("01030", board.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
            generator.TakeDamage(2);
            InPlay(board, "01092");
        }, abilities: runner);
        var action = Assert.Single(game.Pending!.Affordances, offer => offer.AnchorId == source!.ObjectId);
        var values = new Dictionary<string, long> { ["X"] = 1 };
        var allocation = ResourcePayment.Allocate(Assert.Single(action.CostOptions), [generator!.ObjectId], values);
        Assert.NotNull(allocation);
        game.Resolve(Decision.Take(action.Id, [], [generator.ObjectId], values, allocation));
        Assert.Equal(Question.Opportunity, game.Pending!.Asking);
        Assert.Equal(DeckType.RevealingArea, source!.Area.Type);
        Assert.Equal(0, world.TheCardIn(DeckType.VillainArea)!.Damage);
        values["X"] = 99;
        game.Resolve(Decision.Decline);
        Assert.Equal(1, world.TheCardIn(DeckType.VillainArea)!.Damage);
        Assert.Equal(DeckType.DiscardPile, source.Area.Type);
        Assert.Equal(DeckType.DiscardPile, generator.Area.Type);
        Assert.False(world.Agenda.IsBusy);
    }

    [Theory]
    [InlineData(AbilityType.Interrupt)]
    [InlineData(AbilityType.Response)]
    public void WindowPaymentChecksTheChosenEnergyBeforeCommitting(AbilityType timing)
    {
        string effect = Damage.Replace("{\"result\":\"energy\"}",
            "{\"add\":[{\"result\":\"energy\"},-1]}", StringComparison.Ordinal);
        var (world, _, source) = Prepare(effect, "01014");
        var runner = new AbilityRunner(AbilityCatalog.Parse("""
            {"cards":[{"card":"06006","abilities":[{
              "trigger":{"event":"EnergyProbe","timing":"TIMING","subject":"game"},
              "cost":{"spendEnergyX":"Y"},"effect":EFFECT}]}]}
            """.Replace("TIMING", timing.ToString(), StringComparison.Ordinal).Replace("EFFECT", effect, StringComparison.Ordinal)));
        var resource = Assert.Single(world.Seats[0].Hand.Cards, card => card != source);
        var pending = new PendingAbility(source.ObjectId, timing, 0);
        var occurrence = new Occurrence(1, ["EnergyProbe"], Player: 0);
        var window = timing == AbilityType.Interrupt ? WindowKind.Interrupt : WindowKind.Response;
        Assert.Single(runner.Waiting(world, occurrence, window), offer => offer.Card == source.ObjectId);
        var before = world.Digest().Canonical();
        Assert.Throws<RulesNotImplementedException>(() => runner.ResolveWindow(world, occurrence, pending,
            Decision.Take(source.ObjectId, [], [resource.ObjectId], new Dictionary<string, long> { ["X"] = 1 },
                [new(resource.ObjectId, 0, "Y"), new(resource.ObjectId, 1, "Y")])));
        Assert.Equal(before, world.Digest().Canonical());
        var result = runner.ResolveWindow(world, occurrence, pending,
            Decision.Take(source.ObjectId, [], [resource.ObjectId], new Dictionary<string, long> { ["X"] = 2 },
                [new(resource.ObjectId, 0, "Y"), new(resource.ObjectId, 1, "YY")]));
        Sequence.Finish(world, world.Facts, runner, []);
        Assert.True(result.IsComplete);
        Assert.Equal(1, world.TheCardIn(DeckType.VillainArea)!.Damage);
        Assert.Equal(DeckType.DiscardPile, source.Area.Type);
    }

    private static (World, AbilityRunner, Card) Prepare(string effect, string resource, int discount = 0)
    {
        var runner = Runner(effect);
        var (_, world) = Playing(_ => { }, abilities: runner);
        foreach (var held in world.Seats[0].Hand.Cards.ToArray()) World.MoveToTop(held, world.Seats[0].Deck);
        var source = world.CreateCard("06006", world.Seats[0].Hand);
        world.CreateCard(resource, world.Seats[0].Hand);
        if (discount > 0) CardPayment.ReduceNextCardCost(world, world.Seats[0].IdentityCard, 0, discount);
        return (world, runner, source);
    }

    private static AbilityRunner Runner(string effect) => new(AbilityCatalog.Parse("""
        {"cards":[{"card":"06006","abilities":[{
          "trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"game"},
          "cost":{"spendEnergyX":"Y"},"effect":EFFECT}]},
          {"card":"01030","abilities":[{
            "trigger":{"event":"WhenActionTriggered","timing":"Resource","subject":"game"},
            "cost":{"dealDamage":{"cards":"this","amount":2}},"effect":{"generate":"YY"}}]},
          {"card":"01092","abilities":[{
            "trigger":{"event":"WhenCardWouldBeDefeated","timing":"Interrupt","subject":"game"},
            "effect":{"draw":{"player":"you","count":1}}}]}]}
        """.Replace("EFFECT", effect, StringComparison.Ordinal)));
}
