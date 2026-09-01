using Marvel.Tests;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Xunit;

namespace Marvel.Rules.Tests.Play;

/// <summary>
/// The villain phase on boards the recording cannot reach.
/// </summary>
/// <remarks>
/// <para>
/// <c>PlayerPhaseTests</c> holds the phase against the recorded milestone game,
/// which is the strongest check available and has a blind spot: that game has
/// one player, and its round-one boost card is worth nothing. So the modulo in
/// "pass the first player token" and the whole boost-icon addition are exercised
/// by nothing there — both survived a mutation that deleted them.
/// </para>
/// <para>
/// These are the boards that separate them, built by hand. Small enough to
/// reason about, and none of them is asserting anything the recorded game
/// contradicts.
/// </para>
/// </remarks>
public sealed class VillainPhaseTests
{
    [Rule("rr:scheme-enemy-activation.step.2.c")]
    [Rule("rr:scheme-enemy-activation.step.3")]
    [Theory]
    // SCH 1, and a boost card worth nothing: the recorded round one, which is
    // why a resolve that ignored boost icons entirely passed every test.
    [InlineData(0, 1)]
    // SCH 1 plus one boost icon: the recorded round two, at 2.
    [InlineData(1, 2)]
    [InlineData(3, 4)]
    public void BoostIconsAddToTheSchemeValue(int boost, int expected)
    {
        // Threat placed is the *modified* SCH, so the two steps have to be
        // read together: a resolve that placed printed SCH would be right
        // whenever the boost card happened to be worth nothing.
        var printed = new Printed()
            .With("villain", ("SCH", "1"))
            .With("scheme", ("EscalationThreat", "0"))
            .With("boost", ("Boost", boost.ToString()));
        var world = Board(printed, players: 1);

        Run(world, printed);

        Assert.Equal(expected, world.TheCardIn(DeckType.MainSchemesArea)!.Tokens["k_threat"]);
    }

    [Rule("rr:scheme-enemy-activation.step.2.d")]
    [Rule("rr:scheme-enemy-activation.step.2.a")]
    [Rule("rr:boost-boost-icon.5")]
    [Fact]
    public void TheBoostCardIsDiscardedWhicheverWayItCounts()
    {
        // Step 2a says "flip the boost card faceup." A zero-icon card is still
        // flipped and discarded, so neither operation is conditional on the count.
        var printed = new Printed()
            .With("villain", ("SCH", "1"))
            .With("scheme", ("EscalationThreat", "0"));
        var world = Board(printed, players: 1);

        var events = Run(world, printed);

        Assert.Contains(events.OfType<CardsMoved>(), moved =>
            moved.Verb == "Boost"
            && moved.To.Zone == nameof(DeckType.EncounterDiscardPile)
            && moved.Cards.Any(landing => world.Cards[landing.Card].FaceId == "boost"));
        Assert.Contains(events.OfType<CardsFlipped>(), flipped =>
            flipped.Verb == "Boost" && flipped.FaceUp);
    }

    [Rule("rr:villain-phase.step.5")]
    [Fact]
    public void TheFirstPlayerTokenGoesToTheNextSeat()
    {
        // At one player the token returns to the same seat, which is why the
        // recorded game cannot tell a modulo from a no-op.
        var printed = new Printed()
            .With("villain", ("SCH", "0"))
            .With("scheme", ("EscalationThreat", "0"));
        var world = Board(printed, players: 3);

        Assert.Equal(0, world.FirstPlayer);
        Run(world, printed);
        Assert.Equal(1, world.FirstPlayer);
        Run(world, printed);
        Assert.Equal(2, world.FirstPlayer);
        Run(world, printed);
        Assert.Equal(0, world.FirstPlayer);
    }

    [Rule("rr:villain-phase.step.2")]
    [Rule("rr:villain-phase.step.2.a")]
    [Rule("rr:activation.1")]
    [Rule("rr:activation.3")]
    [Fact]
    public void TheVillainActivatesOncePerPlayer()
    {
        // For each player, "the villain activates against the player." Three
        // players therefore means three activations, boost cards, and lots of
        // threat -- not one.
        var printed = new Printed()
            .With("villain", ("SCH", "1"))
            .With("scheme", ("EscalationThreat", "0"));
        var world = Board(printed, players: 3);
        var activations = new ActivationObserver();

        var events = Run(world, printed, activations);

        Assert.Equal([0, 1, 2], activations.Players);
        Assert.Equal(3, world.TheCardIn(DeckType.MainSchemesArea)!.Tokens["k_threat"]);
        Assert.Equal(
            3,
            events.OfType<CardsMoved>().Count(moved =>
                moved.Verb == "Boost"
                && moved.To.Zone == nameof(DeckType.BoostCardsDeck)));
    }

    [Rule("rr:villain-phase.step.2.a")]
    [Rule("rr:player-elimination.5.1")]
    [Fact]
    public void EliminatingAPlayerDoesNotSkipTheNextPlayersVillainActivation()
    {
        // Each player resolves the villain activation in player order. Player
        // zero begins one hit point from defeat; removing that seat changes
        // World.PlayerOrder, but cannot turn player one's continuation into an
        // already-completed activation for player zero.
        var printed = new Printed()
            .With("villain", ("ATK", "1"))
            .With("identity", ("HP", "10"), ("DEF", "0"))
            .With("scheme", ("EscalationThreat", "0"))
            .With("boost", ("Boost", "0"));
        printed.Kinds["identity"] = CardKind.Hero;
        var world = Board(printed, players: 2);
        world.Seats[0].IdentityCard.TakeDamage(9);
        var activations = new ActivationObserver();

        var events = new List<GameEvent>();
        VillainPhase.Schedule(world.Agenda, round: 1);
        var asked = Sequence.Work(world, printed, activations, events);
        for (int answered = 0; asked is not null; answered++)
        {
            Assert.True(answered < 10);
            Sequence.Answer(
                world, printed, activations, asked, Decision.Decline, events);
            asked = Sequence.Work(world, printed, activations, events);
        }

        Assert.True(world.Seats[0].Eliminated);
        Assert.False(world.Seats[1].Eliminated);
        Assert.Equal([0, 1], activations.Players);
        Assert.Equal(1, world.Seats[1].IdentityCard.Damage);
    }

    // Deliberately uncited: no published rule says a card acquires a token
    // pool. It is an artefact of how the digest serialises a card, kept because
    // the digest is a wire format, and `docs/rules-citations.md` uses it as the
    // example of what an uncited test honestly is.
    [Fact]
    public void TokenPoolsSurviveLeavingPlay()
    {
        // The property the recorded discard pile shows and a synthetic board
        // states directly: a treachery has no `k_threat` key in the encounter
        // deck, has one after passing through the revealing area, and still has
        // one from the discard pile.
        var printed = new Printed()
            .With("villain", ("SCH", "0"))
            .With("scheme", ("EscalationThreat", "0"));
        printed.Kinds["encounter"] = CardKind.Treachery;
        var world = Board(printed, players: 1);

        var card = world.AreaOf(DeckType.EncounterDeck).Cards
            .Single(c => c.FaceId == "encounter");
        Assert.False(card.HasRegisteredTokens);

        Run(world, printed);

        Assert.True(card.HasRegisteredTokens);
        Assert.Equal(DeckType.EncounterDiscardPile, card.Area.Type);
        Assert.Contains("k_threat", StateFields.Keys(CardKind.Treachery, hasHeldPools: true));
        Assert.DoesNotContain("k_threat", StateFields.Keys(CardKind.Treachery, hasHeldPools: false));
    }

    [Rule("rr:activation.1")]
    [Fact]
    public void AVillainFacingAHeroAttacksRatherThanSchemes()
    {
        // "If the identity of the player resolving the activation is in hero
        // form, the villain initiates an attack against that player's identity.
        // If the identity [...] is in alter-ego form, the villain initiates a
        // scheme." Which face is showing decides, and the two are exclusive:
        // an engine that schemed anyway would place threat the rules do not.
        var printed = new Printed()
            .With("villain", ("SCH", "1"), ("ATK", "2"))
            .With("scheme", ("EscalationThreat", "0"))
            .With("identity", ("HP", "10"), ("DEF", "3"));
        printed.Kinds["identity"] = CardKind.Hero;
        var world = Board(printed, players: 1);

        // Exhausted, so there is nobody who could defend and the attack asks
        // nothing -- `rr:defend-defense.2`, a hero must exhaust to defend.
        world.Seats[0].IdentityCard.Exhaust();

        Run(world, printed);

        Assert.False(
            world.TheCardIn(DeckType.MainSchemesArea)!.Tokens.ContainsKey("k_threat"));
        Assert.Equal(2, world.Seats[0].IdentityCard.Damage);
    }

    [Rule("rr:villain-phase.step.2.b")]
    [Rule("rr:minion.3")]
    [Rule("rr:activation.2")]
    [Fact]
    public void EachEngagedMinionActivatesAfterTheVillain()
    {
        // "The villain activates against the player", then "each minion engaged
        // with the player activates against them". Skipping the minion silently
        // is the dangerous failure: the board is right about everything except
        // the damage nobody took.
        //
        // Alter-ego form, so both enemies scheme and the threat placed is the
        // sum of their SCH -- which separates "the villain activated" from
        // "everything engaged activated" in one number.
        var printed = new Printed()
            .With("villain", ("SCH", "2"))
            .With("minion", ("SCH", "3"))
            .With("scheme", ("EscalationThreat", "0"))
            .With("boost", ("Boost", "0"));
        var world = Board(printed, players: 1);
        world.CreateCard("minion", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));

        Run(world, printed);

        Assert.Equal(5, world.TheCardIn(DeckType.MainSchemesArea)!.Tokens["k_threat"]);
    }

    [Rule("rr:villain-phase.step.2.b")]
    [Rule("rr:minion.3")]
    [Fact]
    public void TheEngagedPlayerOrdersTheirMinionActivations()
    {
        // "In the order of the engaged player's choice." Different printed
        // scheme values make both minions observable, while the completion
        // ledger holds the chosen order rather than an object-id fallback.
        var printed = new Printed()
            .With("villain", ("SCH", "0"))
            .With("first", ("SCH", "1"))
            .With("second", ("SCH", "2"))
            .With("scheme", ("EscalationThreat", "0"))
            .With("boost", ("Boost", "0"));
        var world = Board(printed, players: 1);
        var first = world.CreateCard(
            "first", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        var second = world.CreateCard(
            "second", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        var observer = new EnemyOrderObserver();
        var events = new List<GameEvent>();
        VillainPhase.Schedule(world.Agenda, round: 1);

        var asked = Sequence.Work(world, printed, observer, events);

        Assert.NotNull(asked);
        Assert.Equal(Question.Order, asked.Asking);
        Assert.Equal(0, asked.Player);
        Sequence.Answer(
            world, printed, observer, asked,
            new Decision(asked.Affordances[0].Id, [second.ObjectId, first.ObjectId]), events);
        Sequence.Finish(world, printed, observer, events);

        Assert.Equal(["villain", "second", "first"], observer.Enemies);
        Assert.Equal(3, world.TheCardIn(DeckType.MainSchemesArea)!.Tokens["k_threat"]);
    }

    [Rule("rr:minion.4")]
    [Fact]
    public void AMinionEngagedDuringActivationsJoinsTheCurrentProcedure()
    {
        // "If a minion engages a player during the resolution of the engaged
        // minions' activations, that minion also activates during this step."
        // The villain's completion creates the minion here so a planner that
        // snapshots the area before the villain activates misses its SCH 3.
        var printed = new Printed()
            .With("villain", ("SCH", "2"))
            .With("arriving", ("SCH", "3"))
            .With("scheme", ("EscalationThreat", "0"))
            .With("boost", ("Boost", "0"));
        var world = Board(printed, players: 1);
        var abilities = new EngageAfterVillain(world.TheCardIn(DeckType.VillainArea)!.ObjectId);

        Run(world, printed, abilities);

        Assert.Equal(5, world.TheCardIn(DeckType.MainSchemesArea)!.Tokens["k_threat"]);
        Assert.Equal(["villain", "arriving"], abilities.Completed);
    }

    [Rule("rr:activation.6")]
    [Fact]
    public void AMinionLeavingPlayCancelsTheRestOfItsSchemeBeforeMoreWindowsOpen()
    {
        // "If an activating minion leaves play, that minion's activation ends
        // immediately and no further steps of that activation resolve." The
        // boost and threat steps have their own windows, so skipping only their
        // Apply bodies would still expose triggering conditions that never occur.
        var printed = new Printed()
            .With("villain", ("SCH", "0"))
            .With("minion", ("SCH", "3"))
            .With("scheme", ("EscalationThreat", "0"))
            .With("boost", ("Boost", "0"));
        var world = Board(printed, players: 1);
        var minion = world.CreateCard(
            "minion", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        world.Agenda.Abandon();
        world.Agenda.Add(new PhaseStep(
            Steps.Scheme, 1, 2, Subject: minion.ObjectId, Seat: 0));
        var abilities = new BoostWindowOffer(minion.ObjectId);
        var events = new List<GameEvent>();

        var asked = Sequence.Work(world, printed, abilities, events)!;
        Assert.Equal(Question.Opportunity, asked.Asking);
        World.MoveToTop(minion, world.AreaOf(DeckType.EncounterDiscardPile));
        Sequence.Answer(world, printed, abilities, asked, Decision.Decline, events);
        Sequence.Finish(world, printed, abilities, events);

        Assert.Equal(
            0,
            world.TheCardIn(DeckType.MainSchemesArea)!.Tokens.GetValueOrDefault("k_threat"));
        Assert.False(abilities.SawBoostFlipped);
        Assert.False(abilities.SawThreat);
        Assert.True(abilities.SawSchemeEnds);
        Assert.Null(world.Activation);
    }

    [Rule("rr:reveal.3")]
    [Rule("rr:engage")]
    [Rule("rr:minion.1")]
    [Fact]
    public void ARevealedMinionEntersPlayEngagedRatherThanBeingDiscarded()
    {
        // `rr:minion.1`: "If a minion enters play, it remains in play" until
        // an effect makes it leave. `rr:reveal.3`: "it enters play in the play area of the player
        // revealing it. **It is considered to engage that player.**" Before
        // this, every revealed minion went straight to the discard pile -- the
        // encounter deck was a pile of treacheries however it was built.
        var printed = new Printed()
            .With("villain", ("SCH", "0"))
            .With("scheme", ("EscalationThreat", "0"))
            .With("boost", ("Boost", "0"));
        var world = Board(printed, players: 1);

        // The board's deck is [encounter, boost] with the boost on top, and the
        // villain's activation takes the top card. Putting the minion under the
        // boost is what makes it the card that gets *dealt*.
        var deck = world.AreaOf(DeckType.EncounterDeck);
        var boost = deck.Cards[^1];
        world.CreateCard("minion", deck);
        World.MoveToTop(boost, deck);

        Run(world, printed);

        var engaged = world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0));
        Assert.Equal(["minion"], engaged.Cards.Select(card => card.FaceId));
    }

    [Rule("rr:main-scheme-main-scheme-deck.2")]
    [Theory]
    // The recorded game reaches 8 threat against a target of 7, so *greater*
    // and *at least* both complete the scheme there and the boundary is
    // untested by it: a resolve that required strictly greater produces every
    // recorded digest and ends the game one round late.
    [InlineData(2, 2, true)]
    [InlineData(2, 3, false)]
    [InlineData(3, 2, true)]
    public void AMainSchemeCompletesAtItsTargetAndNotOnlyPastIt(
        int escalation, int target, bool completes)
    {
        var printed = new Printed()
            .With("villain", ("SCH", "0"))
            .With("scheme", ("EscalationThreat", escalation.ToString()),
                            ("TargetThreat", target.ToString()));
        var world = Board(printed, players: 1);

        Run(world, printed);

        Assert.Equal(completes, world.IsOver);
    }

    [Rule("rr:modifiers.6.1")]
    [Rule("rr:ability.1")]
    [Fact]
    public void AnAttachmentModifiesItsHostOnlyWhileInPlay()
    {
        // The recorded game cannot tell: its one modifier is an attachment in
        // `UpgradesArea`, which is in play, and its one other hosted card is a
        // Tough with no modifier printed on it. So a resolve that counted
        // modifiers from anywhere produces every recorded digest.
        //
        // A discarded attachment does not modify the card it used to be on.
        var printed = new Printed()
            .With("villain", ("SCH", "1"), ("ATK", "2"))
            .With("scheme", ("EscalationThreat", "0"))
            .With("charge", ("ATK+", "3"));
        var world = Board(printed, players: 1);
        var villain = world.TheCardIn(DeckType.VillainArea)!;

        long Attack() => StateFields
            .For(villain, printed, 1, inPlay: true, hasHeldPools: true,
                 hasFirstPlayerToken: false, world: world)["attack"];

        Assert.Equal(2, Attack());

        var upgrades = world.AreaOf(
            DeckType.UpgradesArea, villain.Area.PlayArea, villain.ObjectId);
        var charge = world.CreateCard("charge", upgrades);
        Assert.Equal(5, Attack());

        // Bound to the same host, and out of play. `StatusArea` is the case
        // that exists on a real board: the recorded Tough hangs off Rhino from
        // a zone that is not in play.
        var aside = world.AreaOf(DeckType.StatusArea, villain.Area.PlayArea, villain.ObjectId);
        World.MoveToTop(charge, aside);
        Assert.Equal(2, Attack());
    }

    [Rule("rr:forced.5")]
    [Rule("rr:quickstrike.2")]
    [Rule("rr:teamwork.2")]
    [Fact]
    public void TheFirstPlayerOrdersQuickstrikeAndTeamworkAfterAReveal()
    {
        // Both keywords provide forced responses to the minion entering play
        // and engaging. Neither printed order nor object-id order may decide
        // which initiates first; the first player does.
        var printed = new Printed()
            .With("identity", ("HP", "10"))
            .With(
                "encounter",
                ("Quickstrike", "1"), ("Teamwork", "ACOLYTE"),
                ("ATK", "1"), ("HP", "3"))
            .With("friend", ("HP", "3"))
            .WithTrait("encounter", "ACOLYTE")
            .WithTrait("friend", "ACOLYTE");
        printed.Kinds["encounter"] = CardKind.Minion;
        printed.Kinds["friend"] = CardKind.Minion;
        printed.Kinds["identity"] = CardKind.Hero;
        var world = Board(printed, players: 1);
        world.CreateCard(
            "friend", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        var encounter = world.AreaOf(DeckType.EncounterDeck).Cards
            .Single(card => card.FaceId == "encounter");
        World.MoveToTop(
            encounter,
            world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(0)));
        world.Agenda.Add(new PhaseStep(
            Steps.RevealEncounterCard, 1, 4,
            Subject: encounter.ObjectId, Seat: 0));
        var events = new List<GameEvent>();
        var abilities = new NoCardAbilities();

        var asked = Assert.IsType<Prompt>(
            Sequence.Work(world, printed, abilities, events));

        Assert.Equal(Question.Order, asked.Asking);
        Assert.Equal(
            ["Quickstrike", "Teamwork"],
            asked.Affordances.Select(option => option.Label));

        Sequence.Answer(
            world, printed, abilities, asked,
            Decision.Take(asked.Affordances[1].Id), events);

        Assert.Equal(
            2,
            world.Agenda.Outstanding
                .Where(step => step.What == Steps.Attack)
                .Select(step => step.ActivationId)
                .Distinct()
                .Count());
    }

    [Rule("rr:forced.5")]
    [Rule("rr:incite-x.1")]
    [Rule("rr:surge.1")]
    [Fact]
    public void TheFirstPlayerOrdersTwoKeywordWhenRevealedAbilities()
    {
        var printed = new Printed()
            .With("encounter", ("Incite", "2"), ("Surge", "1"));
        printed.Kinds["encounter"] = CardKind.Minion;
        var world = Board(printed, players: 1);
        var encounter = world.AreaOf(DeckType.EncounterDeck).Cards
            .Single(card => card.FaceId == "encounter");
        World.MoveToTop(
            encounter,
            world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(0)));
        world.Agenda.Add(new PhaseStep(
            Steps.RevealEncounterCard, 1, 4,
            Subject: encounter.ObjectId, Seat: 0));

        var asked = Sequence.Work(
            world, printed, new NoCardAbilities(), new List<GameEvent>());

        Assert.NotNull(asked);
        Assert.Equal(Question.Order, asked.Asking);
        Assert.Equal(
            ["Incite", "Surge"],
            asked.Affordances.Select(option => option.Label));
    }

    /// <summary>Schedules the villain phase and walks it to the end.</summary>
    private static List<GameEvent> Run(
        World world, Printed printed, ICardAbilities? abilities = null)
    {
        var events = new List<GameEvent>();
        VillainPhase.Schedule(world.Agenda, round: 1);
        Sequence.Finish(world, printed, abilities ?? new NoCardAbilities(), events);
        return events;
    }

    /// <summary>Records which player each completed villain activation targeted.</summary>
    private sealed class ActivationObserver : NoCardAbilities
    {
        public List<int> Players { get; } = [];

        public override IReadOnlyList<GameEvent> ActivationCompleted(
            World world, EnemyActivation result)
        {
            Players.Add(result.Player);
            return [];
        }
    }

    private sealed class EnemyOrderObserver : NoCardAbilities
    {
        public List<string> Enemies { get; } = [];

        public override IReadOnlyList<GameEvent> ActivationCompleted(
            World world, EnemyActivation result)
        {
            Enemies.Add(world.Cards[result.Enemy].FaceId);
            return [];
        }
    }

    private sealed class EngageAfterVillain(int villain) : NoCardAbilities
    {
        public List<string> Completed { get; } = [];

        public override IReadOnlyList<GameEvent> ActivationCompleted(
            World world, EnemyActivation result)
        {
            Completed.Add(world.Cards[result.Enemy].FaceId);
            if (result.Enemy == villain)
            {
                world.CreateCard(
                    "arriving",
                    world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(result.Player)));
            }

            return [];
        }
    }

    private sealed class BoostWindowOffer(int card) : NoCardAbilities
    {
        public bool SawBoostFlipped { get; private set; }
        public bool SawThreat { get; private set; }
        public bool SawSchemeEnds { get; private set; }

        public override IReadOnlyList<PendingAbility> Waiting(
            World world, Occurrence occurrence, WindowKind window)
        {
            SawBoostFlipped |= occurrence.Is("WhenBoostCardsFlipped");
            SawThreat |= occurrence.Is(Steps.ThreatWouldBePlaced);
            SawSchemeEnds |= occurrence.Is(Steps.SchemeEnds);
            return window == WindowKind.Interrupt
                && occurrence.Is("WhenBoostCardGiven")
                ? [new PendingAbility(card, AbilityType.Interrupt, 0)]
                : [];
        }

        public override Affordance Describe(World world, PendingAbility ability) =>
            new(77, "Interrupt", ability.Card, 0, "interrupt boost");
    }

    /// <summary>A villain, a main scheme, one identity per seat, two encounter cards each.</summary>
    private static World Board(Printed printed, int players)
    {
        var world = new World(printed, players);
        for (int seat = 0; seat < players; seat++)
        {
            world.CreateSeat($"p{seat}");
            var identity = world.CreateCard("identity", world.Seats[seat].Hero);
            world.Seats[seat].IdentityCard = identity;
        }

        world.CreateCard("villain", world.AreaOf(DeckType.VillainArea));
        world.CreateCard("scheme", world.AreaOf(DeckType.MainSchemesArea));

        // The deck is drawn from the top, which is the end of the list, so the
        // boost card has to be appended last to be taken first.
        var deck = world.AreaOf(DeckType.EncounterDeck);
        for (int seat = 0; seat < players; seat++)
        {
            world.CreateCard("encounter", deck);
            world.CreateCard("boost", deck);
        }

        return world;
    }

    /// <summary>Printed data for a handful of made-up cards.</summary>
    private sealed class Printed : ICardFacts
    {
        private readonly Dictionary<string, Dictionary<string, string>> attributes = new(StringComparer.Ordinal);
        private readonly Dictionary<string, List<string>> traits = new(StringComparer.Ordinal);

        public Dictionary<string, CardKind> Kinds { get; } = new(StringComparer.Ordinal)
        {
            ["identity"] = CardKind.AlterEgo,
            ["villain"] = CardKind.EncounterVillain,
            ["scheme"] = CardKind.MainScheme,
            ["boost"] = CardKind.Treachery,
            ["encounter"] = CardKind.Treachery,
            ["minion"] = CardKind.Minion,
            ["charge"] = CardKind.Attachment,
        };

        public Printed With(string faceId, params (string Key, string Value)[] values)
        {
            var table = attributes.TryGetValue(faceId, out var found)
                ? found
                : attributes[faceId] = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var (key, value) in values)
            {
                table[key] = value;
            }

            return this;
        }

        public Printed WithTrait(string faceId, string trait)
        {
            if (!traits.TryGetValue(faceId, out var found))
            {
                traits[faceId] = found = [];
            }
            found.Add(trait);
            return this;
        }

        public CardKind Kind(string faceId) =>
            Kinds.TryGetValue(faceId, out var kind) ? kind : CardKind.Unknown;

        public IReadOnlyList<string> Traits(string faceId) =>
            traits.TryGetValue(faceId, out var found) ? found : [];

        public IReadOnlyDictionary<string, string> Attributes(string faceId) =>
            attributes.TryGetValue(faceId, out var found)
                ? found
                : new Dictionary<string, string>(StringComparer.Ordinal);

        public long PrintedValue(string faceId, string attribute, int players, long fallback = 0) =>
            Attributes(faceId).TryGetValue(attribute, out string? printed)
            && long.TryParse(printed, out long value)
                ? value
                : fallback;
    }
}
