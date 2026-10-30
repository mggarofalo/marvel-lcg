using System.Text.Json;
using System.Text.Json.Nodes;
using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>
/// Synthetic contracts for the would-ready interrupt window. Media Coverage
/// (02049) and All Tied Up (02048) keep their "attach to your identity" rows and
/// have their abilities replaced by would-ready interrupts; Arc Reactor (01035)
/// has its action replaced by a ready with dependent text. No played scenario is
/// claimed.
/// </summary>
public sealed class WouldReadyReplacementTests : CoreCardTestBase
{
    private const string MediaCoverage = "02049";
    private const string AllTiedUp = "02048";
    private const string ArcReactor = "01035";
    private const string Mockingbird = "01083";

    // "Forced Interrupt: When attached character would ready, discard this card instead."
    private const string Replaces = """
        [{"trigger":{"event":"WhenCardWouldReady","timing":"ForcedInterrupt","would":true,"subject":"attachedTo"},
          "effect":{"seq":[{"replaceReady":"attachedTo"},{"discard":"this"}]}}]
        """;

    // As above, and the replacement also discards All Tied Up wherever it is.
    private const string ReplacesAndDiscardsAllTiedUp = """
        [{"trigger":{"event":"WhenCardWouldReady","timing":"ForcedInterrupt","would":true,"subject":"attachedTo"},
          "effect":{"seq":[{"replaceReady":"attachedTo"},{"discard":"this"},{"discard":{"titled":"All Tied Up"}}]}}]
        """;

    // An optional interrupt to the same condition that changes nothing when declined.
    private const string Optional = """
        [{"trigger":{"event":"WhenCardWouldReady","timing":"Interrupt","subject":"attachedTo"},
          "effect":{"discard":"this"}}]
        """;

    // "Action: Exhaust this → ready your hero. Then draw 1 card."
    private const string ReadyThenDraw = """
        [{"trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"this"},
          "cost":{"exhaust":"this"},
          "effect":{"then":{"effect":{"ready":"you"},"then":{"draw":{"player":"you","count":1}}}}}]
        """;

    [Fact]
    [Rule("rr:end-of-player-phase.step.3")]
    [Rule("rr:replacement-effect.1")]
    [Rule("rr:interrupt.3.1")]
    public void PhaseEndReplacesOnlyTheAttachedCharactersReady()
    {
        // rr:end-of-player-phase.step.3: "Each player simultaneously readies all of
        // their cards. Ready each exhausted encounter card." rr:interrupt.3.1: a
        // "would" interrupt resolves "before its triggering condition initiates".
        // rr:replacement-effect.1: the replaced ready "is no longer considered
        // imminent". Player 1's identity stays exhausted; player 0's identity,
        // player 1's ally and an engaged minion ready in the same batch.
        var (world, runner) = Board(new() { [MediaCoverage] = Replaces });
        var host = world.Seats[1].IdentityCard;
        var other = world.Seats[0].IdentityCard;
        var ally = Ally(world, 1);
        var minion = KangTemporalFixture.Engaged(world, "01101", 1);
        var attached = Attach(world, MediaCoverage, host);
        foreach (var card in new[] { host, other, ally, minion }) card.Exhaust();

        var events = ReadyCards(world, runner);

        Assert.False(host.Ready);
        Assert.True(other.Ready && ally.Ready && minion.Ready);
        Assert.Equal(DeckType.EncounterDiscardPile, attached.Area.Type);
        var readied = events.OfType<FieldSet>().Where(item => item.Verb == "Ready").Select(item => item.Card);
        Assert.Equal(new[] { other.ObjectId, ally.ObjectId, minion.ObjectId }.Order(), readied.Order());
        var activity = Assert.Single(events.OfType<ActivityCompleted>(), item => item.Verb == "Ready");
        Assert.Equal(ActivityPhaseStep.ReadyCards, activity.PhaseStep);
        // The replacement resolves before any card in the batch readies.
        int discarded = events.FindIndex(item => item is CardsMoved moved && moved.Cards.Any(landing => landing.Card == attached.ObjectId));
        Assert.True(discarded >= 0 && discarded < events.FindIndex(item => item is FieldSet { Verb: "Ready" }));
        // Engine choice: the would-ready occurrence has its own identity, distinct
        // from the phase step (round 1, step 3) that instructed the ready.
        Assert.NotEqual(Moment.Id(1, 3, 0), events[discarded].AbilityOccurrence);
        Assert.False(world.Agenda.IsBusy);
    }

    [Fact]
    [Rule("rr:triggering-condition.2")]
    public void AThisSubjectObservesItsOwnReadyInAPhaseEndBatch()
    {
        // One phase-end ready readies several cards in one occurrence
        // (rr:triggering-condition.2). Mockingbird (01083), replaced by "Forced
        // Interrupt: When this would ready, draw 1 card", observes its own ready in
        // that batch; the other copy, already ready, does not.
        var (world, runner) = Board(new()
        {
            [Mockingbird] = """
                [{"trigger":{"event":"WhenCardWouldReady","timing":"ForcedInterrupt","would":true,"subject":"this"},
                  "effect":{"draw":{"player":"you","count":1}}}]
                """,
        });
        var exhausted = Ally(world, 1);
        Ally(world, 0);
        world.Seats[0].IdentityCard.Exhaust();
        exhausted.Exhaust();
        int[] hands = [world.Seats[0].Hand.Cards.Count, world.Seats[1].Hand.Cards.Count];

        ReadyCards(world, runner);

        Assert.True(exhausted.Ready);
        Assert.True(world.Seats[0].IdentityCard.Ready);
        Assert.Equal([hands[0], hands[1] + 1], [world.Seats[0].Hand.Cards.Count, world.Seats[1].Hand.Cards.Count]);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    [Rule("rr:then.2")]
    public void TextAfterAReplacementResolvesOnlyWhenTheReadyWasReplaced(bool exhausted)
    {
        // "Forced Interrupt: When attached character would ready, replace that
        // ready. Then discard this card." rr:then.2: the post-"then" text resolves
        // only if the pre-"then" text fully resolves. An already ready host never
        // would ready, so the interrupt does not trigger at all.
        var (world, runner) = Board(new()
        {
            [MediaCoverage] = """
                [{"trigger":{"event":"WhenCardWouldReady","timing":"ForcedInterrupt","would":true,"subject":"attachedTo"},
                  "effect":{"then":{"effect":{"replaceReady":"attachedTo"},"then":{"discard":"this"}}}}]
                """,
        });
        var host = world.Seats[1].IdentityCard;
        var attached = Attach(world, MediaCoverage, host);
        if (exhausted) host.Exhaust();

        ReadyCards(world, runner);

        Assert.Equal(!exhausted, host.Ready);
        Assert.Equal(exhausted ? DeckType.EncounterDiscardPile : DeckType.UpgradesArea, attached.Area.Type);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    [Rule("rr:replacement-effect.1")]
    [Rule("rr:then.2")]
    public void ACardEffectReadyIsReplacedBeforeItsDependentText(bool attached)
    {
        // rr:then.2: "If the pre-'then' text of an effect does not fully resolve,
        // the post-'then' text does not attempt to resolve." A replaced ready did
        // not happen, so the ability resumes after the window without drawing.
        var (world, runner) = Board(new() { [MediaCoverage] = Replaces, [ArcReactor] = ReadyThenDraw });
        var hero = world.Seats[1].IdentityCard;
        var reactor = world.CreateCard(ArcReactor, world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(1), cardOwner: 1));
        var attachment = attached ? Attach(world, MediaCoverage, hero) : null;
        hero.Exhaust();
        int hand = world.Seats[1].Hand.Cards.Count;

        var action = Assert.Single(runner.Actions(world, 1), ability => ability.Card == reactor.ObjectId);
        runner.ExecuteAction(world, new PlayerAction(action, [], []));
        KangTemporalFixture.Drive(world, runner, _ => null);

        Assert.Equal(!attached, hero.Ready);
        Assert.Equal(hand + (attached ? 0 : 1), world.Seats[1].Hand.Cards.Count);
        Assert.False(reactor.Ready);
        if (attachment is not null) Assert.Equal(DeckType.EncounterDiscardPile, attachment.Area.Type);
        Assert.False(world.Agenda.IsBusy);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    [Rule("rr:delayed-effect.1")]
    [Rule("rr:replacement-effect.1")]
    public void AnUndamagedDefenseReadyIsReplaced(bool attached)
    {
        // Desperate Defense (09015): "If your hero takes no damage from this attack,
        // ready your hero." rr:delayed-effect.1 resolves it "before responses to that
        // point or condition may be used"; the would-ready window it opens is
        // resolved before the attack-end responses.
        var (world, _, source, payment, _) = DesperateDefensePrerequisiteFixture.Board(owner: 1, attack: 4);
        var runner = Runner(new() { [MediaCoverage] = Replaces });
        world.Abilities = runner;
        var hero = world.Seats[1].IdentityCard;
        var attachment = attached ? Attach(world, MediaCoverage, hero) : null;
        var events = new List<GameEvent>();

        Assert.Equal(1, DesperateDefensePrerequisiteFixture.Drain(world, runner, source, payment,
            hero.ObjectId, 1, events));

        Assert.Equal(0, hero.Damage);
        Assert.Equal(!attached, hero.Ready);
        if (attachment is not null) Assert.Equal(DeckType.EncounterDiscardPile, attachment.Area.Type);
        Assert.Single(events.OfType<AttackCompleted>());
    }

    [Fact]
    [Rule("rr:triggering-condition.1")]
    public void AnUnobservedReadyKeepsTheUninterruptedEventsAndRandomState()
    {
        // The would-ready ability waits on player 0's ready identity, so neither the
        // phase-end batch nor the card effect readying player 1's hero observes it.
        string Run(bool observer)
        {
            var (world, runner) = Board(new() { [MediaCoverage] = Replaces, [ArcReactor] = ReadyThenDraw });
            var events = PhaseEndThenAction(world, runner, observer ? world.Seats[0].IdentityCard : null);
            return Snapshot(world, events);
        }

        Assert.Equal(Run(observer: false), Run(observer: true));
    }

    [Fact]
    [Rule("rr:ability.11")]
    [Rule("rr:interrupt.5")]
    public void ADeclinedWouldReadyWindowKeepsTheUninterruptedEventsAndRandomState()
    {
        // rr:ability.11 makes declining an optional interrupt a real answer, and
        // rr:interrupt.5 closes the window. The suspended phase-end batch and the
        // suspended card effect then produce exactly the uninterrupted events.
        string Run(bool observer)
        {
            var (world, runner) = Board(new() { [MediaCoverage] = Optional, [ArcReactor] = ReadyThenDraw });
            var events = PhaseEndThenAction(world, runner, observer ? world.Seats[1].IdentityCard : null);
            return Snapshot(world, events);
        }

        Assert.Equal(Run(observer: false), Run(observer: true));
    }

    [Fact]
    [Rule("rr:ability.11")]
    [Rule("rr:interrupt.1")]
    public void AnOptionalWouldReadyInterruptNamesTheCardAboutToReady()
    {
        // rr:interrupt.1: players trigger interrupts "on cards they control or on
        // encounter cards"; the attachment's "you" makes it its host's controller's
        // (rr:ability.8.1). The prompt names the card that would ready, and
        // declining (rr:ability.11) leaves the attachment and readies the host.
        var (world, runner) = Board(new() { [MediaCoverage] = Optional });
        var host = world.Seats[1].IdentityCard;
        var attached = Attach(world, MediaCoverage, host);
        host.Exhaust();
        world.Agenda.Add(new PhaseStep(new PhaseTransitionOperation(PhaseTransitionKind.ReadyCards), 1, 3));
        var events = new List<GameEvent>();

        var prompt = Sequence.Work(world, world.Facts, runner, events);

        Assert.NotNull(prompt);
        Assert.Equal(1, prompt.Player);
        Assert.True(prompt.Cancellable);
        Assert.Equal("Spider-Man would ready — interrupt?", prompt.DisplayQuestion);
        Assert.Equal([host.ObjectId], prompt.ContextCardIds);
        Assert.False(host.Ready);
        Sequence.Answer(world, world.Facts, runner, prompt, Decision.Decline, events);
        Assert.Empty(Drain(world, runner).OfType<CardsMoved>());
        Assert.True(host.Ready);
        Assert.Equal(DeckType.UpgradesArea, attached.Area.Type);
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(0, false)]
    [InlineData(1, true)]
    [InlineData(1, false)]
    [Rule("rr:forced.5")]
    [Rule("rr:forced.6")]
    public void TheFirstPlayerOrdersSimultaneousReplacements(int first, bool mediaFirst)
    {
        // rr:forced.5: the first player decides the order of simultaneous forced
        // abilities "regardless of who controls the cards". rr:forced.6: each
        // "must resolve as completely as possible before the next". Media
        // Coverage's replacement also discards All Tied Up; resolved first, it
        // takes All Tied Up out of play before its replacement can initiate.
        var (world, runner) = Board(new() { [MediaCoverage] = ReplacesAndDiscardsAllTiedUp, [AllTiedUp] = Replaces });
        world.FirstPlayer = first;
        var zero = world.Seats[0].IdentityCard;
        var one = world.Seats[1].IdentityCard;
        var media = Attach(world, MediaCoverage, zero);
        var tied = Attach(world, AllTiedUp, one);
        zero.Exhaust();
        one.Exhaust();
        int asked = -1;

        world.Agenda.Add(new PhaseStep(new PhaseTransitionOperation(PhaseTransitionKind.ReadyCards), 1, 3));
        KangTemporalFixture.Drive(world, runner, prompt =>
        {
            if (prompt.Asking != Question.Order) return null;
            asked = prompt.Player;
            var chosen = mediaFirst ? media : tied;
            return Decision.Take(Assert.Single(prompt.Affordances, item => item.AnchorId == chosen.ObjectId).Id);
        });

        Assert.Equal(first, asked);
        Assert.False(zero.Ready);
        Assert.Equal(mediaFirst, one.Ready);
        Assert.Equal(DeckType.EncounterDiscardPile, media.Area.Type);
        Assert.Equal(DeckType.EncounterDiscardPile, tied.Area.Type);
        Assert.False(world.Agenda.IsBusy);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    [Rule("rr:interrupt.4")]
    public void AReplacedReadyAdmitsNoFurtherReplacement(bool mediaFirst)
    {
        // rr:interrupt.4: "If an interrupt changes (via a replacement effect) ... an
        // imminent triggering condition, further interrupts to the original
        // triggering condition cannot be triggered." Both replacements wait on one
        // host; only the one the first player orders first resolves.
        var (world, runner) = Board(new() { [MediaCoverage] = Replaces, [AllTiedUp] = Replaces });
        var host = world.Seats[1].IdentityCard;
        var media = Attach(world, MediaCoverage, host);
        var tied = Attach(world, AllTiedUp, host);
        host.Exhaust();

        world.Agenda.Add(new PhaseStep(new PhaseTransitionOperation(PhaseTransitionKind.ReadyCards), 1, 3));
        KangTemporalFixture.Drive(world, runner, prompt => prompt.Asking == Question.Order
            ? Decision.Take(Assert.Single(prompt.Affordances,
                item => item.AnchorId == (mediaFirst ? media : tied).ObjectId).Id)
            : null);

        Assert.False(host.Ready);
        Assert.Equal(mediaFirst ? DeckType.EncounterDiscardPile : DeckType.UpgradesArea, media.Area.Type);
        Assert.Equal(mediaFirst ? DeckType.UpgradesArea : DeckType.EncounterDiscardPile, tied.Area.Type);
        Assert.False(world.Agenda.IsBusy);
    }

    [Theory]
    [InlineData("""{"replaceReady":"this"}""", "expected 'attachedTo'")]
    [InlineData("""{"replaceReady":"attachedTo","extra":1}""", "is not a node")]
    public void AMalformedReplacementIsRefused(string effect, string says)
    {
        var thrown = Assert.Throws<AbilityException>(() => AbilityLowering.Book(AbilityCatalog.Parse("""
            {"cards":[{"card":"02049","abilities":[{
              "trigger":{"event":"WhenCardWouldReady","timing":"ForcedInterrupt","would":true,"subject":"attachedTo"},
              "effect":EFFECT}]}]}
            """.Replace("EFFECT", effect, StringComparison.Ordinal))));
        Assert.Contains(says, thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    [Rule("rr:replacement-effect.1")]
    public void CancellingAWholeWouldReadyOccurrenceRaises()
    {
        // A would-ready occurrence holds every card of one ready instruction; this
        // engine replaces one card's ready with replaceReady and does not guess
        // what cancelling all of them would mean.
        var (world, runner) = Board(new()
        {
            [MediaCoverage] = """
                [{"trigger":{"event":"WhenCardWouldReady","timing":"ForcedInterrupt","would":true,"subject":"attachedTo"},
                  "effect":{"cancelOccurrence":"trigger"}}]
                """,
        });
        var host = world.Seats[1].IdentityCard;
        Attach(world, MediaCoverage, host);
        host.Exhaust();

        world.Agenda.Add(new PhaseStep(new PhaseTransitionOperation(PhaseTransitionKind.ReadyCards), 1, 3));
        Assert.Throws<RulesNotImplementedException>(() => KangTemporalFixture.Drive(world, runner, _ => null));
    }

    // Phase-end ready of player 1's exhausted hero, then the synthetic Arc Reactor
    // action readying it again; the attachment, if any, is moved in without events.
    private static List<GameEvent> PhaseEndThenAction(World world, AbilityRunner runner, Card? host)
    {
        var hero = world.Seats[1].IdentityCard;
        var reactor = world.CreateCard(ArcReactor, world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(1), cardOwner: 1));
        var attachment = world.CreateCard(MediaCoverage, world.AreaOf(DeckType.EncounterDeck));
        if (host is not null) World.MoveToTop(attachment, Upgrades(world, host));
        hero.Exhaust();

        var events = ReadyCards(world, runner);
        hero.Exhaust();
        var action = Assert.Single(runner.Actions(world, 1), ability => ability.Card == reactor.ObjectId);
        // Each stream's activity ids are offsets into it; composing rebases them.
        ActivityBatches.Append(events, runner.ExecuteAction(world, new PlayerAction(action, [], [])).Events);
        ActivityBatches.Append(events, Drain(world, runner));
        Assert.True(hero.Ready);
        return events;
    }

    private static string Snapshot(World world, List<GameEvent> events) => JsonSerializer.Serialize(new
    {
        Events = events.Select(item => JsonSerializer.Serialize(item, item.GetType())),
        Random = world.Random.Generator.GetState(),
        world.Random.Generator.WordsConsumed,
        world.Agenda.IsBusy,
    });

    private static List<GameEvent> ReadyCards(World world, AbilityRunner runner)
    {
        world.Agenda.Add(new PhaseStep(new PhaseTransitionOperation(PhaseTransitionKind.ReadyCards), 1, 3));
        return Drain(world, runner);
    }

    // Drains the agenda, declining every optional prompt.
    private static List<GameEvent> Drain(World world, AbilityRunner runner)
    {
        var events = new List<GameEvent>();
        var prompt = Sequence.Work(world, world.Facts, runner, events);
        for (int step = 0; prompt is not null; step++)
        {
            Assert.True(step < 20 && prompt.Cancellable, prompt.Description);
            Sequence.Answer(world, world.Facts, runner, prompt, Decision.Decline, events);
            prompt = Sequence.Work(world, world.Facts, runner, events);
        }
        return events;
    }

    private static Card Ally(World world, int player) =>
        world.CreateCard(Mockingbird, world.AreaOf(DeckType.AlliesArea, PlayArea.Of(player), cardOwner: player));

    // An encounter attachment moved from the encounter deck onto its host.
    private static Card Attach(World world, string face, Card host)
    {
        var card = world.CreateCard(face, world.AreaOf(DeckType.EncounterDeck));
        World.MoveToTop(card, Upgrades(world, host));
        return card;
    }

    private static Area Upgrades(World world, Card host) =>
        world.AreaOf(DeckType.UpgradesArea, host.Area.PlayArea, host.ObjectId, host.Area.CardOwner);

    private static (World World, AbilityRunner Runner) Board(Dictionary<string, string> rows)
    {
        var runner = Runner(rows);
        var (world, _) = KangTemporalFixture.Board(players: 2, encounter: 2);
        world.Abilities = runner;
        return (world, runner);
    }

    // The authored book with the named rows' abilities replaced.
    private static AbilityRunner Runner(Dictionary<string, string> rows)
    {
        var book = JsonNode.Parse(File.ReadAllText(RepositoryPaths.Dataset("abilities", "abilities.json")))!;
        foreach (var (face, abilities) in rows)
            book["cards"]!.AsArray().Single(card => (string?)card!["card"] == face)!["abilities"] = JsonNode.Parse(abilities);
        return new AbilityRunner(AbilityCatalog.Parse(book.ToJsonString()));
    }
}
