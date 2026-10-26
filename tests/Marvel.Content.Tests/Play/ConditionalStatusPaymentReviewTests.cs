using System.Text.Json.Nodes;
using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>Independent review of test-local printed conditional status attacks.</summary>
public sealed class ConditionalStatusPaymentReviewTests
{
    [Theory]
    [InlineData("05015", "stunned")]
    [InlineData("05031", "confused")]
    [Rule("rr:target.3.4")]
    public void RejectedDeclarationLeavesTheSameOfferRetryable(string face, string status)
    {
        // "The target is valid if at least one of those effects can affect the target."
        var (game, world, runner, card, wild, mental) = Prepare(face, status);
        var action = Assert.Single(game.Pending!.Affordances, a => a.AnchorId == card.ObjectId);
        var before = world.Digest().Canonical();
        var pending = game.Pending;
        var random = world.Random.Generator.GetState();
        var agenda = world.Agenda.Outstanding.ToArray();
        Assert.Throws<RulesNotImplementedException>(() => game.Resolve(new Decision(action.Id, [],
            [wild.ObjectId, mental.ObjectId], Allocations:
            [new(wild.ObjectId, 0, "Y"), new(mental.ObjectId, 0, "BB")])));
        Assert.Equal(before, world.Digest().Canonical());
        Assert.Same(pending, game.Pending);
        Assert.Equal(agenda, world.Agenda.Outstanding.ToArray());
        Assert.Equal(random.Index, world.Random.Generator.GetState().Index);
        Assert.Equal(random.Words, world.Random.Generator.GetState().Words);
        Assert.Single(runner.Actions(world, 0), a => a.Card == card.ObjectId);
        game.Resolve(new Decision(action.Id, [], [wild.ObjectId, mental.ObjectId], Allocations:
            [new(wild.ObjectId, 0, "R"), new(mental.ObjectId, 0, "BB")]));
        var enemy = world.TheCardIn(DeckType.VillainArea)!;
        game.Resolve(Decision.Take(enemy.ObjectId));
        Assert.Equal(3, enemy.Damage);
        Assert.True(Statuses.Has(world, enemy, status));
        Assert.True(game.IsRootPrompt);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:resource.1")]
    public void DeclarationMustBelongToItsSelectedSource(bool forgePhysical)
    {
        // A player generates resources "by discarding cards from their hand".
        var (game, world, _, card, wild, mental) = Prepare("05015", "stunned");
        var action = Assert.Single(game.Pending!.Affordances, a => a.AnchorId == card.ObjectId);
        var before = world.Digest().Canonical();
        ResourceAllocation[] allocations = forgePhysical
            ? [new(wild.ObjectId, 0, "B"), new(mental.ObjectId, 0, "RB")]
            : [new(card.ObjectId, 0, "R"), new(mental.ObjectId, 0, "BB")];
        Assert.Throws<ArgumentException>(() => game.Resolve(new Decision(action.Id, [],
            [wild.ObjectId, mental.ObjectId], Allocations: allocations)));
        Assert.Equal(before, world.Digest().Canonical());
        Assert.Same(world.Seats[0].Hand, card.Area);
        Assert.Same(world.Seats[0].Hand, wild.Area);
        Assert.Same(world.Seats[0].Hand, mental.Area);
    }

    [Theory]
    [InlineData("05015", "stunned", "R", true)]
    [InlineData("05031", "confused", "R", true)]
    [InlineData("05015", "stunned", "Y", false)]
    [InlineData("05031", "confused", "Y", false)]
    [Rule("rr:stalwart.1")]
    [Rule("rr:target.3.4")]
    public void StalwartNeedsDamageFromTheDeclaredPayment(string face, string status, string declared, bool valid)
    {
        // Stalwart characters "cannot have confused or stunned status cards".
        var (game, world, _, card, wild, mental) = Prepare(face, status, stalwart: true);
        var action = Assert.Single(game.Pending!.Affordances, a => a.AnchorId == card.ObjectId);
        var answer = new Decision(action.Id, [], [wild.ObjectId, mental.ObjectId], Allocations:
            [new(wild.ObjectId, 0, declared), new(mental.ObjectId, 0, "BB")]);
        if (!valid)
        {
            var before = world.Digest().Canonical();
            Assert.Throws<RulesNotImplementedException>(() => game.Resolve(answer));
            Assert.Equal(before, world.Digest().Canonical());
            return;
        }
        game.Resolve(answer);
        var enemy = world.TheCardIn(DeckType.VillainArea)!;
        game.Resolve(Decision.Take(enemy.ObjectId));
        Assert.Equal(3, enemy.Damage);
        Assert.False(Statuses.Has(world, enemy, status));
        Assert.Equal(DeckType.DiscardPile, card.Area.Type);
    }

    [Fact]
    public void RepeatedOfferQueriesMatchAnUnprobedControlThroughExecution()
    {
        var probed = Prepare("05015", "stunned");
        var control = Prepare("05015", "stunned");
        var before = probed.World.Digest().Canonical();
        var random = probed.World.Random.Generator.GetState();
        var agenda = probed.World.Agenda.Outstanding.ToArray();
        for (int query = 0; query < 3; query++)
            Assert.Single(probed.Runner.Actions(probed.World, 0), a => a.Card == probed.Card.ObjectId);
        Assert.Equal(before, probed.World.Digest().Canonical());
        Assert.Equal(random.Index, probed.World.Random.Generator.GetState().Index);
        Assert.Equal(random.Words, probed.World.Random.Generator.GetState().Words);
        Assert.Equal(agenda, probed.World.Agenda.Outstanding.ToArray());
        foreach (var fixture in new[] { probed, control })
        {
            var action = Assert.Single(fixture.Game.Pending!.Affordances, a => a.AnchorId == fixture.Card.ObjectId);
            fixture.Game.Resolve(new Decision(action.Id, [], [fixture.Wild.ObjectId, fixture.Mental.ObjectId], Allocations:
                [new(fixture.Wild.ObjectId, 0, "R"), new(fixture.Mental.ObjectId, 0, "BB")]));
            fixture.Game.Resolve(Decision.Take(fixture.World.TheCardIn(DeckType.VillainArea)!.ObjectId));
        }
        Assert.Equal(control.World.Digest().Canonical(), probed.World.Digest().Canonical());
        Assert.Equal(control.World.Random.Generator.GetState().Index, probed.World.Random.Generator.GetState().Index);
        Assert.Equal(control.World.Random.Generator.GetState().Words, probed.World.Random.Generator.GetState().Words);
        Assert.Equal(control.Game.Pending!.Asking, probed.Game.Pending!.Asking);
    }

    private static (Game Game, World World, AbilityRunner Runner, Card Card, Card Wild, Card Mental)
        Prepare(string face, string status, bool stalwart = false)
    {
        var book = JsonNode.Parse(AuthoredCards.Text)!;
        book["cards"]!.AsArray().Add(JsonNode.Parse("""
            {"card":"__FACE__","abilities":[{"trigger":{"event":"WhenActionTriggered",
            "timing":"Action","subject":"this","form":"hero"},"effect":{
            "chooseCard":{"from":{"query":"attackableEnemies"},"effect":{"attack":{
            "target":"chosen","effect":{"seq":[{"giveStatus":{"card":"chosen","status":"__STATUS__"}},
            {"if":{"test":{"paidWithResource":"R"},"then":{"dealAttackDamage":{"cards":"chosen","amount":3}}}}]}}}}}}]}
            """.Replace("__FACE__", face, StringComparison.Ordinal).Replace("__STATUS__", status, StringComparison.Ordinal)));
        var runner = new AbilityRunner(AbilityCatalog.Parse(book.ToJsonString()));
        Card? card = null;
        Card? wild = null;
        Card? mental = null;
        var (game, world) = Playing(board =>
        {
            foreach (var held in board.Seats[0].Hand.Cards.ToArray())
                World.MoveToTop(held, board.AreaOf(DeckType.DiscardPile, PlayArea.Of(0), cardOwner: 0));
            card = board.CreateCard(face, board.Seats[0].Hand);
            wild = board.CreateCard("01011", board.Seats[0].Hand);
            mental = board.CreateCard("01089", board.Seats[0].Hand);
            var enemy = board.TheCardIn(DeckType.VillainArea)!;
            if (stalwart)
                board.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect,
                    Kind: "stalwart", Amount: 1, Card: enemy.ObjectId, Affects: enemy.ObjectId));
            else Statuses.Give(board, enemy, status);
        }, hero: true, abilities: runner);
        return (game, world, runner, card!, wild!, mental!);
    }
}
