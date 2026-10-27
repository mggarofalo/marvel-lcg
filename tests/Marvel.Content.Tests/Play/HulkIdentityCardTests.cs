using System.Text.Json.Nodes;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class HulkIdentityCardTests
{
    [Theory]
    [InlineData(0, true, 2)]
    [InlineData(0, true, 7)]
    [InlineData(0, true, 0)]
    [InlineData(0, false, 2)]
    [InlineData(1, true, 2)]
    [Rule("rr:forced.1")]
    public void EnragedDiscardsOnlyItsCurrentHandAtItsOwnTurnEnd(int owner, bool hero, int count)
    {
        // Forced abilities "must be resolved when their triggering conditions are met".
        var (game, world) = HulkFirstSliceFixture.PlayingHulk(board =>
        {
            HulkFirstSliceFixture.EmptyHand(board, owner);
            for (int i = 0; i < count; i++) board.CreateCard("01088", board.Seats[owner].Hand);
        }, hero, owner);
        HulkFirstSliceFixture.EmptyHand(world, owner);
        for (int i = 0; i < count; i++) world.CreateCard("01088", world.Seats[owner].Hand);
        var hand = world.Seats[owner].Hand.Cards.ToArray();
        Assert.Equal(count, hand.Length);
        if (owner == 1)
        {
            game.Resolve(Decision.Decline);
            Assert.Equal(count, world.Seats[owner].Hand.Cards.Count);
        }
        game.Resolve(Decision.Decline);
        HulkFirstSliceFixture.DiscardWholeHand(game, world, owner);
        Assert.Equal(hero ? 0 : count, world.Seats[owner].Hand.Cards.Count);
        Assert.All(hand, card => Assert.Equal(hero ? DeckType.DiscardPile : DeckType.HandsArea, card.Area.Type));
        Assert.Equal(owner == 0 ? GamePhase.PlayerTurn : GamePhase.EndPhase, game.Phase);
    }

    [Theory]
    [InlineData("Interrupt")]
    [InlineData("Response")]
    [Rule("rr:forced.4")]
    [Rule("rr:interrupt")]
    public void OptionalEndTurnResponseDrawsAfterEnragedAndTheNewCardRemains(string timing)
    {
        // "Forced interrupts take priority and initiate before non-forced interrupts";
        // interrupts resolve before their condition, responses after it.
        var runner = DoctorStrangeAbilityFixture.WithReplacements(JsonNode.Parse("""
            [{"card":"01007","abilities":[{
              "trigger":{"timing":"TIMING","event":"WhenPlayerTurnEnds","subject":"you"},
              "effect":{"draw":{"player":"you","count":1}}}]}]
            """.Replace("TIMING", timing, StringComparison.Ordinal))!.AsArray());
        var (game, world) = HulkFirstSliceFixture.PlayingHulk(board =>
            board.CreateCard("01007", board.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0)), runner: runner);
        var hand = world.Seats[0].Hand.Cards.ToArray();
        game.Resolve(Decision.Decline);
        Assert.Equal(0, game.Active);
        HulkFirstSliceFixture.DiscardWholeHand(game, world, 0);
        Assert.Empty(world.Seats[0].Hand.Cards);
        Assert.All(hand, card => Assert.Equal(DeckType.DiscardPile, card.Area.Type));
        game.Resolve(Decision.Take(game.Pending!.Affordances[0].Id));
        Assert.Single(world.Seats[0].Hand.Cards);
        Assert.Equal(1, game.Active);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    [Rule("rr:forced.5")]
    [Rule("rr:forced.6")]
    public void FirstPlayerOrdersSimultaneousForcedInterruptsBeforeSeatAdvances(bool hulkFirst)
    {
        // "The first player determines the order" and each "must resolve as completely as possible".
        var runner = DoctorStrangeAbilityFixture.WithReplacements(JsonNode.Parse("""
            [{"card":"01007","abilities":[{
              "trigger":{"timing":"ForcedInterrupt","event":"WhenPlayerTurnEnds","subject":"you"},
              "effect":{"draw":{"player":"you","count":1}}}]}]
            """)!.AsArray());
        var (game, world) = HulkFirstSliceFixture.PlayingHulk(board =>
            board.CreateCard("01007", board.AreaOf(DeckType.AlliesArea, PlayArea.Of(1), cardOwner: 1)), owner: 1, runner: runner);
        game.Resolve(Decision.Decline);
        game.Resolve(Decision.Decline);
        Assert.Equal(1, game.Active);
        Assert.Equal(0, game.Pending!.Player);
        var selected = game.Pending.Affordances.Single(offer =>
            (offer.AnchorId == world.Seats[1].IdentityCard.ObjectId) == hulkFirst);
        game.Resolve(Decision.Take(selected.Id));
        HulkFirstSliceFixture.DiscardWholeHand(game, world, 1);
        Assert.Equal(hulkFirst ? 1 : 0, world.Seats[1].Hand.Cards.Count);
        Assert.Equal(GamePhase.EndPhase, game.Phase);
    }

    [Fact]
    [Rule("rr:limit")]
    public void BannerDrawsBeforeSelectedDiscardAndCanBeUsedOnlyOncePerRound()
    {
        // "Each copy of an ability with such a limit may be used X times per the specified period".
        var runner = AuthoredCards.Runner();
        var (_, world) = HulkFirstSliceFixture.PlayingHulk(_ => { }, hero: false);
        var source = world.Seats[0].IdentityCard;
        int before = world.Seats[0].Hand.Cards.Count;
        var deckTop = world.Seats[0].Deck.Cards[^1];
        var action = Assert.Single(runner.Actions(world, 0), offer => offer.Card == source.ObjectId);
        runner.ExecuteAction(world, new PlayerAction(action, [], []));
        Assert.Equal(before + 1, world.Seats[0].Hand.Cards.Count);
        Assert.Contains(deckTop, world.Seats[0].Hand.Cards);
        var prompt = Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, runner, []));
        Assert.Equal(0, prompt.Player);
        Sequence.Answer(world, world.Facts, runner, prompt,
            new Decision(prompt.Affordances[0].Id, [deckTop.ObjectId], []), []);
        Sequence.Finish(world, world.Facts, runner, []);
        Assert.Equal(before, world.Seats[0].Hand.Cards.Count);
        Assert.Equal(DeckType.DiscardPile, deckTop.Area.Type);
        Assert.DoesNotContain(runner.Actions(world, 0), offer => offer.Card == source.ObjectId);
        source.TurnTo("10001a");
        source.TurnTo("10001b");
        Assert.DoesNotContain(runner.Actions(world, 0), offer => offer.Card == source.ObjectId);
    }
}
