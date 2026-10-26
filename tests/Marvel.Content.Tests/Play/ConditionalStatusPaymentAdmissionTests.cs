using System.Text.Json.Nodes;
using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>Test-local interpretations of Tackle and Concussive Blow; no product admission.</summary>
public sealed class ConditionalStatusPaymentAdmissionTests
{
    [Theory]
    [InlineData("05015", "stunned", false)]
    [InlineData("05015", "stunned", true)]
    [InlineData("05031", "confused", false)]
    [InlineData("05031", "confused", true)]
    [Rule("rr:target.3.4")]
    public void PhysicalPaymentCanAffectAnAlreadyStatusedEnemy(string face, string status, bool root)
    {
        // "The target is valid if at least one of those effects can affect the target."
        var fixture = Prepare(face, status, root, existing: true, wild: true);
        var (game, world, runner, card, paying) = fixture;
        var enemy = world.TheCardIn(DeckType.VillainArea)!;
        var action = Assert.Single(game.Pending!.Affordances, offer => offer.AnchorId == card.ObjectId);
        var before = world.Digest().Canonical();
        var random = world.Random.Generator.GetState();
        var pending = world.Agenda.Outstanding.ToArray();
        Assert.Single(runner.Actions(world, 0), offer => offer.Card == card.ObjectId);
        Assert.Single(runner.Actions(world, 0), offer => offer.Card == card.ObjectId);
        Assert.Equal(before, world.Digest().Canonical());
        Assert.Equal(random.Index, world.Random.Generator.GetState().Index);
        Assert.Equal(random.Words, world.Random.Generator.GetState().Words);
        Assert.Equal(pending, world.Agenda.Outstanding.ToArray());

        game.Resolve(new Decision(action.Id, [], [.. paying.Select(c => c.ObjectId)], Allocations:
            [new(paying[0].ObjectId, 0, "R"), new(paying[1].ObjectId, 0, "BB")]));
        game.Resolve(Decision.Take(enemy.ObjectId));
        Assert.True(Statuses.Has(world, enemy, status));
        Assert.Equal(3, enemy.Damage);
        Assert.Equal(DeckType.DiscardPile, card.Area.Type);
        Assert.All(paying, value => Assert.Equal(DeckType.DiscardPile, value.Area.Type));
        Assert.True(game.IsRootPrompt);
    }

    [Theory]
    [InlineData("05015", "stunned", false, false)]
    [InlineData("05015", "stunned", true, false)]
    [InlineData("05031", "confused", false, false)]
    [InlineData("05031", "confused", true, false)]
    [InlineData("05015", "stunned", false, true)]
    [InlineData("05015", "stunned", true, true)]
    [InlineData("05031", "confused", false, true)]
    [InlineData("05031", "confused", true, true)]
    [Rule("rr:target.2")]
    public void NonphysicalPaymentNeedsAFreshStatusTarget(string face, string status, bool root, bool existing)
    {
        // "Can only be initiated if it has at least one valid target."
        var (game, world, runner, card, paying) = Prepare(face, status, root, existing, wild: false);
        if (existing)
        {
            Assert.DoesNotContain(runner.Actions(world, 0), offer => offer.Card == card.ObjectId);
            return;
        }
        var action = Assert.Single(game.Pending!.Affordances, offer => offer.AnchorId == card.ObjectId);
        game.Resolve(new Decision(action.Id, [], [.. paying.Select(c => c.ObjectId)], Allocations:
            [new(paying[0].ObjectId, 0, "Y"), new(paying[1].ObjectId, 0, "BB")]));
        var enemy = world.TheCardIn(DeckType.VillainArea)!;
        game.Resolve(Decision.Take(enemy.ObjectId));
        Assert.True(Statuses.Has(world, enemy, status));
        Assert.Equal(0, enemy.Damage);
        Assert.True(game.IsRootPrompt);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ChoosingNonphysicalWildPaymentIsRefusedBeforeCosts(bool root)
    {
        var (_, world, runner, card, paying) = Prepare("05015", "stunned", root, existing: true, wild: true);
        var action = Assert.Single(runner.Actions(world, 0), offer => offer.Card == card.ObjectId);
        var before = world.Digest().Canonical();
        var random = world.Random.Generator.GetState();
        var pending = world.Agenda.Outstanding.ToArray();
        Assert.Throws<RulesNotImplementedException>(() => runner.ExecuteAction(world,
            new PlayerAction(action, [.. paying.Select(c => c.ObjectId)], [], Allocations:
                [new(paying[0].ObjectId, 0, "B"), new(paying[1].ObjectId, 0, "BB")])));
        Assert.Equal(before, world.Digest().Canonical());
        Assert.Equal(random.Index, world.Random.Generator.GetState().Index);
        Assert.Equal(random.Words, world.Random.Generator.GetState().Words);
        Assert.Equal(pending, world.Agenda.Outstanding.ToArray());
        Assert.Same(world.Seats[0].Hand, card.Area);
        Assert.All(paying, value => Assert.Same(world.Seats[0].Hand, value.Area));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AStatusAndDamageBodyStillRequiresThePrintedCost(bool existing)
    {
        var (_, world, runner, card, paying) = Prepare("05015", "stunned", false, existing, wild: true);
        World.MoveToTop(paying[1], world.AreaOf(DeckType.DiscardPile, PlayArea.Of(0), cardOwner: 0));
        Assert.DoesNotContain(runner.Actions(world, 0), offer => offer.Card == card.ObjectId);
    }
    private static (Game, World, AbilityRunner, Card, Card[]) Prepare(string face, string status,
        bool root, bool existing, bool wild)
    {
        var book = JsonNode.Parse(AuthoredCards.Text)!;
        var rows = book["cards"]!.AsArray();
        foreach (var row in rows.Where(row => row!["card"]!.GetValue<string>() == face).ToArray())
            rows.Remove(row);
        string give = $$$$$$"""{"giveStatus":{"card":"chosen","status":"{{{{{{status}}}}}}"}}""";
        const string damage = """{"dealAttackDamage":{"cards":"chosen","amount":3}}""";
        string Attack(string body) => $$$$$$"""{"chooseCard":{"from":{"query":"attackableEnemies"},"effect":{"attack":{"target":"chosen","effect":{{{{{{body}}}}}}}}}}""";
        string Conditional(string yes, string? no = null) =>
            $$$$$$"""{"if":{"test":{"paidWithResource":"R"},"then":{{{{{{yes}}}}}},"else":{{{{{{no ?? "{}"}}}}}}}}""";
        string effect = root ? Conditional(Attack($$$$$$"""{"seq":[{{{{{{give}}}}}},{{{{{{damage}}}}}}]}"""), Attack(give))
            : Attack($$$$$$"""{"seq":[{{{{{{give}}}}}},{{{{{{Conditional(damage)}}}}}}]}""");
        effect = effect.Replace(",\"else\":{}", string.Empty, StringComparison.Ordinal);
        book["cards"]!.AsArray().Add(JsonNode.Parse($$$$$$"""
            {"card":"{{{{{{face}}}}}}","abilities":[{"trigger":{"event":"WhenActionTriggered",
            "timing":"Action","subject":"this","form":"hero"},"effect":{{{{{{effect}}}}}}}]}
            """));
        var runner = new AbilityRunner(AbilityCatalog.Parse(book.ToJsonString()));
        Card? card = null;
        Card[] paying = [];
        var (game, world) = Playing(board =>
        {
            foreach (var held in board.Seats[0].Hand.Cards.ToArray())
                World.MoveToTop(held, board.AreaOf(DeckType.DiscardPile, PlayArea.Of(0), cardOwner: 0));
            card = board.CreateCard(face, board.Seats[0].Hand);
            paying = [board.CreateCard(wild ? "01011" : "01088", board.Seats[0].Hand),
                board.CreateCard("01089", board.Seats[0].Hand)];
            if (existing) Statuses.Give(board, board.TheCardIn(DeckType.VillainArea)!, status);
        }, hero: true, abilities: runner);
        foreach (var held in world.Seats[0].Hand.Cards.Where(value => value != card && !paying.Contains(value)).ToArray())
            World.MoveToTop(held, world.AreaOf(DeckType.DiscardPile, PlayArea.Of(0), cardOwner: 0));
        return (game, world, runner, card!, paying);
    }
}
