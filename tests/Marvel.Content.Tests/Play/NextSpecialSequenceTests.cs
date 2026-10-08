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

public sealed class NextSpecialSequenceTests
{
    private static readonly CardCatalog Cards = CardCatalog.Parse(
        File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));

    [Rule("rr:special")]
    [Fact]
    public void NextChoiceWaitsForTheCurrentTargetAndIncludesANewUpgrade()
    {
        // rr:special: "Special abilities may only be resolved through the explicit
        // instruction of another card ability." 01043a makes each Special a step.
        // ruling:ef19e5ae2de7d475 admits new upgrades while that sequence is unfinished.
        var world = Board();
        Card claws = Upgrade(world, "01047");
        Card daggers = Upgrade(world, "01046");
        var runner = AuthoredCards.Runner();
        var events = new List<GameEvent>();
        Prompt first = Begin(world, runner, events);
        Assert.Equal(Outcome.Unfinished, world.Result);
        Assert.Equal(PublicDecisionKind.SpecialAbilityNext, first.PublicKind);
        Assert.Contains("Resolve it completely before choosing again", first.Description);
        Assert.Equal([claws.ObjectId, daggers.ObjectId], first.Affordances.Select(offer => offer.AnchorId));
        Assert.All(first.Affordances, offer =>
        {
            Assert.Null(offer.Targets);
            Assert.StartsWith("Resolve ", offer.CommitLabel);
            Assert.Contains("Choose any required targets", offer.Description);
        });

        Sequence.Answer(world, Cards, runner, first, Decision.Take(claws.ObjectId), events);
        Prompt target = Sequence.Work(world, Cards, runner, events)!;
        Assert.NotEqual(PublicDecisionKind.SpecialAbilityNext, target.PublicKind);
        Assert.Equal(0, world.TheCardIn(DeckType.VillainArea)!.Damage);
        Assert.Equal(Outcome.Unfinished, world.Result);
        Card suit = Upgrade(world, "01049");
        Sequence.Answer(world, Cards, runner, target,
            Decision.Take(world.TheCardIn(DeckType.VillainArea)!.ObjectId), events);
        Assert.Equal(Outcome.Unfinished, world.Result);
        Prompt next = Sequence.Work(world, Cards, runner, events)!;
        Assert.Equal(2, world.TheCardIn(DeckType.VillainArea)!.Damage);
        Assert.Equal(PublicDecisionKind.SpecialAbilityNext, next.PublicKind);
        Assert.Equal([daggers.ObjectId, suit.ObjectId], next.Affordances.Select(offer => offer.AnchorId));
        Assert.DoesNotContain(next.Affordances, offer => offer.AnchorId == claws.ObjectId);
    }

    [Fact]
    public void AnUpgradeArrivingDuringTheFinalSpecialDoesNotReopenTheSequence()
    {
        // ruling:ef19e5ae2de7d475 ends the sequence when the final Special resolves; arrival
        // during that final effect does not retroactively remove its final status.
        var world = Board();
        Upgrade(world, "01047");
        var runner = AuthoredCards.Runner();
        var events = new List<GameEvent>();
        Prompt target = Begin(world, runner, events);
        Assert.NotEqual(PublicDecisionKind.SpecialAbilityNext, target.PublicKind);
        Upgrade(world, "01046");
        Card villain = world.TheCardIn(DeckType.VillainArea)!;
        Sequence.Answer(world, Cards, runner, target, Decision.Take(villain.ObjectId), events);
        Assert.Null(Sequence.Work(world, Cards, runner, events));
        Assert.Equal(4, villain.Damage);
    }

    [Rule("rr:in-play-and-out-of-play.5")]
    [Fact]
    public void ARemainingUpgradeThatLeavesPlayIsNotOfferedAtTheNextBoundary()
    {
        // rr:in-play-and-out-of-play.5: card abilities interact only with cards
        // in play unless the ability specifically refers to an out-of-play area.
        var world = Board();
        Card claws = Upgrade(world, "01047");
        Card daggers = Upgrade(world, "01046");
        Card genius = Upgrade(world, "01048");
        var runner = AuthoredCards.Runner();
        var events = new List<GameEvent>();
        Prompt choice = Begin(world, runner, events);
        Sequence.Answer(world, Cards, runner, choice, Decision.Take(claws.ObjectId), events);
        Prompt target = Sequence.Work(world, Cards, runner, events)!;
        World.MoveToTop(daggers, world.AreaOf(DeckType.DiscardPile, PlayArea.Of(0), cardOwner: 0));
        World.MoveToTop(genius, world.AreaOf(DeckType.DiscardPile, PlayArea.Of(0), cardOwner: 0));
        Sequence.Answer(world, Cards, runner, target,
            Decision.Take(world.TheCardIn(DeckType.VillainArea)!.ObjectId), events);
        Assert.Null(Sequence.Work(world, Cards, runner, events));
        Assert.Equal(2, world.TheCardIn(DeckType.VillainArea)!.Damage);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ASequenceAnswerCannotDeclareFutureTargetsOrInventACard(bool invented)
    {
        var world = Board();
        Card claws = Upgrade(world, "01047");
        Card daggers = Upgrade(world, "01046");
        var runner = AuthoredCards.Runner();
        var events = new List<GameEvent>();
        Prompt choice = Begin(world, runner, events);
        Decision answer = invented ? Decision.Take(999)
            : Decision.Take(claws.ObjectId, [daggers.ObjectId], []);
        Assert.Throws<RulesNotImplementedException>(() =>
            Sequence.Answer(world, Cards, runner, choice, answer, events));
        Assert.Equal(0, world.TheCardIn(DeckType.VillainArea)!.Damage);
    }

    [Rule("rr:response")]
    [Fact]
    public void ADefeatResponseResolvesBeforeTheNextSpecialIsOffered()
    {
        // rr:response: "Response abilities may be resolved after the specified
        // triggering condition occurs".
        // 01052 responds after the hero attacks and defeats an enemy.
        var world = Board();
        Card claws = Upgrade(world, "01047");
        Upgrade(world, "01046");
        Upgrade(world, "01049");
        Card minion = world.CreateCard("01121",
            world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        world.CreateCard("01052", world.Seats[0].Hand);
        Card scheme = world.TheCardIn(DeckType.MainSchemesArea)!;
        scheme.PlaceTokens("k_threat", 3);
        var runner = AuthoredCards.Runner();
        var events = new List<GameEvent>();
        Prompt first = Begin(world, runner, events);
        Sequence.Answer(world, Cards, runner, first, Decision.Take(claws.ObjectId), events);
        Prompt target = Sequence.Work(world, Cards, runner, events)!;
        Sequence.Answer(world, Cards, runner, target, Decision.Take(minion.ObjectId), events);
        Prompt response = Sequence.Work(world, Cards, runner, events)!;
        Assert.Equal(Question.Opportunity, response.Asking);
        Assert.Equal(TimingPriority.Response, response.When);
        Assert.NotEqual(PublicDecisionKind.SpecialAbilityNext, response.PublicKind);
        Sequence.Answer(world, Cards, runner, response, Decision.Decline, events);
        Prompt next = Sequence.Work(world, Cards, runner, events)!;
        Assert.Equal(PublicDecisionKind.SpecialAbilityNext, next.PublicKind);
        Assert.Equal(2, next.Affordances.Count);
    }

    private static Prompt Begin(World world, AbilityRunner runner, List<GameEvent> events)
    {
        world.Abilities = runner;
        Card played = world.CreateCard("01043a", world.Seats[0].Hand);
        Card resource = world.CreateCard("01044", world.Seats[0].Hand);
        runner.Act(world, new PendingAbility(played.ObjectId, AbilityType.Action, 0),
            [resource.ObjectId], []);
        return Sequence.Work(world, Cards, runner, events)!;
    }

    private static World Board()
    {
        var world = new World(Cards, 1);
        var seat = world.CreateSeat("Black Panther");
        seat.IdentityCard = world.CreateCard("01040a", seat.Hero);
        world.CreateCard("01134", world.AreaOf(DeckType.VillainArea));
        world.CreateCard("01137b", world.AreaOf(DeckType.MainSchemesArea));
        world.CreateCard("01088", seat.Deck);
        world.CreateCard("01154", world.AreaOf(DeckType.EncounterDeck));
        return world;
    }

    private static Card Upgrade(World world, string face) => world.CreateCard(face,
        world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
}
