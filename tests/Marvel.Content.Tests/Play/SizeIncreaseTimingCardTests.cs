using static Marvel.Content.Tests.Play.AntManNemesisCardFixture;
using Marvel.Cards.Run;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class SizeIncreaseTimingCardTests
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    [Rule("rr:forced.5")]
    public void AttachedSizeAndAbsorbingMansPrintedResponseShareTheOrderingChoice(
        bool scheme, bool sizeFirst)
    {
        // rr:forced.5: "the first player determines the order in which the abilities initiate".
        // Both actual printed responses follow the same attack/scheme activation.
        var runner = Runner();
        var (_, world) = Playing(_ => { }, hero: true, abilities: runner);
        World.MoveToTop(world.TheCardIn(DeckType.VillainArea)!, world.AreaOf(DeckType.RemovedArea));
        var villain = world.CreateCard("04078", world.AreaOf(DeckType.VillainArea));
        world.CreateCard("04081", world.AreaOf(DeckType.EnvironmentArea));
        var size = world.CreateCard(SizeIncrease, world.AreaOf(DeckType.RevealingArea));
        Reveal.Resolve(world, Facts, size, 0, []);
        Assert.Equal(villain.ObjectId, size.Area.Host);
        Statuses.Give(world, world.Seats[0].IdentityCard, "tough");
        // Advance is a legal encounter boost; keep the ordinary dealt deck underneath it.
        world.CreateCard("01186", world.AreaOf(DeckType.EncounterDeck));
        world.Agenda.Abandon();
        world.Agenda.Add(new PhaseStep(scheme ? new EnemySchemeOperation() : new EnemyAttackOperation(),
            1, 0, Subject: villain.ObjectId, Seat: 0));
        var events = new List<GameEvent>();
        var order = ReachResponseOrder(world, runner, events);
        Assert.Contains(order.Affordances, option => option.AnchorId == size.ObjectId);
        Assert.Contains(order.Affordances, option => option.AnchorId == villain.ObjectId);
        Assert.Equal(0, order.Player);
        int chosen = sizeFirst ? size.ObjectId : villain.ObjectId;
        var choice = Assert.Single(order.Affordances, option => option.AnchorId == chosen);
        int beforeResponse = events.Count;
        Sequence.Answer(world, Facts, runner, order, Decision.Take(choice.Id), events);
        events.AddRange(Finish(world, runner));
        AssertResponseOrder(world, size, events.Skip(beforeResponse).ToList(), sizeFirst);
    }

    private static Prompt ReachResponseOrder(World world, AbilityRunner runner, List<GameEvent> events)
    {
        for (int count = 0; count < 20; count++)
        {
            var prompt = Assert.IsType<Prompt>(Sequence.Work(world, Facts, runner, events));
            if (prompt.Asking == Question.Order) return prompt;
            Assert.True(prompt.Cancellable, prompt.Description);
            Sequence.Answer(world, Facts, runner, prompt, Decision.Decline, events);
        }
        throw new InvalidOperationException("Activation did not reach its forced-response ordering choice.");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:retaliate-x.1")]
    [Rule("rr:forced.5")]
    public void DefeatingTheHostWithRetaliateCanPrecedeItsAttachmentsResponse(bool sizeFirst)
    {
        // rr:retaliate-x.1: "Forced Response: After this character is attacked, deal X damage to the attacker."
        // rr:forced.5: "the first player determines the order in which the abilities initiate".
        // If retaliation defeats the host first, its discarded attachment cannot resolve later.
        var (_, world) = CaptainAmericaFixture.PlayingCaptain(board =>
            board.CreateCard("03009", board.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0)));
        var runner = Runner();
        world.Abilities = runner;
        var captain = world.Seats[0].IdentityCard;
        Assert.Equal(1, StateFields.Modified(world, captain, "retaliate", Facts, world.Players));
        var enemy = world.CreateCard(Yellowjacket, world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        enemy.TakeDamage(3);
        var size = world.CreateCard(SizeIncrease, world.AreaOf(DeckType.RevealingArea));
        Reveal.Resolve(world, Facts, size, 0, []);
        Assert.Equal(enemy.ObjectId, size.Area.Host);
        world.Agenda.Abandon();
        world.Agenda.Add(new PhaseStep(new EnemyAttackOperation(), 1, 0, Subject: enemy.ObjectId, Seat: 0));
        var events = new List<GameEvent>();
        var order = ReachResponseOrder(world, runner, events);
        Assert.Contains(order.Affordances, option => option.AnchorId == size.ObjectId);
        Assert.Contains(order.Affordances, option => option.AnchorId == captain.ObjectId);
        int chosen = sizeFirst ? size.ObjectId : captain.ObjectId;
        var choice = Assert.Single(order.Affordances, option => option.AnchorId == chosen);
        Sequence.Answer(world, Facts, runner, order, Decision.Take(choice.Id), events);
        events.AddRange(Finish(world, runner));
        Assert.Equal(sizeFirst, events.OfType<FieldSet>().Any(field =>
            field.Card == size.ObjectId && field.Field == "c_size" && field.To == 2));
        Assert.Equal(DeckType.EncounterDiscardPile, enemy.Area.Type);
        Assert.Equal(DeckType.EncounterDiscardPile, size.Area.Type);
        Assert.Null(world.Activation);
        Assert.Empty(world.Agenda.Outstanding);
        Assert.False(world.IsOver);
    }

    [Fact]
    [Rule("rr:activation.8.2")]
    public void NestedSchemeWaitsForTheOriginalAttacksSizeResponse()
    {
        // "All abilities triggered by the initial activation resolve before subsequent activations initiate."
        var runner = Runner(nested: true, nestedScheme: true);
        var (_, world) = Playing(_ => { }, hero: true, abilities: runner);
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        var first = world.CreateCard(SizeIncrease, world.AreaOf(DeckType.RevealingArea));
        Reveal.Resolve(world, Facts, first, 0, []);
        var enemy = world.CreateCard(Yellowjacket, world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        var second = world.CreateCard(SizeIncrease, world.AreaOf(DeckType.RevealingArea));
        Reveal.Resolve(world, Facts, second, 0, []);
        Assert.Equal(villain.ObjectId, first.Area.Host);
        Assert.Equal(enemy.ObjectId, second.Area.Host);
        Statuses.Give(world, world.Seats[0].IdentityCard, "tough");
        world.Agenda.Abandon();
        world.Agenda.Add(new PhaseStep(new EnemyAttackOperation(), 1, 0, Subject: villain.ObjectId, Seat: 0));
        var events = Finish(world, runner);
        Assert.Single(events.OfType<AttackCompleted>());
        int removal = events.FindIndex(item => item is FieldSet field
            && field.Card == first.ObjectId && field.Field == "c_size" && field.To == 2);
        int scheme = world.TheCardIn(DeckType.MainSchemesArea)!.ObjectId;
        int threat = events.FindIndex(item => item is FieldSet field
            && field.Card == scheme && field.Field == "k_threat");
        Assert.True(removal >= 0 && threat > removal);
        Assert.Equal(4, world.Cards[scheme].Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(2, second.Tokens.GetValueOrDefault("c_size"));
        Assert.Null(world.Activation);
        Assert.Empty(world.Agenda.Outstanding);
        Assert.False(world.IsOver);
    }

    private static void AssertResponseOrder(World world, Card size, List<GameEvent> events, bool sizeFirst)
    {
        int scheme = world.TheCardIn(DeckType.MainSchemesArea)!.ObjectId;
        int removal = events.FindIndex(item => item is FieldSet field
            && field.Card == size.ObjectId && field.Field == "c_size" && field.To == 2);
        int threat = events.FindIndex(item => item is FieldSet field
            && field.Card == scheme && field.Field == "k_threat");
        Assert.True(removal >= 0);
        Assert.True(threat >= 0);
        Assert.Equal(sizeFirst, removal < threat);
        Assert.Equal(2, size.Tokens.GetValueOrDefault("c_size"));
        Assert.Null(world.Activation);
        Assert.Empty(world.Agenda.Outstanding);
        Assert.False(world.IsOver);
    }
}
