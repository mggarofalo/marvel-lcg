using System.Text.Json;
using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>Test-local Momentum Shift interpretation; no expansion admission.</summary>
public sealed class FullHealingCostTests
{
    private const string Book = """
        {"cards":[{"card":"09016","abilities":[{
          "trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"this","form":"hero"},
          "cost":{"heal":{"card":"you","amount":2}},
          "effect":{"chooseCard":{"from":{"query":"attackableEnemies"},"effect":{
            "attack":{"target":"chosen","effect":{"dealAttackDamage":{"cards":"chosen","amount":2}}}}}}
        }]},{"card":"01091","abilities":[{
          "trigger":{"event":"WhenActionTriggered","timing":"Resource","subject":"this"},
          "cost":{"dealDamage":{"cards":"you","amount":1}},"effect":{"generate":"YY"}
        }]}]}
        """;

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [Rule("rr:initiating-abilities.step.5")]
    public void OfferAndExecutionRequireTheFullHealTwoCost(int damage)
    {
        // "Pay the cost(s)." Ruling ecabf270719f2990 confirms healing two is
        // a full cost; ordinary heal effects still resolve as much as possible.
        var (game, world, runner, card, payment) = Prepare(damage);
        var prompt = game.Pending;
        string before = Snapshot(world);
        var actions = runner.Actions(world, 0).Where(action => action.Card == card.ObjectId).ToArray();
        Assert.Equal(damage == 2 ? 1 : 0, actions.Length);
        Assert.Equal(before, Snapshot(world));
        Assert.Same(prompt, game.Pending);
        if (damage < 2)
        {
            var forged = new PendingAbility(card.ObjectId, AbilityType.Action, 0, 0);
            Assert.Throws<RulesNotImplementedException>(() => runner.ExecuteAction(world,
                new PlayerAction(forged, [payment.ObjectId], [])));
            Assert.Equal(before, Snapshot(world));
            Assert.Same(prompt, game.Pending);
            return;
        }
        var offer = Assert.Single(game.Pending!.Affordances, action => action.AnchorId == card.ObjectId);
        Assert.Throws<ArgumentException>(() => game.Resolve(Decision.Take(offer.Id)));
        Assert.Equal(before, Snapshot(world));
        Assert.Same(prompt, game.Pending);
        Complete(game, world, card, payment);
        Assert.Equal(0, world.Seats[0].IdentityCard.Damage);
        Assert.Equal(2, world.TheCardIn(DeckType.VillainArea)!.Damage);
        Assert.Equal(DeckType.DiscardPile, payment.Area.Type);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:cost.5")]
    [Rule("rr:tough.2")]
    public void SelectedResourceDamageCanSupplyTheMissingHealingCost(bool tough)
    {
        // Multiple costs "must be paid simultaneously". Tough instead says
        // "prevent all of that damage", so that damage cannot fund the heal.
        var (game, world, runner, card, payment) = Prepare(1, generator: true, tough: tough);
        string before = Snapshot(world);
        var generator = world.Cards.Single(value => value.FaceId == "01091"
            && DeckTypes.IsInPlay(value.Area.Type));
        var actions = runner.Actions(world, 0).Where(action => action.Card == card.ObjectId).ToArray();
        Assert.Equal(tough ? 0 : 1, actions.Length);
        Assert.Equal(before, Snapshot(world));
        if (tough) return;
        // An available damaging generator does not make a hand-only payment valid.
        Assert.Throws<RulesNotImplementedException>(() => runner.ExecuteAction(world,
            new PlayerAction(Assert.Single(actions), [payment.ObjectId], [])));
        Assert.Equal(before, Snapshot(world));
        Complete(game, world, card, generator);
        Assert.Equal(0, world.Seats[0].IdentityCard.Damage);
        Assert.Equal(2, world.TheCardIn(DeckType.VillainArea)!.Damage);
        Assert.Same(world.Seats[0].Hand, payment.Area);
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(1, true)]
    [InlineData(2, false)]
    [Rule("rr:prevent.1")]
    public void EveryActivePreventionReducesDamageAvailableToHeal(int preventions, bool offered)
    {
        // Prevention "reduce[s] the amount of damage the target takes".
        var (game, world, runner, card, _) = Prepare(1, generator: true,
            book: Book.Replace("\"amount\":1}", "\"amount\":2}", StringComparison.Ordinal),
            preventions: preventions);
        string before = Snapshot(world);
        Assert.Equal(offered, runner.Actions(world, 0).Any(action => action.Card == card.ObjectId));
        Assert.Equal(before, Snapshot(world));
        if (!offered) return;
        var generator = world.Cards.Single(value => value.FaceId == "01091" && DeckTypes.IsInPlay(value.Area.Type));
        Complete(game, world, card, generator);
        Assert.Equal(preventions == 0 ? 1 : 0, world.Seats[0].IdentityCard.Damage);
    }

    [Fact]
    [Rule("rr:cost.12")]
    public void PartiallyPreventedTakingCostCannotSupplyHealing()
    {
        // A taking cost is unpaid "unless all of that damage was taken".
        string book = Book.Replace("\"dealDamage\":{\"cards\":\"you\",\"amount\":1}",
            "\"takeDamage\":{\"cards\":\"you\",\"amount\":2}", StringComparison.Ordinal);
        var (_, world, runner, card, _) = Prepare(1, generator: true, book: book, preventions: 1);
        Assert.DoesNotContain(runner.Actions(world, 0), action => action.Card == card.ObjectId);
    }

    [Fact]
    [Rule("rr:cost.5")]
    public void NonEventSpendAndHealCanUseItsSelectedDamagingGenerator()
    {
        // Multiple costs "must be paid simultaneously"; this is a synthetic ability.
        string book = Book.Replace("\"card\":\"09016\"", "\"card\":\"01006\"", StringComparison.Ordinal)
            .Replace("\"cost\":{\"heal\":{\"card\":\"you\",\"amount\":2}}",
                "\"cost\":{\"seq\":[{\"spend\":\"Y\"},{\"heal\":{\"card\":\"you\",\"amount\":2}}]}", StringComparison.Ordinal);
        var runner = new AbilityRunner(AbilityCatalog.Parse(book));
        Card? source = null;
        Card? generator = null;
        var (game, world) = Playing(board =>
        {
            board.Seats[0].IdentityCard.TakeDamage(1);
            source = InPlay(board, "01006");
            generator = InPlay(board, "01091");
        }, hero: true, abilities: runner);
        Complete(game, world, source!, generator!);
        Assert.Equal(0, world.Seats[0].IdentityCard.Damage);
        Assert.Equal(2, world.TheCardIn(DeckType.VillainArea)!.Damage);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [Rule("rr:heal.1")]
    public void OrdinaryHealingStillResolvesUpToItsAmount(int damage)
    {
        // A heal "can only bring a character to its maximum hit points".
        var runner = new AbilityRunner(AbilityCatalog.Parse("""
            {"cards":[{"card":"01006","abilities":[{
              "trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"this"},
              "effect":{"heal":{"card":"you","amount":2}}
            }]}]}
            """));
        Card? source = null;
        var (game, world) = Playing(board =>
        {
            board.Seats[0].IdentityCard.TakeDamage(damage);
            source = InPlay(board, "01006");
        }, abilities: runner);
        if (damage == 0) Assert.DoesNotContain(runner.Actions(world, 0), action => action.Card == source!.ObjectId);
        else
        {
            var action = Assert.Single(game.Pending!.Affordances, offer => offer.AnchorId == source!.ObjectId);
            game.Resolve(Decision.Take(action.Id));
        }
        Assert.Equal(0, world.Seats[0].IdentityCard.Damage);
    }

    [Fact]
    public void RepeatedQueriesPreserveExecutionAgainstAnUnprobedControl()
    {
        var probed = Prepare(1, generator: true);
        var control = Prepare(1, generator: true);
        string before = Snapshot(probed.Item2);
        var pending = probed.Item1.Pending;
        for (int i = 0; i < 3; i++)
            Assert.Contains(probed.Item3.Actions(probed.Item2, 0), action => action.Card == probed.Item4.ObjectId);
        Assert.Equal(before, Snapshot(probed.Item2));
        Assert.Same(pending, probed.Item1.Pending);
        var generator = probed.Item2.Cards.Single(card => card.FaceId == "01091" && DeckTypes.IsInPlay(card.Area.Type));
        var controlGenerator = control.Item2.Cards[generator.ObjectId];
        Complete(probed.Item1, probed.Item2, probed.Item4, generator);
        Complete(control.Item1, control.Item2, control.Item4, controlGenerator);
        Assert.Equal(Snapshot(control.Item2), Snapshot(probed.Item2));
    }

    [Fact]
    public void UnsupportedCombinedDamageProjectionRaisesBeforeAnyPayment()
    {
        var (_, world, runner, card, _) = Prepare(2, generator: true);
        InPlay(world, "01091");
        var identity = world.Seats[0].IdentityCard;
        DamageRecovery.Heal(world, world.Facts, identity, 2, "test", "Heal", []);
        string before = Snapshot(world);
        Assert.Throws<RulesNotImplementedException>(() => runner.Actions(world, 0));
        Assert.Equal(before, Snapshot(world));
        int[] paying = world.Cards.Where(value => value.FaceId == "01091"
            && DeckTypes.IsInPlay(value.Area.Type)).Select(value => value.ObjectId).ToArray();
        Assert.Throws<RulesNotImplementedException>(() => runner.ExecuteAction(world,
            new PlayerAction(new PendingAbility(card.ObjectId, AbilityType.Action, 0, 0), paying, [])));
        Assert.Equal(before, Snapshot(world));
        identity.TakeDamage(2);
        Assert.Contains(runner.Actions(world, 0), action => action.Card == card.ObjectId);
    }

    [Fact]
    public void HealingAvailabilityUsesTheAnsweringSecondSeat()
    {
        var runner = new AbilityRunner(AbilityCatalog.Parse(Book));
        Card? card = null;
        Card? generator = null;
        var (game, world) = Playing(board =>
        {
            board.FirstPlayer = 1;
            board.Seats[1].IdentityCard.TurnTo("01010a");
            board.Seats[1].IdentityCard.TakeDamage(1);
            card = board.CreateCard("09016", board.Seats[1].Hand);
            generator = board.CreateCard("01091",
                board.AreaOf(DeckType.SupportsArea, PlayArea.Of(1), cardOwner: 1));
        }, heroes: ["spider_man", "captain_marvel"], abilities: runner);
        string before = Snapshot(world);
        Assert.Contains(runner.Actions(world, 1), action => action.Card == card!.ObjectId && action.Player == 1);
        Assert.Equal(before, Snapshot(world));
        Complete(game, world, card!, generator!);
        Assert.Equal(0, world.Seats[1].IdentityCard.Damage);
        Assert.Equal(0, world.Seats[0].IdentityCard.Damage);
    }

    private static void Complete(Game game, World world, Card card, Card payment)
    {
        var offer = Assert.Single(game.Pending!.Affordances, action => action.AnchorId == card.ObjectId);
        int[] paying = [payment.ObjectId];
        var allocation = ResourcePayment.Allocate(Assert.Single(offer.Costs!), paying)!;
        game.Resolve(new Decision(offer.Id, [], paying, Allocations: allocation));
        var target = world.TheCardIn(DeckType.VillainArea)!;
        game.Resolve(Decision.Take(Assert.Single(game.Pending!.Affordances,
            action => action.AnchorId == target.ObjectId).Id));
        if (card.FaceId == "09016") Assert.Equal(DeckType.DiscardPile, card.Area.Type);
        Assert.Equal(Question.TurnOption, game.Pending!.Asking);
        Assert.Empty(world.Agenda.Outstanding);
    }

    private static (Game, World, AbilityRunner, Card, Card) Prepare(int damage, bool generator = false, bool tough = false, string? book = null, int preventions = 0)
    {
        var runner = new AbilityRunner(AbilityCatalog.Parse(book ?? Book));
        Card? card = null;
        Card? payment = null;
        var (game, world) = Playing(board =>
        {
            board.Seats[0].IdentityCard.TakeDamage(damage);
            card = board.CreateCard("09016", board.Seats[0].Hand);
            payment = board.CreateCard("01088", board.Seats[0].Hand);
            if (generator) InPlay(board, "01091");
            for (int i = 0; i < preventions; i++)
                board.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect, "preventDamage",
                    Amount: 1, Affects: board.Seats[0].IdentityCard.ObjectId));
            if (tough) Statuses.Give(board, board.Seats[0].IdentityCard, Statuses.Tough);
        }, hero: true, abilities: runner);
        return (game, world, runner, card!, payment!);
    }

    private static string Snapshot(World world) => JsonSerializer.Serialize(new
    {
        Digest = world.Digest().Canonical(), Random = world.Random.Generator.GetState(),
        world.Random.Generator.WordsConsumed, world.Agenda.Stage,
        Pending = world.Agenda.Outstanding.Select(step => new { step.What, step.Index, step.Subject, step.Seat }),
    });
}
