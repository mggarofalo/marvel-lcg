using System.Text.Json;
using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

// Synthetic Melee body isolates staged selection; no expansion admission is claimed.
public sealed class StagedAttackSafetyTests
{
    internal const string Body = """
        {"chooseCard":{"from":{"query":"attackableEnemies"},"effect":{
          "attack":{"target":"chosen","effect":{"seq":[
            {"dealAttackDamage":{"cards":"chosen","amount":3}},
            {"attackAnotherEnemy":{"amount":3}}
          ]}}}}}
        """;

    internal static AbilityRunner Runner(string body = Body) => new(AbilityCatalog.Parse("""
        {"cards":[{"card":"05030","abilities":[{
          "trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"this","form":"hero"},
          "effect":BODY}]},{"card":"02002b","abilities":[]},{"card":"07003","abilities":[]}]}
        """.Replace("BODY", body, StringComparison.Ordinal)));

    [Rule("rr:leaves-play.1")]
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void InvalidAnswersAndQueriesPreserveExecutionAndSubsequentTrace(bool stale)
    {
        // "there is no memory of its previous state ... a new copy of the card".
        var control = Trace(false, stale);
        var probed = Trace(true, stale);
        Assert.Equal(control, probed);
    }

    private static string[] Trace(bool probe, bool stale)
    {
        var runner = Runner();
        Card? source = null;
        Card? first = null;
        Card? second = null;
        var (game, world) = Playing(board =>
        {
            source = Payable(board);
            var area = board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0));
            first = board.CreateCard("01130", area);
            second = board.CreateCard("01130", area);
        }, hero: true, abilities: runner);
        Pay(game, world, source!);
        var events = game.Resolve(Decision.Take(first!.ObjectId)).Events.ToList();
        Assert.Equal("Choose another enemy", game.Pending!.DisplayQuestion);
        if (stale)
        {
            var area = second!.Area;
            World.MoveToTop(second, world.AreaOf(DeckType.EncounterDiscardPile));
            World.MoveToTop(second, area);
        }
        if (probe)
        {
            var pending = game.Pending;
            string digest = world.Digest().Canonical();
            var outstanding = world.Agenda.Outstanding.ToArray();
            var current = world.Agenda.Current;
            var occurrence = world.Agenda.Occurrence;
            var random = world.Random.Generator.GetState();
            var consumed = world.Random.Generator.WordsConsumed;
            for (int index = 0; index < 3; index++)
            {
                _ = runner.Actions(world, 0);
                Assert.Throws<ArgumentException>(() => game.Resolve(Decision.Take(first.ObjectId)));
                Assert.Throws<ArgumentException>(() => game.Resolve(Decision.Take(int.MaxValue)));
                Assert.Throws<ArgumentException>(() => game.Resolve(new Decision(second!.ObjectId, [first.ObjectId], [])));
                if (stale) Assert.Throws<ArgumentException>(() => game.Resolve(Decision.Take(second!.ObjectId)));
                Assert.Equal(digest, world.Digest().Canonical());
                Assert.Same(pending, game.Pending);
                Assert.Equal(outstanding, world.Agenda.Outstanding);
                Assert.Equal(current, world.Agenda.Current);
                Assert.Same(occurrence, world.Agenda.Occurrence);
                Assert.Equal(random.Words, world.Random.Generator.GetState().Words);
                Assert.Equal(random.Index, world.Random.Generator.GetState().Index);
                Assert.Equal(consumed, world.Random.Generator.WordsConsumed);
            }
        }
        var target = stale ? world.TheCardIn(DeckType.VillainArea)! : second!;
        events.AddRange(game.Resolve(Decision.Take(target.ObjectId)).Events);
        Assert.Equal(3, target.Damage);
        Assert.Equal(3, first.Damage);
        Assert.Equal(stale ? 0 : 3, second!.Damage);
        Assert.Equal(DeckType.DiscardPile, source!.Area.Type);
        Assert.False(world.Agenda.IsBusy);
        return [world.Digest().Canonical(), .. events.Select(item => JsonSerializer.Serialize(item, item.GetType())),
            .. world.Random.Generator.GetState().Words.Select(word => word.ToString()),
            world.Random.Generator.GetState().Index.ToString(), world.Random.Generator.WordsConsumed.ToString()];
    }

    [Rule("rr:leaves-play.1")]
    [Fact]
    public void FirstMinionReturningBeforeSecondSentenceIsAnotherCopy()
    {
        // "there is no memory of its previous state ... a new copy of the card".
        // Synthetic interrupt returns the first recipient before damage placement.
        var book = System.Text.Json.Nodes.JsonNode.Parse("""
            {"cards":[{"card":"01092","abilities":[{
              "trigger":{"event":"WhenDamageWouldBeDealt","timing":"ForcedInterrupt","target":"enemy"},
              "effect":{"seq":[
                {"discard":{"titled":"Whirlwind"}},
                {"putIntoPlay":{"card":{"cardsIn":{"areas":["encounterDiscardPile"],"title":"Whirlwind"}},"where":"engagedWithYou"}}
              ]}
            }]}]}
            """);
        var row = System.Text.Json.Nodes.JsonNode.Parse("""
            {"card":"05030","abilities":[{
              "trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"this","form":"hero"},
              "effect":BODY}]}
            """.Replace("BODY", Body, StringComparison.Ordinal));
        book!["cards"]!.AsArray().Add(row);
        Card? first = null;
        Card? source = null;
        var (game, world) = Playing(board =>
        {
            source = Payable(board);
            first = board.CreateCard("01130", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            InPlay(board, "01092");
        }, hero: true, abilities: new AbilityRunner(AbilityCatalog.Parse(book.ToJsonString())));
        int incarnation = first!.Incarnation;
        Pay(game, world, source!);
        game.Resolve(Decision.Take(first.ObjectId));
        Assert.Equal("Choose another enemy", game.Pending!.DisplayQuestion);
        Assert.True(first.Incarnation > incarnation);
        Assert.Equal(0, first.Damage);
        Assert.Contains(game.Pending.Affordances, option => option.AnchorId == first.ObjectId);
        game.Resolve(Decision.Take(first.ObjectId));
        Assert.Equal(DeckType.DiscardPile, source!.Area.Type);
        Assert.False(world.Agenda.IsBusy);
    }

    [Rule("rr:villain-defeat.3.1")]
    [Fact]
    public void RiskyBusinessPromotesOnCurrentSideAndExcludesConfiguredSuccessor()
    {
        // "The new stage ... is considered the same character"; pack:mc02
        // promotes Risky Business on the defeated stage's current side.
        Card? first = null;
        Card? next = null;
        Card? minion = null;
        Card? source = null;
        var (game, world) = Playing(board =>
        {
            World.MoveToTop(board.TheCardIn(DeckType.VillainArea)!, board.AreaOf(DeckType.RemovedArea));
            first = board.CreateCard("02001a,02001b", board.AreaOf(DeckType.VillainArea));
            first.TurnTo("02001b");
            next = board.CreateCard("02002a,02002b", board.AreaOf(DeckType.VillainDeck));
            first.TakeDamage(board.Facts.PrintedValue(first.FaceId, "HP", 1) - 3);
            source = Payable(board);
            minion = board.CreateCard("01130", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        }, hero: true, abilities: Runner());
        Assert.NotEqual(world.Facts.Title(first!.FaceId), world.Facts.Title(next!.FaceId));
        Pay(game, world, source!);
        game.Resolve(Decision.Take(first.ObjectId));
        Assert.Same(next, world.TheCardIn(DeckType.VillainArea));
        Assert.Equal("02002b", next.FaceId);
        Assert.Equal(minion!.ObjectId, Assert.Single(game.Pending!.Affordances).AnchorId);
        game.Resolve(Decision.Take(minion.ObjectId));
        Assert.Equal(0, next.Damage);
        Assert.Equal(3, minion.Damage);
    }

    [Rule("rr:villain-defeat.3.1")]
    [Fact]
    public void ConfiguredGroupSuccessorIsExcludedWhileAnotherVillainRemainsEligible()
    {
        // "The new stage ... is considered the same character". Group membership
        // distinguishes characters even when another group's title is the same.
        Card? first = null;
        Card? next = null;
        Card? other = null;
        Card? source = null;
        var (game, world) = Playing(board =>
        {
            World.MoveToTop(board.TheCardIn(DeckType.VillainArea)!, board.AreaOf(DeckType.RemovedArea));
            first = board.CreateCard("07002", board.AreaOf(DeckType.VillainArea));
            next = board.CreateCard("07003", board.AreaOf(DeckType.VillainDeck));
            other = board.CreateCard("07002", board.AreaOf(DeckType.VillainArea));
            var groups = new[]
            {
                Group(board, first, "07004", [first, next]),
                Group(board, other, "07019", [other]),
            };
            board.BindEncounterTopology(new EncounterTopology(board, groups, other));
            first.TakeDamage(board.Facts.PrintedValue(first.FaceId, "HP", 1) - 3);
            source = Payable(board);
        }, hero: true, abilities: Runner());
        Pay(game, world, source!);
        game.Resolve(Decision.Take(first!.ObjectId));
        Assert.Equal(DeckType.VillainArea, next!.Area.Type);
        Assert.Same(other, world.EncounterTopology!.Active.CurrentVillain);
        Assert.Equal(other!.ObjectId, Assert.Single(game.Pending!.Affordances).AnchorId);
        game.Resolve(Decision.Take(other.ObjectId));
        Assert.Equal(0, next.Damage);
        Assert.Equal(3, other.Damage);
        Assert.Equal(DeckType.DiscardPile, source!.Area.Type);
    }

    private static EncounterDeckGroup Group(World world, Card villain, string scheme, Card[] stages) =>
        new(villain, world.CreateCard(scheme, world.AreaOf(DeckType.SideSchemesArea)),
            world.CreateArea(DeckType.EncounterDeck), world.CreateArea(DeckType.EncounterDiscardPile), [], stages);

    [Theory]
    [InlineData("{\"attackAnotherEnemy\":{\"amount\":3}}")]
    [InlineData("{\"seq\":[{\"ready\":\"you\"},{\"attackAnotherEnemy\":{\"amount\":3}}]}")]
    [InlineData("{\"attack\":{\"target\":\"chosen\",\"effect\":{\"attackAnotherEnemy\":{\"amount\":3}}}}")]
    [InlineData("{\"thwart\":{\"target\":\"chosen\",\"effect\":{\"seq\":[{\"dealAttackDamage\":{\"cards\":\"chosen\",\"amount\":3}},{\"attackAnotherEnemy\":{\"amount\":3}}]}}}")]
    public void MisplacedInstructionIsRejectedBeforeGameplay(string body)
    {
        Assert.Contains("attackAnotherEnemy", Assert.Throws<AbilityException>(() => Runner(body)).Message, StringComparison.Ordinal);
    }

    [Fact]
    public void StagedAttackRequiresTheChosenFirstEnemyAsItsPowerTarget()
    {
        string body = Body.Replace("\"target\":\"chosen\"", "\"target\":{\"query\":\"villain\"}", StringComparison.Ordinal);
        Assert.Contains("attackAnotherEnemy", Assert.Throws<AbilityException>(() => Runner(body)).Message,
            StringComparison.Ordinal);
    }

    internal static Card Payable(World world)
    {
        var source = world.CreateCard("05030", world.Seats[0].Hand);
        world.CreateCard("01088", world.Seats[0].Hand);
        world.CreateCard("01089", world.Seats[0].Hand);
        return source;
    }

    internal static void Pay(Game game, World world, Card source)
    {
        int[] paying = [.. world.Seats[0].Hand.Cards.Where(card => card.FaceId is "01088" or "01089").Select(card => card.ObjectId)];
        game.Resolve(PaidDecision.Take(Assert.Single(game.Pending!.Affordances, item => item.AnchorId == source.ObjectId), paying));
    }
}
