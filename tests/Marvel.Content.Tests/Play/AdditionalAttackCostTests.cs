using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class AdditionalAttackCostTests
{
    private static Marvel.Cards.Run.AbilityRunner CostRunner() => new(Marvel.Cards.Dsl.AbilityCatalog.Parse("""
        {"cards":[{"card":"03014","abilities":[{
          "trigger":{"timing":"Constant","subject":"this"},
          "effect":{"grant":{"card":"this","keyword":"attack_hand_discard_cost","amount":1}}
        }]}]}
        """));

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    [Rule("rr:cost.10")]
    [Rule("rr:stun-stunned.5")]
    public void BasicAttackPaysChosenDiscardAndExhaustionTogetherBeforeStun(bool hero, bool stunned)
    {
        // rr:cost.10: pay "all additional costs simultaneously with the cost that is being added to".
        // rr:stun-stunned.5: "costs associated with the attack attempt ... must still be paid."
        var runner = CostRunner();
        Card? ally = null;
        Card? payment = null;
        var (game, world) = Playing(board =>
        {
            ally = board.CreateCard("03014", board.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
            payment = board.CreateCard("01088", board.Seats[0].Hand);
            if (stunned) Statuses.Give(board, ally, Statuses.Stunned);
        }, hero: hero, abilities: runner);
        var attack = Assert.Single(game.Pending!.Affordances,
            offer => offer.AnchorId == ally!.ObjectId && offer.Verb == BasicPowers.AttackVerb);
        Assert.Contains("Discard 1", attack.CostDescription);
        var enemy = world.TheCardIn(DeckType.VillainArea)!;
        game.Resolve(new Decision(attack.Id, [enemy.ObjectId]));
        Assert.True(ally!.Ready);
        Assert.Same(world.Seats[0].Hand, payment!.Area);
        Assert.Equal(0, enemy.Damage);
        var cost = Assert.Single(game.Pending!.Affordances);
        string digest = world.Digest().Canonical();
        Assert.Throws<ArgumentException>(() => game.Resolve(new Decision(cost.Id, [enemy.ObjectId])));
        Assert.Throws<ArgumentException>(() => game.Resolve(new Decision(cost.Id, [])));
        Assert.Equal(digest, world.Digest().Canonical());
        game.Resolve(new Decision(cost.Id, [payment.ObjectId]));
        Assert.False(ally.Ready);
        Assert.Equal(DeckType.DiscardPile, payment.Area.Type);
        Assert.Equal(stunned ? 0 : 3, enemy.Damage);
        Assert.Equal(stunned ? 0 : 1, ally.Damage);
        Assert.False(Statuses.Afflicted(world, world.Facts, ally, Statuses.Stunned));
    }

    [Fact]
    public void EmptyHandPreventsAttackButNotThwart()
    {
        var runner = CostRunner();
        Card? ally = null;
        var (game, world) = Playing(board =>
        {
            ally = board.CreateCard("03014", board.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
            board.TheCardIn(DeckType.MainSchemesArea)!.PlaceTokens("k_threat", 2);
        }, abilities: runner);
        foreach (var card in world.Seats[0].Hand.Cards.ToArray()) World.MoveToTop(card, world.Seats[0].Deck);
        game.Resolve(Decision.Take(Assert.Single(game.Pending!.Affordances, offer => offer.Verb == Game.ChangeForm).Id));
        Assert.Empty(world.Seats[0].Hand.Cards);
        Assert.DoesNotContain(game.Pending!.Affordances,
            offer => offer.AnchorId == ally!.ObjectId && offer.Verb == BasicPowers.AttackVerb);
        Assert.Contains(game.Pending.Affordances,
            offer => offer.AnchorId == ally!.ObjectId && offer.Verb == BasicPowers.ThwartVerb);
        string digest = world.Digest().Canonical();
        Assert.Throws<RulesNotImplementedException>(() => BasicPowers.AllyPower(world, world.Facts,
            ally!, world.TheCardIn(DeckType.VillainArea)!, BasicPowers.AttackVerb, []));
        Assert.Equal(digest, world.Digest().Canonical());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:attack-player-ability-type.1.2")]
    public void GrantedAttackWithoutExhaustionStillPaysItsAdditionalCost(bool stunned)
    {
        // rr:attack-player-ability-type.1.2 allows the character to attack "without exhausting";
        // that permission does not waive a separately printed additional cost.
        var runner = CostRunner();
        Card? ally = null;
        Card? payment = null;
        var (_, world) = Playing(board =>
        {
            ally = board.CreateCard("03014", board.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
            ally.Exhaust();
            payment = board.CreateCard("01088", board.Seats[0].Hand);
            if (stunned) Statuses.Give(board, ally, Statuses.Stunned);
        }, abilities: runner);
        var enemy = world.TheCardIn(DeckType.VillainArea)!;
        BasicPowers.BasicAttackWithoutExhausting(world, world.Facts, ally!, enemy, []);
        var prompt = Sequence.Work(world, world.Facts, runner, [])!;
        Assert.Contains("does not require exhaustion", prompt.Description);
        Assert.Same(world.Seats[0].Hand, payment!.Area);
        Sequence.Answer(world, world.Facts, runner, prompt,
            new Decision(Assert.Single(prompt.Affordances).Id, [payment.ObjectId]), []);
        Sequence.Finish(world, world.Facts, runner, []);
        Assert.Equal(DeckType.DiscardPile, payment.Area.Type);
        Assert.False(ally!.Ready);
        Assert.Equal(stunned ? 0 : 3, enemy.Damage);
        Assert.Equal(stunned ? 0 : 1, ally.Damage);
    }

    [Fact]
    [Rule("rr:stun-stunned.5.1")]
    public void TargetlessStunnedAttemptStillAsksForAndPaysAdditionalDiscard()
    {
        // rr:stun-stunned.5.1 permits a stunned attack without a valid target.
        var runner = CostRunner();
        Card? ally = null;
        var (game, world) = Playing(board =>
        {
            ally = board.CreateCard("03014", board.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
            Statuses.Give(board, ally, Statuses.Stunned);
        }, abilities: runner);
        foreach (var enemy in BasicPowers.Attackable(world, world.Facts, 0).ToArray())
            World.MoveToTop(enemy, world.AreaOf(DeckType.RemovedArea));
        game.Resolve(Decision.Take(Assert.Single(game.Pending!.Affordances, offer => offer.Verb == Game.ChangeForm).Id));
        var attack = Assert.Single(game.Pending!.Affordances,
            offer => offer.AnchorId == ally!.ObjectId && offer.Verb == BasicPowers.AttackVerb);
        Assert.Equal(0, attack.Targets!.Max);
        game.Resolve(Decision.Take(attack.Id));
        Assert.True(ally!.Ready);
        var pay = Assert.Single(game.Pending!.Affordances);
        var card = world.Seats[0].Hand.Cards[0];
        game.Resolve(new Decision(pay.Id, [card.ObjectId]));
        Assert.Equal(DeckType.DiscardPile, card.Area.Type);
        Assert.False(ally.Ready);
        Assert.Equal(0, ally.Damage);
        Assert.False(Statuses.Afflicted(world, world.Facts, ally, Statuses.Stunned));
    }
}
