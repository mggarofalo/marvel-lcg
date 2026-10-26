using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>Actual Target Acquired row, with isolated synthetic boost effects.</summary>
public sealed class TargetAcquiredBoostAbilityTests : CoreCardTestBase
{
    private static AbilityRunner Runner() => ActualPreparations("""
        {"cards":[
          {"card":"08024","abilities":[{
            "trigger":{"event":"WhenBoostCardTurnedFaceup","timing":"Response","subject":"game","form":"hero"},
            "cost":{"discard":"this"},"effect":{"cancelBoostAbility":"trigger"}
          }]},
          {"card":"02030","abilities":[{
            "trigger":{"event":"WhenCardRevealed","timing":"Boost","subject":"this"},
            "effect":{"seq":[{"giveStatus":{"card":"you","status":"stunned"}},{"dealDamage":{"cards":"you","amount":1}}]}
          }]},
          {"card":"08009","abilities":[{
            "trigger":{"event":"WhenPreparationAbilityResolved","timing":"Response","subject":"you"},
            "cost":{"exhaust":"this"},"effect":{"ready":"you"}
          }]}
        ]}
        """);

    [Rule("rr:response")]
    [Rule("rr:boost-boost-icon.1")]
    [Theory]
    [InlineData(true, 0)]
    [InlineData(false, 1)]
    public void PrintedResponseCancelsOnlyTheExactStarBeforeItResolves(bool attacking, int owner)
    {
        Assert.Equal(Exercise(attacking, owner, true), Exercise(attacking, owner, false));
    }

    private static string Exercise(bool attacking, int owner, bool probe)
    {
        // Printed 08024: "After a boost card is turned faceup, discard Target
        // Acquired → cancel that card's boost ability." Ruling a66ca21bca34a71f:
        // resolve it "before the boost ability resolves". "A star icon is not
        // itself considered a boost icon" (rr:boost-boost-icon.1).
        var runner = Runner();
        var world = Hero("08001a", 2);
        world.Abilities = runner;
        foreach (var seat in world.Seats) world.CreateCard("01089", seat.Deck);
        var villain = world.CreateCard("01094", world.AreaOf(DeckType.VillainArea));
        var preparation = world.CreateCard("08024", world.AreaOf(DeckType.UpgradesArea,
            PlayArea.Of(owner), cardOwner: owner));
        int incarnation = preparation.Incarnation;
        world.CreateCard("08009", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(owner), cardOwner: owner));
        world.Seats[owner].IdentityCard.Exhaust();
        var deck = world.AreaOf(DeckType.EncounterDeck);
        for (int i = 0; i < 8; i++) world.CreateCard("01104", deck);
        var second = world.CreateCard("02030", deck);
        var first = world.CreateCard("02030", world.AreaOf(DeckType.BoostCardsDeck,
            villain.Area.PlayArea, host: villain.ObjectId));
        var events = new List<GameEvent>();
        world.Agenda.Add(new PhaseStep(attacking ? new EnemyAttackOperation() : new EnemySchemeOperation(),
            1, 2, Subject: villain.ObjectId, Seat: 1));
        var prompt = UntilOffer(world, runner, preparation.ObjectId, events);
        Assert.Equal(Stage.Responses, world.Agenda.Stage);
        Assert.Equal(owner, prompt.Player);
        Assert.False(Statuses.Has(world, world.Seats[1].IdentityCard, Statuses.Stunned));
        var occurrence = world.Agenda.Occurrence!;
        Assert.Equal(first.ObjectId, occurrence.Subject);
        var boostReceipt = occurrence.BoostAbility!;
        var digest = world.Digest().Canonical();
        var pending = world.Agenda.Outstanding.ToArray();
        long rng = world.Random.Generator.WordsConsumed;
        int count = events.Count;
        if (probe)
        {
            Assert.NotEmpty(runner.Waiting(world, occurrence, WindowKind.Response));
            Assert.NotEmpty(runner.Waiting(world, occurrence, WindowKind.Response));
            Assert.Throws<RulesNotImplementedException>(() => Sequence.Answer(world, Cards, runner, prompt,
                Decision.Take(int.MaxValue), events));
            Assert.Throws<InvalidOperationException>(() => boostReceipt.Cancel(world));
            Assert.Equal(digest, world.Digest().Canonical());
            Assert.Equal(pending, world.Agenda.Outstanding);
            Assert.Equal(rng, world.Random.Generator.WordsConsumed);
            Assert.Equal(count, events.Count);
            Assert.True(boostReceipt.CanCancel(world));
        }
        Sequence.Answer(world, Cards, runner, prompt,
            Decision.Take(Assert.Single(prompt.Affordances, a => a.AnchorId == preparation.ObjectId).Id), events);
        Assert.Equal(DeckType.DiscardPile, preparation.Area.Type);
        var response = Sequence.Work(world, Cards, runner, events)!;
        var receipt = world.Agenda.Occurrence!.ActorFacts!.PreparationAbility!;
        Assert.Equal("08024", receipt.Face);
        Assert.Equal(incarnation, receipt.Incarnation);
        Assert.Equal(owner, receipt.Controller);
        Assert.False(Statuses.Has(world, world.Seats[1].IdentityCard, Statuses.Stunned));
        Sequence.Answer(world, Cards, runner, response, Decision.Decline, events);
        while (Sequence.Work(world, Cards, runner, events) is { } next)
            Sequence.Answer(world, Cards, runner, next, Decision.Decline, events);
        Assert.True(Statuses.Has(world, world.Seats[1].IdentityCard, Statuses.Stunned));
        Assert.False(Statuses.Has(world, world.Seats[0].IdentityCard, Statuses.Stunned));
        Assert.Equal(attacking ? 5 : 1, world.Seats[1].IdentityCard.Damage);
        Assert.Equal(0, world.Seats[0].IdentityCard.Damage);
        Assert.Equal(new[] { first.ObjectId, second.ObjectId }, events.OfType<BoostResolved>().Select(e => e.Card));
        Assert.Equal(new long[] { 1, 1 }, events.OfType<BoostResolved>().Select(e => e.Icons));
        Assert.False(world.Agenda.IsBusy);
        Assert.False(boostReceipt.CanCancel(world));
        Assert.Throws<InvalidOperationException>(() => boostReceipt.Cancel(world));
        Assert.Throws<ArgumentException>(() => Sequence.Answer(world, Cards, runner, prompt,
            Decision.Take(prompt.Affordances[0].Id), events));
        return world.Digest().Canonical() + ":" + world.Random.Generator.WordsConsumed
            + ":" + System.Text.Json.JsonSerializer.Serialize(events);

    }

    [Theory]
    [InlineData("01102", "08001a", false)]
    [InlineData("02030", "08001b", false)]
    [InlineData("02030", "08001a", true)]
    [Rule("rr:response.4")]
    public void NoStarAndAlterEgoHaveNoOfferAndDecliningLeavesTheStar(string boost, string identity, bool offered)
    {
        // "Once all players decide they do not wish to resolve any (further)
        // responses", that instance cannot be used. Hero Response requires hero form.
        var runner = Runner();
        var world = Hero(identity);
        world.Abilities = runner;
        world.CreateCard("01089", world.Seats[0].Deck);
        var villain = world.CreateCard("01094", world.AreaOf(DeckType.VillainArea));
        var preparation = world.CreateCard("08024", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
        var deck = world.AreaOf(DeckType.EncounterDeck);
        for (int i = 0; i < 8; i++) world.CreateCard("01104", deck);
        world.CreateCard(boost, deck);
        var events = new List<GameEvent>();
        world.Agenda.Add(new PhaseStep(new EnemySchemeOperation(), 1, 2, Subject: villain.ObjectId, Seat: 0));
        bool seen = false;
        while (Sequence.Work(world, Cards, runner, events) is { } prompt)
        {
            seen |= prompt.Affordances.Any(a => a.AnchorId == preparation.ObjectId);
            Sequence.Answer(world, Cards, runner, prompt, Decision.Decline, events);
        }
        Assert.Equal(offered, seen);
        Assert.Equal(DeckType.UpgradesArea, preparation.Area.Type);
        Assert.Equal(boost == "02030", Statuses.Has(world, world.Seats[0].IdentityCard, Statuses.Stunned));
        Assert.DoesNotContain(world.Agenda.Outstanding, step => step.Operation is PreparationAbilityResolvedOperation);
        Assert.Equal(boost == "02030" ? 1 : 2, Assert.Single(events.OfType<BoostResolved>()).Icons);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ExactBoostReceiptRejectsReplacementActivationOrIncarnation(bool incarnation)
    {
        var runner = Runner();
        var world = Hero("08001a");
        world.Abilities = runner;
        var villain = world.CreateCard("01094", world.AreaOf(DeckType.VillainArea));
        var preparation = world.CreateCard("08024", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
        var deck = world.AreaOf(DeckType.EncounterDeck);
        for (int i = 0; i < 8; i++) world.CreateCard("01104", deck);
        var boost = world.CreateCard("02030", deck);
        var events = new List<GameEvent>();
        world.Agenda.Add(new PhaseStep(new EnemySchemeOperation(), 1, 2, Subject: villain.ObjectId, Seat: 0));
        _ = UntilOffer(world, runner, preparation.ObjectId, events);
        var occurrence = world.Agenda.Occurrence!;
        var receipt = occurrence.BoostAbility!;
        Assert.True(receipt.CanCancel(world));
        if (incarnation)
        {
            World.MoveToTop(boost, world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
            World.MoveToTop(boost, world.AreaOf(DeckType.EncounterDiscardPile));
            World.MoveToTop(boost, world.AreaOf(DeckType.BoostingArea));
        }
        else world.Activation = world.Activation! with { Id = world.Activation.Id + 1 };
        Assert.False(receipt.CanCancel(world));
        Assert.Throws<InvalidOperationException>(() => receipt.Cancel(world));
        Assert.Empty(runner.Waiting(world, occurrence, WindowKind.Response));
        Assert.Equal(DeckType.UpgradesArea, preparation.Area.Type);
    }

    private static Prompt UntilOffer(World world, AbilityRunner runner, int source, List<GameEvent> events)
    {
        for (int i = 0; i < 20; i++)
        {
            var prompt = Sequence.Work(world, Cards, runner, events);
            Assert.NotNull(prompt);
            if (prompt.Affordances.Any(a => a.AnchorId == source)) return prompt;
            Sequence.Answer(world, Cards, runner, prompt, Decision.Decline, events);
        }
        throw new InvalidOperationException("Target Acquired was not offered");
    }
    // The local Suit readies its controller to isolate cancellation across either seat;
    // actual named-identity Synth-Suit composition is covered by EspionageSurgeTests.
    private static AbilityRunner ActualPreparations(string synthetic)
    {
        var root = System.Text.Json.Nodes.JsonNode.Parse(synthetic)!;
        var rows = root["cards"]!.AsArray();
        var actual = System.Text.Json.Nodes.JsonNode.Parse(AuthoredCards.Text)!["cards"]!.AsArray();
        foreach (string face in new[] { "08024" })
        {
            rows.Remove(rows.Single(row => (string?)row!["card"] == face));
            rows.Add(actual.Single(row => (string?)row!["card"] == face)!.DeepClone());
        }
        return new(AbilityCatalog.Parse(root.ToJsonString()));
    }

}
