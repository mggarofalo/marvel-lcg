using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed partial class ActionAbilityTests
{
    private static readonly string[] FixedCountFaces = ["01002", "01003", "01004", "01005"];

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void TopCardChoiceKeepsItsCompiledCountAcrossSuspension(bool editBeforeAction)
    {
        var (runner, fields) = MutableEffectRunner("chooseTopForHand", """{"count":3}""", false);
        var (world, source) = FixedCountBoard(runner);
        var deck = world.Seats[0].Deck;
        var bottomToTop = FixedCountFaces
            .Select(face => world.CreateCard(face, deck)).ToArray();
        if (editBeforeAction) fields["count"] = new AbilityValue.Number(0);

        runner.Act(world, new PendingAbility(source.ObjectId, AbilityType.Action, 0), [], []);
        var choice = Assert.Single(world.Agenda.Outstanding);
        var prompt = runner.Choosing(world, source, 0, choice.Index, choice.Tier)!;

        Assert.True(prompt.ExposesConcealedCandidates);
        Assert.Equal(bottomToTop.Skip(1).Reverse().Select(card => card.ObjectId),
            prompt.Affordances.Select(option => option.Id));
        // Widening the original map cannot admit the unoffered fourth card.
        fields["count"] = new AbilityValue.Number(4);
        Assert.Throws<RulesNotImplementedException>(() => runner.Chose(
            world, source, 0, choice.Index, Decision.Take(bottomToTop[0].ObjectId), choice.Tier));
        Assert.Equal(bottomToTop, deck.Cards);
        Assert.Empty(world.Seats[0].Hand.Cards);
        // Narrowing it cannot invalidate an already offered third card either.
        fields["count"] = new AbilityValue.Number(1);
        runner.Chose(world, source, 0, choice.Index,
            Decision.Take(bottomToTop[1].ObjectId), choice.Tier);

        Assert.Equal([bottomToTop[0]], deck.Cards);
        Assert.Equal([bottomToTop[1]], world.Seats[0].Hand.Cards);
        Assert.Equal([bottomToTop[3], bottomToTop[2]],
            world.AreaOf(DeckType.DiscardPile, PlayArea.Of(0), cardOwner: 0).Cards);
        Assert.Equal(0, world.Random.Generator.WordsConsumed);
        Assert.False(source.Ready);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DiscardShuffleUsesTheCompiledMaximumAndOneDeterministicShuffle(bool editBeforeAction)
    {
        var (runner, fields) = MutableEffectRunner("chooseDiscardToShuffle", """{"max":4}""", false);
        var (world, source) = FixedCountBoard(runner);
        var discard = world.AreaOf(DeckType.DiscardPile, PlayArea.Of(0), cardOwner: 0);
        var cards = FixedCountFaces
            .Select(face => world.CreateCard(face, discard)).ToArray();
        if (editBeforeAction) fields["max"] = new AbilityValue.Number(1);

        runner.Act(world, new PendingAbility(source.ObjectId, AbilityType.Action, 0), [], []);
        var choice = Assert.Single(world.Agenda.Outstanding);
        var prompt = runner.Choosing(world, source, 0, choice.Index, choice.Tier)!;
        var selection = Assert.Single(prompt.Affordances);
        Assert.Equal(1, selection.Targets!.Min);
        Assert.Equal(4, selection.Targets.Max);
        Assert.Equal(cards.Select(card => card.ObjectId), selection.Targets.Legal);
        fields["max"] = new AbilityValue.Number(1);

        runner.Chose(world, source, 0, choice.Index,
            Decision.Take(selection.Id, cards.Select(card => card.ObjectId).ToArray(), []), choice.Tier);

        // Engine contract: max is data, not a hard-coded three-card limit.
        // MT19937 seed 5489 starts 3499211612, 581869302, 3890346734.
        // Masked bounds 4, 3, 2 give swap indexes 0, 2, 0, without rejection.
        Assert.Equal([cards[1], cards[3], cards[2], cards[0]], world.Seats[0].Deck.Cards);
        Assert.Equal(3, world.Random.Generator.WordsConsumed);
        Assert.Empty(discard.Cards);
        Assert.False(source.Ready);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DiscardShuffleRefusesExcessOrDuplicateTitlesBeforeMovingCards(bool duplicateTitle)
    {
        var (runner, fields) = MutableEffectRunner("chooseDiscardToShuffle", """{"max":2}""", false);
        var (world, source) = FixedCountBoard(runner);
        var discard = world.AreaOf(DeckType.DiscardPile, PlayArea.Of(0), cardOwner: 0);
        var cards = new[] { "01002", duplicateTitle ? "01002" : "01003", "01004" }
            .Select(face => world.CreateCard(face, discard)).ToArray();
        runner.Act(world, new PendingAbility(source.ObjectId, AbilityType.Action, 0), [], []);
        var choice = Assert.Single(world.Agenda.Outstanding);
        fields["max"] = new AbilityValue.Number(4);
        int[] targets = cards.Take(duplicateTitle ? 2 : 3).Select(card => card.ObjectId).ToArray();

        Assert.Throws<RulesNotImplementedException>(() => runner.Chose(world, source, 0, choice.Index,
            Decision.Take(source.ObjectId, targets, []), choice.Tier));

        Assert.Equal(cards, discard.Cards);
        Assert.Empty(world.Seats[0].Deck.Cards);
        Assert.Equal(0, world.Random.Generator.WordsConsumed);
    }

    [Theory]
    [InlineData("draw", false)]
    [InlineData("draw", true)]
    [InlineData("createDrones", false)]
    [InlineData("createDrones", true)]
    public void FixedCountEligibilityUsesTheCompiledPositiveCount(string operation, bool choice)
    {
        var (runner, fields) = MutableNumericRunner(operation, """{"player":"you","count":2}""", choice);
        fields["count"] = new AbilityValue.Number(0);
        Card? source = null;
        var (game, world) = Playing(board => source = InPlay(board, AuthoredCards.AuntMay), abilities: runner);
        var seat = world.Seats[0];
        int held = seat.Hand.Cards.Count;
        int remaining = seat.Deck.Cards.Count;
        var top = seat.Deck.Cards.TakeLast(2).Reverse().ToArray();
        long words = world.Random.Generator.WordsConsumed;

        var action = Assert.Single(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        game.Resolve(Decision.Take(action.Id));
        if (choice)
        {
            Assert.Equal(Question.Option, game.Pending!.Asking);
            Assert.Equal(2, game.Pending.Affordances.Count);
            game.Resolve(Decision.Take(Assert.Single(game.Pending.Affordances, option => option.Id == 0).Id));
        }

        Assert.Equal(remaining - 2, seat.Deck.Cards.Count);
        Assert.Equal(held + (operation == "draw" ? 2 : 0), seat.Hand.Cards.Count);
        Assert.All(top, card => Assert.Equal(operation == "draw" ? DeckType.HandsArea : DeckType.EngagedEnemiesArea,
            card.Area.Type));
        Assert.Equal(words, world.Random.Generator.WordsConsumed);
        Assert.False(source!.Ready);
        Assert.Equal(Question.TurnOption, game.Pending!.Asking);
        Assert.False(world.Agenda.IsBusy);
    }

    [Fact]
    public void ProjectedDrawConsumesCardsBeforeTestingALaterDependentEffect()
    {
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
            {"attack":{"target":{"query":"villain"},"effect":{"seq":[
              {"draw":{"player":"you","count":2}},
              {"otherwise":{"effect":{"draw":{"player":"you","count":1}},
                "otherwise":{"heal":{"card":{"query":"villain"},"amount":1}}}},
              {"moveDamage":{"from":{"query":"villain"},"to":"you","amount":1}},
              {"if":{"test":{"inForm":{"player":"firstPlayer","form":"hero"}},
                "then":{"dealAttackDamage":{"cards":{"query":"villain"},"amount":1}},
                "else":{"enemyAttacks":{"enemies":{"query":"villain"}}}}}
            ]}}}
            """, cost: """{"exhaust":"this"}""");
        var (world, source) = FixedCountBoard(runner, players: 2);
        world.CreateCard("01002", world.Seats[0].Deck);
        world.CreateCard("01003", world.Seats[0].Deck);
        world.Seats[0].IdentityCard.TakeDamage(9);
        world.TheCardIn(DeckType.VillainArea)!.TakeDamage(1);
        // The first draw consumes the only available cards. The second draw
        // does nothing, so otherwise heals the villain. No damage remains to
        // move to the first player: the later suspended branch is unreachable.
        Assert.Contains(runner.Actions(world, 0), action => action.Card == source.ObjectId);

        Assert.True(source.Ready);
        Assert.Equal(2, world.Seats[0].Deck.Cards.Count);
        Assert.Empty(world.Seats[0].Hand.Cards);
        Assert.Equal(0, world.Random.Generator.WordsConsumed);
        Assert.Equal(9, world.Seats[0].IdentityCard.Damage);
        Assert.Equal(1, world.TheCardIn(DeckType.VillainArea)!.Damage);
    }

    private static (World World, Card Source) FixedCountBoard(AbilityRunner runner, int players = 1)
    {
        var world = new World(Cards, players, seed: 5489) { Abilities = runner };
        var seat = world.CreateSeat("p0");
        seat.IdentityCard = world.CreateCard("01001a", seat.Hero);
        if (players == 2)
        {
            var second = world.CreateSeat("p1");
            second.IdentityCard = world.CreateCard("01010b", second.Hero);
        }
        world.CreateCard("01113", world.AreaOf(DeckType.VillainArea));
        var source = world.CreateCard(AuthoredCards.AuntMay,
            world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        return (world, source);
    }
}
