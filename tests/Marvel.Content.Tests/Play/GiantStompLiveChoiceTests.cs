using Marvel.Cards.Run;
using Marvel.Cards.Dsl;
using Marvel.Rules.Play;
using Marvel.Rules.Events;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class GiantStompLiveChoiceTests
{
    private const string Tail = """{"attackAnyEnemy":{"amount":8}}""";

    [Rule("rr:ability.4")]
    [Rule("rr:attack-player-ability-type.2.1")]
    [Rule("rr:attack-player-ability-type.5")]
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void ChoiceFollowsSplashAndKeepsOneAttack(int minions)
    {
        // "resolve the ability one sentence at a time"; multiple damage instances
        // are "a single attack", and the attacker "attacked each of those enemies".
        var runner = GiantStompCompositionTests.Runner(Tail, targetless: true);
        var world = GiantStompCompositionTests.Board(runner, minions);
        var minion = world.Cards.SingleOrDefault(card => card.FaceId == "01102");
        if (minion is not null)
        {
            world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect,
                Kind: "health", Amount: 10, Affects: minion.ObjectId));
            world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect,
                Kind: "retaliate", Amount: 1, Affects: minion.ObjectId));
        }
        var game = Play(world, runner);
        var enemy = minion ?? world.TheCardIn(DeckType.VillainArea)!;
        if (minion is not null) Assert.Equal(1, minion.Damage);
        Assert.Equal(Question.Element, game.Pending!.Asking);
        var choice = Assert.Single(game.Pending.Affordances, offer => offer.Id == enemy.ObjectId);
        string before = world.Digest().Canonical();
        var random = world.Random.Generator.GetState();
        var pending = game.Pending;
        Assert.Throws<ArgumentException>(() => game.Resolve(Decision.Take(int.MaxValue)));
        Assert.Equal(before, world.Digest().Canonical());
        Assert.Equal(random.Words, world.Random.Generator.GetState().Words);
        Assert.Equal(random.Index, world.Random.Generator.GetState().Index);
        Assert.Same(pending, game.Pending);
        game.Resolve(Decision.Take(choice.Id));
        DrainOrder(game);
        Assert.Equal(minion is null ? 8 : 9, enemy.Damage);
        Assert.Equal(1, enemy.Tokens.GetValueOrDefault("c_recipient"));
        Assert.Equal(1, world.Seats[0].IdentityCard.Tokens.GetValueOrDefault("c_whole"));
        Assert.Equal(minions, world.Seats[0].IdentityCard.Damage);
        Assert.False(world.Agenda.IsBusy);
    }

    [Rule("rr:ability.4")]
    [Rule("rr:guard.1")]
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void FirstSentenceInterruptSettlesBeforeTheEnemyChoice(bool prevent)
    {
        // Sentence order means the interrupt and resulting defeat finish before
        // the later enemy selection; Guard applies while the minion survives.
        var runner = GiantStompCompositionTests.Runner(Tail, targetless: true, interrupt: true);
        var world = GiantStompCompositionTests.Board(runner, 0);
        var guard = world.CreateCard("08028", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        guard.TakeDamage(Damage.Health(world, world.Facts, guard) - 1);
        world.CreateCard("01091", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        var game = Play(world, runner);
        Assert.Equal(Question.Option, game.Pending!.Asking);
        Assert.DoesNotContain(game.Pending.Affordances, option => option.Id == villain.ObjectId);
        game.Resolve(Decision.Take(game.Pending.Affordances[prevent ? 0 : 1].Id));
        Assert.Equal(Question.Element, game.Pending!.Asking);
        Assert.Equal(!prevent, game.Pending.Affordances.Any(option => option.Id == villain.ObjectId));
        Assert.Equal(prevent ? DeckType.EngagedEnemiesArea : DeckType.EncounterDiscardPile, guard.Area.Type);
        game.Resolve(Decision.Take(prevent ? guard.ObjectId : villain.ObjectId));
        if (prevent) game.Resolve(Decision.Take(game.Pending!.Affordances[0].Id));
        DrainOrder(game);
        Assert.False(world.Agenda.IsBusy);
    }

    [Fact]
    public void LiveChoiceRetainsUninterruptedAttackDamageCausality()
    {
        var actual = DamageEvents(Tail, true);
        var expected = DamageEvents("""{"dealAttackDamage":{"cards":{"query":"villain"},"amount":8}}""", false);
        Assert.Equal(2, actual.Count);
        Assert.Equal(expected, actual);
    }

    private static List<(int Card, long? From, long? To, string Trigger, string Verb,
        AttackIdentity? AttackIdentity, EventCause? Cause)> DamageEvents(string tail, bool targetless)
    {
        var runner = GiantStompCompositionTests.Runner(tail, targetless);
        var world = GiantStompCompositionTests.Board(runner, 1);
        var source = world.Seats[0].Hand.Cards.Single(card => card.FaceId == "12003");
        var action = Assert.Single(runner.Actions(world, 0), item => item.Card == source.ObjectId);
        var payment = world.Seats[0].Hand.Cards.Where(card => card.FaceId is "12021" or "12023")
            .Select(card => card.ObjectId).ToArray();
        var events = runner.ExecuteAction(world, new PlayerAction(action, payment, [])).Events.ToList();
        for (int i = 0; i < 30; i++)
        {
            var prompt = Sequence.Work(world, world.Facts, runner, events);
            if (prompt is null) break;
            var villain = world.TheCardIn(DeckType.VillainArea)!;
            var answer = prompt.Affordances.FirstOrDefault(option => option.Id == villain.ObjectId);
            Sequence.Answer(world, world.Facts, runner, prompt,
                answer is null ? Decision.Take(prompt.Affordances[0].Id) : Decision.Take(answer.Id), events);
        }
        Assert.False(world.Agenda.IsBusy);
        if (targetless)
        {
            var boundary = Assert.Single(events.OfType<AttackBoundary>(), item => !item.Started);
            Assert.Equal(-1, boundary.Target);
            Assert.Equal(world.Seats[0].IdentityCard.ObjectId, boundary.Source);
        }
        return events.OfType<FieldSet>().Where(item => item.Field == "health" && item.AttackIdentity is not null)
            .Select(item => (item.Card, item.From, item.To, item.Trigger, item.Verb, item.AttackIdentity, item.Cause)).ToList();
    }

    [Rule("rr:attack-player-ability-type.5")]
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void OptionalRecipientResponsesRemainIndependentlySelectable(int first)
    {
        // The attacker is considered to have "attacked each of those enemies".
        var runner = GiantStompCompositionTests.Runner(Tail, targetless: true, optionalResponse: true);
        var world = GiantStompCompositionTests.Board(runner, 1);
        var minion = world.Cards.Single(card => card.FaceId == "01102");
        world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect,
            Kind: "health", Amount: 10, Affects: minion.ObjectId));
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        var game = Play(world, runner);
        game.Resolve(Decision.Take(villain.ObjectId));
        var responses = game.Pending!.Affordances.Where(option => option.AnchorId ==
            world.Cards.Single(card => card.FaceId == "01093").ObjectId).ToArray();
        Assert.Equal(2, responses.Length);
        game.Resolve(Decision.Take(responses[first].Id));
        Assert.Equal(1, minion.Tokens.GetValueOrDefault("c_recipient") + villain.Tokens.GetValueOrDefault("c_recipient"));
        var remaining = Assert.Single(game.Pending!.Affordances, option => option.AnchorId ==
            world.Cards.Single(card => card.FaceId == "01093").ObjectId);
        game.Resolve(Decision.Take(remaining.Id));
        Assert.Equal(1, minion.Tokens.GetValueOrDefault("c_recipient"));
        Assert.Equal(1, villain.Tokens.GetValueOrDefault("c_recipient"));
        Assert.False(world.Agenda.IsBusy);
    }

    [Rule("rr:target.3.7")]
    [Fact]
    public void NoDamageableEnemyRejectsBeforePaymentWithSameCostControl()
    {
        // A target that "cannot take damage" is not valid for damage alone.
        var runner = GiantStompCompositionTests.Runner(Tail, targetless: true);
        var world = GiantStompCompositionTests.Board(runner, 0);
        var madame = world.CreateCard("01181", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect,
            Kind: "guard", Amount: 1, Affects: madame.ObjectId));
        var scheme = world.CreateCard("01180", world.AreaOf(DeckType.SideSchemesArea));
        var source = world.Seats[0].Hand.Cards.Single(card => card.FaceId == "12003");
        var payment = world.Seats[0].Hand.Cards.Where(card => card.FaceId is "12021" or "12023").ToArray();
        string before = world.Digest().Canonical();
        var random = world.Random.Generator.GetState();
        Assert.DoesNotContain(runner.Actions(world, 0), action => action.Card == source.ObjectId);
        Assert.Equal(before, world.Digest().Canonical());
        Assert.Equal(random.Words, world.Random.Generator.GetState().Words);
        Assert.Equal(random.Index, world.Random.Generator.GetState().Index);
        Assert.All(payment, card => Assert.Same(world.Seats[0].Hand, card.Area));
        World.MoveToTop(scheme, world.AreaOf(DeckType.EncounterDiscardPile));
        Assert.Contains(runner.Actions(world, 0), action => action.Card == source.ObjectId);
    }

    [Rule("rr:damage.step.7")]
    [Rule("rr:damage.step.8")]
    [Rule("rr:forced.5")]
    [Fact]
    public void SimultaneousDefeatOrderFinishesBeforeEnemyChoice()
    {
        // "the first player determines the order" of simultaneous forced abilities;
        // When Defeated resolves before discarding the defeated characters.
        var runner = GiantStompCompositionTests.Runner(Tail, targetless: true);
        var world = GiantStompCompositionTests.Board(runner, 0);
        var soldiers = Enumerable.Range(0, 2).Select(_ =>
            world.CreateCard("01182", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)))).ToArray();
        foreach (var soldier in soldiers) soldier.TakeDamage(3);
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        var game = Play(world, runner);
        Assert.Equal(Question.Order, game.Pending!.Asking);
        Assert.All(soldiers, soldier => Assert.Equal(DeckType.EngagedEnemiesArea, soldier.Area.Type));
        game.Resolve(Decision.Take(soldiers[1].ObjectId));
        Assert.All(soldiers, soldier => Assert.Equal(DeckType.EncounterDiscardPile, soldier.Area.Type));
        Assert.Equal(2, world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(0)).Cards.Count);
        Assert.Equal(Question.Element, game.Pending!.Asking);
        game.Resolve(Decision.Take(villain.ObjectId));
        DrainOrder(game);
        Assert.False(world.Agenda.IsBusy);
    }

    [Rule("rr:ability.4")]
    [Rule("rr:guard.1")]
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void GuardEligibilityIsReadAfterSplash(bool guardDies)
    {
        // Guard says the engaged player "cannot attack any villain" while it is engaged.
        var runner = GiantStompCompositionTests.Runner(Tail, targetless: true);
        var world = GiantStompCompositionTests.Board(runner, 0);
        var guard = world.CreateCard("08028", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        if (guardDies) guard.TakeDamage(Damage.Health(world, world.Facts, guard) - 1);
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        var game = Play(world, runner);
        Assert.Equal(Question.Element, game.Pending!.Asking);
        Assert.Equal(guardDies, game.Pending.Affordances.Any(offer => offer.Id == villain.ObjectId));
        if (!guardDies) Assert.Contains(game.Pending.Affordances, offer => offer.Id == guard.ObjectId);
        var target = guardDies ? villain : guard;
        game.Resolve(Decision.Take(target.ObjectId));
        DrainOrder(game);
        Assert.False(world.Agenda.IsBusy);
    }

    [Rule("rr:labeled-ability.6")]
    [Fact]
    public void StunCancelsBothSentencesAfterPayment()
    {
        // The "entire ability (except for its costs) is canceled".
        var runner = GiantStompCompositionTests.Runner(Tail, targetless: true);
        var world = GiantStompCompositionTests.Board(runner, 1);
        var minion = world.Cards.Single(card => card.FaceId == "01102");
        Statuses.Give(world, world.Seats[0].IdentityCard, Statuses.Stunned);
        var game = Play(world, runner);
        Assert.Equal(0, minion.Damage);
        Assert.Equal(0, world.TheCardIn(DeckType.VillainArea)!.Damage);
        Assert.False(Statuses.Has(world, world.Seats[0].IdentityCard, Statuses.Stunned));
        Assert.False(world.Agenda.IsBusy);
        Assert.DoesNotContain(game.Pending!.Affordances, offer => offer.Id == minion.ObjectId);
    }

    [Rule("rr:guard.1")]
    [Fact]
    public void NonFirstControllerChoosesAgainstTheirOwnGuard()
    {
        // Guard restricts the "engaged player"; the ability's controller chooses.
        var runner = GiantStompCompositionTests.Runner(Tail, targetless: true);
        var world = ThorActualFixture.Board("06001a", 2);
        var seat = world.Seats[1];
        World.MoveToTop(seat.IdentityCard, world.AreaOf(DeckType.RemovedArea));
        seat.IdentityCard = world.CreateCard("12001a,12001b,12001c", seat.Hero);
        seat.IdentityCard.TurnTo("12001c");
        world.Abilities = runner;
        var source = world.CreateCard("12003", seat.Hand);
        int[] payment = Enumerable.Range(0, 3)
            .Select(_ => world.CreateCard("01087", seat.Hand).ObjectId).ToArray();
        var guard = world.CreateCard("08028", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1)));
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        runner.ExecuteAction(world, new PlayerAction(new PendingAbility(source.ObjectId, AbilityType.Action, 1), payment, []));
        var prompt = Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, runner, []));
        Assert.Equal(1, prompt.Player);
        Assert.DoesNotContain(prompt.Affordances, offer => offer.Id == villain.ObjectId);
        Assert.Contains(prompt.Affordances, offer => offer.Id == guard.ObjectId);
        Sequence.Answer(world, world.Facts, runner, prompt, Decision.Take(guard.ObjectId), []);
        Sequence.Finish(world, world.Facts, runner, []);
        Assert.False(world.Agenda.IsBusy);
    }

    [Rule("rr:villain-defeat.3.1")]
    [Rule("rr:winning-the-game")]
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void LaterDamageUsesOrdinaryVillainDefeatSettlement(bool terminal)
    {
        // A defeated villain advances to the next stage, or the players win if
        // the final stage is defeated. No excess damage carries to a new stage.
        var runner = GiantStompCompositionTests.Runner(Tail, targetless: true);
        var world = GiantStompCompositionTests.Board(runner, 0);
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        villain.TakeDamage(Damage.Health(world, world.Facts, villain) - 4);
        if (terminal)
        {
            var next = world.TheCardIn(DeckType.VillainDeck)!;
            World.MoveToTop(next, world.AreaOf(DeckType.RemovedArea));
        }
        var game = Play(world, runner);
        game.Resolve(Decision.Take(villain.ObjectId));
        if (!terminal) DrainOrder(game);
        Assert.Equal(terminal, world.IsOver);
        if (!terminal)
        {
            Assert.Equal("01095", world.TheCardIn(DeckType.VillainArea)!.FaceId);
            Assert.Equal(0, world.TheCardIn(DeckType.VillainArea)!.Damage);
            Assert.False(world.Agenda.IsBusy);
        }
    }

    [Theory]
    [InlineData("{\"dealAttackDamage\":{\"cards\":{\"query\":\"minions\"},\"amount\":1}}")]
    [InlineData("{\"dealAttackDamage\":{\"cards\":{\"query\":\"enemies\"},\"amount\":1,\"simultaneous\":1}}")]
    public void TargetlessShapeRejectsUnsupportedFirstSentences(string first)
    {
        string text = $$$$"""
            {"cards":[{"card":"12003","abilities":[{
              "trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"this"},
              "effect":{"attack":{"effect":{"seq":[{{{{first}}}},{{{{Tail}}}}]}}}
            }]}]}
            """;
        Assert.Throws<AbilityException>(() => new AbilityRunner(AbilityCatalog.Parse(text)));
    }

    private static Game Play(World world, AbilityRunner runner)
    {
        var source = world.Seats[0].Hand.Cards.Single(card => card.FaceId == "12003");
        var game = Game.Begin(world, world.Facts, runner);
        while (game.Pending!.Affordances.Any(option => option.Verb == Game.ResolveMulligans))
            game.Resolve(Decision.Take(game.Pending.Affordances.Single(option => option.Verb == Game.ResolveMulligans).Id));
        var action = Assert.Single(game.Pending.Affordances, offer => offer.AnchorId == source.ObjectId);
        int[] paying = world.Seats[0].Hand.Cards.Where(card => card.FaceId is "12021" or "12023")
            .Select(card => card.ObjectId).ToArray();
        game.Resolve(new Decision(action.Id, [], paying,
            Allocations: ResourcePayment.Allocate(Assert.Single(action.Costs!), paying)));
        return game;
    }

    private static void DrainOrder(Game game)
    {
        while (game.Pending!.Asking == Question.Order)
            game.Resolve(Decision.Take(game.Pending.Affordances[0].Id));
    }
}
