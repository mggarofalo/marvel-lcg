using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>Actual authored event rows in prepared nonpublic engine games.</summary>
public sealed class MsMarvelStatusCardTests
{
    [Theory]
    [InlineData("05015", "stunned", "R", false)]
    [InlineData("05015", "stunned", "B", false)]
    [InlineData("05015", "stunned", "R", true)]
    [InlineData("05031", "confused", "R", false)]
    [InlineData("05031", "confused", "B", false)]
    [InlineData("05031", "confused", "R", true)]
    [Rule("rr:wild-resource.1")]
    public void StatusAttackUsesDeclaredPaymentEvenWhenAlreadyStatused(string face, string status, string wildAs, bool existing)
    {
        // A player may "specify which resource type" a wild is used as.
        // Printed Tackle/Concussive Blow: status, then conditional three damage.
        Card? played = null;
        Card[] paying = [];
        var (game, world) = Playing(board =>
        {
            played = board.CreateCard(face, board.Seats[0].Hand);
            paying = [board.CreateCard("01011", board.Seats[0].Hand),
                board.CreateCard("01089", board.Seats[0].Hand)];
            if (existing) Statuses.Give(board, board.TheCardIn(DeckType.VillainArea)!, status);
        }, hero: true, abilities: AuthoredCards.Runner());
        var enemy = world.TheCardIn(DeckType.VillainArea)!;
        var action = Assert.Single(game.Pending!.Affordances, a => a.AnchorId == played!.ObjectId);
        game.Resolve(new Decision(action.Id, [], [.. paying.Select(c => c.ObjectId)], Allocations:
            [new(paying[0].ObjectId, 0, wildAs), new(paying[1].ObjectId, 0, "BB")]));
        game.Resolve(Decision.Take(enemy.ObjectId));
        Assert.True(Statuses.Has(world, enemy, status));
        Assert.Equal(wildAs == "R" ? 3 : 0, enemy.Damage);
        Assert.Equal(DeckType.DiscardPile, played!.Area.Type);
        Assert.All(paying, c => Assert.Equal(DeckType.DiscardPile, c.Area.Type));
        Assert.True(game.IsRootPrompt);
    }


    [Theory]
    [InlineData("05015", "stunned", "R")]
    [InlineData("05015", "stunned", "B")]
    [InlineData("05031", "confused", "R")]
    [InlineData("05031", "confused", "B")]
    [Rule("rr:target.3.4")]
    public void EventBoundSuitWildCanPayForDamageAgainstStalwart(string face, string status, string wildAs)
    {
        // "The target is valid if at least one of those effects can affect the target."
        // Prepared nonpublic state: Kree Lieutenant supplies printed Guard/Stalwart only.
        Card played = null!;
        Card suit = null!;
        Card mental = null!;
        Card enemy = null!;
        var runner = AuthoredCards.Runner();
        var (game, world) = Playing(board =>
        {
            foreach (var card in board.Seats[0].Hand.Cards.ToArray())
                World.MoveToTop(card, board.AreaOf(DeckType.DiscardPile, PlayArea.Of(0), cardOwner: 0));
            played = board.CreateCard(face, board.Seats[0].Hand);
            mental = board.CreateCard("01089", board.Seats[0].Hand);
            suit = InPlay(board, "05009");
            enemy = board.CreateCard("16133", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        }, hero: true, abilities: runner);
        var action = Assert.Single(runner.Actions(world, 0), a => a.Card == played.ObjectId);
        var before = world.Digest().Canonical();
        var random = world.Random.Generator.GetState();
        var pending = world.Agenda.Outstanding.ToArray();
        var input = new PlayerAction(action, [suit.ObjectId, mental.ObjectId], [enemy.ObjectId], Allocations:
            [new(suit.ObjectId, 0, wildAs), new(mental.ObjectId, 0, "BB")]);
        if (wildAs == "B")
        {
            Assert.Throws<RulesNotImplementedException>(() => runner.ExecuteAction(world, input));
            Assert.Equal(before, world.Digest().Canonical());
            Assert.Equal(random.Index, world.Random.Generator.GetState().Index);
            Assert.Equal(random.Words, world.Random.Generator.GetState().Words);
            Assert.Equal(pending, world.Agenda.Outstanding.ToArray());
            Assert.True(suit.Ready);
        }
        else
        {
            var offered = Assert.Single(game.Pending!.Affordances, a => a.AnchorId == played.ObjectId);
            game.Resolve(new Decision(offered.Id, [], [suit.ObjectId, mental.ObjectId], Allocations:
                [new(suit.ObjectId, 0, wildAs), new(mental.ObjectId, 0, "BB")]));
            game.Resolve(Decision.Take(enemy.ObjectId));
            Assert.False(suit.Ready);
            Assert.Equal(3, enemy.Damage);
            Assert.False(Statuses.Has(world, enemy, status));
            Assert.Equal(DeckType.DiscardPile, played.Area.Type);
            Assert.Equal(DeckType.DiscardPile, mental.Area.Type);
        }
    }

    [Theory]
    [InlineData("05015", "stunned", false)]
    [InlineData("05015", "stunned", true)]
    [InlineData("05031", "confused", false)]
    [InlineData("05031", "confused", true)]
    [Rule("rr:target.2")]
    public void MentalOnlyPaymentRequiresAnEnemyThatCanReceiveStatus(string face, string status, bool existing)
    {
        // "Can only be initiated if it has at least one valid target."
        Card played = null!;
        Card[] mental = [];
        var runner = AuthoredCards.Runner();
        var (_, world) = Playing(board =>
        {
            foreach (var card in board.Seats[0].Hand.Cards.ToArray())
                World.MoveToTop(card, board.AreaOf(DeckType.DiscardPile, PlayArea.Of(0), cardOwner: 0));
            played = board.CreateCard(face, board.Seats[0].Hand);
            mental = [board.CreateCard("01089", board.Seats[0].Hand),
                board.CreateCard("01089", board.Seats[0].Hand)];
            if (existing) Statuses.Give(board, board.TheCardIn(DeckType.VillainArea)!, status);
        }, hero: true, abilities: runner);
        foreach (var card in world.Seats[0].Hand.Cards.Where(c => c != played && !mental.Contains(c)).ToArray())
            World.MoveToTop(card, world.AreaOf(DeckType.DiscardPile, PlayArea.Of(0), cardOwner: 0));
        Assert.Equal(3, world.Seats[0].Hand.Cards.Count);
        var actions = runner.Actions(world, 0).Where(a => a.Card == played.ObjectId).ToArray();
        Assert.Equal(existing ? 0 : 1, actions.Length);
        Assert.Same(world.Seats[0].Hand, played.Area);
        Assert.Equal(0, world.TheCardIn(DeckType.VillainArea)!.Damage);
    }
}


